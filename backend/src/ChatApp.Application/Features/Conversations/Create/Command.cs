using ChatApp.Application.Common.Results;
using MediatR;

namespace ChatApp.Application.Features.Conversations.Create;

/// <summary>
/// Creates a new conversation owned by the caller, with the caller plus every user named in
/// <see cref="ParticipantUsernames"/> as participants (F-3, A8: requires at least one other participant).
/// The backend generates a unique <c>PublicId</c> and an initial <c>DisplayName</c> from participant usernames.
/// </summary>
/// <param name="ParticipantUsernames">Usernames of the other participants to add alongside the caller; each must resolve to an existing user or the whole request fails (404).</param>
public sealed record Command(IReadOnlyCollection<string> ParticipantUsernames) : IRequest<Result<ConversationDto>>;
