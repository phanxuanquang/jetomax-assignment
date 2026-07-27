using ChatApp.Application.Abstractions;
using ChatApp.Application.Common.Results;
using ChatApp.Domain.Entities;
using MediatR;

namespace ChatApp.Application.Features.Conversations.Join;

/// <summary>Handles <see cref="Command"/>.</summary>
public sealed class Handler(IAppDbContext db, ICurrentUser currentUser, IChatNotifier notifier)
    : IRequestHandler<Command, Result<ConversationDto>>
{
    /// <summary>
    /// Looks up the conversation by <c>PublicId</c>, rejects frozen/deleted/missing conversations,
    /// and adds the caller as a participant unless already joined. Mirrors the DB's 1↔2 readonly
    /// auto-clear boundary when this join brings membership to exactly 2. Loads the participant list
    /// once and reuses it for both the membership check and the returned DTO, rather than querying
    /// it multiple times.
    /// </summary>
    public async Task<Result<ConversationDto>> Handle(Command request, CancellationToken cancellationToken)
    {
        if (currentUser.UserId is not { } callerId)
        {
            return Result<ConversationDto>.Failure(Error.Forbidden("conversation.join.no_identity", "The caller has no user identity."));
        }

        var conversation = await db.FirstOrDefaultAsync(
            db.Conversations.Where(c => c.PublicId == request.PublicId && !c.IsDeleted),
            cancellationToken);

        if (conversation is null)
        {
            return Result<ConversationDto>.Failure(Error.NotFound("conversation.not_found", "No joinable conversation has this public id."));
        }

        if (conversation.OwnerId is null)
        {
            return Result<ConversationDto>.Failure(Error.Conflict("conversation.frozen", "This conversation is frozen and cannot be joined."));
        }

        var participantIds = await db.ToListAsync(
            db.Participants.Where(p => p.ConversationId == conversation.Id).Select(p => p.UserId),
            cancellationToken);

        if (!participantIds.Contains(callerId))
        {
            if (participantIds.Count + 1 == 2)
            {
                conversation.IsReadonly = false;
            }

            db.Add(new Participant { ConversationId = conversation.Id, UserId = callerId });
            await db.SaveChangesAsync(cancellationToken);
            await notifier.NotifyMemberChangedAsync(conversation.Id, callerId, MemberChangeAction.Joined, cancellationToken);

            participantIds.Add(callerId);
        }

        var dto = new ConversationDto(
            conversation.Id,
            conversation.PublicId,
            conversation.DisplayName,
            conversation.OwnerId,
            conversation.IsReadonly,
            conversation.CreatedTime,
            conversation.LastMessageTime,
            participantIds);

        return Result<ConversationDto>.Success(dto);
    }
}
