using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace ChatApp.Api.Auth;

/// <summary>
/// Restricts a controller/action to specific caller <see cref="Client"/> kinds (§4.2). Reads the
/// <see cref="ClientClaimTypes.Client"/> claim stamped by whichever authentication scheme handled the
/// request and returns 403 if it isn't in <paramref name="allowed"/>. Runs in the authorization
/// pipeline, so it composes with any other <c>[Authorize]</c>/ownership checks rather than replacing
/// them — <see cref="Order"/> places it after the built-in <c>AuthorizeFilter</c> (order 0), so a
/// caller with no credentials at all is challenged (401) by <c>[Authorize]</c> before this attribute
/// ever runs, rather than falling through to an anonymous-principal 403 here.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class AllowedClientsAttribute(params Client[] allowed) : Attribute, IAuthorizationFilter, IOrderedFilter
{
    public int Order => 1;

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var claim = context.HttpContext.User.FindFirst(ClientClaimTypes.Client)?.Value;
        if (!Enum.TryParse<Client>(claim, out var client) || !allowed.Contains(client))
        {
            context.Result = new ForbidResult();
        }
    }
}
