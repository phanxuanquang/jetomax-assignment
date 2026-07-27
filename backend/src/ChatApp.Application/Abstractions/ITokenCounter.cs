namespace ChatApp.Application.Abstractions;

/// <summary>
/// The port onto local token counting (<c>Microsoft.ML.Tokenizers</c> in Infrastructure). A cheap,
/// local operation — never an LLM call — used to maintain <c>ConversationMemory.PendingTokens</c>.
/// </summary>
public interface ITokenCounter
{
    /// <summary>Counts the tokens in <paramref name="text"/>. For an image message, callers pass its caption.</summary>
    int CountTokens(string text);
}
