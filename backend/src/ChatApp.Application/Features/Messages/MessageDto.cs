using ChatApp.Domain.Enums;

namespace ChatApp.Application.Features.Messages;

/// <summary>
/// A message as returned to callers. <see cref="Content"/> is set for <see cref="MessageType.Text"/>;
/// <see cref="ImageUrl"/>/<see cref="Caption"/> are set for <see cref="MessageType.Image"/> — check
/// <see cref="Type"/> to know which group applies.
/// </summary>
/// <param name="Id">The message's id.</param>
/// <param name="ConversationId">The conversation this message belongs to.</param>
/// <param name="SenderUserId">The sender's user id; may be the hidden AI Agent's id.</param>
/// <param name="Type">Which payload group is populated.</param>
/// <param name="RepliesToMessageId">The message this one replies to, if any.</param>
/// <param name="SentAt">When the message was sent.</param>
/// <param name="Content">The text body; set only when <paramref name="Type"/> is <see cref="MessageType.Text"/>.</param>
/// <param name="ImageUrl">The image's Storage location; set only when <paramref name="Type"/> is <see cref="MessageType.Image"/>.</param>
/// <param name="Caption">The AI-generated caption; set only for images, and only if captioning succeeded.</param>
public sealed record MessageDto(
    Guid Id,
    Guid ConversationId,
    Guid SenderUserId,
    MessageType Type,
    Guid? RepliesToMessageId,
    DateTime SentAt,
    string? Content,
    string? ImageUrl,
    string? Caption);
