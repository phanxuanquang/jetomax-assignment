namespace ChatApp.Domain.Entities;

/// <summary>
/// The 1:1 rolling memory state for a conversation: an evolving overall summary plus a token
/// counter that decides when to fold the next chunk. Backed by the <c>conversation_memory</c> table.
/// </summary>
public sealed class ConversationMemory
{
    /// <summary>
    /// The conversation this memory belongs to; also its primary key.
    /// </summary>
    public Guid ConversationId { get; init; }

    /// <summary>
    /// The evolving overall summary, folded in each time a chunk is produced. Defaults to empty for a brand-new conversation.
    /// </summary>
    public string GlobalMemory { get; set; } = string.Empty;

    /// <summary>
    /// Tokens accrued since the last chunk; an image message counts its caption's tokens. Never negative; starts at zero.
    /// </summary>
    public int PendingTokens { get; set; } = 0;

    /// <summary>
    /// When this memory was last folded or its counter last advanced. Defaults to construction time and is advanced by the backend.
    /// </summary>
    public DateTime LastUpdatedTime { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// The conversation this memory belongs to.
    /// </summary>
    public Conversation? Conversation { get; set; }

    public Message? AssociatedEndMessage { get; set; }

    public ConversationMemory(Guid conversationId)
    {
        ConversationId = conversationId;
    }
}
