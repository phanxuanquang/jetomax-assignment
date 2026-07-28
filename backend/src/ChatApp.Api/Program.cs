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
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options =>
{
    options.AddPolicy(FrontendCorsPolicy, policy =>
        policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod().AllowCredentials());
});

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// ConversationMemoryService is a plain Application service (not a port), so nothing in Application's
// or Infrastructure's own DI registers it — the composition root does, same as any other consumer of
// a concrete Application type (used by the Internal.SummarizeConversation(s) handlers and by
// ChatHub's detached memory-update task).
builder.Services.Configure<MemoryOptions>(builder.Configuration.GetSection("Memory"));
builder.Services.AddScoped<ConversationMemoryService>();

builder.Services.AddChatAppAuthentication(builder.Configuration);
builder.Services.AddAuthorization();

builder.Services.AddScoped<ICurrentUserProvider, CurrentUserProvider>();
builder.Services.AddScoped<IConversationAccess, ConversationAccess>();
builder.Services.AddSingleton<IUserConnectionTracker, UserConnectionTracker>();
builder.Services.AddScoped<IConversationNotifier, SignalRConversationNotifier>();

builder.Services.AddSignalR();

builder.Services.AddExceptionHandler<PostgresExceptionHandler>();
builder.Services.AddProblemDetails();

// MediatR v14+ is the commercial line (§4.1); no Community license key is configured (Q5), so this
// silences its startup warning instead of setting cfg.LicenseKey (which lives in Application's own
// AddMediatR call and is out of scope here).
builder.Logging.AddFilter("LuckyPennySoftware.MediatR.License", LogLevel.None);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseExceptionHandler();

app.UseHttpsRedirection();

// Serves wwwroot/test-console.html — a manual GUI test harness (login, REST via Scalar, SignalR
// realtime) for reviewers/PM to exercise the API without a terminal, since there's no frontend yet.
app.UseDefaultFiles();
app.UseStaticFiles();

app.UseCors(FrontendCorsPolicy);

app.UseAuthentication();

// Populates ICurrentUserProvider for REST requests from the now-authenticated HttpContext.User. The
// Hub does the equivalent itself per-invocation from Context.User (see ChatHub) since
// IHttpContextAccessor isn't reliable inside hub method invocations on an already-open connection.
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
