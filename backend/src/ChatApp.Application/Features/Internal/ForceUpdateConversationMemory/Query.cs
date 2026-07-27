using ChatApp.Application.Common.Results;
using MediatR;

namespace ChatApp.Application.Features.Internal.ForceUpdateConversationMemory;

public sealed record Query(Guid ConversationId) : IRequest<Result<ConversationMemoryDto>>;