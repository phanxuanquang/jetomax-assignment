import { useQuery } from "@tanstack/react-query";
import { fetchUserByIdOrUsername } from "@/lib/api/users";
import { queryKeys } from "@/lib/query/keys";

/** Shortened fallback shown while a username hasn't resolved yet (or the id truly has none). */
export function shortenUserId(userId: string): string {
  return `User ${userId.slice(0, 6)}`;
}

/**
 * Resolves a userId (or username) to a display username via GET /api/users/{idOrUsername}.
 * Usernames are auto-derived at sign-up and never editable, so the result is cached
 * indefinitely — no need to ever refetch a given id within a session.
 */
export function useUsername(idOrUsername: string | null | undefined) {
  const query = useQuery({
    queryKey: queryKeys.user(idOrUsername ?? ""),
    queryFn: () => fetchUserByIdOrUsername(idOrUsername as string),
    enabled: Boolean(idOrUsername),
    staleTime: Infinity,
    gcTime: Infinity,
    retry: false,
  });

  const displayName = query.data?.username ?? (idOrUsername ? shortenUserId(idOrUsername) : "");

  return { username: query.data?.username, displayName, isLoading: query.isLoading };
}
