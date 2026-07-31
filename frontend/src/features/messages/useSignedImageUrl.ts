import { useEffect, useState } from "react";
import { resolveSignedImageUrl, IMAGE_SIGNED_URL_TTL_MS } from "@/lib/supabase/storage";

interface CacheEntry {
  url: string;
  expiresAt: number;
}

// Module-level cache: many messages can reference the same path across a
// session, and a signed URL is valid well past a single component's lifetime.
const signedUrlCache = new Map<string, CacheEntry>();

export function useSignedImageUrl(storedImageUrl: string) {
  const [url, setUrl] = useState<string | null>(
    () => getFreshCacheEntry(storedImageUrl)?.url ?? null,
  );
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const cached = getFreshCacheEntry(storedImageUrl);
    if (cached) {
      setUrl(cached.url);
      return;
    }

    let cancelled = false;
    setError(null);

    resolveSignedImageUrl(storedImageUrl)
      .then((signedUrl) => {
        if (cancelled) return;
        signedUrlCache.set(storedImageUrl, {
          url: signedUrl,
          expiresAt: Date.now() + IMAGE_SIGNED_URL_TTL_MS,
        });
        setUrl(signedUrl);
      })
      .catch(() => {
        if (!cancelled) setError("Image failed to load.");
      });

    return () => {
      cancelled = true;
    };
  }, [storedImageUrl]);

  return { url, error };
}

function getFreshCacheEntry(key: string): CacheEntry | undefined {
  const entry = signedUrlCache.get(key);
  if (entry && entry.expiresAt > Date.now()) {
    return entry;
  }
  return undefined;
}
