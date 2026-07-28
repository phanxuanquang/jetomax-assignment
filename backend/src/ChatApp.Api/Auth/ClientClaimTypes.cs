namespace ChatApp.Api.Auth;

/// <summary>Claim type literals shared by every authentication scheme and <see cref="AllowedClientsAttribute"/>.</summary>
internal static class ClientClaimTypes
{
    /// <summary>Carries the resolved <see cref="Client"/> (as its enum name), read by <see cref="AllowedClientsAttribute"/>.</summary>
    public const string Client = "client";

    /// <summary>
    /// Carries the caller's resolved user id (a <see cref="System.Guid"/>): the Supabase JWT's own
    /// <c>sub</c> claim for App, or the <c>X-On-Behalf-Of</c> header's value for Mcp. Absent for N8n,
    /// which carries no user identity. Uses the raw "sub" name (not <see cref="System.Security.Claims.ClaimTypes.NameIdentifier"/>)
    /// since JWT inbound claim mapping is disabled for the Supabase scheme (see <see cref="AuthenticationSetup"/>).
    /// </summary>
    public const string Subject = "sub";
}
