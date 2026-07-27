using ChatApp.Application.Common.Results;
using MediatR;

namespace ChatApp.Application.Features.Conversations.RemoveParticipants;

/// <summary>Removes one or more participants from a conversation (F-4). Owner-only; the owner cannot remove themself this way (use Transfer or Leave).</summary>
/// <param name="ConversationId">The conversation to remove participants from.</param>
/// <param name="UserIds">The users to remove.</param>
public sealed record Command(Guid ConversationId, IReadOnlyCollection<Guid> UserIds) : IRequest<Result>;
