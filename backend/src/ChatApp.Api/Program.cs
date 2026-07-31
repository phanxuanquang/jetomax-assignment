using ChatApp.Api.Auth;
using ChatApp.Api.ErrorHandling;
using ChatApp.Api.Memory;
using ChatApp.Api.OpenApi;
using ChatApp.Api.Realtime;
using ChatApp.Application;
using ChatApp.Application.Abstractions;
using ChatApp.Application.Memory;
using ChatApp.Infrastructure;
using Scalar.AspNetCore;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddOpenApi(options => options.AddDocumentTransformer<SecuritySchemeTransformer>());

// The frontend PWA runs on its own origin (Vite dev server, or wherever it's deployed) and calls
// both the REST API and the SignalR hub from the browser — neither works cross-origin without this.
// AllowCredentials is required because the SignalR JS client defaults to withCredentials: true, which
// in turn requires explicit origins (WithOrigins), not AllowAnyOrigin. Cors:AllowedOrigins is plain,
// non-secret config, so it lives in appsettings.json/appsettings.Development.json, not user-secrets.
const string FrontendCorsPolicy = "Frontend";
var allowedOrigins = builder.Configuration.GetSection("CORS:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options =>
{
    options.AddPolicy(FrontendCorsPolicy, policy =>
        policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod().AllowCredentials());
});

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddOptions<ConversationMemoryOptions>()
                .Bind(builder.Configuration.GetSection(nameof(ConversationMemoryOptions)))
                .ValidateOnStart();
builder.Services.AddScoped<ConversationMemoryService>();

builder.Services.AddChatAppAuthentication(builder.Configuration);
builder.Services.AddAuthorization();

builder.Services.AddScoped<ICurrentUserProvider, CurrentUserProvider>();
builder.Services.AddScoped<IConversationAccess, ConversationAccess>();
builder.Services.AddSingleton<IUserConnectionTracker, UserConnectionTracker>();
builder.Services.AddScoped<IConversationNotifier, SignalRConversationNotifier>();

builder.Services.AddSignalR()
    .AddJsonProtocol(options => options.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddExceptionHandler<PostgresExceptionHandler>();
builder.Services.AddProblemDetails();

// Disable license warning of MediatR 
builder.Logging.AddFilter("LuckyPennySoftware.MediatR.License", LogLevel.None);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseExceptionHandler();

app.UseHttpsRedirection();

app.UseCors(FrontendCorsPolicy);

app.UseAuthentication();

app.Use(async (context, next) =>
{
    var currentUserProvider = context.RequestServices.GetRequiredService<ICurrentUserProvider>();
    currentUserProvider.Principal = context.User;
    await next();
});

app.UseAuthorization();

app.MapControllers();
app.MapHub<ChatHub>("/hub/chat");

await app.RunAsync();
