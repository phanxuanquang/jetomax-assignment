import { useQuery } from "@tanstack/react-query";
import { fetchSigninUserMeta } from "@/lib/api/users";
import { queryKeys } from "@/lib/query/keys";

/**
 * The signed-in caller's own `{ id, username }`, resolved server-side from the auth token — no id
 * or username to pass in, unlike {@link import("./useUsername").useUsername}, which resolves someone else's.
 */
export function useSigninUser() {
  const query = useQuery({
    queryKey: queryKeys.signinUser,
    queryFn: fetchSigninUserMeta,
    staleTime: Infinity,
    gcTime: Infinity,
    retry: false,
  });

  return { username: query.data?.username, isLoading: query.isLoading };
}
