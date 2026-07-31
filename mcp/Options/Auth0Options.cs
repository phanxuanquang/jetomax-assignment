using System.ComponentModel.DataAnnotations;

namespace ChatApp.Mcp.Options;

/// <summary>The Auth0 tenant this server trusts as its OAuth authorization server (RFC 9728 protected resource).</summary>
public sealed class Auth0Options
{
    /// <summary>Auth0 tenant domain, e.g. <c>your-tenant.us.auth0.com</c> (no scheme, no trailing slash).</summary>
    [Required]
    public required string Domain { get; init; }

    /// <summary>The API identifier registered in Auth0 — this server's own public base URL.</summary>
    [Required]
    public required string Audience { get; init; }
}
