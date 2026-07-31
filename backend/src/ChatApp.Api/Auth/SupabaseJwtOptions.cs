namespace ChatApp.Api.Auth;

public sealed class SupabaseJwtOptions
{
    /// <summary>Supabase project API URL, e.g. <c>http://127.0.0.1:54321</c>. Used to derive both the JWKS endpoint and the expected token issuer.</summary>
    public required string Url { get; init; }
}
