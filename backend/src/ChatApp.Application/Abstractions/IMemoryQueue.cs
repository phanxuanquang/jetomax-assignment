namespace ChatApp.Application.Abstractions;

/// <summary>
/// The port onto the in-memory background queue that decouples the hot chat path from the cold
/// summarization path (§6). <c>SendMessage</c>/<c>SendImage</c> enqueue after saving a message;
/// Api's background worker dequeues and asks <see cref="Memory.MemoryService"/> to summarize if the
/// conversation's pending token count has crossed the configured threshold.
/// </summary>
public interface IMemoryQueue
{
    /// <summary>Enqueues <paramref name="conversationId"/> for the background worker to check. Never blocks the caller.</summary>
    Task EnqueueAsync(Guid conversationId, CancellationToken cancellationToken);
}
