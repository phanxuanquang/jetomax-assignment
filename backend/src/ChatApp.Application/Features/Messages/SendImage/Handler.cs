using ChatApp.Application.Abstractions;
using ChatApp.Application.Common.Results;
using ChatApp.Application.Memory;
using ChatApp.Domain.Entities;
using MediatR;

namespace ChatApp.Application.Features.Messages.SendImage;

/// <summary>Persists and broadcasts only; the conversation memory update runs detached, in its own DI scope, fired by the Api layer after this handler returns.</summary>
public sealed class Handler(
    IAppDbContext db,
    IConversationAccess conversationAccess,
    IStorageClient storageClient,
    IGenerativeAiService generativeAiService,
    IConversationNotifier notifier) : IRequestHandler<Command, Result<MessageDto>>
{
    private static readonly TimeSpan VisionTimeout = TimeSpan.FromSeconds(8);

    /// <summary>The caption is shown directly to chat participants, so it reuses the same human-facing style as conversation summaries.</summary>
    private const string AnalysisSystemInstruction = ConversationMemoryService.HumanFacingSystemInstruction;

    private const string AnalysisPrompt = "Look at this image and write a short, one-sentence caption describing what it shows.";

    /// <summary>Runs the on-send vision pass synchronously with a short timeout, falling back to no caption on failure or timeout so the image still sends; then persists and broadcasts it.</summary>
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
