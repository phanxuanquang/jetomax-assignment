using ChatApp.Application.Abstractions;
using ChatApp.Domain.Enums;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace ChatApp.Api.Auth;

/// <summary>
/// Authenticates Mcp/N8n callers via X-Client-Key + X-On-Behalf-Of (a username). Mcp may only
/// impersonate a User-role account, capping blast radius if the key leaks; N8n has no role cap since
/// its digest job needs an Administrator.
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
