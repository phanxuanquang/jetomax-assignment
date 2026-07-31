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
/// <para>
/// <b>Two distinct writing styles, by audience.</b> <c>chunk_memories.memory</c> (the per-chunk fold
/// input) is never read by anything except this service's own next fold — it is a pure machine-to-machine
/// artifact, so it is generated in a terse, "caveman" register (articles/filler/hedging dropped,
/// telegraphic fragments) to keep it token-frugal. <c>conversation_memory.global_memory</c> and the
/// on-demand "recent tail" summary are both directly returned to a human (or ChatGPT/n8n on a human's
/// behalf) — see <see cref="GetOnDemandSummaryAsync"/>, which can return <c>global_memory</c> completely
/// unprocessed — so both are always produced/maintained in clear, concise, natural English.
/// </para>
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
            : await SummarizeRecentAsync(globalMemory, recentMessages, cancellationToken);

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

        var chunkSummary = await SummarizeChunkAsync(memory.GlobalMemory, pending, cancellationToken);
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

    /// <summary>
    /// System instruction for <c>chunk_memories.memory</c>: pure machine-to-machine notes, re-fed only
    /// to this same pipeline's future prompts — never rendered to a human. Deliberately "caveman"-style
    /// (terse, telegraphic, articles/filler/hedging dropped) to minimize the token cost of carrying
    /// this context forward through every later fold; see the class doc for why this register is safe
    /// here specifically (this text is never a human-facing output).
    /// </summary>
    private const string ChunkSystemInstruction = """
        Compress chat into terse notes for AI memory only. Never read by human.
        Drop: articles (a/an/the), filler words, pleasantries, hedging (likely/probably), full grammar.
        Keep: names, numbers, decisions, dates, negations (what was ruled out or denied).
        Format: short fragments. subject-verb-object. one fact per line. no prose, no narration.
        """;

    /// <summary>
    /// System instruction shared by every AI output a human (or an assistant reading on a human's
    /// behalf — ChatGPT via MCP, n8n's digest) can see directly: <c>global_memory</c>, the on-demand
    /// "recent tail", the digest roll-up (<c>Internal.SummarizeConversations</c>), and image captions
    /// (<c>Messages.SendImage</c>). Natural language, English, concise, clear. Internal (not private)
    /// so those other call sites share the exact same wording rather than drifting duplicates.
    /// </summary>
    internal const string HumanFacingSystemInstruction = """
        Write for a human reader. Natural English. Concise, clear, specific — no filler, no hedging,
        no meta-commentary about what you're doing. Preserve concrete facts: names, decisions,
        numbers, and negations (things explicitly ruled out or denied).
        """;

    /// <summary>
    /// Produces the pending chunk's summary (<c>chunk_memories.memory</c>) — internal-only input to
    /// the next fold, so it uses <see cref="ChunkSystemInstruction"/> to stay token-frugal.
    /// </summary>
    private async Task<string> SummarizeChunkAsync(string currentGlobalMemory, IReadOnlyList<Message> messages, CancellationToken cancellationToken)
    {
        var transcript = string.Join('\n', messages.Select(FormatMessageForPrompt));
        var prompt = $"""
            Context (do not repeat back):
            {currentGlobalMemory}

            New messages:
            {transcript}
            """;

        return await generativeAiService.GenerateContentAsync<string>(prompt, ChunkSystemInstruction, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Produces the on-demand "recent tail" summary — returned directly as part of a human-facing
    /// summary (§6), so it uses <see cref="HumanFacingSystemInstruction"/>.
    /// </summary>
    private async Task<string> SummarizeRecentAsync(string currentGlobalMemory, IReadOnlyList<Message> messages, CancellationToken cancellationToken)
    {
        var transcript = string.Join('\n', messages.Select(FormatMessageForPrompt));
        var prompt = $"""
            Summarize the new messages below for a human reader, using the current overall memory only
            for context (do not repeat it back).

            Current overall memory (may be empty for a brand-new conversation):
            {currentGlobalMemory}

            New messages to summarize:
            {transcript}
            """;

        return await generativeAiService.GenerateContentAsync<string>(prompt, HumanFacingSystemInstruction, cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Folds <paramref name="newSummary"/> into <paramref name="currentGlobalMemory"/>, producing the
    /// next <c>global_memory</c> — always human-facing (§6, C-3: can be returned to a caller
    /// completely unprocessed), so this always writes/maintains clear natural English even though
    /// <paramref name="newSummary"/> may itself be a terse chunk note from <see cref="SummarizeChunkAsync"/>.
    /// </summary>
    private async Task<string> CombineAsync(string currentGlobalMemory, string newSummary, CancellationToken cancellationToken)
    {
        var prompt = $"""
            Fold the new note below into the existing overall summary, producing one updated overall
            summary. The new note may be terse/shorthand (internal AI notes) — expand it into full,
            natural sentences in the result. Keep the result concise and bounded in size — do not let
            it grow without bound as more content is folded in over time.

            Existing overall summary (may be empty):
            {currentGlobalMemory}

            New note to fold in:
            {newSummary}
            """;

        return await generativeAiService.GenerateContentAsync<string>(prompt, HumanFacingSystemInstruction, cancellationToken: cancellationToken);
    }

    private static string FormatMessageForPrompt(Message message) => message switch
    {
        TextMessage text => $"[{text.SentAt:u}] {text.Content}",
        ImageMessage image => $"[{image.SentAt:u}] (image) {image.Caption ?? "(no caption)"}",
        _ => string.Empty
    };
}
