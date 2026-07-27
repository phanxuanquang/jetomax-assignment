using ChatApp.Application.Abstractions;
using ChatApp.Application.Common.Results;
using ChatApp.Domain.Entities;
using ChatApp.Domain.Enums;
using MediatR;

namespace ChatApp.Application.Features.Messages.SendImage;

/// <summary>Handles <see cref="Command"/>.</summary>
public sealed class Handler(
    IAppDbContext db,
    ICurrentUser currentUser,
    IStorageClient storageClient,
    IVisionService visionService,
    ITokenCounter tokenCounter,
    IMemoryQueue memoryQueue,
    IChatNotifier notifier) : IRequestHandler<Command, Result<MessageDto>>
{
    private static readonly TimeSpan VisionTimeout = TimeSpan.FromSeconds(8);

    /// <summary>
    /// Runs the single on-send vision pass synchronously (caption + text-detection) with a short
    /// timeout; on failure or timeout it falls back to no caption and <see cref="OcrStatus.TextNotFound"/>
    /// so the image still sends (F-5's edge case: AI failure never blocks the message). Then persists
    /// the image message, accrues its caption's token count, enqueues for summarization, and broadcasts it.
    /// </summary>
    public async Task<Result<MessageDto>> Handle(Command request, CancellationToken cancellationToken)
    {
        var guard = await currentUser.EnsureCanSendAsync(request.ConversationId, cancellationToken);
        if (!guard.IsSuccess)
        {
            return Result<MessageDto>.Failure(guard.Error!);
        }

        var conversation = guard.Value!;
        var callerId = currentUser.UserId!.Value;

        var (caption, ocrStatus) = await AnalyzeImageAsync(request.ImageUrl, cancellationToken);

        var message = new ImageMessage
        {
            ConversationId = conversation.Id,
            UserId = callerId,
            ImageUrl = request.ImageUrl,
            Caption = caption,
            OcrStatus = ocrStatus
        };
        db.Add(message);

        await tokenCounter.AddPendingTokensAsync(conversation.Id, caption ?? string.Empty, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        await memoryQueue.EnqueueAsync(conversation.Id, cancellationToken);
        await notifier.NotifyNewMessageAsync(conversation.Id, message, cancellationToken);

        return Result<MessageDto>.Success(MessageMapper.ToDto(message));
    }

    private async Task<(string? Caption, OcrStatus Status)> AnalyzeImageAsync(string imageUrl, CancellationToken cancellationToken)
    {
        try
        {
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(VisionTimeout);

            var bytes = await storageClient.DownloadAsync(imageUrl, timeoutCts.Token);
            var analysis = await visionService.AnalyzeAsync(bytes, timeoutCts.Token);
            return (analysis.Caption, analysis.ContainsText ? OcrStatus.NotRequested : OcrStatus.TextNotFound);
        }
        catch (Exception) when (!cancellationToken.IsCancellationRequested)
        {
            return (null, OcrStatus.TextNotFound);
        }
    }
}
