using ChatApp.Application.Common.Results;
using MediatR;

namespace ChatApp.Application.Features.Messages.Send;

/// <summary>Sends a text message into a conversation. Blocked when the conversation is read-only, unless the caller is its owner.</summary>
/// <param name="ConversationId">The conversation to send into.</param>
/// <param name="Content">The Markdown message body.</param>
public sealed record Command(Guid ConversationId, string Content) : IRequest<Result<MessageDto>>;
