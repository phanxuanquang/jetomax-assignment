using ChatApp.Application.Abstractions;
using ChatApp.Application.Common.Results;
using ChatApp.Application.Memory;
using ChatApp.Domain.Entities;
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

    /// <summary>The caption is shown directly to chat participants (F-5), so this reuses the same human-facing style as conversation summaries.</summary>
    private const string AnalysisSystemInstruction = ConversationMemoryService.HumanFacingSystemInstruction;

    private const string AnalysisPrompt = "Look at this image and write a short, one-sentence caption describing what it shows.";

    /// <summary>
    /// Runs the single on-send vision pass synchronously (caption) with a short timeout; on failure
    /// or timeout it falls back to no caption so the image still sends (F-5's edge case: AI failure
    /// never blocks the message). Then persists the image message and broadcasts it.
    /// </summary>
    public async Task<Result<MessageDto>> Handle(Command request, CancellationToken cancellationToken)
    {
        var callerId = conversationAccess.UserId;

        var guard = await conversationAccess.EnsureCanSendAsync(request.ConversationId, cancellationToken);
        if (!guard.IsSuccess)
        {
            return Result<MessageDto>.Failure(guard.Error!);
        }

        var conversation = guard.Value!;

        var caption = await AnalyzeImageAsync(request.ImageUrl, cancellationToken);

        var message = new ImageMessage
        {
            ConversationId = conversation.Id,
            UserId = callerId,
            ImageUrl = request.ImageUrl,
            Caption = caption
        };
        db.Add(message);
        await db.SaveChangesAsync(cancellationToken);

        await notifier.NotifyNewMessageAsync(conversation.Id, message, cancellationToken);

        return Result<MessageDto>.Success(MessageMapper.ToDto(message));
    }

    private async Task<string?> AnalyzeImageAsync(string imageUrl, CancellationToken cancellationToken)
    {
        try
        {
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(VisionTimeout);

            var bytes = await storageClient.DownloadAsync(imageUrl, timeoutCts.Token);
            return await generativeAiService.GenerateContentFromImageAsync<string>(AnalysisPrompt, bytes, AnalysisSystemInstruction, cancellationToken: timeoutCts.Token);
        }
        catch (Exception) when (!cancellationToken.IsCancellationRequested)
        {
            return null;
        }
    }
}
