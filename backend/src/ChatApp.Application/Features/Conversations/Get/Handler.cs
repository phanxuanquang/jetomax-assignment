using ChatApp.Application.Abstractions;
using ChatApp.Application.Common.Results;
using ChatApp.Domain.Entities;
using MediatR;

namespace ChatApp.Application.Features.Conversations.Get;

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
        if (currentUser.UserId is not { } callerId)
        {
            return Result<IReadOnlyList<ConversationDto>>.Failure(Error.Forbidden("conversation.list.no_identity", "The caller has no user identity."));
        }

        IQueryable<Conversation> query = db.Conversations
            .Where(c => !c.IsDeleted)
            .Where(c => db.Participants.Any(p => p.ConversationId == c.Id && p.UserId == callerId));

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var term = request.SearchTerm.ToLowerInvariant().Trim();
            query = query.Where(c => c.DisplayName.ToLower().Contains(term));
        }

        query = query.OrderByDescending(c => c.LastMessageTime);

        var conversations = await db.ToListAsync(query, cancellationToken);
        var conversationIds = conversations.Select(c => c.Id).ToList();

        var participants = await db.ToListAsync(
            db.Participants.Where(p => conversationIds.Contains(p.ConversationId)),
            cancellationToken);

        var participantIdsByConversation = participants
            .GroupBy(p => p.ConversationId)
            .ToDictionary(g => g.Key, g => (IReadOnlyCollection<Guid>)[.. g.Select(p => p.UserId)]);

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
