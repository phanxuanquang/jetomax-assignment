using ChatApp.Application.Abstractions;
using ChatApp.Application.Common.Results;
using ChatApp.Domain.Entities;
using MediatR;

namespace ChatApp.Application.Features.Messages.Send;

/// <summary>Persists and broadcasts only; the conversation memory update runs detached, in its own DI scope, fired by the Api layer after this handler returns.</summary>
public sealed class Handler(
    IAppDbContext db,
    IConversationAccess conversationAccess,
    IConversationNotifier notifier) : IRequestHandler<Command, Result<MessageDto>>
{
    /// <summary>Persists the text message and broadcasts it.</summary>
    public async Task<Result<MessageDto>> Handle(Command request, CancellationToken cancellationToken)
    {
        var callerId = conversationAccess.UserId;

        var guard = await conversationAccess.EnsureCanSendAsync(request.ConversationId, cancellationToken);
        if (!guard.IsSuccess)
        {
            return Result<MessageDto>.Failure(guard.Error!);
        }

        var conversation = guard.Value!;

        var message = new TextMessage
        {
            ConversationId = conversation.Id,
            UserId = callerId,
            Content = request.Content
        };
        db.Add(message);
        await db.SaveChangesAsync(cancellationToken);

        await notifier.NotifyNewMessageAsync(conversation.Id, message, cancellationToken);

        return Result<MessageDto>.Success(MessageMapper.ToDto(message));
    }
}
