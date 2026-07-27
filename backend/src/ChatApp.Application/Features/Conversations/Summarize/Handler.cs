using ChatApp.Application.Abstractions;
using ChatApp.Application.Common;
using ChatApp.Application.Common.Results;
using ChatApp.Application.Memory;
using MediatR;

namespace ChatApp.Application.Features.Conversations.Summarize;

/// <summary>Handles <see cref="Query"/>.</summary>
public sealed class Handler(IAppDbContext db, ICurrentUser currentUser, MemoryService memoryService)
    : IRequestHandler<Query, Result<ThreadSummaryDto>>
{
    /// <summary>
    /// Confirms the conversation exists and, for App/Mcp callers, that the caller is a participant
    /// — <see cref="Client.N8n"/> carries no user and summarizes any thread for the daily digest.
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

        if (currentUser.Client != Client.N8n)
        {
            if (currentUser.UserId is not { } callerId)
            {
                return Result<ThreadSummaryDto>.Failure(Error.Forbidden("summary.no_identity", "The caller has no user identity."));
            }

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
