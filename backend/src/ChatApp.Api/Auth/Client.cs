namespace ChatApp.Api.Auth;

/// <summary>
/// The three kinds of caller the backend accepts (§4.2, §10 decision): declared here because
/// client-type authorization is entirely an Api concern — Application never references this type.
/// </summary>
public enum Client
{
    /// <summary>The React PWA, authenticated with a Supabase user JWT.</summary>
    App,

    /// <summary>The external MCP server, authenticated with the Mcp service key plus an on-behalf-of user id.</summary>
    Mcp,

    /// <summary>The external n8n workflow, authenticated with the N8n service key alone (no user identity).</summary>
    N8n
}
