using ChatApp.Application.Common.Results;
using ChatApp.Domain.Enums;
using MediatR;

namespace ChatApp.Application.Features.Internal.SetUserRole;

/// <summary>
/// Sets the system-wide <see cref="UserRole"/> for one or more existing users, identified by
/// username (F-1a). A manual, Administrator-only operation — gated entirely by
/// <c>[AllowedRoles(UserRole.Administrator)]</c> at the Api layer (§4.2); this is not a self-service
/// "become an admin" action, since only an existing Administrator can reach it.
/// </summary>
/// <param name="Usernames">Usernames of the users whose role is being set; each must resolve to an existing user or the whole batch fails (404).</param>
/// <param name="Role">The role to assign to every named user.</param>
public sealed record Command(IReadOnlyCollection<string> Usernames, UserRole Role) : IRequest<Result>;
