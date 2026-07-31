namespace ChatApp.Api.Auth;

/// <summary>Claim type literals shared by every authentication scheme and <see cref="AllowedRolesAttribute"/>.</summary>
internal static class ClientClaimTypes
{
    /// <summary>
    /// Carries the caller's resolved user id (a <see cref="System.Guid"/>): the Supabase JWT's own
    /// <c>sub</c> claim for App, or the resolved <c>X-On-Behalf-Of</c> username's id for Mcp/N8n.
    /// Every call now resolves to a real user (§4.2) — there is no absent case. Uses the raw "sub"
    /// name (not <see cref="System.Security.Claims.ClaimTypes.NameIdentifier"/>) since JWT inbound
    /// claim mapping is disabled for the Supabase scheme (see <see cref="AuthenticationSetup"/>).
    /// </summary>
    public const string Subject = "sub";

    /// <summary>
    /// Carries the caller's resolved <see cref="Domain.Enums.UserRole"/> (as its enum name), read by
    /// <see cref="AllowedRolesAttribute"/> and <see cref="ConversationAccess"/>. Resolved fresh per
    /// request from <c>user_roles</c> — never baked into a long-lived token — so a mid-session role
    /// change (e.g. a demotion) takes effect on the caller's very next request. Deliberately NOT named
    /// "role": Supabase's own JWT already carries a claim literally typed "role" (its Postgres role,
    /// always "authenticated" for a signed-in user) — <see cref="ClaimsPrincipal.FindFirst"/> returns
    /// the first matching claim, so reusing "role" would silently read Supabase's claim instead of ours.
    /// </summary>
    public const string Role = "chatapp_role";
}
