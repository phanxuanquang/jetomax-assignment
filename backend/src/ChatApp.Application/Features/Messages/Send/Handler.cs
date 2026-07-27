using ChatApp.Application.Abstractions;
using ChatApp.Application.Common.Results;
using ChatApp.Domain.Entities;
using MediatR;

namespace ChatApp.Application.Features.Messages.Send;

/// <summary>Handles <see cref="Command"/>.</summary>
public sealed class Handler(
    IAppDbContext db,
    ICurrentUser currentUser,
    ITokenCounter tokenCounter,
    IMemoryQueue memoryQueue,
    IChatNotifier notifier) : IRequestHandler<Command, Result<MessageDto>>
{
    /// <summary>
    /// Persists the text message, accrues its token count onto the conversation's pending memory
    /// counter, enqueues the conversation for the background summarizer, and broadcasts it.
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

        var message = new TextMessage
        {
            ConversationId = conversation.Id,
            UserId = callerId,
            Content = request.Content
        };
        db.Add(message);

        await tokenCounter.AddPendingTokensAsync(conversation.Id, request.Content, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);

        await memoryQueue.EnqueueAsync(conversation.Id, cancellationToken);
        await notifier.NotifyNewMessageAsync(conversation.Id, message, cancellationToken);

        return Result<MessageDto>.Success(MessageMapper.ToDto(message));
    }
}
