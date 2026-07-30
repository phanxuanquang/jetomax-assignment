using ChatApp.Application.Common.Results;
using ChatApp.Domain.Enums;
using MediatR;

namespace ChatApp.Application.Features.Internal.SetSystemRoleForUsers;

public sealed record Command(IReadOnlyCollection<Guid> TargetUserIds, UserRole TargetRole) : IRequest<Result>;