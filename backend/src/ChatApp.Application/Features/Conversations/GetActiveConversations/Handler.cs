using ChatApp.Application.Abstractions;
using ChatApp.Application.Common.Results;
using ChatApp.Domain.Entities;
using MediatR;
using System.Collections.Frozen;

namespace ChatApp.Application.Features.Conversations.GetActiveConversations;

/// <summary>Handles <see cref="Query"/>.</summary>
public sealed class Handler(IAppDbContext db, ICurrentUser currentUser)
    : IRequestHandler<Query, Result<IReadOnlyList<ConversationDto>>>
{
    /// <summary>
    /// Returns the caller's non-deleted conversations, most recently active first, optionally
    /// filtered by <c>DisplayName</c>. Fetches every returned conversation's participants in a
    /// single batched query rather than one query per conversation.
    /// </summary>
    public async Task<Result<IReadOnlyList<ConversationDto>>> Handle(Query request, CancellationToken cancellationToken)
    {
        var currentUserResult = await currentUser.GetCurrentUserAsync(cancellationToken);
        if (!currentUserResult.IsSuccess)
        {
            return Result<IReadOnlyList<ConversationDto>>.Failure(currentUserResult.Error!);
        }

        var currentUserId = currentUserResult.Value!.Id;

        IQueryable<Conversation> query = db.Conversations
            .Where(c => !c.IsDeleted)
            .Where(c => db.Participants.Any(p => p.ConversationId == c.Id && p.UserId == currentUserId));

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var pattern = $"%{request.SearchTerm.Trim()}%";
            query = query.Where(c => db.ILike(c.DisplayName, pattern));
        }

        query = query.OrderByDescending(c => c.LastMessageTime);

        var conversations = await db.ToListAsync(query, cancellationToken);
        var conversationIds = conversations.Select(c => c.Id).ToFrozenSet();

        var participants = await db.ToListAsync(
            db.Participants.Where(p => conversationIds.Contains(p.ConversationId)),
            cancellationToken);

        var participantIdsByConversation = participants
            .GroupBy(p => p.ConversationId)
            .ToFrozenDictionary(g => g.Key, g => (IReadOnlyCollection<Guid>)[.. g.Select(p => p.UserId)]);

        var dtos = conversations
            .Select(c => new ConversationDto(
                c.Id,
                c.PublicId,
                c.DisplayName,
                c.OwnerId,
                c.IsReadonly,
                c.CreatedTime,
                c.LastMessageTime,
                participantIdsByConversation.GetValueOrDefault(c.Id, [])))
            .ToList();

        return Result<IReadOnlyList<ConversationDto>>.Success(dtos);
    }
}
