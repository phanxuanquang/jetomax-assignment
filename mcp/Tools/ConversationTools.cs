using System.ComponentModel;
using ChatApp.Mcp.Backend;
using ModelContextProtocol.Server;

namespace ChatApp.Mcp.Tools;

public sealed record ConversationSummary(string Id, string PublicId, string DisplayName, int ParticipantCount);

/// <summary>The three chat-specific tools from the brief: list conversations, summarize a thread, join a group.</summary>
[McpServerToolType]
public sealed class ConversationTools(BackendClient backend)
{
    [McpServerTool(Name = "list_conversations", ReadOnly = true, UseStructuredContent = true)]
    [Description("List every conversation the backend account participates in.")]
    public async Task<IReadOnlyList<ConversationSummary>> ListConversations(CancellationToken cancellationToken)
    {
        var conversations = await backend.ListConversationsAsync(query: null, cancellationToken);
        return conversations
            .Select(c => new ConversationSummary(c.Id.ToString(), c.PublicId, c.DisplayName, c.ParticipantUserIds.Count))
            .ToList();
    }

    [McpServerTool(Name = "summarize_thread", ReadOnly = true)]
    [Description("Summarize a conversation's activity to date, by conversation id.")]
    public Task<string> SummarizeThread(
        [Description("Conversation id.")] string conversationId,
        CancellationToken cancellationToken)
        => backend.SummarizeAsync(Guid.Parse(conversationId), cancellationToken);

    [McpServerTool(Name = "join_group")]
    [Description("Join a group conversation using its public join code.")]
    public async Task<string> JoinGroup(
        [Description("The conversation's 6-character public join code.")] string publicId,
        CancellationToken cancellationToken)
    {
        await backend.JoinAsync(publicId, cancellationToken);
        return $"Joined conversation {publicId}.";
    }
}
