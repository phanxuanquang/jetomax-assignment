using ChatApp.Application.Abstractions;
using ChatApp.Application.Common.Results;
using ChatApp.Domain.Entities;
using MediatR;

namespace ChatApp.Application.Features.Conversations.AddParticipants;

/// <summary>Handles <see cref="Command"/>.</summary>
public sealed class Handler(IAppDbContext db, ICurrentUser currentUser, IChatNotifier notifier)
    : IRequestHandler<Command, Result>
{
    /// <summary>
    /// Owner-only: adds every id in <see cref="Command.UserIds"/> that isn't already a participant.
    /// If any remaining id doesn't resolve to an existing, non-Agent user, the whole batch fails and
    /// nothing is added. Mirrors the DB's 1↔2 readonly auto-clear boundary when this batch brings
    /// membership from below 2 up to 2 or more.
    /// </summary>
    public async Task<Result> Handle(Command request, CancellationToken cancellationToken)
    {
        var guard = await currentUser.GetOwnedConversationAsync(request.ConversationId, cancellationToken);
        if (!guard.IsSuccess)
        {
            return Result.Failure(guard.Error!);
        }

        var conversation = guard.Value!;

        var existingParticipantIds = await db.ToListAsync(
            db.Participants.Where(p => p.ConversationId == conversation.Id).Select(p => p.UserId),
            cancellationToken);

        var toAdd = request.UserIds.Distinct().Except(existingParticipantIds).ToList();
        if (toAdd.Count == 0)
        {
            return Result.Success();
        }

        var validUserCount = await db.CountAsync(
            db.Users.Where(u => toAdd.Contains(u.Id) && !u.IsAgent),
            cancellationToken);

        if (validUserCount != toAdd.Count)
        {
            return Result.Failure(Error.NotFound("user.not_found", "One or more users to add do not exist."));
        }

        var existingCount = existingParticipantIds.Count;
        if (existingCount < 2 && existingCount + toAdd.Count >= 2)
        {
            conversation.IsReadonly = false;
        }

        db.AddRange(toAdd.Select(id => new Participant { ConversationId = conversation.Id, UserId = id }));
        await db.SaveChangesAsync(cancellationToken);

        foreach (var userId in toAdd)
        {
            await notifier.NotifyMemberChangedAsync(conversation.Id, userId, MemberChangeAction.Joined, cancellationToken);
        }

        return Result.Success();
    }
}
