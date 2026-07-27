using ChatApp.Application.Common.Results;
using MediatR;

namespace ChatApp.Application.Features.Conversations.Create;

/// <summary>
/// Creates a new conversation owned by the caller, with the caller plus every id in
/// <see cref="ParticipantUserIds"/> as participants (F-3, A8: requires at least one other participant).
/// The backend generates a unique <c>PublicId</c> and an initial <c>DisplayName</c> from participant usernames.
/// </summary>
/// <param name="ParticipantUserIds">User ids of the other participants to add alongside the caller.</param>
public sealed record Command(IReadOnlyCollection<Guid> ParticipantUserIds) : IRequest<Result<ConversationDto>>;
