using ChatApp.Application.Abstractions;
using ChatApp.Domain.Enums;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace ChatApp.Api.Auth;

/// <summary>
/// Authenticates Mcp/N8n callers via the <c>X-Client-Key</c> header (§4.2), resolved against
/// <c>Clients:McpKey</c>/<c>Clients:N8nKey</c> (see <c>mcp-integration.md</c>'s concrete header
/// shape). Both a matching Mcp key and a matching N8n key now additionally require the
/// <c>X-On-Behalf-Of</c> header, carrying the real user's <em>username</em> (not a raw id) the call
/// acts on behalf of — resolved here the same way the rest of the API resolves a username (§9.2).
/// There is no more "no identity" case for either client: a missing/unresolvable on-behalf-of is 401.
/// Mcp additionally may only impersonate a <see cref="UserRole.User"/>-role account (401), capping
/// blast radius if the Mcp key leaks — an N8n on-behalf-of has no such restriction, since the daily
/// digest workflow specifically needs an Administrator (§9.2's <c>/api/internal/*</c> group).
/// </summary>
public sealed class ClientKeyAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> schemeOptions,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IOptions<ClientKeyOptions> clientKeyOptions,
    IAppDbContext db)
    : AuthenticationHandler<AuthenticationSchemeOptions>(schemeOptions, logger, encoder)
{
    public const string SchemeName = "ClientKey";
    private const string ClientKeyHeader = "X-Client-Key";
    private const string OnBehalfOfHeader = "X-On-Behalf-Of";

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(ClientKeyHeader, out var providedKey) || string.IsNullOrEmpty(providedKey))
        {
            return AuthenticateResult.Fail($"Missing {ClientKeyHeader} header.");
        }

        var options = clientKeyOptions.Value;
        bool isMcp;
        if (providedKey == options.McpKey)
        {
            isMcp = true;
        }
        else if (providedKey == options.N8nKey)
        {
            isMcp = false;
        }
        else
        {
            return AuthenticateResult.Fail("Invalid client key.");
        }

        if (!Request.Headers.TryGetValue(OnBehalfOfHeader, out var onBehalfOfUsername) || string.IsNullOrWhiteSpace(onBehalfOfUsername))
        {
            return AuthenticateResult.Fail($"Callers must send a valid {OnBehalfOfHeader} header.");
        }

        var username = onBehalfOfUsername.ToString();
        var user = await db.FirstOrDefaultAsync(
            db.Users.Where(u => u.Username == username),
            Context.RequestAborted);

        if (user is null)
        {
            return AuthenticateResult.Fail($"{OnBehalfOfHeader} does not resolve to an existing user.");
        }

        if (isMcp && user.Role != UserRole.User)
        {
            return AuthenticateResult.Fail("Mcp callers may only act on behalf of a User-role account.");
        }

        var claims = new List<Claim>
        {
            new(ClientClaimTypes.Subject, user.Id.ToString()),
            new(ClientClaimTypes.Role, user.Role.ToString())
        };

        var identity = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);
        return AuthenticateResult.Success(ticket);
    }
}
