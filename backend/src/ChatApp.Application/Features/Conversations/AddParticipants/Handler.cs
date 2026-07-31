using ChatApp.Application.Abstractions;
using ChatApp.Application.Common.Results;
using ChatApp.Domain.Entities;
using MediatR;

namespace ChatApp.Application.Features.Conversations.AddParticipants;

/// <summary>Handles <see cref="Command"/>.</summary>
public sealed class Handler(IAppDbContext db, IConversationAccess conversationAccess, IConversationNotifier notifier)
    : IRequestHandler<Command, Result>
{
    /// <summary>
    /// Owner-only: resolves every username in <see cref="Command.Usernames"/> to an existing user —
    /// if any doesn't resolve, the whole batch fails and nothing is added — then adds whichever
    /// resolved ids aren't already participants. Mirrors the DB's 1↔2 readonly auto-clear boundary
    /// when this batch brings membership from below 2 up to 2 or more.
    /// </summary>
    public async Task<Result> Handle(Command request, CancellationToken cancellationToken)
    {
        var guard = await conversationAccess.GetOwnedConversationAsync(request.ConversationId, cancellationToken);
        if (!guard.IsSuccess)
        {
            return Result.Failure(guard.Error!);
        }

        var conversation = guard.Value!;

        var distinctUsernames = request.Usernames.Distinct(StringComparer.Ordinal).ToList();
        var resolvedUsers = await db.ToListAsync(
            db.Users.Where(u => distinctUsernames.Contains(u.Username)),
            cancellationToken);

        if (resolvedUsers.Count != distinctUsernames.Count)
        {
            return Result.Failure(Error.NotFound("user.not_found", "One or more users to add do not exist."));
        }

        var existingParticipantIds = await db.ToListAsync(
            db.Participants.Where(p => p.ConversationId == conversation.Id).Select(p => p.UserId),
            cancellationToken);

        var toAdd = resolvedUsers.Select(u => u.Id).Except(existingParticipantIds).ToList();
        if (toAdd.Count == 0)
        {
            return Result.Success();
        }

        var existingCount = existingParticipantIds.Count;
        if (existingCount < 2 && existingCount + toAdd.Count >= 2)
        {
            conversation.IsReadonly = false;
        }

        db.AddRange(toAdd.Select(id => new Participant(conversation.Id, id)));
        await db.SaveChangesAsync(cancellationToken);

        foreach (var userId in toAdd)
        {
            await notifier.NotifyMemberChangedAsync(conversation.Id, userId, MemberChangeAction.Joined, cancellationToken);
        }

        return Result.Success();
    }
}
