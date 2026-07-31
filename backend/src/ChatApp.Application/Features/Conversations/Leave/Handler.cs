using ChatApp.Application.Abstractions;
using ChatApp.Application.Common.Results;
using MediatR;

namespace ChatApp.Application.Features.Conversations.Leave;

/// <summary>Handles <see cref="Command"/>.</summary>
public sealed class Handler(IAppDbContext db, IConversationAccess conversationAccess, IConversationNotifier notifier)
    : IRequestHandler<Command, Result>
{
    /// <summary>
    /// Removes the caller's participant row (mirroring the DB's 1↔2 readonly auto-set boundary),
    /// and — only when the caller is the owner — soft-deletes or freezes the conversation per
    /// <see cref="Command.Mode"/>. On delete (decision B-5), every participant is notified in
    /// realtime — via <see cref="IConversationNotifier.NotifyMemberChangedAsync"/> with
    /// <see cref="MemberChangeAction.Left"/>, the same event the API already documents for realtime
    /// membership changes — and the owner's own participant row is removed, same as any other leave;
    /// every other participant's row is retained (soft-delete keeps rows, per the no-hard-delete
    /// rule; only the owner's leave-style removal applies).
    /// </summary>
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
            if (request.Mode is not { } mode)
            {
                return Result.Failure(Error.Validation("conversation.leave.mode_required", "The owner must choose delete or freeze when leaving."));
            }

            if (mode == LeaveMode.Delete)
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
