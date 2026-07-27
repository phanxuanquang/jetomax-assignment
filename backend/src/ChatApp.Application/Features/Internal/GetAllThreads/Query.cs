using ChatApp.Application.Common.Results;
using MediatR;

namespace ChatApp.Application.Features.Internal.GetAllThreads;

/// <summary>Lists every non-deleted conversation, for n8n's daily digest job (§6.2). N8n-only; takes no input.</summary>
public sealed record Query : IRequest<Result<IReadOnlyList<ThreadDto>>>;
