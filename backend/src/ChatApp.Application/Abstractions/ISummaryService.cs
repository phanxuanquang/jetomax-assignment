using ChatApp.Domain.Entities;

namespace ChatApp.Application.Abstractions;

/// <summary>
/// The port onto the summarization-capable model (Semantic Kernel + Gemini), used by
/// <see cref="Memory.ConversationMemoryService"/> for the hierarchical rolling summarization pipeline (§6).
/// Single, stateless calls — no memory constructs on the model side; all state lives in Postgres.
/// </summary>
public interface ISummaryService
{
    /// <summary>
    /// Summarizes <paramref name="messages"/> in light of <paramref name="currentConversationGlobalMemory"/>,
    /// producing a token-frugal summary that still preserves names, decisions, numbers, and
    /// negations. Used both for a threshold-triggered chunk and for an on-demand tail summary.
    /// </summary>
    Task<string> GenerateSummaryAsync(string currentConversationGlobalMemory, IReadOnlyList<Message> messages, CancellationToken cancellationToken);

    /// <summary>
    /// Folds <paramref name="chunkSummary"/> into <paramref name="currentConversationGlobalMemory"/>, producing
    /// the new, still size-bounded, rolling global memory.
    /// </summary>
    Task<string> FoldAsync(string currentConversationGlobalMemory, string chunkSummary, CancellationToken cancellationToken);
}
