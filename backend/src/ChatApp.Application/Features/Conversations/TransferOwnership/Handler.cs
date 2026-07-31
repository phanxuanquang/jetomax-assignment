using ChatApp.Application.Abstractions;
using ChatApp.Application.Common.Results;
using MediatR;

namespace ChatApp.Application.Features.Conversations.TransferOwnership;

/// <summary>Handles <see cref="Command"/>.</summary>
public sealed class Handler(IAppDbContext db, IConversationAccess conversationAccess)
    : IRequestHandler<Command, Result>
{
    /// <summary>Owner-only: resolves <see cref="Command.NewOwnerUsername"/> to a user who must already be a participant, then transfers ownership to them.</summary>
    public async Task<Result> Handle(Command request, CancellationToken cancellationToken)
    {
        var guard = await conversationAccess.GetOwnedConversationAsync(request.ConversationId, cancellationToken);
        if (!guard.IsSuccess)
        {
            return Result.Failure(guard.Error!);
        }

        var conversation = guard.Value!;

        var newOwner = await db.FirstOrDefaultAsync(
            db.Users.Where(u => u.Username == request.NewOwnerUsername),
            cancellationToken);

        if (newOwner is null)
        {
            return Result.Failure(Error.NotFound("user.not_found", "The new owner's username does not resolve to an existing user."));
        }

        var newOwnerIsParticipant = await db.AnyAsync(
            db.Participants.Where(p => p.ConversationId == conversation.Id && p.UserId == newOwner.Id),
            cancellationToken);

        if (!newOwnerIsParticipant)
        {
            return Result.Failure(Error.NotFound("conversation.transfer.not_participant", "The new owner must already be a participant of this conversation."));
        }

        conversation.OwnerId = newOwner.Id;
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
