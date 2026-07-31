namespace ChatApp.Application.Features.Conversations;

/// <summary>A conversation as returned to callers: identity, ownership/lifecycle flags, and current membership.</summary>
/// <param name="Id">The conversation's internal id.</param>
/// <param name="PublicId">The 6-character code used to join this conversation.</param>
/// <param name="DisplayName">The conversation's display name.</param>
/// <param name="OwnerId">The current owner's user id; null means the conversation is frozen.</param>
/// <param name="IsReadonly">True when only the owner may send.</param>
/// <param name="CreatedTime">When the conversation was created.</param>
/// <param name="LastMessageTime">When the most recent message was sent; null if none yet.</param>
/// <param name="ParticipantUserIds">The user ids currently participating.</param>
public sealed record ConversationDto(
    Guid Id,
    string PublicId,
    string DisplayName,
    Guid? OwnerId,
    bool IsReadonly,
    DateTime CreatedTime,
    DateTime? LastMessageTime,
    IReadOnlyCollection<Guid> ParticipantUserIds);
