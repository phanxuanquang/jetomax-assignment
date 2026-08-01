using ChatApp.Application.Abstractions;
using ChatApp.Application.Common.Results;
using ChatApp.Application.Memory;
using ChatApp.Domain.Enums;
using MediatR;

namespace ChatApp.Application.Features.Internal.SummarizeConversation;

/// <summary>
/// Handles <see cref="Query"/>. Shared by the in-app Summarize action and the MCP summarize_thread
/// tool; caller must already be a participant. A pure read: calls <see cref="ConversationMemoryService"/>
/// directly — never through <c>IMediator</c> — and never mutates stored memory or the pending-token counter.
/// </summary>
public sealed class Handler(IAppDbContext db, IConversationAccess conversationAccess, ConversationMemoryService memoryService)
    : IRequestHandler<Query, Result<string>>
{
    /// <summary>Returns the conversation's current global memory combined with a fresh summary of everything since the last checkpoint.</summary>
    public async Task<Result<string>> Handle(Query request, CancellationToken cancellationToken)
    {
        var callerId = conversationAccess.UserId;

        var isAdmin = await db.AnyAsync(
            db.Users.Where(u => u.Id == callerId && u.Role == UserRole.Administrator),
            cancellationToken);

        if (!isAdmin)
        {
            return Result<string>.Failure(Error.Forbidden("conversation.summary.not_admin", "The caller is not an administrator."));
        }

        var conversationExists = await db.AnyAsync(
            db.Conversations.Where(c => c.Id == request.ConversationId),
            cancellationToken);

        if (!conversationExists)
        {
            return Result<string>.Failure(Error.NotFound("conversation.not_found", "Conversation not found"));
        }

        var summary = await memoryService.GetOnDemandSummaryAsync(request.ConversationId, cancellationToken);
        return Result<string>.Success(summary);
    }
}
