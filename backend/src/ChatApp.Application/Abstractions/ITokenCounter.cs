namespace ChatApp.Application.Abstractions;

/// <summary>
/// The port onto local token counting (<c>Microsoft.ML.Tokenizers</c> in Infrastructure) and the
/// pending-token bookkeeping it feeds. Both members are cheap, local operations — never an LLM
/// call — used to maintain <c>ConversationMemory.PendingTokens</c> (§6's hot path).
/// </summary>
public interface ITokenCounter
{
    /// <summary>Counts the tokens in <paramref name="text"/>. For an image message, callers pass its caption.</summary>
    Task CountTokensAsync(string text);

    /// <summary>
    /// Counts the tokens in <paramref name="text"/> and adds them to <paramref name="conversationId"/>'s
    /// pending counter. A missing memory row is treated as nothing to accrue rather than a failure —
    /// bookkeeping must never block the message that triggered it.
    /// </summary>
    Task UpdatePendingTokensAsync(Guid conversationId, string text, CancellationToken cancellationToken);
}
