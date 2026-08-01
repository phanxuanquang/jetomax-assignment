using ChatApp.Mcp.Backend;
using ChatApp.Mcp.DTOs;
using ModelContextProtocol.Server;
using System.ComponentModel;

namespace ChatApp.Mcp.Tools;

[McpServerToolType]
public sealed class WhoamiTools(BackendClient backend)
{
    [McpServerTool(Name = "whoami", ReadOnly = true, UseStructuredContent = true)]
    [Description("Get your own signed-in identity. Use before create_conversation if you need to recognize your own username in a list.")]
    public async Task<UserMetaDto> Whoami(CancellationToken cancellationToken = default)
    {
        return await backend.GetSigninUserMetaAsync(cancellationToken);
    }
}
