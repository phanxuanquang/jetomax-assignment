using ChatApp.Mcp.Auth;
using ChatApp.Mcp.Backend;
using ChatApp.Mcp.Options;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using ModelContextProtocol.AspNetCore.Authentication;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.AddConsole(consoleLogOptions =>
{
    consoleLogOptions.LogToStandardErrorThreshold = LogLevel.Trace;
});

// Behind ngrok/a reverse proxy, Kestrel only sees plain HTTP - without this, every URL this server
// generates (WWW-Authenticate, the resource_metadata document) comes out http:// instead of https://,
// which breaks OAuth discovery for a client connecting over the real https:// tunnel URL. The proxy's
// IP isn't known ahead of time (ngrok, or whatever host this deploys to), so trust any forwarder.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

builder.Services.AddOptions<BackendOptions>()
    .Bind(builder.Configuration.GetSection("Backend"))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddOptions<SupabaseOptions>()
    .Bind(builder.Configuration.GetSection("Supabase"))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddHttpClient<BackendClient>((services, client) =>
{
    var options = services.GetRequiredService<IOptions<BackendOptions>>().Value;
    client.BaseAddress = new Uri(options.BaseUrl);
    client.DefaultRequestHeaders.Add("X-Client-Key", options.ClientKey);
    client.DefaultRequestHeaders.Add("X-On-Behalf-Of", options.OnBehalfOfUsername);
});

var supabase = builder.Configuration.GetSection("Supabase").Get<SupabaseOptions>()
    ?? throw new InvalidOperationException("Missing Supabase configuration.");
var authority = supabase.Url.TrimEnd('/') + "/auth/v1";

builder.Services.AddHttpClient<SupabaseJwksProvider>();

// This server only checks "is the token a valid Supabase OAuth-server access token" - not who the
// human behind it is, since every tool call acts on the one fixed Backend:OnBehalfOfUsername account
// regardless. Real per-user identity is Supabase's job (its OAuth consent screen), not this server's.
builder.Services.AddAuthentication(options =>
{
    options.DefaultChallengeScheme = McpAuthenticationDefaults.AuthenticationScheme;
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options => { })
.AddMcp(options =>
{
    options.ResourceMetadata = new()
    {
        Resource = supabase.ResourceUri,
        AuthorizationServers = { authority },
    };
});

// Configured out-of-line (rather than inline in AddJwtBearer above) because it needs the
// DI-registered SupabaseJwksProvider, which the plain Action<JwtBearerOptions> delegate can't reach.
builder.Services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
    .Configure<SupabaseJwksProvider>((options, jwks) =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = authority,
            ValidateAudience = true,
            ValidAudience = "authenticated",
            ValidateLifetime = true,
            IssuerSigningKeyResolver = (_, _, _, _) =>
                jwks.GetSigningKeysAsync(CancellationToken.None).GetAwaiter().GetResult(),
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddMcpServer()
    .WithHttpTransport(o => o.Stateless = false)
    .WithToolsFromAssembly();

var app = builder.Build();

app.UseForwardedHeaders();
app.UseAuthentication();
app.UseAuthorization();
app.MapMcp("/mcp").RequireAuthorization();

app.Run();
