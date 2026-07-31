using System.ComponentModel.DataAnnotations;

namespace ChatApp.Mcp.Options;

/// <summary>Config for the one backend account this server always calls the API as.</summary>
public sealed class BackendOptions
{
    [Required]
    public required string BaseUrl { get; init; }

    /// <summary>The <c>Clients:McpKey</c> value on the backend.</summary>
    [Required]
    public required string ClientKey { get; init; }

    /// <summary>Backend username every tool call acts on behalf of.</summary>
    [Required]
    public required string OnBehalfOfUsername { get; init; }
}
