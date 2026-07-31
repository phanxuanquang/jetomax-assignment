namespace ChatApp.Api.Auth;

/// <summary>Binds the <c>Clients</c> configuration section (see <c>prerequisite-setups.md</c>, <c>mcp-integration.md</c>).</summary>
public sealed class ClientKeyOptions
{
    /// <summary>The service key that authenticates the external MCP server, alongside its required <c>X-On-Behalf-Of</c> username.</summary>
    public required string McpKey { get; init; }

    /// <summary>The service key that authenticates the external n8n workflow, alongside its required <c>X-On-Behalf-Of</c> username.</summary>
    public required string N8nKey { get; init; }
}
