namespace ChatApp.Mcp.DTOs;

public sealed record ConversationDto(
    Guid Id,
    string PublicId,
    string DisplayName,
    Guid? OwnerId,
    bool IsReadonly,
    DateTime? LastMessageTime,
    IReadOnlyCollection<Guid> ParticipantUserIds);