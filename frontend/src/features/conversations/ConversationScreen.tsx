import { Link, useParams } from "react-router-dom";
import { ArrowLeft, Loader2, Lock } from "lucide-react";
import { Tooltip, TooltipContent, TooltipTrigger } from "@/components/ui/tooltip";
import { useSession } from "@/features/auth/SessionContext";
import { useConversations } from "./useConversations";
import { ConversationAvatar } from "./ConversationAvatar";
import { ParticipantsDialog } from "./ParticipantsDialog";
import { MessageList } from "@/features/messages/MessageList";
import { MessageComposer } from "@/features/messages/MessageComposer";
import { SummaryDialog } from "@/features/summary/SummaryDialog";
import { ManageConversationSheet } from "./manage/ManageConversationSheet";
import { LeaveSection } from "./manage/LeaveSection";

export function ConversationScreen() {
  const { conversationId } = useParams<{ conversationId: string }>();
  const { user } = useSession();
  // No single-conversation-by-id endpoint exists; the full (unfiltered) list
  // is the source of truth and is kept live by the same realtime patches as
  // the conversation list screen.
  const { data: conversations, isLoading } = useConversations("");
  const conversation = conversations?.find((item) => item.id === conversationId);

  if (isLoading) {
    return (
      <div className="flex h-full items-center justify-center">
        <Loader2 className="text-muted-foreground size-6 animate-spin" aria-hidden="true" />
      </div>
    );
  }

  if (!conversation || !conversationId) {
    return (
      <div className="flex h-full flex-col items-center justify-center gap-3">
        <p className="text-muted-foreground text-sm">
          This conversation isn't available anymore.
        </p>
        <Link to="/" className="text-primary text-sm underline">
          Back to conversations
        </Link>
      </div>
    );
  }

  const isOwner = conversation.ownerId === user?.id;
  const canSend = !conversation.isReadonly || isOwner;

  return (
    <div className="flex h-full min-h-0 flex-col">
      <header className="flex items-center gap-3 border-b px-3 py-2.5">
        <Link to="/" aria-label="Back to conversations" className="md:hidden">
          <ArrowLeft className="size-5" />
        </Link>
        <ConversationAvatar conversation={conversation} size="sm" />
        <div className="flex min-w-0 flex-1 flex-col">
          <h1 className="truncate text-sm font-semibold">{conversation.displayName}</h1>
          {conversation.ownerId === null && (
            <span className="text-muted-foreground text-xs">Frozen</span>
          )}
        </div>
        <div className="flex items-center gap-0.5">
          {conversation.isReadonly && (
            <Tooltip>
              <TooltipTrigger asChild>
                <span className="text-muted-foreground flex size-8 items-center justify-center">
                  <Lock className="size-4" />
                </span>
              </TooltipTrigger>
              <TooltipContent>Read-only conversation</TooltipContent>
            </Tooltip>
          )}
          <ParticipantsDialog conversation={conversation} />
          <SummaryDialog conversationId={conversation.id} />
          {isOwner ? (
            <ManageConversationSheet conversation={conversation} />
          ) : (
            <LeaveSection conversationId={conversation.id} isOwner={false} compact />
          )}
        </div>
      </header>

      <MessageList conversationId={conversation.id} />
      <MessageComposer conversationId={conversation.id} disabled={!canSend} />
    </div>
  );
}
