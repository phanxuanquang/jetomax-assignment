using System.ComponentModel;
using System.Text;
using ChatApp.Mcp.Backend;
using ModelContextProtocol.Server;

namespace ChatApp.Mcp.Tools;

public sealed record SearchResultItem(string Id, string Title, string Url);
public sealed record SearchResponse(IReadOnlyList<SearchResultItem> Results);
public sealed record FetchResponse(string Id, string Title, string Text, string Url, object? Metadata);

/// <summary>
/// The standard <c>search</c>/<c>fetch</c> pair ChatGPT's default connector mode calls — required
/// alongside the custom tools in <see cref="ConversationTools"/> so this server works whether or not
/// Developer Mode is on.
/// </summary>
[McpServerToolType]
public sealed class SearchAndFetchTools(BackendClient backend)
{
    [McpServerTool(Name = "search", ReadOnly = true, UseStructuredContent = true)]
    [Description("Search the backend account's conversations by name. Empty query returns every conversation.")]
    public async Task<SearchResponse> Search(
        [Description("Text to match against conversation display names.")] string query,
        CancellationToken cancellationToken)
    {
        var conversations = await backend.ListConversationsAsync(query, cancellationToken);
        var results = conversations
            .Select(c => new SearchResultItem(c.Id.ToString(), c.DisplayName, ConversationUrl(c.Id)))
            .ToList();

        return new SearchResponse(results);
    }

    [McpServerTool(Name = "fetch", ReadOnly = true, UseStructuredContent = true)]
    [Description("Fetch the full message transcript of a conversation, by the id returned from search.")]
    public async Task<FetchResponse> Fetch(
        [Description("Conversation id, as returned by search.")] string id,
        CancellationToken cancellationToken)
    {
        var conversationId = Guid.Parse(id);
        var messages = await backend.GetMessagesAsync(conversationId, cancellationToken);
        var transcript = FormatTranscript(messages);

        return new FetchResponse(id, $"Conversation {id}", transcript, ConversationUrl(conversationId), Metadata: null);
    }

    private static string ConversationUrl(Guid conversationId) => $"chatapp:conversation/{conversationId}";

    private static string FormatTranscript(IReadOnlyList<BackendMessage> messages)
    {
        if (messages.Count == 0)
        {
            return "(no messages yet)";
        }

        var builder = new StringBuilder();
        foreach (var message in messages.OrderBy(m => m.SentAt))
        {
            var body = message.Type == "Image" ? $"[image] {message.Caption ?? "(no caption)"}" : message.Content ?? string.Empty;
            builder.AppendLine($"[{message.SentAt:u}] {message.SenderUserId}: {body}");
        }

        return builder.ToString();
    }
}
