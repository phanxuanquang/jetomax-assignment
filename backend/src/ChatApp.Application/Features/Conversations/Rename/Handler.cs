using ChatApp.Application.Abstractions;
using ChatApp.Application.Common.Results;
using MediatR;

namespace ChatApp.Application.Features.Conversations.Rename;

/// <summary>Handles <see cref="Command"/>.</summary>
public sealed class Handler(IAppDbContext db, ICurrentUser currentUser)
    : IRequestHandler<Command, Result>
{
    /// <summary>Owner-only: updates the conversation's <c>DisplayName</c>.</summary>
    public async Task<Result> Handle(Command request, CancellationToken cancellationToken)
    {
        var guard = await currentUser.GetOwnedConversationAsync(request.ConversationId, cancellationToken);
        if (!guard.IsSuccess)
        {
            return Result.Failure(guard.Error!);
        }

        guard.Value!.DisplayName = request.DisplayName;
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
