namespace ChatApp.Infrastructure.Options;

public sealed class SupabaseStorageOptions
{
    /// <summary>
    /// Supabase project API URL, e.g. <c>http://127.0.0.1:54321</c>.
    /// </summary>
    public required string Url { get; init; }

    /// <summary>
    /// Service-role key (server-only). Used here — not any client-issued signed URL — to
    /// authenticate object downloads, since the frontend's own signed URLs may have expired by the
    /// time the backend needs the bytes for a vision call.
    /// </summary>
    public required string ServiceRoleKey { get; init; }
    public required string StorageBucket { get; init; }
}
