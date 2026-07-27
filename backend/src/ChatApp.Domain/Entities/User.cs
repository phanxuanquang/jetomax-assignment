namespace ChatApp.Domain.Entities;

/// <summary>
/// A registered human, or the hidden system AI Agent that posts OCR results. Backed by the
/// <c>profiles</c> table; for real users <see cref="Id"/> equals the corresponding Supabase
/// <c>auth.users</c> id.
/// </summary>
public sealed class User
{
    /// <summary>
    /// Unique identifier; equals the Supabase auth user id for real users.
    /// </summary>
    public Guid Id { get; } = Guid.NewGuid();

    /// <summary>
    /// Unique login handle; the user's public identity. Format and uniqueness are enforced by
    /// FluentValidation (Application) and the DB CHECK/UNIQUE constraint (Infrastructure), not Domain.
    /// </summary>
    public required string Username { get; init; }

    /// <summary>
    /// True only for the single hidden system Agent that posts OCR results; the Agent is never a conversation participant.
    /// </summary>
    public bool IsAgent { get; set; } = false;

    /// <summary>
    /// When the user was created. Set automatically when the instance is constructed.
    /// </summary>
    public DateTime CreatedTime { get; } = DateTime.UtcNow;

    /// <summary>
    /// Conversations currently owned by this user. Populated by the persistence layer; empty until loaded.
    /// </summary>
    public ICollection<Conversation> OwnedConversations { get; set; } = [];

    /// <summary>
    /// Conversations this user participates in. Populated by the persistence layer; empty until loaded.
    /// </summary>
    public ICollection<Participant> Participations { get; set; } = [];

    /// <summary>
    /// Messages sent by this user, including OCR replies when this is the hidden Agent. Populated by the persistence layer; empty until loaded.
    /// </summary>
    public ICollection<Message> SentMessages { get; set; } = [];
}
