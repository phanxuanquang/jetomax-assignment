using ChatApp.Application.Common.Results;
using MediatR;

namespace ChatApp.Application.Features.Internal.UpdateConversationMemory;

public sealed record Query(Guid ConversationId, Guid FromMessageId) : IRequest<Result<ConversationMemoryDto>>;