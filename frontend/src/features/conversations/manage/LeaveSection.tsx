import { useState } from "react";
import { useNavigate } from "react-router-dom";
import { LogOut } from "lucide-react";
import { toast } from "sonner";
import { Button } from "@/components/ui/button";
import {
  AlertDialog,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
  AlertDialogTrigger,
  AlertDialogCancel,
} from "@/components/ui/alert-dialog";
import { Tooltip, TooltipContent, TooltipTrigger } from "@/components/ui/tooltip";
import { getErrorMessage } from "@/lib/api/client";
import { useLeaveConversation } from "@/features/conversations/useConversationMutations";
import type { LeaveMode } from "@/types";

export function LeaveSection({
  conversationId,
  isOwner,
  compact,
}: {
  conversationId: string;
  isOwner: boolean;
  /** Icon-only trigger with a tooltip, for tight spaces like the conversation header. */
  compact?: boolean;
}) {
  const [isOpen, setIsOpen] = useState(false);
  const mutation = useLeaveConversation(conversationId);
  const navigate = useNavigate();

  function handleLeave(mode?: LeaveMode) {
    mutation.mutate(
      { mode },
      {
        onSuccess: () => {
          setIsOpen(false);
          navigate("/", { replace: true });
        },
        onError: (error) => toast.error(getErrorMessage(error)),
      },
    );
  }

  return (
    <AlertDialog open={isOpen} onOpenChange={setIsOpen}>
      {compact ? (
        <Tooltip>
          <TooltipTrigger asChild>
            <AlertDialogTrigger asChild>
              <Button variant="ghost" size="icon" className="text-destructive hover:text-destructive" aria-label="Leave conversation">
                <LogOut className="size-4" />
              </Button>
            </AlertDialogTrigger>
          </TooltipTrigger>
          <TooltipContent>Leave conversation</TooltipContent>
        </Tooltip>
      ) : (
        <AlertDialogTrigger asChild>
          <Button variant="destructive" className="gap-2">
            <LogOut className="size-4" />
            Leave conversation
          </Button>
        </AlertDialogTrigger>
      )}
      <AlertDialogContent>
        <AlertDialogHeader>
          <AlertDialogTitle>Leave this conversation?</AlertDialogTitle>
          <AlertDialogDescription>
            {isOwner
              ? "As the owner, choose what happens to the conversation when you leave."
              : "You can rejoin later with the join code, if you still have it."}
          </AlertDialogDescription>
        </AlertDialogHeader>
        <AlertDialogFooter>
          <AlertDialogCancel disabled={mutation.isPending}>Cancel</AlertDialogCancel>
          {isOwner ? (
            <>
              <Button
                variant="outline"
                onClick={() => handleLeave("freeze")}
                disabled={mutation.isPending}
              >
                Freeze conversation
              </Button>
              <Button
                variant="destructive"
                onClick={() => handleLeave("delete")}
                disabled={mutation.isPending}
              >
                Delete conversation
              </Button>
            </>
          ) : (
            <Button
              variant="destructive"
              onClick={() => handleLeave(undefined)}
              disabled={mutation.isPending}
            >
              Leave
            </Button>
          )}
        </AlertDialogFooter>
      </AlertDialogContent>
    </AlertDialog>
  );
}
