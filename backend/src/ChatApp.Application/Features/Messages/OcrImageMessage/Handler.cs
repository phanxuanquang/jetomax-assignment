using ChatApp.Application.Abstractions;
using ChatApp.Application.Common.Results;
using ChatApp.Domain.Entities;
using MediatR;

namespace ChatApp.Application.Features.Messages.OcrImageMessage;

/// <summary>Handles <see cref="Command"/>.</summary>
public sealed class Handler(
    IAppDbContext db,
    ICurrentUser currentUser,
    IChatNotifier notifier,
    IOcrService ocrService) : IRequestHandler<Command, Result>
{
    /// <summary>
    /// Confirms the caller is a participant, attempts the first-tap-wins lock, and — only for the
    /// caller that wins it — notifies <c>OcrStarted</c> and fires the transcription pipeline without
    /// awaiting it, so this call returns immediately regardless of how long transcription takes.
    /// </summary>
    public async Task<Result> Handle(Command request, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } callerId)
        {
            return Result.Failure(Error.Forbidden("ocr.trigger.no_identity", "The caller has no user identity."));
        }

        var image = await db.FirstOrDefaultAsync(
            db.Messages.OfType<ImageMessage>().Where(m => m.Id == request.MessageId),
            cancellationToken);

        if (image is null)
        {
            return Result.Failure(Error.NotFound("ocr.trigger.not_found", "Image message not found."));
        }

        var isParticipant = await db.AnyAsync(
            db.Participants.Where(p => p.ConversationId == image.ConversationId && p.UserId == callerId),
            cancellationToken);

        if (!isParticipant)
        {
            return Result.Failure(Error.Forbidden("ocr.trigger.not_participant", "The caller is not a participant of this conversation."));
        }

        var wonLock = await ocrService.TryStartAsync(image.Id, cancellationToken);
        if (!wonLock)
        {
            // Someone already triggered it (or it's already finished / has no text) — idempotent no-op.
            return Result.Success();
        }

        await notifier.NotifyOcrStartedAsync(image.Id, cancellationToken);

        // Detached on purpose (§7): must not block this call, and must outlive this request's scope,
        // so it uses CancellationToken.None rather than the request's own token.
        _ = ocrService.ProcessAsync(image.Id, CancellationToken.None);

        return Result.Success();
    }
}
