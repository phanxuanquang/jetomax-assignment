using ChatApp.Application.Abstractions;
using ChatApp.Application.Common.Results;
using ChatApp.Domain.Enums;
using MediatR;

namespace ChatApp.Application.Features.Internal.SetSystemRoleForUsers;

public sealed class Handler(IAppDbContext db, IConversationAccess conversationAccess)
    : IRequestHandler<Command, Result>
{
    /// <summary>Owner-only: sets the conversation's <c>IsReadonly</c> flag manually.</summary>
    public async Task<Result> Handle(Command request, CancellationToken cancellationToken)
    {
        var getCurrentUserResult = await conversationAccess.GetCurrentUserAsync(cancellationToken);
        if (!getCurrentUserResult.IsSuccess)
        {
            return Result.Failure(getCurrentUserResult.Error!);
        }

        var currentUser = getCurrentUserResult.Value;
        if (currentUser == null)
        {
            return Result.Failure(Error.NotFound("404", "User not found."));
        }

        if (currentUser.Role != UserRole.Administrator)
        {
            return Result.Failure(Error.Forbidden("403", "This action is only for administrator."));
        }

        var targetUsers = await db.ToListAsync(
            db.Users.Where(u => request.TargetUserIds.Contains(u.Id) && u.Role != request.TargetRole),
            cancellationToken);

        if (targetUsers.Count > 0)
        {
            foreach (var user in targetUsers)
            {
                user.Role = request.TargetRole;
            }

            await db.SaveChangesAsync(cancellationToken);
        }

        return Result.Success();
    }
}