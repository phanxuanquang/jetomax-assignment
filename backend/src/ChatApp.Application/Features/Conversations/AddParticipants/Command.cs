using ChatApp.Application.Common.Results;
using MediatR;

namespace ChatApp.Application.Features.Conversations.AddParticipants;

/// <summary>Adds one or more participants to a conversation (F-4). Owner-only.</summary>
/// <param name="ConversationId">The conversation to add participants to.</param>
/// <param name="UserIds">The users to add; ids already participating are skipped.</param>
public sealed record Command(Guid ConversationId, IReadOnlyCollection<Guid> UserIds) : IRequest<Result>;
