using ChatApp.Application.Abstractions;
using ChatApp.Application.Common.Results;
using ChatApp.Application.Memory;
using MediatR;

namespace ChatApp.Application.Features.Internal.Summarize;

/// <summary>Handles <see cref="Query"/>.</summary>
public sealed class Handler(IAppDbContext db, ICurrentUser currentUser, ConversationMemoryService memoryService)
    : IRequestHandler<Query, Result<ThreadSummaryDto>>
{
    /// <summary>
    /// Confirms the conversation exists and, when the caller carries a user identity, that they are
    /// a participant. A caller with no user identity (e.g. n8n) skips the participant check and
    /// summarizes any thread for the daily digest; which caller kinds may reach this endpoint at all
    /// is enforced by the Api layer (<c>[AllowedClients]</c>, §4.2), not here.
    /// </summary>
    public async Task<Result<ThreadSummaryDto>> Handle(Query request, CancellationToken cancellationToken)
    {
        var conversationExists = await db.AnyAsync(
            db.Conversations.Where(c => c.Id == request.ConversationId && !c.IsDeleted),
            cancellationToken);

        if (!conversationExists)
        {
            return Result<ThreadSummaryDto>.Failure(Error.NotFound("conversation.not_found", "Conversation not found."));
        }

        var summary = await memoryService.UpdateMemoryAsync(request.ConversationId, cancellationToken);
        return Result<ThreadSummaryDto>.Success(new ThreadSummaryDto(summary.GlobalMemory, summary.RecentSummary));
    }
}
