using ChatApp.Application.Common.Results;
using MediatR;

namespace ChatApp.Application.Features.Conversations.Join;

/// <summary>
/// Joins the caller into the conversation identified by <paramref name="PublicId"/> (F-3, A7).
/// Rejected if the conversation is frozen or deleted; a no-op if the caller already joined.
/// </summary>
/// <param name="PublicId">The exact, case-sensitive public code of the conversation to join.</param>
public sealed record Command(string PublicId) : IRequest<Result<ConversationDto>>;
