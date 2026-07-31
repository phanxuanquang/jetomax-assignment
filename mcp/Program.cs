using ChatApp.Mcp.Auth;
using ChatApp.Mcp.Backend;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOptions<BackendOptions>()
    .Bind(builder.Configuration.GetSection("Backend"))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddOptions<McpAccessOptions>()
    .Bind(builder.Configuration.GetSection("Mcp"))
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddHttpClient<BackendClient>((services, client) =>
{
    var options = services.GetRequiredService<IOptions<BackendOptions>>().Value;
    client.BaseAddress = new Uri(options.BaseUrl);
    client.DefaultRequestHeaders.Add("X-Client-Key", options.ClientKey);
    client.DefaultRequestHeaders.Add("X-On-Behalf-Of", options.OnBehalfOfUsername);
});

// Stateless: no tool here needs server-to-client sampling or session state, so each request is independent.
builder.Services.AddMcpServer()
    .WithHttpTransport(o => o.Stateless = true)
    .WithToolsFromAssembly();

var app = builder.Build();

app.UseMiddleware<ApiKeyMiddleware>();
app.MapMcp("/mcp");

app.Run();
