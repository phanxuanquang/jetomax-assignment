using ChatApp.Application.Common.Results;
using MediatR;

namespace ChatApp.Application.Features.Internal.GetAllConversations;

public sealed record Query : IRequest<Result<IReadOnlyList<ConversationMetaDto>>>;
