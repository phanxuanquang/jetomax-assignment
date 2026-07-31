namespace ChatApp.Api.Memory;

/// <summary>Binds the <c>Memory</c> configuration section (see <c>prerequisite-setups.md</c>).</summary>
public sealed class ConversationMemoryOptions
{
    /// <summary>Pending tokens a conversation must accrue before its next chunk is summarized and folded (§6).</summary>
    public required int TokenThreshold { get; init; }
}
