namespace ChatApp.Application.Abstractions;

/// <summary>
/// The port onto Supabase Storage. Uploads happen client-side directly to Storage (principle 2) —
/// the backend never streams upload bytes. This port exists only for the backend to fetch an
/// already-uploaded image's bytes when it needs to hand them to <see cref="IVisionService"/>.
/// </summary>
public interface IStorageClient
{
    /// <summary>Downloads the raw bytes of the image at <paramref name="imageUrl"/>.</summary>
    Task<byte[]> DownloadAsync(string imageUrl, CancellationToken cancellationToken);
}
