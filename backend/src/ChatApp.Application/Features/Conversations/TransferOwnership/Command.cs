using ChatApp.Application.Common.Results;
using MediatR;

namespace ChatApp.Application.Features.Conversations.TransferOwnership;

/// <summary>Transfers ownership of a conversation to another participant (F-4). Owner-only.</summary>
/// <param name="ConversationId">The conversation whose ownership is transferred.</param>
/// <param name="NewOwnerUserId">The existing participant who becomes the new owner.</param>
public sealed record Command(Guid ConversationId, Guid NewOwnerUserId) : IRequest<Result>;
