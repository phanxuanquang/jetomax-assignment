namespace ChatApp.Domain.Entities;

/// <summary>
/// A direct (1:1) or group chat thread. Backed by the <c>conversations</c> table.
/// </summary>
public sealed class Conversation
{
    /// <summary>
    /// Unique identifier. Generated when the instance is constructed.
    /// </summary>
    public Guid Id { get; } = Guid.NewGuid();

    /// <summary>
    /// Backend-generated, case-sensitive alphanumeric code used to join this conversation. Format
    /// and uniqueness are enforced by FluentValidation (Application) and the DB CHECK/UNIQUE
    /// constraint (Infrastructure), not Domain.
    /// </summary>
    public required string PublicId { get; init; }

    /// <summary>
    /// Auto-generated at creation from participant usernames; renamable by the owner. This is a
    /// cosmetic field validated only by FluentValidation (Application) — it is not an integrity
    /// concern, so Domain places no constraint on it.
    /// </summary>
    public required string DisplayName { get; init; }

    /// <summary>
    /// The current owner's user id. Null means the conversation is frozen: no new joins are allowed, but existing participants may still chat or leave.
    /// </summary>
    public required Guid? OwnerId { get; set; }

    /// <summary>
    /// True once the owner has soft-deleted the conversation. Rows are retained, never dropped.
    /// </summary>
    public bool IsDeleted { get; set; } = false;

    /// <summary>
    /// True when only the owner may send. Auto-set when participants drop to 1, auto-cleared when a join brings the count back to 2; the owner may also set it manually.
    /// </summary>
    public bool IsReadonly { get; set; } = false;

    /// <summary>
    /// When the conversation was created.
    /// </summary>
    public DateTime CreatedTime { get; } = DateTime.UtcNow;

    /// <summary>
    /// When the most recent message was sent; used to order the conversation list by recency. Null value means there are not any messages sent.
    /// </summary>
    public DateTime? LastMessageTime { get; set; }

    /// <summary>
    /// The current owner, or null when <see cref="OwnerId"/> is null (the conversation is frozen).
    /// </summary>
    public User? Owner { get; set; }

    /// <summary>
    /// Members of this conversation. 
    /// Populated by the persistence layer; empty until loaded.
    /// </summary>
    public ICollection<Participant> Participants { get; set; } = [];

    /// <summary>
    /// Messages posted in this conversation. Populated by the persistence layer; empty until loaded.
    /// </summary>
    public ICollection<Message> Messages { get; set; } = [];

    /// <summary>
    /// The 1:1 rolling memory state for this conversation; created automatically alongside it.
    /// </summary>
    public ConversationMemory? Memory { get; set; }

    /// <summary>
    /// Append-only chunk summary history for this conversation, ordered by <see cref="ChunkMemory.Id"/>.
    /// </summary>
    public ICollection<ChunkMemory> ChunkMemories { get; set; } = [];
}
