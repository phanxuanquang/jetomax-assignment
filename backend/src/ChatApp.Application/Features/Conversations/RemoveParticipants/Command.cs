using ChatApp.Application.Common.Results;
using MediatR;

namespace ChatApp.Application.Features.Conversations.RemoveParticipants;

/// <summary>Removes one or more participants from a conversation. Owner-only; the owner cannot remove themself this way (use Transfer or Leave).</summary>
/// <param name="ConversationId">The conversation to remove participants from.</param>
/// <param name="Usernames">Usernames of the users to remove; each must resolve to an existing user or the whole batch fails (404).</param>
public sealed record Command(Guid ConversationId, IReadOnlyCollection<string> Usernames) : IRequest<Result>;
