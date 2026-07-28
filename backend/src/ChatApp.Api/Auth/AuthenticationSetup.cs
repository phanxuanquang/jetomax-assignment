using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;

namespace ChatApp.Api.Auth;

/// <summary>
/// Wires the three-client authentication model (§4.2): a Supabase-JWT scheme for App, a service-key
/// scheme for Mcp/N8n, and a policy scheme that picks between them per-request so
/// <c>[Authorize]</c>/<see cref="AllowedClientsAttribute"/> work without controllers or the Hub
/// choosing a scheme themselves.
/// </summary>
public static class AuthenticationSetup
{
    private const string HubPath = "/hub/chat";
    private const string PolicySchemeName = "AppOrClientKey";

    public static IServiceCollection AddChatAppAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<SupabaseJwtOptions>(configuration.GetSection("Supabase"));
        services.Configure<ClientKeyOptions>(configuration.GetSection("Clients"));
        services.AddHttpClient<SupabaseJwksProvider>();

        services.AddAuthentication(PolicySchemeName)
            .AddPolicyScheme(PolicySchemeName, "App JWT or client-key selector", policy =>
            {
                policy.ForwardDefaultSelector = context =>
                    context.Request.Headers.ContainsKey("X-Client-Key")
                        ? ClientKeyAuthenticationHandler.SchemeName
                        : JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, _ => { })
            .AddScheme<AuthenticationSchemeOptions, ClientKeyAuthenticationHandler>(
                ClientKeyAuthenticationHandler.SchemeName, _ => { });

        // A dependency-aware options configurator: JwtBearerOptions needs the DI-registered
        // SupabaseJwksProvider (for IssuerSigningKeyResolver) and SupabaseJwtOptions (for the expected
        // issuer), neither of which are reachable from the plain Action<JwtBearerOptions> delegate above.
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<SupabaseJwksProvider, IOptions<SupabaseJwtOptions>>((options, jwks, supabaseOptions) =>
            {
                var issuer = supabaseOptions.Value.Url.TrimEnd('/') + "/auth/v1";

                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = issuer,
                    ValidateAudience = true,
                    ValidAudience = "authenticated",
                    ValidateLifetime = true,
                    IssuerSigningKeyResolver = (_, _, _, _) =>
                        jwks.GetSigningKeysAsync(CancellationToken.None).GetAwaiter().GetResult()
                };

                options.Events = new JwtBearerEvents
                {
                    // SignalR can't set the Authorization header for WebSocket/SSE transports (browser
                    // API limitation), so accessTokenFactory sends the token as a query string param
                    // instead; this reads it back out for requests to our hub specifically (confirmed
                    // against current ASP.NET Core SignalR auth guidance).
                    OnMessageReceived = context =>
                    {
                        var accessToken = context.Request.Query["access_token"];
                        var path = context.HttpContext.Request.Path;
                        if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments(HubPath))
                        {
                            context.Token = accessToken;
                        }
                        return Task.CompletedTask;
                    },
                    OnTokenValidated = context =>
                    {
                        if (context.Principal?.Identity is ClaimsIdentity identity)
                        {
                            identity.AddClaim(new Claim(ClientClaimTypes.Client, nameof(Client.App)));
                        }
                        return Task.CompletedTask;
                    }
                };
            });

        return services;
    }
}
