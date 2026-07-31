import type { InfiniteData, QueryClient } from "@tanstack/react-query";
import type { Conversation, Message } from "@/types";
import { queryKeys } from "@/lib/query/keys";

type MessagePages = InfiniteData<Message[], string | undefined>;

/**
 * Upserts a message into the cached message pages for its conversation:
 * prepends if new, replaces in place if the id already exists (this is how a
 * caption that arrives after the image itself gets applied).
 */
export function upsertMessageInCache(queryClient: QueryClient, message: Message) {
  queryClient.setQueryData<MessagePages>(queryKeys.messages(message.conversationId), (old) => {
    if (!old) {
      return old;
    }

    const pages = old.pages.map((page) => page.filter((existing) => existing.id !== message.id));
    pages[0] = [message, ...pages[0]];

    return { ...old, pages };
  });
}

/** Updates a conversation's lastMessageTime in every cached conversation-list query and re-sorts it. */
export function bumpConversationLastMessageTime(
  queryClient: QueryClient,
  conversationId: string,
  sentAt: string,
) {
  queryClient.setQueriesData<Conversation[]>(
    { queryKey: queryKeys.conversationListAll, exact: false },
    (old) => {
      if (!old) {
        return old;
      }
      return old
        .map((conversation) =>
          conversation.id === conversationId
            ? { ...conversation, lastMessageTime: sentAt }
            : conversation,
        )
        .sort((a, b) => (b.lastMessageTime ?? "").localeCompare(a.lastMessageTime ?? ""));
    },
  );
}

/** Removes a conversation from every cached conversation-list query (left/removed/deleted). */
export function removeConversationFromCache(queryClient: QueryClient, conversationId: string) {
  queryClient.setQueriesData<Conversation[]>(
    { queryKey: queryKeys.conversationListAll, exact: false },
    (old) => old?.filter((conversation) => conversation.id !== conversationId),
  );
}

/** Refetches conversation lists — used for membership changes other than "I was removed". */
export function invalidateConversationLists(queryClient: QueryClient) {
  return queryClient.invalidateQueries({ queryKey: queryKeys.conversationListAll, exact: false });
}
