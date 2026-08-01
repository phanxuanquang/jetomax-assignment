using ChatApp.Application.Common.Results;
using MediatR;

namespace ChatApp.Application.Features.Users.GetSigninUserMeta;

/// <summary>Returns the signed-in caller's own <c>{ id, username }</c>. No input — always resolves from the caller's authenticated identity, never another user's.</summary>
public sealed record Query : IRequest<Result<UserMetaDto>>;
