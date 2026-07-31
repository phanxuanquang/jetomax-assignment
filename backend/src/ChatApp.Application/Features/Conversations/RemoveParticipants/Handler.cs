using ChatApp.Application.Abstractions;
using ChatApp.Application.Common.Results;
using MediatR;
using System.Collections.Frozen;

namespace ChatApp.Application.Features.Conversations.RemoveParticipants;

/// <summary>Handles <see cref="Command"/>.</summary>
public sealed class Handler(IAppDbContext db, IConversationAccess conversationAccess, IConversationNotifier notifier)
    : IRequestHandler<Command, Result>
{
    /// <summary>Owner-only: fails the whole batch if a username is unresolved, includes the owner, or isn't a current participant; sets <c>IsReadonly</c> if this drops membership to 1 or fewer.</summary>
    public async Task<Result> Handle(Command request, CancellationToken cancellationToken)
    {
        var getOwnedConversationResult = await conversationAccess.GetOwnedConversationAsync(request.ConversationId, cancellationToken);
        if (!getOwnedConversationResult.IsSuccess)
        {
            return Result.Failure(getOwnedConversationResult.Error!);
        }

        var conversation = getOwnedConversationResult.Value!;

        var distinctUsernames = request.Usernames.Distinct(StringComparer.Ordinal).ToList();
        var resolvedUsers = await db.ToListAsync(
            db.Users.Where(u => distinctUsernames.Contains(u.Username)),
            cancellationToken);

        if (resolvedUsers.Count != distinctUsernames.Count)
        {
            return Result.Failure(Error.NotFound("user.not_found", "One or more usernames do not resolve to an existing user."));
        }

        var toBeRemovedUserIds = resolvedUsers.Select(u => u.Id).ToFrozenSet();

        if (toBeRemovedUserIds.Contains(conversation.OwnerId!.Value))
        {
            return Result.Failure(Error.Conflict("conversation.remove.owner", "The owner cannot be removed this way; transfer ownership or leave instead."));
        }

        var participants = await db.ToListAsync(
            db.Participants.Where(p => p.ConversationId == conversation.Id),
            cancellationToken);

        var toBeRemovedParticipants = participants.Where(p => toBeRemovedUserIds.Contains(p.UserId)).ToList();
        if (toBeRemovedParticipants.Count != toBeRemovedUserIds.Count)
        {
            return Result.Failure(Error.NotFound("conversation.remove.not_participant", "One or more users are not participants of this conversation."));
        }

        var remainingCount = participants.Count - toBeRemovedParticipants.Count;
        if (remainingCount <= 1)
        {
            conversation.IsReadonly = true;
        }

        db.RemoveRange(toBeRemovedParticipants);
        await db.SaveChangesAsync(cancellationToken);

        foreach (var userId in toBeRemovedUserIds)
        {
            await notifier.NotifyMemberChangedAsync(conversation.Id, userId, MemberChangeAction.Left, cancellationToken);
        }

        return Result.Success();
    }
}
