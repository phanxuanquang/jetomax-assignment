import { Users } from "lucide-react";
import { Badge } from "@/components/ui/badge";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from "@/components/ui/dialog";
import { ResolvedUserAvatar } from "@/features/users/ResolvedUserAvatar";
import { Username } from "@/features/users/Username";
import type { Conversation } from "@/types";

/** Read-only participant list, opened by clicking the participant count in the conversation header. */
export function ParticipantsDialog({ conversation }: { conversation: Conversation }) {
  const participantCount = conversation.participantUserIds.length;

  return (
    <Dialog>
      <DialogTrigger asChild>
        <button
          type="button"
          className="text-muted-foreground hover:bg-accent hover:text-accent-foreground flex items-center gap-1 rounded-md px-2 py-1.5 text-xs transition-colors"
          aria-label={`View ${participantCount} participants`}
        >
          <Users className="size-3.5" />
          {participantCount}
        </button>
      </DialogTrigger>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>Participants</DialogTitle>
          <DialogDescription>
            {participantCount} participant{participantCount === 1 ? "" : "s"} in "
            {conversation.displayName}"
          </DialogDescription>
        </DialogHeader>
        <ul className="flex flex-col gap-1">
          {conversation.participantUserIds.map((userId) => (
            <li key={userId} className="flex items-center gap-2.5 rounded-md p-1.5">
              <ResolvedUserAvatar userId={userId} size="sm" />
              <span className="flex-1 text-sm">
                <Username userId={userId} />
              </span>
              {userId === conversation.ownerId && <Badge variant="outline">Owner</Badge>}
            </li>
          ))}
        </ul>
      </DialogContent>
    </Dialog>
  );
}
