using ChatApp.Application.Abstractions;
using ChatApp.Application.Common.Results;
using ChatApp.Application.Memory;
using MediatR;

namespace ChatApp.Application.Features.Internal.SummarizeConversation;

/// <summary>
/// Handles <see cref="Query"/>. Serves the one on-demand-summary endpoint shared by the in-app
/// "Summarize" action and the MCP <c>summarize_thread</c> tool (F-7) — both reach it as a "member"
/// of the conversation, per §9.2. A pure read (decision C-3): calls <see cref="ConversationMemoryService"/>
/// directly — never through <c>IMediator</c> — and never mutates stored memory or the pending-token counter.
/// </summary>
public sealed class Handler(IAppDbContext db, IConversationAccess conversationAccess, ConversationMemoryService memoryService)
    : IRequestHandler<Query, Result<string>>
{
    /// <summary>Returns the conversation's current global memory combined with a fresh summary of everything since the last checkpoint.</summary>
    public async Task<Result<string>> Handle(Query request, CancellationToken cancellationToken)
    {
        var callerId = conversationAccess.UserId;

        var isParticipant = await db.AnyAsync(
            db.Participants.Where(p => p.ConversationId == request.ConversationId && p.UserId == callerId),
            cancellationToken);

        if (!isParticipant)
        {
            return Result<string>.Failure(Error.Forbidden("conversation.summary.not_participant", "The caller is not a participant of this conversation."));
        }

        var conversationExists = await db.AnyAsync(
            db.Conversations.Where(c => c.Id == request.ConversationId && c.Messages.Any()),
            cancellationToken);

        if (!conversationExists)
        {
            return Result<string>.Failure(Error.NotFound("conversation.not_found", "Conversation not found or does not have any messages yet."));
        }

        var summary = await memoryService.GetOnDemandSummaryAsync(request.ConversationId, cancellationToken);
        return Result<string>.Success(summary);
    }
}
