namespace ChatApp.Api.Auth;

/// <summary>Binds the <c>Clients</c> configuration section (see <c>prerequisite-setups.md</c>, <c>mcp-integration.md</c>).</summary>
public sealed class ClientKeyOptions
{
    /// <summary>The service key that resolves a caller to <see cref="Client.Mcp"/>.</summary>
    public required string McpKey { get; init; }

    /// <summary>The service key that resolves a caller to <see cref="Client.N8n"/>.</summary>
    public required string N8nKey { get; init; }
}
