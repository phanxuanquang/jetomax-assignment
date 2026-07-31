using ChatApp.Application.Common.Results;
using MediatR;

namespace ChatApp.Application.Features.Conversations.AddParticipants;

/// <summary>Adds one or more participants to a conversation. Owner-only.</summary>
/// <param name="ConversationId">The conversation to add participants to.</param>
/// <param name="Usernames">Usernames of the users to add; each must resolve to an existing user or the whole batch fails (404). Users already participating are skipped.</param>
public sealed record Command(Guid ConversationId, IReadOnlyCollection<string> Usernames) : IRequest<Result>;
