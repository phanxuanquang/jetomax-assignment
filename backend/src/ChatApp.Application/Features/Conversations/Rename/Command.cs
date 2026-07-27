using ChatApp.Application.Common.Results;
using MediatR;

namespace ChatApp.Application.Features.Conversations.Rename;

/// <summary>Renames a conversation's <c>DisplayName</c> (F-4). Owner-only.</summary>
/// <param name="ConversationId">The conversation to rename.</param>
/// <param name="DisplayName">The new display name.</param>
public sealed record Command(Guid ConversationId, string DisplayName) : IRequest<Result>;
