import { useState } from "react";
import { ImageIcon, Loader2, Search } from "lucide-react";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from "@/components/ui/dialog";
import { Input } from "@/components/ui/input";
import { Tooltip, TooltipContent, TooltipTrigger } from "@/components/ui/tooltip";
import { ResolvedUserAvatar } from "@/features/users/ResolvedUserAvatar";
import { Username } from "@/features/users/Username";
import { useDebouncedValue } from "@/hooks/useDebouncedValue";
import { formatDateTime } from "@/lib/format";
import { useSearchMessages } from "./useSearchMessages";

export function MessageSearchDialog({ conversationId }: { conversationId: string }) {
  const [isOpen, setIsOpen] = useState(false);
  const [keyword, setKeyword] = useState("");
  const debouncedKeyword = useDebouncedValue(keyword, 300);
  const { data: results, isFetching } = useSearchMessages(conversationId, debouncedKeyword);

  return (
    <Dialog
      open={isOpen}
      onOpenChange={(open) => {
        setIsOpen(open);
        if (!open) setKeyword("");
      }}
    >
      <Tooltip>
        <TooltipTrigger asChild>
          <DialogTrigger asChild>
            <button
              type="button"
              className="text-muted-foreground hover:bg-accent hover:text-accent-foreground flex size-8 items-center justify-center rounded-md transition-colors"
              aria-label="Search messages"
            >
              <Search className="size-4" />
            </button>
          </DialogTrigger>
        </TooltipTrigger>
        <TooltipContent>Search messages</TooltipContent>
      </Tooltip>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>Search messages</DialogTitle>
          <DialogDescription>Searches text messages and image captions.</DialogDescription>
        </DialogHeader>
        <Input
          autoFocus
          value={keyword}
          onChange={(event) => setKeyword(event.target.value)}
          placeholder="Search this conversation"
        />
        <div className="flex max-h-80 flex-col gap-1 overflow-y-auto">
          {isFetching ? (
            <div className="flex justify-center py-6">
              <Loader2 className="text-muted-foreground size-5 animate-spin" aria-hidden="true" />
            </div>
          ) : !debouncedKeyword.trim() ? (
            <p className="text-muted-foreground py-6 text-center text-sm">
              Type to search this conversation.
            </p>
          ) : results && results.length > 0 ? (
            <ul className="flex flex-col gap-1">
              {results.map((message) => (
                <li key={message.id} className="flex items-start gap-2.5 rounded-md p-2">
                  <ResolvedUserAvatar userId={message.senderUserId} size="sm" className="mt-0.5" />
                  <div className="flex min-w-0 flex-1 flex-col gap-0.5">
                    <div className="flex items-center justify-between gap-2">
                      <span className="text-xs font-medium">
                        <Username userId={message.senderUserId} />
                      </span>
                      <span className="text-muted-foreground shrink-0 text-[11px]">
                        {formatDateTime(message.sentAt)}
                      </span>
                    </div>
                    <p className="text-sm break-words">
                      {message.type === "Text" ? (
                        message.content
                      ) : (
                        <span className="text-muted-foreground inline-flex items-center gap-1">
                          <ImageIcon className="size-3.5" />
                          {message.caption ?? "Image"}
                        </span>
                      )}
                    </p>
                  </div>
                </li>
              ))}
            </ul>
          ) : (
            <p className="text-muted-foreground py-6 text-center text-sm">No matches.</p>
          )}
        </div>
      </DialogContent>
    </Dialog>
  );
}
