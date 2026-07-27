using ChatApp.Domain.Enums;

namespace ChatApp.Domain.Entities;

/// <summary>
/// Base for a single message posted to a <see cref="Conversation"/>. Concrete payloads are
/// <see cref="TextMessage"/> or <see cref="ImageMessage"/>; <see cref="Type"/> mirrors the
/// discriminator column in the <c>messages</c> table that says which one it is.
/// </summary>
public abstract class Message
{
    /// <summary>
    /// Unique identifier of the message, shared with its <see cref="TextMessage"/>/<see cref="ImageMessage"/> row.
    /// </summary>
    public Guid Id { get; } = Guid.NewGuid();

    /// <summary>
    /// The conversation this message belongs to.
    /// </summary>
    public Guid ConversationId { get; init; }

    /// <summary>
    /// The sender's user id. May be the hidden AI Agent's id (OCR replies).
    /// </summary>
    public Guid UserId { get; init; }

    /// <summary>
    /// Which concrete payload this message carries. Computed from the runtime type, so it can never disagree with whether this instance is actually a <see cref="TextMessage"/> or <see cref="ImageMessage"/>.
    /// </summary>
    public abstract MessageType Type { get; }

    /// <summary>
    /// The message this one replies to, e.g. an AI Agent's OCR transcription replying to the source image. Null for ordinary messages.
    /// </summary>
    public Guid? RepliesToMessageId { get; set; }

    /// <summary>
    /// When the message was sent (the spec's "Timestamp").
    /// </summary>
    public DateTime SentAt { get; } = DateTime.UtcNow;

    /// <summary>
    /// The conversation this message belongs to.
    /// </summary>
    public Conversation? Conversation { get; set; }

    /// <summary>
    /// The sender of this message; may be the hidden AI Agent.
    /// </summary>
    public User? Sender { get; set; }

    /// <summary>
    /// The message this one replies to, if any.
    /// </summary>
    public Message? RepliesToMessage { get; set; }
}
