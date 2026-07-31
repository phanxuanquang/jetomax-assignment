namespace ChatApp.Api.Memory;

public sealed class ConversationMemoryOptions
{
    /// <summary>Pending tokens a conversation must accrue before its next chunk is summarized and folded.</summary>
    public required int TokenThreshold { get; init; }
}
