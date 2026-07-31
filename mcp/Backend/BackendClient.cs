using System.Net.Http.Json;
using System.Text.Json;

namespace ChatApp.Mcp.Backend;

/// <summary>
/// Thin wrapper over the ChatApp REST API. The typed <see cref="HttpClient"/> (registered in
/// Program.cs) already carries the base address and the <c>X-Client-Key</c>/<c>X-On-Behalf-Of</c>
/// headers, so every call here already resolves to the configured backend user.
/// </summary>
public sealed class BackendClient(HttpClient httpClient)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<IReadOnlyList<BackendConversation>> ListConversationsAsync(string? query, CancellationToken cancellationToken)
    {
        var url = string.IsNullOrWhiteSpace(query) ? "api/conversations" : $"api/conversations?q={Uri.EscapeDataString(query)}";
        using var response = await httpClient.GetAsync(url, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return await response.Content.ReadFromJsonAsync<IReadOnlyList<BackendConversation>>(JsonOptions, cancellationToken) ?? [];
    }

    public async Task<IReadOnlyList<BackendMessage>> GetMessagesAsync(Guid conversationId, CancellationToken cancellationToken)
    {
        using var response = await httpClient.GetAsync($"api/conversations/{conversationId}/messages?limit=100", cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return await response.Content.ReadFromJsonAsync<IReadOnlyList<BackendMessage>>(JsonOptions, cancellationToken) ?? [];
    }

    public async Task<string> SummarizeAsync(Guid conversationId, CancellationToken cancellationToken)
    {
        using var response = await httpClient.PostAsync($"api/conversations/{conversationId}/summary", new ByteArrayContent([]), cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return await response.Content.ReadFromJsonAsync<string>(JsonOptions, cancellationToken) ?? string.Empty;
    }

    public async Task JoinAsync(string publicId, CancellationToken cancellationToken)
    {
        using var response = await httpClient.PostAsJsonAsync("api/conversations/join", new { publicId }, JsonOptions, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        throw new InvalidOperationException($"Backend call failed ({(int)response.StatusCode}): {body}");
    }
}
