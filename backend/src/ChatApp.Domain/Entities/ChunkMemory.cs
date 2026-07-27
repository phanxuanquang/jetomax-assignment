namespace ChatApp.Domain.Entities;

/// <summary>
/// An immutable, append-only summary covering a contiguous range of messages. Backed by the
/// <c>chunk_memories</c> table; rows are ordered by <see cref="Id"/>, and the newest row's
/// <see cref="EndMessageId"/> is the implicit pointer marking where summarization last reached.
/// </summary>
public sealed class ChunkMemory
{
    /// <summary>
    /// Chunk sequence number, also its creation order.
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// The conversation this chunk summarizes.
    /// </summary>
    public Guid ConversationId { get; init; }

    /// <summary>
    /// First message covered by this chunk. Null if that message was later removed.
    /// </summary>
    public Guid? StartMessageId { get; init; }

    /// <summary>
    /// Last message covered by this chunk; the rolling pointer. Null if that message was later removed.
    /// </summary>
    public Guid? EndMessageId { get; init; }

    /// <summary>
    /// The chunk's summary text.
    /// </summary>
    public required string Memory { get; init; }

    /// <summary>
    /// When this chunk was produced.
    /// </summary>
    public DateTime CreatedTime { get; } = DateTime.UtcNow;

    /// <summary>
    /// The conversation this chunk summarizes.
    /// </summary>
    public Conversation? Conversation { get; set; }

    /// <summary>
    /// The first message covered by this chunk.
    /// </summary>
    public Message? StartMessage { get; set; }

    /// <summary>
    /// The last message covered by this chunk.
    /// </summary>
    public Message? EndMessage { get; set; }
}
