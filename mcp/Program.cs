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
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

builder.Services.AddOptions<BackendOptions>()
    .Bind(builder.Configuration.GetSection("Backend"))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddOptions<Auth0Options>()
    .Bind(builder.Configuration.GetSection("Auth0"))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddHttpClient<BackendClient>((services, client) =>
{
    var options = services.GetRequiredService<IOptions<BackendOptions>>().Value;
    client.BaseAddress = new Uri(options.BaseUrl);
    client.DefaultRequestHeaders.Add("X-Client-Key", options.ClientKey);
    client.DefaultRequestHeaders.Add("X-On-Behalf-Of", options.OnBehalfOfUsername);
});

var auth0 = builder.Configuration.GetSection("Auth0").Get<Auth0Options>()
    ?? throw new InvalidOperationException("Missing Auth0 configuration.");
var authority = $"https://{auth0.Domain}/";

// This server only checks "is the token a valid Auth0 access token for our audience" - not who the
// human behind it is, since every tool call acts on the one fixed Backend:OnBehalfOfUsername account
// regardless. Real per-user identity is Auth0's job (Universal Login), not this server's.
builder.Services.AddAuthentication(options =>
{
    options.DefaultChallengeScheme = McpAuthenticationDefaults.AuthenticationScheme;
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.Authority = authority;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidIssuer = authority,
        ValidateAudience = true,
        ValidAudience = auth0.Audience,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
    };
})
.AddMcp(options =>
{
    options.ResourceMetadata = new()
    {
        Resource = auth0.Audience,
        AuthorizationServers = { authority },
    };
});

builder.Services.AddAuthorization();

// Stateless: no tool here needs server-to-client sampling or session state, so each request is independent.
builder.Services.AddMcpServer()
    .WithHttpTransport(o => o.Stateless = true)
    .WithToolsFromAssembly();

var app = builder.Build();

app.UseForwardedHeaders();
app.UseAuthentication();
app.UseAuthorization();
app.MapMcp("/mcp").RequireAuthorization();

app.Run();
