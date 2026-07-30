using ChatApp.Application.Abstractions;
using ChatApp.Domain.Entities;

namespace ChatApp.Application.Memory;

/// <summary>
/// The one plain service that owns the hierarchical rolling summarization pipeline (§6): counting a
/// new message's tokens, folding a chunk into the rolling global memory once the pending-token
/// threshold is crossed, and serving on-demand summaries as a pure read. Uses only Application ports
/// (<see cref="IAppDbContext"/>, <see cref="IGenerativeAiService"/>) — no direct AI SDK or EF Core
/// calls, and no <c>IMediator</c> dispatch (§4.1). Called directly by <c>Send</c>/<c>SendImage</c>'s
/// detached, own-scope trigger (wired in the Api task) and by the <c>Internal.SummarizeConversation</c>/
/// <c>SummarizeConversations</c> on-demand handlers.
/// </summary>
public sealed class ConversationMemoryService(IAppDbContext db, IGenerativeAiService generativeAiService)
{
    /// <summary>
    /// The detached-per-send seam (§6, A-1/B-2/B-7): counts <paramref name="messageText"/>'s tokens
    /// remotely, adds them to the conversation's pending counter, and — only if that crosses
    /// <paramref name="thresholdTokens"/> — summarizes the pending chunk, folds it into global
    /// memory, and resets the counter. A no-op if the conversation has no memory row. This method is
    /// stateless with respect to any ambient request and safe to call from a freshly opened DI scope;
    /// it does not open that scope itself — the caller (Api's send-triggered detached task) is
    /// </summary>
    public async Task RecordMessageAndProcessAsync(Guid conversationId, string messageText, int thresholdTokens, CancellationToken cancellationToken)
    {
        var memory = await db.FirstOrDefaultAsync(db.ConversationMemories.Where(m => m.ConversationId == conversationId), cancellationToken);
        if (memory is null)
        {
            return;
        }

        var tokenCount = await generativeAiService.CountTokensAsync(messageText, cancellationToken);
        memory.PendingTokens += tokenCount;
        memory.LastUpdatedTime = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);

        if (memory.PendingTokens < thresholdTokens)
        {
            return;
        }

        await FoldPendingAsync(conversationId, memory, cancellationToken);
    }

    /// <summary>
    /// Returns the conversation's current global memory combined with a fresh summary of every
    /// message after the last chunk's pointer, as one on-demand summary (§6, C-3: pure read — never
    /// mutates stored memory or resets the pending counter, regardless of the threshold).
    /// </summary>
    public async Task<string> GetOnDemandSummaryAsync(Guid conversationId, CancellationToken cancellationToken)
    {
        var summary = await GetOnDemandSummaryPartsAsync(conversationId, cancellationToken);

        if (string.IsNullOrEmpty(summary.RecentSummary))
        {
            return summary.GlobalMemory;
        }

        if (string.IsNullOrEmpty(summary.GlobalMemory))
        {
            return summary.RecentSummary;
        }

        return await CombineAsync(summary.GlobalMemory, summary.RecentSummary, cancellationToken);
    }

    private async Task<OnDemandSummary> GetOnDemandSummaryPartsAsync(Guid conversationId, CancellationToken cancellationToken)
    {
        var memory = await db.FirstOrDefaultAsync(db.ConversationMemories.Where(m => m.ConversationId == conversationId), cancellationToken);
        var globalMemory = memory?.GlobalMemory ?? string.Empty;

        var pointer = await GetPointerAsync(conversationId, cancellationToken);
        var recentMessages = await LoadMessagesAfterAsync(conversationId, pointer, cancellationToken);

        var recentSummary = recentMessages.Count == 0
            ? string.Empty
            : await SummarizeAsync(globalMemory, recentMessages, cancellationToken);

        return new OnDemandSummary(globalMemory, recentSummary);
    }

    private async Task FoldPendingAsync(Guid conversationId, ConversationMemory memory, CancellationToken cancellationToken)
    {
        var pointer = await GetPointerAsync(conversationId, cancellationToken);
        var pending = await LoadMessagesAfterAsync(conversationId, pointer, cancellationToken);
        if (pending.Count == 0)
        {
            memory.PendingTokens = 0;
            await db.SaveChangesAsync(cancellationToken);
            return;
        }

        var chunkSummary = await SummarizeAsync(memory.GlobalMemory, pending, cancellationToken);
        var foldedMemory = await CombineAsync(memory.GlobalMemory, chunkSummary, cancellationToken);

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

        return await db.ToListAsync(
            db.Messages.Where(m => m.ConversationId == conversationId).OrderBy(m => m.SentAt),
            cancellationToken);
    }

    private async Task<string> SummarizeAsync(string currentGlobalMemory, IReadOnlyList<Message> messages, CancellationToken cancellationToken)
    {
        var transcript = string.Join('\n', messages.Select(FormatMessageForPrompt));
        var prompt = $"""
            You are maintaining a rolling summary of an ongoing chat conversation. Summarize the
            new messages below in a concise, token-frugal way, in light of the current overall
            memory for context (do not repeat it back). Preserve concrete facts: names, decisions,
            numbers, and negations (things explicitly ruled out or denied).

            Current overall memory (may be empty for a brand-new conversation):
            
            ```markdown
            {currentGlobalMemory}
            ```

            New messages to summarize:
            {transcript}
            """;

        return await generativeAiService.GenerateContentAsync<string>(prompt, cancellationToken: cancellationToken);
    }

    private async Task<string> CombineAsync(string currentGlobalMemory, string newSummary, CancellationToken cancellationToken)
    {
        var prompt = $"""
            You maintain a single rolling summary of an entire chat conversation. Fold the new
            summary below into the existing overall summary, producing one updated overall summary.
            Preserve concrete facts (names, decisions, numbers, negations) from both. Keep the
            result concise and bounded in size — do not let it grow without bound as more content
            is folded in over time.

            Existing overall summary (may be empty):
            
            ```markdown
            {currentGlobalMemory}
            ```

            New summary to fold in:
            {newSummary}
            """;

        return await generativeAiService.GenerateContentAsync<string>(prompt, cancellationToken: cancellationToken);
    }

    private static string FormatMessageForPrompt(Message message) => message switch
    {
        TextMessage text => $"[{text.SentAt:u}] {text.Content}",
        ImageMessage image => $"[{image.SentAt:u}] (image) {image.Caption ?? "(no caption)"}",
        _ => string.Empty
    };
}
