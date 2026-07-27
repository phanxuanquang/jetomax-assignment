using ChatApp.Application.Abstractions;
using ChatApp.Application.Common.Results;
using MediatR;

namespace ChatApp.Application.Features.Conversations.RemoveParticipants;

/// <summary>Handles <see cref="Command"/>.</summary>
public sealed class Handler(IAppDbContext db, ICurrentUser currentUser, IChatNotifier notifier)
    : IRequestHandler<Command, Result>
{
    /// <summary>
    /// Owner-only: removes every id in <see cref="Command.UserIds"/> from the conversation. Rejects
    /// the whole batch if it includes the owner (use <c>TransferOwnership</c> or
    /// <c>LeaveConversation</c> instead) or any id that isn't currently a participant. Mirrors the
    /// DB's 1↔2 readonly auto-set boundary when this batch drops membership to 1 or fewer.
    /// </summary>
    public async Task<Result> Handle(Command request, CancellationToken cancellationToken)
    {
        var guard = await currentUser.GetOwnedConversationAsync(request.ConversationId, cancellationToken);
        if (!guard.IsSuccess)
        {
            return Result.Failure(guard.Error!);
        }

        var conversation = guard.Value!;
        var targetIds = request.UserIds.Distinct().ToList();

        if (targetIds.Contains(conversation.OwnerId!.Value))
        {
            return Result.Failure(Error.Conflict("conversation.remove.owner", "The owner cannot be removed this way; transfer ownership or leave instead."));
        }

        var participants = await db.ToListAsync(
            db.Participants.Where(p => p.ConversationId == conversation.Id),
            cancellationToken);

        var toRemove = participants.Where(p => targetIds.Contains(p.UserId)).ToList();
        if (toRemove.Count != targetIds.Count)
        {
            return Result.Failure(Error.NotFound("conversation.remove.not_participant", "One or more users are not participants of this conversation."));
        }

        var remainingCount = participants.Count - toRemove.Count;
        if (remainingCount <= 1)
        {
            conversation.IsReadonly = true;
        }

        foreach (var participant in toRemove)
        {
            db.Remove(participant);
        }

        await db.SaveChangesAsync(cancellationToken);

        foreach (var userId in targetIds)
        {
            await notifier.NotifyMemberChangedAsync(conversation.Id, userId, MemberChangeAction.Left, cancellationToken);
        }

        return Result.Success();
    }
}
