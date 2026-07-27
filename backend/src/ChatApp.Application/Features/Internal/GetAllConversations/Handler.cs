using ChatApp.Application.Abstractions;
using ChatApp.Application.Common.Results;
using MediatR;

namespace ChatApp.Application.Features.Internal.GetAllConversations;

/// <summary>Handles <see cref="Query"/>.</summary>
public sealed class Handler(IAppDbContext db) : IRequestHandler<Query, Result<IReadOnlyList<ConversationMetaDto>>>
{
    /// <summary>Returns every conversation that is not soft-deleted, regardless of membership.</summary>
    public async Task<Result<IReadOnlyList<ConversationMetaDto>>> Handle(Query request, CancellationToken cancellationToken)
    {
        var threads = await db.ToListAsync(
            db.Conversations
                .Where(c => !c.IsDeleted)
                .OrderByDescending(c => c.CreatedTime)
                .Select(c => new ConversationMetaDto(c.Id, c.DisplayName)),
            cancellationToken);

        return Result<IReadOnlyList<ConversationMetaDto>>.Success(threads);
    }
}
