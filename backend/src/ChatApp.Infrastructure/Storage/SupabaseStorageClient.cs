using ChatApp.Application.Abstractions;
using ChatApp.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace ChatApp.Infrastructure.Storage;

/// <summary>
/// Implements <see cref="IStorageClient"/> over Supabase Storage's REST API, authenticating with the
/// service-role key rather than trusting the client-supplied (frontend-issued, possibly expired)
/// signed URL. Only ever downloads — uploads happen client-side directly to Storage.
/// </summary>
public sealed class SupabaseStorageClient : IStorageClient
{
    private readonly HttpClient _httpClient;
    private readonly string _bucket;

    public SupabaseStorageClient(HttpClient httpClient, IOptions<SupabaseStorageOptions> options)
    {
        _bucket = options.Value.StorageBucket;

        httpClient.BaseAddress = new Uri(options.Value.Url.TrimEnd('/') + "/");
        httpClient.DefaultRequestHeaders.Add("apikey", options.Value.ServiceRoleKey);
        httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {options.Value.ServiceRoleKey}");
        _httpClient = httpClient;
    }

    public async Task<byte[]> DownloadAsync(string imageUrl, CancellationToken cancellationToken = default)
    {
        var objectPath = ExtractObjectPath(imageUrl);
        using var response = await _httpClient.GetAsync($"storage/v1/object/{_bucket}/{objectPath}", cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsByteArrayAsync(cancellationToken);
    }

    /// <summary>
    /// Recovers the object's path within <see cref="_bucket"/> out of whatever URL shape the caller
    /// holds — a signed URL (<c>/object/sign/{bucket}/{path}?token=...</c>), a public URL
    /// (<c>/object/public/{bucket}/{path}</c>), or a bare <c>{bucket}/{path}</c> — since only the
    /// path matters for the authenticated download call, not the signature/query.
    /// </summary>
    private string ExtractObjectPath(string imageUrl)
    {
        var withoutQuery = imageUrl.Split('?')[0];

        var marker = $"/{_bucket}/";
        var markerIndex = withoutQuery.IndexOf(marker, StringComparison.Ordinal);
        if (markerIndex >= 0)
        {
            return withoutQuery[(markerIndex + marker.Length)..];
        }

        var prefix = $"{_bucket}/";
        if (withoutQuery.StartsWith(prefix, StringComparison.Ordinal))
        {
            return withoutQuery[prefix.Length..];
        }

        return withoutQuery.TrimStart('/');
    }
}
