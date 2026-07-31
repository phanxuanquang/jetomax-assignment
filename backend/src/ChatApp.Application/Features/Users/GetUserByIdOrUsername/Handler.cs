using ChatApp.Application.Abstractions;
using ChatApp.Application.Common.Results;
using MediatR;

namespace ChatApp.Application.Features.Users.GetUserByIdOrUsername;

public sealed class Handler(IAppDbContext db) : IRequestHandler<Query, Result<UserMetaDto>>
{
    public async Task<Result<UserMetaDto>> Handle(Query request, CancellationToken cancellationToken)
    {
        var input = request.IdOrUsername;
        var isGuid = Guid.TryParse(input, out var id);

        var user = await db.FirstOrDefaultAsync(
            db.Users.Where(u => (isGuid && u.Id == id) || u.Username == input),
            cancellationToken);

        if (user is null)
        {
            return Result<UserMetaDto>.Failure(Error.NotFound("user.not_found", "No user matches the given id or username."));
        }

        return Result<UserMetaDto>.Success(new UserMetaDto(user.Id, user.Username));
    }
}
