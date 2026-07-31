import { useState } from "react";
import { LogOut, MessageCircle, Search } from "lucide-react";
import { Input } from "@/components/ui/input";
import { Button } from "@/components/ui/button";
import { Skeleton } from "@/components/ui/skeleton";
import { Tooltip, TooltipContent, TooltipTrigger } from "@/components/ui/tooltip";
import { supabase } from "@/lib/supabase/client";
import { useDebouncedValue } from "@/hooks/useDebouncedValue";
import { useSession } from "@/features/auth/SessionContext";
import { ResolvedUserAvatar } from "@/features/users/ResolvedUserAvatar";
import { Username } from "@/features/users/Username";
import { useConversations } from "./useConversations";
import { ConversationListItem } from "./ConversationListItem";
import { CreateConversationDialog } from "./CreateConversationDialog";
import { JoinConversationDialog } from "./JoinConversationDialog";

export function ConversationSidebar({ className }: { className?: string }) {
  const [filterInput, setFilterInput] = useState("");
  const filter = useDebouncedValue(filterInput, 300);
  const { data: conversations, isLoading } = useConversations(filter);
  const { user } = useSession();

  return (
    <aside className={`flex flex-col border-r bg-sidebar text-sidebar-foreground ${className ?? ""}`}>
      <header className="flex items-center justify-between gap-2 p-3">
        <div className="flex items-center gap-2 px-1">
          <MessageCircle className="text-primary size-5" aria-hidden="true" />
          <h1 className="text-sm font-semibold">ChatApp</h1>
        </div>
        <div className="flex items-center gap-1">
          <JoinConversationDialog />
          <CreateConversationDialog />
        </div>
      </header>

      <div className="px-3 pb-2">
        <div className="relative">
          <Search
            className="text-muted-foreground pointer-events-none absolute left-2.5 top-1/2 size-3.5 -translate-y-1/2"
            aria-hidden="true"
          />
          <Input
            value={filterInput}
            onChange={(event) => setFilterInput(event.target.value)}
            placeholder="Search"
            className="h-8 pl-8 text-sm"
            aria-label="Filter conversations by name"
          />
        </div>
      </div>

      <nav className="flex-1 overflow-y-auto px-2 pb-2" aria-label="Conversations">
        {isLoading ? (
          <div className="flex flex-col gap-1.5 px-1">
            {Array.from({ length: 6 }, (_, index) => (
              <Skeleton key={index} className="h-14 w-full rounded-lg" />
            ))}
          </div>
        ) : conversations && conversations.length > 0 ? (
          <ul className="flex flex-col gap-0.5">
            {conversations.map((conversation) => (
              <ConversationListItem key={conversation.id} conversation={conversation} />
            ))}
          </ul>
        ) : (
          <p className="text-muted-foreground px-2 py-8 text-center text-xs">
            {filter ? "No matches." : "No conversations yet."}
          </p>
        )}
      </nav>

      <footer className="flex items-center gap-2 border-t p-3">
        <Tooltip>
          <TooltipTrigger asChild>
            <Button
              variant="ghost"
              size="icon"
              onClick={() => supabase.auth.signOut()}
              aria-label="Sign out"
            >
              <LogOut className="size-4" />
            </Button>
          </TooltipTrigger>
          <TooltipContent>Sign out</TooltipContent>
        </Tooltip>
        <div className="flex min-w-0 items-center gap-2">
          {user && <ResolvedUserAvatar userId={user.id} size="sm" />}
          <span className="truncate text-sm">{user && <Username userId={user.id} />}</span>
        </div>
      </footer>
    </aside>
  );
}
