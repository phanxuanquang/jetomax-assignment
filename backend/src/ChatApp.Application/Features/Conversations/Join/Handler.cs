using ChatApp.Application.Abstractions;
using ChatApp.Application.Common.Results;
using ChatApp.Domain.Entities;
using MediatR;

namespace ChatApp.Application.Features.Conversations.Join;

/// <summary>Handles <see cref="Command"/>.</summary>
public sealed class Handler(IAppDbContext db, IConversationAccess conversationAccess, IConversationNotifier notifier)
    : IRequestHandler<Command, Result>
{
    /// <summary>Rejects frozen/deleted/missing conversations; adds the caller as a participant unless already joined, clearing <c>IsReadonly</c> if membership reaches exactly 2.</summary>
    public async Task<Result> Handle(Command request, CancellationToken cancellationToken)
    {
        var currentUserId = conversationAccess.UserId;

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
