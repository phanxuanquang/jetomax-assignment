namespace ChatApp.Mcp.Backend;

/// <summary>Mirrors the backend's <c>ConversationDto</c> JSON shape — only the fields tools use.</summary>
public sealed record BackendConversation(
    Guid Id,
    string PublicId,
    string DisplayName,
    Guid? OwnerId,
    bool IsReadonly,
    DateTime? LastMessageTime,
    IReadOnlyCollection<Guid> ParticipantUserIds);

/// <summary>Mirrors the backend's <c>MessageDto</c> JSON shape. <see cref="Type"/> is <c>"Text"</c> or <c>"Image"</c>.</summary>
public sealed record BackendMessage(
    Guid Id,
    Guid SenderUserId,
    string Type,
    DateTime SentAt,
    string? Content,
    string? ImageUrl,
    string? Caption);
