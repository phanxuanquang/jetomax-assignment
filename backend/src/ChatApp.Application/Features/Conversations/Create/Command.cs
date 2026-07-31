using ChatApp.Application.Common.Results;
using MediatR;

namespace ChatApp.Application.Features.Conversations.Create;

/// <summary>Creates a new conversation owned by the caller with the caller plus every named participant; requires at least one other participant.</summary>
/// <param name="ParticipantUsernames">Usernames of the other participants to add alongside the caller; each must resolve to an existing user or the whole request fails (404).</param>
public sealed record Command(IReadOnlyCollection<string> ParticipantUsernames) : IRequest<Result<ConversationDto>>;
