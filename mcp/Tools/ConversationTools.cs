using ChatApp.Mcp.Backend;
using ChatApp.Mcp.DTOs;
using ModelContextProtocol.Server;
using System.ComponentModel;

namespace ChatApp.Mcp.Tools;

[McpServerToolType]
public sealed class ConversationTools(BackendClient backend)
{
    [McpServerTool(Name = "list_joined_conversations", ReadOnly = true, UseStructuredContent = true)]
    [Description("List conversations you participate in. Feed conversationId to fetch_messages_from_a_conversation, search_messages_in_a_conversation, send_text_message, leave_conversation.")]
    public async Task<IReadOnlyList<ConversationMetaDto>> ListConversations(
        [Description("Filter by display name substring. Omit/empty for all.")] string? query = null,
        CancellationToken cancellationToken = default)
    {
        var conversations = await backend.ListConversationsAsync(query, cancellationToken);
        return conversations
            .Select(c => new ConversationMetaDto(c.Id, c.PublicId, c.DisplayName, c.ParticipantUserIds.Count))
            .ToList();
    }

    [McpServerTool(Name = "create_conversation", UseStructuredContent = true)]
    [Description("Create conversation. You're added automatically as owner, so skip your own username. Returns created conversation including publicId; share it so others can join_conversation.")]
    public async Task<ConversationDto?> CreateConversation(
        [Description("Other participants' usernames. At least one required, not your own.")] IReadOnlyCollection<string> participantUsernames,
        CancellationToken cancellationToken = default)
    {
        return await backend.CreateConversationAsync(participantUsernames, cancellationToken);
    }

    [McpServerTool(Name = "join_conversation")]
    [Description("Join conversation via its 6-character publicId. Get publicId from the conversation's owner or from create_conversation's result.")]
    public async Task JoinConversation(
        [Description("Conversation's 6-character public join code.")] string publicId,
        CancellationToken cancellationToken = default)
    {
        await backend.JoinAsync(publicId, cancellationToken);
    }

    [McpServerTool(Name = "leave_conversation")]
    [Description("Leave conversation. Get conversationId from list_joined_conversations.")]
    public async Task LeaveConversation(
        [Description("Conversation to leave.")] Guid conversationId,
        [Description("Owner only, else omit: \"Delete\" removes conversation for everyone; \"Freeze\" keeps it, blocks new joins.")] string? mode = null,
        CancellationToken cancellationToken = default)
    {
        await backend.LeaveAsync(conversationId, mode, cancellationToken);
    }

    [McpServerTool(Name = "send_text_message", UseStructuredContent = true)]
    [Description("Send text message into conversation. Get conversationId from list_joined_conversations or create_conversation.")]
    public async Task<MessageDto?> SendTextMessage(
        [Description("Conversation to send into.")] Guid conversationId,
        [Description("Message body. Markdown supported.")] string content,
        CancellationToken cancellationToken = default)
    {
        return await backend.SendMessageAsync(conversationId, content, cancellationToken);
    }

    [McpServerTool(Name = "fetch_messages_from_a_conversation", ReadOnly = true, UseStructuredContent = true)]
    [Description("Fetch conversation's message history, newest first. Get conversationId from list_joined_conversations.")]
    public async Task<IReadOnlyList<MessageDto>> FetchMessages(
        [Description("Conversation to read.")] Guid conversationId,
        [Description("Page backward: only messages older than this id. Omit to start from newest.")] Guid? beforeMessageId = null,
        [Description("Max messages to return.")] int limit = 30,
        CancellationToken cancellationToken = default)
    {
        return await backend.GetMessagesAsync(conversationId, beforeMessageId, limit, cancellationToken);
    }

    [McpServerTool(Name = "search_messages_in_a_conversation", ReadOnly = true, UseStructuredContent = true)]
    [Description("Search conversation's messages by keyword, newest match first. Get conversationId from list_joined_conversations.")]
    public async Task<IReadOnlyList<MessageDto>> SearchMessages(
        [Description("Conversation to search.")] Guid conversationId,
        [Description("Text to match against message content/captions, case-insensitive substring.")] string keyword,
        [Description("Max results, 1-10.")] int limit = 10,
        CancellationToken cancellationToken = default)
    {
        return await backend.SearchMessagesAsync(conversationId, keyword, limit, cancellationToken);
    }
}
