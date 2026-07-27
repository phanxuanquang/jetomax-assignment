namespace ChatApp.Application.Common;

/// <summary>
/// Identifies which kind of external caller issued the current request, as resolved from the
/// authentication scheme at the Api layer (§4.2). Used by <see cref="ICurrentUser.Client"/> and by
/// handlers that must behave differently for user-driven vs. service-to-service callers (e.g.
/// <c>SummarizeThread</c> skips the membership check for <see cref="N8n"/>, which carries no user).
/// </summary>
public enum Client
{
    /// <summary>The end-user PWA, authenticated with a Supabase JWT. Always carries a user identity.</summary>
    App,

    /// <summary>The MCP server, authenticated with a service key acting on behalf of a user. Carries a user identity.</summary>
    Mcp,

    /// <summary>The n8n scheduler, authenticated with a service key. Carries no user identity.</summary>
    N8n
}
