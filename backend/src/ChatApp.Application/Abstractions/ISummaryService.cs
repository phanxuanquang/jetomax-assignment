using ChatApp.Domain.Entities;

namespace ChatApp.Application.Abstractions;

/// <summary>
/// The port onto the summarization-capable model (Semantic Kernel + Gemini), used by
/// <see cref="Memory.MemoryService"/> for the hierarchical rolling summarization pipeline (§6).
/// Single, stateless calls — no memory constructs on the model side; all state lives in Postgres.
/// Also carries the pending-token bookkeeping that feeds this pipeline (see
/// <see cref="AddPendingTokensAsync"/>), even though that specific operation is local and never calls the model.
/// </summary>
public interface ISummaryService
{
    /// <summary>
    /// Summarizes <paramref name="messages"/> in light of <paramref name="currentGlobalMemory"/>,
    /// producing a token-frugal summary that still preserves names, decisions, numbers, and
    /// negations. Used both for a threshold-triggered chunk and for an on-demand tail summary.
    /// </summary>
    Task<string> SummarizeAsync(string currentGlobalMemory, IReadOnlyList<Message> messages, CancellationToken cancellationToken);

    /// <summary>
    /// Folds <paramref name="chunkSummary"/> into <paramref name="previousGlobalMemory"/>, producing
    /// the new, still size-bounded, rolling global memory.
    /// </summary>
    Task<string> FoldAsync(string previousGlobalMemory, string chunkSummary, CancellationToken cancellationToken);

    /// <summary>
    /// Counts the tokens in <paramref name="text"/> — a cheap local operation, not a model call; for
    /// an image message, callers pass its caption — and adds them to <paramref name="conversationId"/>'s
    /// pending counter (§6's hot path). A missing memory row is treated as nothing to accrue rather
    /// than a failure — bookkeeping must never block the message that triggered it.
    /// </summary>
    Task AddPendingTokensAsync(Guid conversationId, string text, CancellationToken cancellationToken);
}
