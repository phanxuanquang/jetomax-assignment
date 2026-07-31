using ChatApp.Application.Common.Results;
using MediatR;

namespace ChatApp.Application.Features.Conversations.SetReadonly;

/// <summary>Manually sets a conversation's <c>IsReadonly</c> flag. Owner-only.</summary>
/// <param name="ConversationId">The conversation to update.</param>
/// <param name="IsReadonly">True to restrict sending to the owner only; false to allow all participants.</param>
public sealed record Command(Guid ConversationId, bool IsReadonly) : IRequest<Result>;
