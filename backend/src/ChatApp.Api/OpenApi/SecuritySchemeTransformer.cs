using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace ChatApp.Api.OpenApi;

/// <summary>
/// Declares the two ways a request can authenticate (§4.2) in the generated OpenAPI document, so the
/// Scalar UI's "Authorize" panel lets a developer supply a Supabase JWT (App) or the
/// <c>X-Client-Key</c>+<c>X-On-Behalf-Of</c> headers (Mcp/N8n — both now require the pair together)
/// directly, without hand-crafting requests. Dev/test tooling only — has no effect on runtime
/// authentication, which is entirely driven by <see cref="Auth.AuthenticationSetup"/>.
/// </summary>
public sealed class SecuritySchemeTransformer : IOpenApiDocumentTransformer
{
    private const string BearerScheme = "Bearer";
    private const string ClientKeyScheme = "ClientKey";
    private const string OnBehalfOfScheme = "OnBehalfOf";

    public Task TransformAsync(OpenApiDocument document, OpenApiDocumentTransformerContext context, CancellationToken cancellationToken = default)
    {
        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();

        document.Components.SecuritySchemes[BearerScheme] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            Description = "App client: the Supabase user access token (Authorization: Bearer <token>)."
        };

        document.Components.SecuritySchemes[ClientKeyScheme] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.ApiKey,
            In = ParameterLocation.Header,
            Name = "X-Client-Key",
            Description = "MCP/n8n client: Clients:McpKey or Clients:N8nKey."
        };

        document.Components.SecuritySchemes[OnBehalfOfScheme] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.ApiKey,
            In = ParameterLocation.Header,
            Name = "X-On-Behalf-Of",
            Description = "MCP/n8n client, alongside X-Client-Key: the username of the real user the call acts on behalf of."
        };

        // Two alternatives, matching the two real caller shapes: App (Bearer alone), Mcp/N8n
        // (ClientKey + OnBehalfOf together — both now require the pair, never ClientKey alone).
        document.Security =
        [
            new OpenApiSecurityRequirement { [new OpenApiSecuritySchemeReference(BearerScheme, document)] = [] },
            new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference(ClientKeyScheme, document)] = [],
                [new OpenApiSecuritySchemeReference(OnBehalfOfScheme, document)] = []
            }
        ];

        return Task.CompletedTask;
    }
}
