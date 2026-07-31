namespace ChatApp.Mcp.DTOs;

public sealed record ConversationMetaDto(
    Guid Id,
    string PublicId, 
    string DisplayName, 
    int TotalParticipant);