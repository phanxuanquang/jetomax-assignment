using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace ChatApp.Api.Auth;

/// <summary>
/// Authenticates Mcp/N8n callers via the <c>X-Client-Key</c> header (§4.2), resolved against
/// <c>Clients:McpKey</c>/<c>Clients:N8nKey</c> (see <c>mcp-integration.md</c>'s concrete header
/// shape). A matching Mcp key additionally requires the <c>X-On-Behalf-Of</c> header (the ChatGPT
/// user the call acts as); a matching N8n key carries no user identity by design.
/// </summary>
public sealed class ClientKeyAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> schemeOptions,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IOptions<ClientKeyOptions> clientKeyOptions)
    : AuthenticationHandler<AuthenticationSchemeOptions>(schemeOptions, logger, encoder)
{
    public const string SchemeName = "ClientKey";
    private const string ClientKeyHeader = "X-Client-Key";
    private const string OnBehalfOfHeader = "X-On-Behalf-Of";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(ClientKeyHeader, out var providedKey) || string.IsNullOrEmpty(providedKey))
        {
            return Task.FromResult(AuthenticateResult.Fail($"Missing {ClientKeyHeader} header."));
        }

        var options = clientKeyOptions.Value;
        var claims = new List<Claim>();

        if (providedKey == options.McpKey)
        {
            if (!Request.Headers.TryGetValue(OnBehalfOfHeader, out var onBehalfOf) ||
                !Guid.TryParse(onBehalfOf, out var onBehalfOfUserId))
            {
                return Task.FromResult(AuthenticateResult.Fail($"Mcp callers must send a valid {OnBehalfOfHeader} header."));
            }

            claims.Add(new Claim(ClientClaimTypes.Client, nameof(Client.Mcp)));
            claims.Add(new Claim(ClientClaimTypes.Subject, onBehalfOfUserId.ToString()));
        }
        else if (providedKey == options.N8nKey)
        {
            claims.Add(new Claim(ClientClaimTypes.Client, nameof(Client.N8n)));
        }
        else
        {
            return Task.FromResult(AuthenticateResult.Fail("Invalid client key."));
        }

        var identity = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
