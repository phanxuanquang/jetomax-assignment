namespace ChatApp.Mcp.DTOs;
                                                                                                                                   
public sealed record MessageDto(
    Guid Id,
    Guid SenderId,
    string Type,
    DateTime SentAt,
    string? Content,
    string? ImageUrl,
    string? Caption);