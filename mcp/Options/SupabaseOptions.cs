using System.ComponentModel.DataAnnotations;

namespace ChatApp.Mcp.Options;

/// <summary>The Supabase project this server trusts as its OAuth 2.1 authorization server (RFC 9728 protected resource).</summary>
public sealed class SupabaseOptions
{
    /// <summary>Supabase project API URL, e.g. <c>https://xxxxx.supabase.co</c> (no trailing slash). Used to derive both the JWKS endpoint and the expected token issuer.</summary>
    [Required]
    public required string Url { get; init; }

    /// <summary>Stable identifier for this MCP server as an RFC 9728 protected resource. Any fixed string — doesn't need to be reachable or match this server's real host.</summary>
    [Required]
    public required string ResourceUri { get; init; }
}
