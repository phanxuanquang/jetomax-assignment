import { useEffect, useLayoutEffect, useRef } from "react";
import { Loader2 } from "lucide-react";
import { useMessages, flattenMessagePages } from "./useMessages";
import { MessageBubble } from "./MessageBubble";
import type { Message } from "@/types";

const GROUP_GAP_MS = 5 * 60 * 1000;

/** First message from a sender, or the first after a 5+ minute gap, starts a new visual group. */
function isFirstOfGroup(messages: Message[], index: number): boolean {
  if (index === 0) return true;
  const previous = messages[index - 1];
  const current = messages[index];
  if (previous.senderUserId !== current.senderUserId) return true;
  return new Date(current.sentAt).getTime() - new Date(previous.sentAt).getTime() > GROUP_GAP_MS;
}

export function MessageList({ conversationId }: { conversationId: string }) {
  const { data, fetchNextPage, hasNextPage, isFetchingNextPage, isLoading } =
    useMessages(conversationId);
  const messages = flattenMessagePages(data?.pages);

  const scrollRef = useRef<HTMLDivElement>(null);
  const topSentinelRef = useRef<HTMLDivElement>(null);
  const prevScrollHeightRef = useRef(0);
  const isNearBottomRef = useRef(true);
  const lastMessageIdRef = useRef<string | undefined>(undefined);

  // Load older messages once the sentinel above the oldest loaded one becomes visible.
  useEffect(() => {
    const sentinel = topSentinelRef.current;
    const container = scrollRef.current;
    if (!sentinel || !container) return;

    const observer = new IntersectionObserver(
      (entries) => {
        if (entries[0].isIntersecting && hasNextPage && !isFetchingNextPage) {
          prevScrollHeightRef.current = container.scrollHeight;
          fetchNextPage();
        }
      },
      { root: container },
    );
    observer.observe(sentinel);
    return () => observer.disconnect();
  }, [hasNextPage, isFetchingNextPage, fetchNextPage]);

  function handleScroll() {
    const container = scrollRef.current;
    if (!container) return;
    const distanceFromBottom =
      container.scrollHeight - container.scrollTop - container.clientHeight;
    isNearBottomRef.current = distanceFromBottom < 150;
  }

  useLayoutEffect(() => {
    const container = scrollRef.current;
    if (!container) return;
    const latestMessageId = messages[messages.length - 1]?.id;

    if (prevScrollHeightRef.current > 0) {
      // An older page was just prepended — hold the viewport steady instead of jumping.
      container.scrollTop += container.scrollHeight - prevScrollHeightRef.current;
      prevScrollHeightRef.current = 0;
    } else if (latestMessageId !== lastMessageIdRef.current && isNearBottomRef.current) {
      container.scrollTop = container.scrollHeight;
    }

    lastMessageIdRef.current = latestMessageId;
  }, [messages]);

  if (isLoading) {
    return (
      <div className="flex flex-1 items-center justify-center">
        <Loader2 className="text-muted-foreground size-6 animate-spin" aria-hidden="true" />
      </div>
    );
  }

  return (
    <div
      ref={scrollRef}
      onScroll={handleScroll}
      className="bg-muted/20 flex-1 overflow-y-auto p-4"
    >

      <div ref={topSentinelRef} />
      {isFetchingNextPage && (
        <div className="flex justify-center py-2">
          <Loader2 className="text-muted-foreground size-4 animate-spin" aria-hidden="true" />
        </div>
      )}
      {messages.length === 0 ? (
        <p className="text-muted-foreground py-12 text-center text-sm">
          No messages yet — say hello.
        </p>
      ) : (
        <ul className="flex flex-col gap-0.5">
          {messages.map((message, index) => {
            const firstOfGroup = isFirstOfGroup(messages, index);
            return (
              <MessageBubble
                key={message.id}
                message={message}
                showSenderInfo={firstOfGroup}
                className={firstOfGroup && index > 0 ? "mt-3" : undefined}
              />
            );
          })}
        </ul>
      )}
    </div>
  );
}
