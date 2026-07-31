import { useInfiniteQuery } from "@tanstack/react-query";
import { fetchMessages } from "@/lib/api/conversations";
import { queryKeys } from "@/lib/query/keys";
import type { Message } from "@/types";

export function useMessages(conversationId: string) {
  return useInfiniteQuery({
    queryKey: queryKeys.messages(conversationId),
    queryFn: ({ pageParam }) => fetchMessages(conversationId, { before: pageParam }),
    initialPageParam: undefined as string | undefined,
    getNextPageParam: (lastPage: Message[]) =>
      lastPage.length > 0 ? lastPage[lastPage.length - 1].id : undefined,
    // Always refetch the newest page on opening a conversation, even if the
    // cache is still "fresh" — messages sent while this conversation wasn't
    // open should show up immediately, not after the default staleTime.
    refetchOnMount: "always",
  });
}

/** Flattens paginated (newest-first-per-page, newest-page-first) data into oldest-to-newest order. */
export function flattenMessagePages(pages: Message[][] | undefined): Message[] {
  if (!pages) return [];
  return [...pages].reverse().flatMap((page) => [...page].reverse());
}
