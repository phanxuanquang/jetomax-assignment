using ChatApp.Application.Abstractions;
using ChatApp.Application.Common.Results;
using MediatR;

namespace ChatApp.Application.Features.Conversations.Rename;

/// <summary>Handles <see cref="Command"/>.</summary>
public sealed class Handler(IAppDbContext db, IConversationAccess conversationAccess)
    : IRequestHandler<Command, Result>
{
    /// <summary>Owner-only: updates the conversation's <c>DisplayName</c>.</summary>
    public async Task<Result> Handle(Command request, CancellationToken cancellationToken)
    {
        var getOwnedConversationResult = await conversationAccess.GetOwnedConversationAsync(request.ConversationId, cancellationToken);
        if (!getOwnedConversationResult.IsSuccess)
        {
            return Result.Failure(getOwnedConversationResult.Error!);
        }

        var conversation = getOwnedConversationResult.Value!;

        if (conversation.DisplayName == request.DisplayName.Trim())
        {
            return Result.Success();
        }

        conversation.DisplayName = request.DisplayName.Trim();
        await db.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}