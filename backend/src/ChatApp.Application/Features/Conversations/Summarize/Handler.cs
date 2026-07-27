using ChatApp.Application.Abstractions;
using ChatApp.Application.Common.Results;
using ChatApp.Application.Memory;
using MediatR;

namespace ChatApp.Application.Features.Conversations.Summarize;

/// <summary>Handles <see cref="Query"/>.</summary>
public sealed class Handler(IAppDbContext db, ICurrentUser currentUser, MemoryService memoryService)
    : IRequestHandler<Query, Result<ThreadSummaryDto>>
{
    /// <summary>
    /// Confirms the conversation exists and, when the caller carries a user identity, that they are
    /// a participant — a caller with no identity (e.g. n8n) skips the check and summarizes any
    /// thread for the daily digest.
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

        var callerResult = await currentUser.GetCurrentUserAsync(cancellationToken);
        if (callerResult.IsSuccess)
        {
            var callerId = callerResult.Value!.Id;

            var isParticipant = await db.AnyAsync(
                db.Participants.Where(p => p.ConversationId == request.ConversationId && p.UserId == callerId),
                cancellationToken);

            if (!isParticipant)
            {
                return Result<ThreadSummaryDto>.Failure(Error.Forbidden("summary.not_participant", "The caller is not a participant of this conversation."));
            }
        }

        var summary = await memoryService.GetOnDemandSummaryAsync(request.ConversationId, cancellationToken);
        return Result<ThreadSummaryDto>.Success(new ThreadSummaryDto(summary.GlobalMemory, summary.RecentSummary));
    }
}
