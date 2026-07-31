using ChatApp.Application.Common.Results;
using MediatR;

namespace ChatApp.Application.Features.Users.GetUserByIdOrUsername;

public sealed record Query(string IdOrUsername) : IRequest<Result<UserMetaDto>>;
