using ChatApp.Application.Abstractions;
using ChatApp.Application.Common.Results;
using MediatR;

namespace ChatApp.Application.Features.Conversations.SetReadonly;

/// <summary>Handles <see cref="Command"/>.</summary>
public sealed class Handler(IAppDbContext db, ICurrentUser currentUser)
    : IRequestHandler<Command, Result>
{
    /// <summary>Owner-only: sets the conversation's <c>IsReadonly</c> flag manually.</summary>
    public async Task<Result> Handle(Command request, CancellationToken cancellationToken)
    {
        var getOwnedConversationResult = await currentUser.GetOwnedConversationAsync(request.ConversationId, cancellationToken);
        if (!getOwnedConversationResult.IsSuccess)
        {
            return Result.Failure(getOwnedConversationResult.Error!);
        }

        var conversation = getOwnedConversationResult.Value!;
        if (conversation.IsReadonly == request.IsReadonly)
        {
            return Result.Success();
        }

        conversation.IsReadonly = request.IsReadonly;
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
