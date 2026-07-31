using ChatApp.Mcp.Backend;
using ChatApp.Mcp.DTOs;
using ModelContextProtocol.Server;
using System.ComponentModel;

namespace ChatApp.Mcp.Tools;

/// <summary>The three chat-specific tools from the brief: list conversations, summarize a thread, join a group.</summary>
[McpServerToolType]
public sealed class ConversationTools(BackendClient backend)
{
    [McpServerTool(Name = "list_joined_conversations", ReadOnly = true, UseStructuredContent = true)]
    [Description("List conversations that your account currently participates in. Empty query returns all conversations.")]
    public async Task<IReadOnlyList<ConversationMetaDto>> ListConversations(
        [Description("Text to match against conversation display names.")] string query,
        CancellationToken cancellationToken = default)
    {
        var conversations = await backend.ListConversationsAsync(query, cancellationToken);
        return conversations
            .Select(c => new ConversationMetaDto(c.Id, c.PublicId, c.DisplayName, c.ParticipantUserIds.Count))
            .ToList();
    }

    [McpServerTool(Name = "get_conversation_summarization", ReadOnly = true)]
    [Description("Retrieve the summarization for a specific conversation's activity to date, by conversation ID.")]
    public Task<string> SummarizeThread(
        Guid conversationId,
        CancellationToken cancellationToken = default)
        => backend.SummarizeAsync(conversationId, cancellationToken);

    [McpServerTool(Name = "join_conversation")]
    [Description("Join a specific conversation using its public ID.")]
    public async Task JoinGroup(
        [Description("The conversation's 6-character public join code.")] string publicId,
        CancellationToken cancellationToken = default)
    {
        await backend.JoinAsync(publicId, cancellationToken);
    }

    [McpServerTool(Name = "fetch_conversation_messages", ReadOnly = true, UseStructuredContent = true)]
    [Description("Fetch message list from a specific conversation.")]
    public async Task<IReadOnlyList<MessageDto>> FetchMessages(
        Guid conversationId,
        Guid? beforeMessageId = null,
        int limit = 30,
        CancellationToken cancellationToken = default)
    {
        return await backend.GetMessagesAsync(conversationId, beforeMessageId, limit, cancellationToken);
    }
}
