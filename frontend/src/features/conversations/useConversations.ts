import { useQuery } from "@tanstack/react-query";
import { fetchConversations } from "@/lib/api/conversations";
import { queryKeys } from "@/lib/query/keys";

export function useConversations(filter: string) {
  return useQuery({
    queryKey: queryKeys.conversationList(filter),
    queryFn: () => fetchConversations(filter),
    // Refetch whenever a component mounts a new observer on this query — this
    // is what makes opening a conversation pick up the latest
    // owner/readonly/participants state instead of trusting a stale cache.
    refetchOnMount: "always",
  });
}
