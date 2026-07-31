using ChatApp.Application.Common.Results;
using MediatR;

namespace ChatApp.Application.Features.Conversations.Leave;

/// <summary>
/// Removes the caller from a conversation's membership (F-4). When the caller is the owner,
/// <paramref name="Mode"/> is required and chooses between soft-deleting the conversation or
/// freezing it (<c>OwnerId = null</c>); for a non-owner, <paramref name="Mode"/> is ignored.
/// </summary>
/// <param name="ConversationId">The conversation to leave.</param>
/// <param name="Mode">Required only when the caller is the owner; otherwise ignored.</param>
public sealed record Command(Guid ConversationId, LeaveMode? Mode) : IRequest<Result>;
