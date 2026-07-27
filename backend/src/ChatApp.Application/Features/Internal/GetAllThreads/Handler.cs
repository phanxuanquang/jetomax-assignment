using ChatApp.Application.Abstractions;
using ChatApp.Application.Common.Results;
using MediatR;

namespace ChatApp.Application.Features.Internal.GetAllThreads;

/// <summary>Handles <see cref="Query"/>.</summary>
public sealed class Handler(IAppDbContext db) : IRequestHandler<Query, Result<IReadOnlyList<ThreadDto>>>
{
    /// <summary>Returns every conversation that is not soft-deleted, regardless of membership.</summary>
    public async Task<Result<IReadOnlyList<ThreadDto>>> Handle(Query request, CancellationToken cancellationToken)
    {
        var threads = await db.ToListAsync(
            db.Conversations.Where(c => !c.IsDeleted).Select(c => new ThreadDto(c.Id, c.DisplayName)),
            cancellationToken);

        return Result<IReadOnlyList<ThreadDto>>.Success(threads);
    }
}
