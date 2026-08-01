using ChatApp.Application.Abstractions;
using ChatApp.Application.Common.Results;
using MediatR;

namespace ChatApp.Application.Features.Users.GetSigninUserMeta;

/// <summary>Handles <see cref="Query"/>.</summary>
public sealed class Handler(IConversationAccess conversationAccess) : IRequestHandler<Query, Result<UserMetaDto>>
{
    /// <summary>Resolves the caller's own <c>{ id, username }</c> from their authenticated identity.</summary>
    public async Task<Result<UserMetaDto>> Handle(Query request, CancellationToken cancellationToken)
    {
        var result = await conversationAccess.GetCurrentUserAsync(cancellationToken);
        if (!result.IsSuccess)
        {
            return Result<UserMetaDto>.Failure(result.Error!);
        }

        var user = result.Value!;
        return Result<UserMetaDto>.Success(new UserMetaDto(user.Id, user.Username));
    }
}
