using ChatApp.Application.Abstractions;
using ChatApp.Application.Common.Results;
using MediatR;

namespace ChatApp.Application.Features.Internal.SetUserRole;

/// <summary>
/// Handles <see cref="Command"/>. Role-based authorization is enforced entirely by
/// <c>[AllowedRoles(UserRole.Administrator)]</c> at the Api layer (§4.2) — this handler only resolves
/// usernames and applies the role, mirroring every other batch command's all-or-nothing username resolution.
/// </summary>
public sealed class Handler(IAppDbContext db) : IRequestHandler<Command, Result>
{
    public async Task<Result> Handle(Command request, CancellationToken cancellationToken)
    {
        var distinctUsernames = request.Usernames.Distinct(StringComparer.Ordinal).ToList();
        var resolvedUsers = await db.ToListAsync(
            db.Users.Where(u => distinctUsernames.Contains(u.Username)),
            cancellationToken);

        if (resolvedUsers.Count != distinctUsernames.Count)
        {
            return Result.Failure(Error.NotFound("user.not_found", "One or more usernames do not resolve to an existing user."));
        }

        foreach (var user in resolvedUsers.Where(u => u.Role != request.Role))
        {
            user.Role = request.Role;
        }

        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
