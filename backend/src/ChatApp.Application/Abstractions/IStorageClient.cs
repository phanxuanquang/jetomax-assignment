namespace ChatApp.Application.Abstractions;

/// <summary>
/// The port onto Supabase Storage. Uploads happen client-side directly to Storage — the backend never
/// streams upload bytes; this port only fetches an already-uploaded image's bytes to hand to
/// <see cref="IGenerativeAiService"/>'s byte-array overloads.
/// </summary>
public interface IStorageClient
{
    /// <summary>Downloads the raw bytes of the image at <paramref name="imageUrl"/>.</summary>
    Task<byte[]> DownloadAsync(string imageUrl, CancellationToken cancellationToken = default);
}
