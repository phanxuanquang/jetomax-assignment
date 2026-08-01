using ChatApp.Mcp.Options;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace ChatApp.Mcp.Auth;

/// <summary>
/// Fetches and caches Supabase's JWKS (<c>{Supabase:Url}/auth/v1/.well-known/jwks.json</c>). Supabase
/// defaults to asymmetric (ES256) signing, with the legacy HS256 secret also published here as a
/// symmetric JWK, so validating against the JWKS works for either mode with no static secret to
/// configure. <see cref="GetSigningKeysAsync"/> is safe to call from the synchronous
/// <c>IssuerSigningKeyResolver</c> callback because steady-state calls just hit the cache.
/// </summary>
public sealed class SupabaseJwksProvider(HttpClient httpClient, IOptions<SupabaseOptions> options)
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromHours(12);

    private readonly SemaphoreSlim _refreshLock = new(1, 1);
    private IReadOnlyList<SecurityKey> _cachedKeys = [];
    private DateTimeOffset _cachedAt = DateTimeOffset.MinValue;

    /// <summary>Returns the current signing keys, refreshing from Supabase's JWKS endpoint if the cache is stale.</summary>
    public async Task<IReadOnlyList<SecurityKey>> GetSigningKeysAsync(CancellationToken cancellationToken = default)
    {
        if (DateTimeOffset.UtcNow - _cachedAt < CacheDuration && _cachedKeys.Count > 0)
        {
            return _cachedKeys;
        }

        await _refreshLock.WaitAsync(cancellationToken);
        try
        {
            if (DateTimeOffset.UtcNow - _cachedAt < CacheDuration && _cachedKeys.Count > 0)
            {
                return _cachedKeys;
            }

            var jwksUri = options.Value.Url.TrimEnd('/') + "/auth/v1/.well-known/jwks.json";
            var json = await httpClient.GetStringAsync(jwksUri, cancellationToken);
            var jwks = JsonWebKeySet.Create(json);

            _cachedKeys = jwks.GetSigningKeys().ToList();
            _cachedAt = DateTimeOffset.UtcNow;
            return _cachedKeys;
        }
        finally
        {
            _refreshLock.Release();
        }
    }
}
