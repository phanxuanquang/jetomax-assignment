import { MessageCircle } from "lucide-react";

export function ConversationEmptyState() {
  return (
    <div className="flex flex-1 flex-col items-center justify-center gap-2 text-center">
      <MessageCircle className="text-muted-foreground/40 size-12" aria-hidden="true" />
      <p className="text-muted-foreground text-sm">Select a conversation to start chatting</p>
    </div>
  );
}
