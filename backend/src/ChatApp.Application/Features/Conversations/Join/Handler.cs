using ChatApp.Application.Abstractions;
using ChatApp.Application.Common.Results;
using ChatApp.Domain.Entities;
using MediatR;

namespace ChatApp.Application.Features.Conversations.Join;

/// <summary>Handles <see cref="Command"/>.</summary>
public sealed class Handler(IAppDbContext db, ICurrentUser currentUser, IConversationNotifier notifier)
    : IRequestHandler<Command, Result>
{
    /// <summary>
    /// Looks up the conversation by <c>PublicId</c>, rejects frozen/deleted/missing conversations,
    /// and adds the caller as a participant unless already joined. Mirrors the DB's 1↔2 readonly
    /// auto-clear boundary when this join brings membership to exactly 2.
    /// </summary>
    public async Task<Result> Handle(Command request, CancellationToken cancellationToken)
    {
        var currentUserResult = await currentUser.GetCurrentUserAsync(cancellationToken);
        if (!currentUserResult.IsSuccess)
        {
            return Result.Failure(currentUserResult.Error!);
        }

        var currentUserId = currentUserResult.Value!.Id;

        var conversation = await db.FirstOrDefaultAsync(
            db.Conversations.Where(c => c.PublicId == request.PublicId && !c.IsDeleted),
            cancellationToken);

        if (conversation is null)
        {
            return Result.Failure(Error.NotFound("conversation.not_found", "Conversation not found."));
        }

        if (conversation.OwnerId is null)
        {
            return Result.Failure(Error.Conflict("conversation.frozen", "This conversation is frozen and cannot be joined."));
        }

        var participantIds = await db.ToListAsync(
            db.Participants.Where(p => p.ConversationId == conversation.Id).Select(p => p.UserId),
            cancellationToken);

        if (!participantIds.Contains(currentUserId))
        {
            if (participantIds.Count + 1 == 2)
            {
                conversation.IsReadonly = false;
            }

            db.Add(new Participant(conversation.Id, currentUserId));
            await db.SaveChangesAsync(cancellationToken);
            await notifier.NotifyMemberChangedAsync(conversation.Id, currentUserId, MemberChangeAction.Joined, cancellationToken);
        }

        return Result.Success();
    }
}
