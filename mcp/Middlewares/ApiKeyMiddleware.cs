using ChatApp.Mcp.Options;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;

namespace ChatApp.Mcp.Middlewares;

/// <summary>Rejects any request whose <c>Authorization: Bearer &lt;key&gt;</c> doesn't match the configured API key — this server has no other access control.</summary>
public sealed class ApiKeyMiddleware(RequestDelegate next, IOptions<McpAccessOptions> options)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var expected = Encoding.UTF8.GetBytes($"Bearer {options.Value.ApiKey}");
        var provided = Encoding.UTF8.GetBytes(context.Request.Headers.Authorization.ToString());

        if (provided.Length != expected.Length || !CryptographicOperations.FixedTimeEquals(provided, expected))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            return;
        }

        await next(context);
    }
}
