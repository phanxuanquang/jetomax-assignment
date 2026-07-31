using ChatApp.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ChatApp.Api.Auth;

/// <summary>
/// Restricts a controller/action to specific caller <see cref="UserRole"/>s (§4.2) — the sole
/// authorization gate now that client-type (App/Mcp/N8n) is purely an authentication detail. Reads
/// the <see cref="ClientClaimTypes.Role"/> claim stamped by whichever authentication scheme handled
/// the request and returns 403 if it isn't in <paramref name="allowed"/>. An action-level attribute
/// overrides a controller-level one for that action; no attribute anywhere means any authenticated
/// role may call it (the only requirement is a resolved identity, which <c>[Authorize]</c> already
/// guarantees) — reserve this attribute for endpoints narrower than "any signed-in user". Runs in the
/// authorization pipeline, so it composes with any other <c>[Authorize]</c>/ownership checks rather
/// than replacing them — <see cref="Order"/> places it after the built-in <c>AuthorizeFilter</c>
/// (order 0), so a caller with no credentials at all is challenged (401) by <c>[Authorize]</c> before
/// this attribute ever runs, rather than falling through to an anonymous-principal 403 here.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class AllowedRolesAttribute(params UserRole[] allowed) : Attribute, IAuthorizationFilter, IOrderedFilter
{
    public int Order => 1;

    public IReadOnlyCollection<UserRole> Allowed => allowed;

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        // Action-level attribute wins over controller-level if both are present.
        var effective = context.ActionDescriptor.EndpointMetadata
            .OfType<AllowedRolesAttribute>()
            .LastOrDefault() ?? this;

        var claim = context.HttpContext.User.FindFirst(ClientClaimTypes.Role)?.Value;
        if (!Enum.TryParse<UserRole>(claim, out var role) || !effective.Allowed.Contains(role))
        {
            context.Result = new ForbidResult();
        }
    }
}
