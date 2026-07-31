using ChatApp.Application.Common.Results;
using MediatR;

namespace ChatApp.Application.Features.Messages.SendImage;

/// <summary>Sends an image message into a conversation; the image was already uploaded client-side directly to Storage, so the backend only ever handles its URL. Blocked when the conversation is read-only, unless the caller is its owner.</summary>
/// <param name="ConversationId">The conversation to send into.</param>
/// <param name="ImageUrl">The Storage URL of the already-uploaded image.</param>
public sealed record Command(Guid ConversationId, string ImageUrl) : IRequest<Result<MessageDto>>;
