namespace ChatApp.Domain.Entities;

/// <summary>
/// Membership of a <see cref="User"/> in a <see cref="Conversation"/>. Backed by the
/// <c>participants</c> table; identity is the composite (<see cref="ConversationId"/>,
/// <see cref="UserId"/>) pair — there is no surrogate id.
/// </summary>
public sealed class Participant(Guid conversationId, Guid userId)
{
    /// <summary>The conversation the user joined.</summary>
    public Guid ConversationId { get; init; } = conversationId;

    /// <summary>The joining user's id.</summary>
    public Guid UserId { get; init; } = userId;

    /// <summary>When the user joined this conversation. Set automatically when the instance is constructed.</summary>
    public DateTime JoinedTime { get; } = DateTime.UtcNow;

    /// <summary>The conversation this row joins the user to.</summary>
    public Conversation? Conversation { get; set; }

    /// <summary>The user who joined.</summary>
    public User? User { get; set; }
}
