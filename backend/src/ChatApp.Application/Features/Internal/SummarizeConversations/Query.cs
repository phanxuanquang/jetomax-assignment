using ChatApp.Application.Common.Results;
using MediatR;

namespace ChatApp.Application.Features.Internal.SummarizeConversations;

public sealed record Query(double HoursAgo) : IRequest<Result<string>>;
