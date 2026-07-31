import { useSession } from "@/features/auth/SessionContext";
import { ResolvedUserAvatar } from "@/features/users/ResolvedUserAvatar";
import { Username } from "@/features/users/Username";
import { Tooltip, TooltipContent, TooltipTrigger } from "@/components/ui/tooltip";
import { formatDateTime, formatShortTime } from "@/lib/format";
import type { Message } from "@/types";
import { MessageImage } from "./MessageImage";

/** Avatar width (size="sm" = 24px) + the gap after it — continuation bubbles indent by this to stay aligned under the first bubble in a run. */
const AVATAR_OFFSET_CLASS = "pl-8";

export function MessageBubble({
  message,
  showSenderInfo,
  className,
}: {
  message: Message;
  showSenderInfo: boolean;
  className?: string;
}) {
  const { user } = useSession();
  const isOwn = message.senderUserId === user?.id;

  return (
    <li className={`flex gap-2 ${isOwn ? "justify-end" : "justify-start"} ${className ?? ""}`}>
      {!isOwn && showSenderInfo && (
        <ResolvedUserAvatar userId={message.senderUserId} size="sm" className="mt-0.5 shrink-0" />
      )}
      <div className={`flex max-w-[75%] flex-col gap-0.5 ${!isOwn && !showSenderInfo ? AVATAR_OFFSET_CLASS : ""}`}>
        {!isOwn && showSenderInfo && (
          <span className="text-muted-foreground px-1 text-xs font-medium">
            <Username userId={message.senderUserId} />
          </span>
        )}
        <Tooltip>
          <TooltipTrigger asChild>
            <div
              className={`rounded-2xl px-3 py-2 ${
                isOwn ? "bg-primary text-primary-foreground" : "bg-muted"
              }`}
            >
              {message.type === "Text" ? (
                <p className="whitespace-pre-wrap break-words text-sm">{message.content}</p>
              ) : (
                <MessageImage imageUrl={message.imageUrl!} caption={message.caption} />
              )}
              <span
                className={`mt-0.5 block text-right text-[10px] ${
                  isOwn ? "text-primary-foreground/70" : "text-muted-foreground"
                }`}
              >
                {formatShortTime(message.sentAt)}
              </span>
            </div>
          </TooltipTrigger>
          <TooltipContent side={isOwn ? "left" : "right"}>
            {formatDateTime(message.sentAt)}
          </TooltipContent>
        </Tooltip>
      </div>
    </li>
  );
}
