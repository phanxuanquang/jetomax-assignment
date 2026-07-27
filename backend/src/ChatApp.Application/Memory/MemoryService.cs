using ChatApp.Application.Abstractions;
using ChatApp.Domain.Entities;

namespace ChatApp.Application.Memory;

/// <summary>
/// Orchestrates the hierarchical rolling summarization pipeline (§6): snapshotting the pending
/// message range, summarizing it, and folding that into the conversation's rolling global memory.
/// Uses only Application ports — no direct AI SDK or EF Core calls. Called by Api's background
/// worker (threshold trigger) and by <c>SummarizeThread</c> (on-demand trigger).
/// </summary>
public sealed class MemoryService(IAppDbContext db, ISummaryService summaryService)
{
    /// <summary>
    /// If the conversation's pending token count has reached <paramref name="thresholdTokens"/>,
    /// summarizes every message after the last chunk's pointer, folds that summary into the global
    /// memory, persists a new chunk, and resets the pending counter. A no-op otherwise, or if there
    /// are no pending messages to summarize.
    /// </summary>
    public async Task ProcessPendingIfThresholdCrossedAsync(Guid conversationId, int thresholdTokens, CancellationToken cancellationToken)
    {
        var memory = await db.FirstOrDefaultAsync(db.ConversationMemories.Where(m => m.ConversationId == conversationId), cancellationToken);
        if (memory is null || memory.PendingTokens < thresholdTokens)
        {
            return;
        }

        var pointer = await GetPointerAsync(conversationId, cancellationToken);
        var pending = await LoadMessagesAfterAsync(conversationId, pointer, cancellationToken);
        if (pending.Count == 0)
        {
            return;
        }

        var chunkSummary = await summaryService.SummarizeAsync(memory.GlobalMemory, pending, cancellationToken);
        var foldedMemory = await summaryService.FoldAsync(memory.GlobalMemory, chunkSummary, cancellationToken);

        db.Add(new ChunkMemory
        {
            ConversationId = conversationId,
            StartMessageId = pending[0].Id,
            EndMessageId = pending[^1].Id,
            Memory = chunkSummary
        });

        memory.GlobalMemory = foldedMemory;
        memory.PendingTokens = 0;
        memory.LastUpdatedTime = DateTime.UtcNow;

        await db.SaveChangesAsync(cancellationToken);
    }

    /// <summary>
    /// Returns the conversation's current global memory plus a fresh summary of every message
    /// after the last chunk's pointer — regardless of whether the pending-token threshold has been
    /// crossed. The fresh summary is never persisted as a chunk; it is always recomputed.
    /// </summary>
    public async Task<OnDemandSummary> GenerateSummaryAsync(Guid conversationId, CancellationToken cancellationToken)
    {
        var memory = await db.FirstOrDefaultAsync(db.ConversationMemories.Where(m => m.ConversationId == conversationId), cancellationToken);
        var globalMemory = memory?.GlobalMemory ?? string.Empty;

        var pointer = await GetPointerAsync(conversationId, cancellationToken);
        var recentMessages = await LoadMessagesAfterAsync(conversationId, pointer, cancellationToken);

        var recentSummary = recentMessages.Count == 0
            ? string.Empty
            : await summaryService.SummarizeAsync(globalMemory, recentMessages, cancellationToken);

        return new OnDemandSummary(globalMemory, recentSummary);
    }

    private async Task<Guid?> GetPointerAsync(Guid conversationId, CancellationToken cancellationToken)
    {
        var newestChunk = await db.FirstOrDefaultAsync(
            db.ChunkMemories.Where(c => c.ConversationId == conversationId).OrderByDescending(c => c.Id),
            cancellationToken);

        return newestChunk?.EndMessageId;
    }

    private async Task<List<Message>> LoadMessagesAfterAsync(Guid conversationId, Guid? pointer, CancellationToken cancellationToken)
    {
        if (pointer is { } pointerId)
        {
            var pointerMessage = await db.FirstOrDefaultAsync(db.Messages.Where(m => m.Id == pointerId), cancellationToken);
            if (pointerMessage is not null)
            {
                return await db.ToListAsync(
                    db.Messages.Where(m => m.ConversationId == conversationId && m.SentAt > pointerMessage.SentAt).OrderBy(m => m.SentAt),
                    cancellationToken);
            }
        }

        // No chunk yet, or the pointer message was removed (replies_to-style FK set null elsewhere):
        // fall back to the conversation's full history.
        return await db.ToListAsync(
            db.Messages.Where(m => m.ConversationId == conversationId).OrderBy(m => m.SentAt),
            cancellationToken);
    }
}
