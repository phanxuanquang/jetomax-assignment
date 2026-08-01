import { useQuery } from "@tanstack/react-query";
import { searchMessages } from "@/lib/api/conversations";
import { queryKeys } from "@/lib/query/keys";

export function useSearchMessages(conversationId: string, keyword: string) {
  const trimmed = keyword.trim();

  return useQuery({
    queryKey: queryKeys.messageSearch(conversationId, trimmed),
    queryFn: () => searchMessages(conversationId, trimmed),
    enabled: trimmed.length > 0,
  });
}
