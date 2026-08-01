using ChatApp.Mcp.DTOs;
using System.Text.Json;

namespace ChatApp.Mcp.Backend;

public sealed class BackendClient(HttpClient httpClient)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<IReadOnlyList<ConversationDto>> ListConversationsAsync(string? query, CancellationToken cancellationToken = default)
    {
        var url = string.IsNullOrWhiteSpace(query) ? "api/conversations" : $"api/conversations?q={Uri.EscapeDataString(query)}";
        using var response = await httpClient.GetAsync(url, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return await response.Content.ReadFromJsonAsync<IReadOnlyList<ConversationDto>>(JsonOptions, cancellationToken) ?? [];
    }

    public async Task<IReadOnlyList<MessageDto>> GetMessagesAsync(Guid conversationId, Guid? beforeMessageId = null, int limit = 30, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.GetAsync($"api/conversations/{conversationId}/messages?limit={limit}&before={beforeMessageId}", cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return await response.Content.ReadFromJsonAsync<IReadOnlyList<MessageDto>>(JsonOptions, cancellationToken) ?? [];
    }

    public async Task<IReadOnlyList<MessageDto>> SearchMessagesAsync(Guid conversationId, string keyword, int limit = 10, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.GetAsync($"api/conversations/{conversationId}/messages/search?q={Uri.EscapeDataString(keyword)}&limit={limit}", cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return await response.Content.ReadFromJsonAsync<IReadOnlyList<MessageDto>>(JsonOptions, cancellationToken) ?? [];
    }

    public async Task JoinAsync(string publicId, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PostAsJsonAsync("api/conversations/join", new { publicId }, JsonOptions, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }

    public async Task<ConversationDto?> CreateConversationAsync(IReadOnlyCollection<string> participantUsernames, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PostAsJsonAsync("api/conversations", new { participantUsernames }, JsonOptions, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return await response.Content.ReadFromJsonAsync<ConversationDto>(JsonOptions, cancellationToken);
    }

    public async Task LeaveAsync(Guid conversationId, string? mode = null, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PostAsJsonAsync($"api/conversations/{conversationId}/leave", new { mode }, JsonOptions, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }

    public async Task<MessageDto?> SendMessageAsync(Guid conversationId, string content, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PostAsJsonAsync($"api/conversations/{conversationId}/messages", new { content }, JsonOptions, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return await response.Content.ReadFromJsonAsync<MessageDto>(JsonOptions, cancellationToken);
    }

    public async Task<UserMetaDto> GetSigninUserMetaAsync(CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.GetAsync("api/users/me", cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return await response.Content.ReadFromJsonAsync<UserMetaDto>(JsonOptions, cancellationToken)
            ?? throw new InvalidOperationException("Backend returned an empty response for the signed-in user.");
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken = default)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        throw new InvalidOperationException($"Backend call failed ({(int)response.StatusCode}): {body}");
    }
}
