using ChatApp.Application.Abstractions;
using ChatApp.Application.Common.Results;
using MediatR;

namespace ChatApp.Application.Features.Messages.Get;

/// <summary>Handles <see cref="Query"/>.</summary>
public sealed class Handler(IAppDbContext db, IConversationAccess conversationAccess)
    : IRequestHandler<Query, Result<IReadOnlyList<MessageDto>>>
{
    /// <summary>Returns up to <see cref="Query.Limit"/> messages, newest first, strictly older than <see cref="Query.Before"/> when given.</summary>
    public async Task<Result<IReadOnlyList<MessageDto>>> Handle(Query request, CancellationToken cancellationToken)
    {
        var callerId = conversationAccess.UserId;

        var isParticipant = await db.AnyAsync(
            db.Participants.Where(p => p.ConversationId == request.ConversationId && p.UserId == callerId),
            cancellationToken);

        if (!isParticipant)
        {
            return Result<IReadOnlyList<MessageDto>>.Failure(Error.Forbidden("message.list.not_participant", "The caller is not a participant of this conversation."));
        }

        var query = db.Messages.Where(m => m.ConversationId == request.ConversationId);

        if (request.Before is { } beforeId)
        {
            var before = await db.FirstOrDefaultAsync(db.Messages.Where(m => m.Id == beforeId && m.ConversationId == request.ConversationId), cancellationToken);
            if (before is null)
            {
                return Result<IReadOnlyList<MessageDto>>.Failure(Error.NotFound("message.list.before_not_found", "The 'before' message was not found in this conversation."));
            }

            query = query.Where(m => m.SentAt < before.SentAt);
        }

        query = query.OrderByDescending(m => m.SentAt).Take(request.Limit);

        var messages = await db.ToListAsync(query, cancellationToken);
        return Result<IReadOnlyList<MessageDto>>.Success(messages.Select(MessageMapper.ToDto).ToList());
    }
}
