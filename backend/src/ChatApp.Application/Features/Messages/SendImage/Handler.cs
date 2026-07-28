using ChatApp.Application.Abstractions;
using ChatApp.Application.Common.Results;
using ChatApp.Domain.Entities;
using ChatApp.Domain.Enums;
using MediatR;

namespace ChatApp.Application.Features.Messages.SendImage;

/// <summary>
/// Handles <see cref="Command"/>. Persists and broadcasts only — never touches conversation memory
/// (§6, A-1/B-2): the memory update runs detached, in its own DI scope, fired by the Api layer after
/// this handler returns, via <see cref="Memory.ConversationMemoryService.RecordMessageAndProcessAsync"/>
/// with this message's <see cref="ImageMessage.Caption"/> as the text to count.
/// </summary>
public sealed class Handler(
    IAppDbContext db,
    IConversationAccess conversationAccess,
    IStorageClient storageClient,
    IGenerativeAiService generativeAiService,
    IConversationNotifier notifier) : IRequestHandler<Command, Result<MessageDto>>
{
    private static readonly TimeSpan VisionTimeout = TimeSpan.FromSeconds(8);

    private const string AnalysisPrompt = """
        Look at this image and respond with two things: a short, one-sentence caption describing
        what it shows, and whether it contains any readable text (signage, screenshots, documents,
        handwriting, UI, etc.) that a user might want to extract verbatim.
        """;

    /// <summary>
    /// Runs the single on-send vision pass synchronously (caption + text-detection) with a short
    /// timeout; on failure or timeout it falls back to no caption and <see cref="OcrStatus.NotRequested"/>
    /// (assume text may be present and let a participant retry "Extract text" later) so the image still
    /// sends (F-5's edge case: AI failure never blocks the message). Then persists the image message and broadcasts it.
    /// </summary>
    public async Task<Result<MessageDto>> Handle(Command request, CancellationToken cancellationToken)
    {
        if (conversationAccess.UserId is not { } callerId)
        {
            return Result<MessageDto>.Failure(Error.Unexpected("caller.identity_required", "This action requires a signed-in user."));
        }

        var guard = await conversationAccess.EnsureCanSendAsync(request.ConversationId, cancellationToken);
        if (!guard.IsSuccess)
        {
            return Result<MessageDto>.Failure(guard.Error!);
        }

        var conversation = guard.Value!;

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
        await db.SaveChangesAsync(cancellationToken);

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
            var analysis = await generativeAiService.GenerateContentFromImageAsync<ImageAnalysis>(AnalysisPrompt, bytes, cancellationToken: timeoutCts.Token);
            return (analysis.Caption, analysis.ContainsText ? OcrStatus.NotRequested : OcrStatus.TextNotFound);
        }
        catch (Exception) when (!cancellationToken.IsCancellationRequested)
        {
            return (null, OcrStatus.NotRequested);
        }
    }
}
