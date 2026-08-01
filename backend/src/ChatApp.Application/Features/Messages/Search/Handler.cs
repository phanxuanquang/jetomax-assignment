using ChatApp.Application.Abstractions;
using ChatApp.Application.Common.Results;
using ChatApp.Domain.Entities;
using MediatR;

namespace ChatApp.Application.Features.Messages.Search;

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

        var pattern = $"%{request.Keyword.Trim()}%";

        var matchingTextIds = db.Messages.OfType<TextMessage>()
            .Where(m => m.ConversationId == request.ConversationId && db.ILike(m.Content, pattern))
            .Select(m => m.Id);

        var matchingImageIds = db.Messages.OfType<ImageMessage>()
            .Where(m => m.ConversationId == request.ConversationId && m.Caption != null && db.ILike(m.Caption, pattern))
            .Select(m => m.Id);

        var matchingIds = await db.ToListAsync(matchingTextIds.Concat(matchingImageIds), cancellationToken);

        var messages = await db.ToListAsync(
            db.Messages
                .Where(m => matchingIds.Contains(m.Id))
                .OrderByDescending(m => m.SentAt)
                .Take(request.Limit),
            cancellationToken);

        return Result<IReadOnlyList<MessageDto>>.Success(messages.Select(MessageMapper.ToDto).ToList());
    }
}
