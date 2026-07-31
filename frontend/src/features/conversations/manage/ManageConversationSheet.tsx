import { Settings } from "lucide-react";
import { Button } from "@/components/ui/button";
import {
  Sheet,
  SheetContent,
  SheetHeader,
  SheetTitle,
  SheetDescription,
  SheetTrigger,
} from "@/components/ui/sheet";
import { Separator } from "@/components/ui/separator";
import type { Conversation } from "@/types";
import { RenameSection } from "./RenameSection";
import { ReadonlySection } from "./ReadonlySection";
import { ParticipantsSection } from "./ParticipantsSection";
import { LeaveSection } from "./LeaveSection";

export function ManageConversationSheet({ conversation }: { conversation: Conversation }) {
  return (
    <Sheet>
      <SheetTrigger asChild>
        <Button variant="ghost" size="icon" aria-label="Manage conversation">
          <Settings className="size-4" />
        </Button>
      </SheetTrigger>
      <SheetContent className="flex flex-col gap-5 overflow-y-auto p-4">
        <SheetHeader className="p-0">
          <SheetTitle>Manage conversation</SheetTitle>
          <SheetDescription>Only visible to you because you own this conversation.</SheetDescription>
        </SheetHeader>

        <RenameSection conversationId={conversation.id} currentName={conversation.displayName} />
        <Separator />
        <ReadonlySection conversationId={conversation.id} isReadonly={conversation.isReadonly} />
        <Separator />
        <ParticipantsSection
          conversationId={conversation.id}
          participantUserIds={conversation.participantUserIds}
          ownerId={conversation.ownerId}
        />
        <Separator />
        <LeaveSection conversationId={conversation.id} isOwner />
      </SheetContent>
    </Sheet>
  );
}
