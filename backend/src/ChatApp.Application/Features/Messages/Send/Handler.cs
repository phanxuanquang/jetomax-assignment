using ChatApp.Application.Abstractions;
using ChatApp.Application.Common.Results;
using ChatApp.Domain.Entities;
using MediatR;

namespace ChatApp.Application.Features.Messages.Send;

/// <summary>
/// Handles <see cref="Command"/>. Persists and broadcasts only — never touches conversation memory
/// (§6, A-1/B-2): the memory update runs detached, in its own DI scope, fired by the Api layer after
/// this handler returns, via <see cref="Memory.ConversationMemoryService.RecordMessageAndProcessAsync"/>
/// with this message's <see cref="Domain.Entities.TextMessage.Content"/> as the text to count.
/// </summary>
public sealed class Handler(
    IAppDbContext db,
    ICurrentUser currentUser,
    IConversationNotifier notifier) : IRequestHandler<Command, Result<MessageDto>>
{
    /// <summary>Persists the text message and broadcasts it.</summary>
    public async Task<Result<MessageDto>> Handle(Command request, CancellationToken cancellationToken)
    {
        var guard = await currentUser.EnsureCanSendAsync(request.ConversationId, cancellationToken);
        if (!guard.IsSuccess)
        {
            return Result<MessageDto>.Failure(guard.Error!);
        }

        var conversation = guard.Value!;
        var callerId = (await currentUser.GetCurrentUserAsync(cancellationToken)).Value!.Id;

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
