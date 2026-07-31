using System.ComponentModel.DataAnnotations;

namespace ChatApp.Mcp.Auth;

/// <summary>The static bearer token that gates this MCP server, configured on the ChatGPT/Claude connector as an API key.</summary>
public sealed class McpAccessOptions
{
    [Required]
    public required string ApiKey { get; init; }
}
