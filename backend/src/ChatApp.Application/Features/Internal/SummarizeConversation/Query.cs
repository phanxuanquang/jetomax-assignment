using ChatApp.Application.Common.Results;
using MediatR;

namespace ChatApp.Application.Features.Internal.SummarizeConversation;

public sealed record Query(Guid ConversationId) : IRequest<Result<string>>;
