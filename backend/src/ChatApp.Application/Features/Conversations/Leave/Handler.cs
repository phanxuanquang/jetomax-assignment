using ChatApp.Application.Abstractions;
using ChatApp.Application.Common.Results;
using MediatR;

namespace ChatApp.Application.Features.Conversations.Leave;

/// <summary>Handles <see cref="Command"/>.</summary>
public sealed class Handler(IAppDbContext db, IConversationAccess conversationAccess, IConversationNotifier notifier)
    : IRequestHandler<Command, Result>
{
    /// <summary>Removes the caller's participant row, setting <c>IsReadonly</c> if membership drops to 1 or fewer; an owner additionally soft-deletes or freezes per <see cref="Command.Mode"/>. On delete, every participant's row is retained (never hard-deleted) and all are notified as <see cref="MemberChangeAction.Left"/>.</summary>
    public async Task<Result> Handle(Command request, CancellationToken cancellationToken)
    {
        var currentUserId = conversationAccess.UserId;

        var conversation = await db.FirstOrDefaultAsync(
            db.Conversations.Where(c => c.Id == request.ConversationId && !c.IsDeleted),
            cancellationToken);

        if (conversation is null)
        {
            return Result.Failure(Error.NotFound("conversation.not_found", "Conversation not found."));
        }

        var participant = await db.FirstOrDefaultAsync(
            db.Participants.Where(p => p.ConversationId == conversation.Id && p.UserId == currentUserId),
            cancellationToken);

        if (participant is null)
        {
            return Result.Failure(Error.Forbidden("conversation.leave.not_participant", "You are not a participant of this conversation."));
        }

        var isOwner = conversation.OwnerId == currentUserId;

        if (isOwner)
        {
            if (request.Mode == LeaveMode.Delete)
            {
                var allParticipantIds = await db.ToListAsync(
                    db.Participants.Where(p => p.ConversationId == conversation.Id).Select(p => p.UserId),
                    cancellationToken);

                conversation.IsDeleted = true;
                db.Remove(participant);
                await db.SaveChangesAsync(cancellationToken);

                foreach (var participantId in allParticipantIds)
                {
                    await notifier.NotifyMemberChangedAsync(conversation.Id, participantId, MemberChangeAction.Left, cancellationToken);
                }

                return Result.Success();
            }

            conversation.OwnerId = null;
        }

        var remainingCount = await db.CountAsync(
            db.Participants.Where(p => p.ConversationId == conversation.Id && p.UserId != currentUserId),
            cancellationToken);
        if (remainingCount <= 1)
        {
            conversation.IsReadonly = true;
        }

        db.Remove(participant);
        await db.SaveChangesAsync(cancellationToken);
        await notifier.NotifyMemberChangedAsync(conversation.Id, currentUserId, MemberChangeAction.Left, cancellationToken);
        return Result.Success();
    }
}
