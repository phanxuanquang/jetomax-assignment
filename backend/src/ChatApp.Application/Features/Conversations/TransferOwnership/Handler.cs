using ChatApp.Application.Abstractions;
using ChatApp.Application.Common.Results;
using MediatR;

namespace ChatApp.Application.Features.Conversations.TransferOwnership;

/// <summary>Handles <see cref="Command"/>.</summary>
public sealed class Handler(IAppDbContext db, ICurrentUser currentUser)
    : IRequestHandler<Command, Result>
{
    /// <summary>Owner-only: transfers ownership to <see cref="Command.NewOwnerUserId"/>, who must already be a participant.</summary>
    public async Task<Result> Handle(Command request, CancellationToken cancellationToken)
    {
        var guard = await currentUser.GetOwnedConversationAsync(request.ConversationId, cancellationToken);
        if (!guard.IsSuccess)
        {
            return Result.Failure(guard.Error!);
        }

        var conversation = guard.Value!;

        var newOwnerIsParticipant = await db.AnyAsync(
            db.Participants.Where(p => p.ConversationId == conversation.Id && p.UserId == request.NewOwnerUserId),
            cancellationToken);

        if (!newOwnerIsParticipant)
        {
            return Result.Failure(Error.NotFound("conversation.transfer.not_participant", "The new owner must already be a participant of this conversation."));
        }

        conversation.OwnerId = request.NewOwnerUserId;
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
