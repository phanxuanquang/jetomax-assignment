namespace ChatApp.Application.Abstractions;

/// <summary>
/// The port that runs collaborative OCR (§7): a first-tap-wins lock followed by a detached
/// transcription pipeline, so triggering OCR never blocks the requesting call.
/// </summary>
public interface IOcrService
{
    /// <summary>
    /// Atomically attempts to move the image message's <c>OcrStatus</c> from <c>NotRequested</c> to
    /// <c>Processing</c>. Returns <c>true</c> only for the single caller that wins the race; every
    /// other concurrent caller (and any caller after the image already left <c>NotRequested</c>,
    /// including the terminal <c>TextNotFound</c> state) gets <c>false</c>. This is the first-tap-wins
    /// lock — implementations must make the read-then-write atomic (e.g. a single conditional UPDATE),
    /// not a read followed by a separate write.
    /// </summary>
    Task<bool> TryStartAsync(Guid imageMessageId, CancellationToken cancellationToken);

    /// <summary>
    /// Runs the full transcription pipeline for an image message already locked into
    /// <c>Processing</c> by <see cref="TryStartAsync"/>: one vision call to Markdown, persisting the
    /// result to <c>OcrContent</c> and flipping status to <c>Finished</c>, posting the AI Agent's
    /// reply message, and notifying participants.
    /// <para>
    /// Callers are expected to invoke this without awaiting it to completion, so a request can
    /// return immediately after acquiring the lock (§7's 202/<c>PROCESSING</c> response) while this
    /// keeps running after that request's scope has ended. Implementations must therefore open
    /// their own DI scope (e.g. via <c>IServiceScopeFactory</c>) and resolve their own
    /// <see cref="IAppDbContext"/>/<see cref="IConversationNotifier"/> instances from it — they must never
    /// reuse the scoped instances that were injected into the caller, since those are disposed once
    /// the caller's scope (the original HTTP request) ends.
    /// </para>
    /// <para>
    /// On failure or timeout while in <c>Processing</c> (decision B-4), implementations must reset
    /// <c>OcrStatus</c> back to <c>NotRequested</c> rather than leaving it stuck — the lock acquired by
    /// <see cref="TryStartAsync"/> is transitional, not permanent, so any participant can retry.
    /// </para>
    /// </summary>
    Task ProcessAsync(Guid imageMessageId, CancellationToken cancellationToken);
}
