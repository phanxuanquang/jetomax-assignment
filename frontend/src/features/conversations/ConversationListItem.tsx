import { NavLink } from "react-router-dom";
import { Lock } from "lucide-react";
import { ConversationAvatar } from "./ConversationAvatar";
import { formatRelativeTime } from "@/lib/format";
import type { Conversation } from "@/types";

export function ConversationListItem({ conversation }: { conversation: Conversation }) {
  return (
    <li>
      <NavLink
        to={`/conversations/${conversation.id}`}
        className={({ isActive }) =>
          `flex items-center gap-3 rounded-lg p-2.5 transition-colors ${
            isActive ? "bg-sidebar-accent text-sidebar-accent-foreground" : "hover:bg-sidebar-accent/60"
          }`
        }
      >
        <ConversationAvatar conversation={conversation} />
        <div className="flex min-w-0 flex-1 items-center justify-between gap-2">
          <span className="truncate text-sm font-medium">{conversation.displayName}</span>
          <div className="flex shrink-0 items-center gap-1">
            {conversation.isReadonly && (
              <Lock className="text-muted-foreground size-3" aria-label="Read-only" />
            )}
            {conversation.lastMessageTime && (
              <span className="text-muted-foreground text-xs">
                {formatRelativeTime(conversation.lastMessageTime)}
              </span>
            )}
          </div>
        </div>
      </NavLink>
    </li>
  );
}
