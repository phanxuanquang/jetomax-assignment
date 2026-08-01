import { useState } from "react";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { useNavigate } from "react-router-dom";
import { Plus, Copy } from "lucide-react";
import { toast } from "sonner";
import { Button } from "@/components/ui/button";
import { Label } from "@/components/ui/label";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from "@/components/ui/dialog";
import { Tooltip, TooltipContent, TooltipTrigger } from "@/components/ui/tooltip";
import { UsernameChipInput } from "@/components/UsernameChipInput";
import { useSigninUser } from "@/features/users/useSigninUser";
import { createConversation } from "@/lib/api/conversations";
import { getErrorMessage } from "@/lib/api/client";
import { queryKeys } from "@/lib/query/keys";
import type { Conversation } from "@/types";

export function CreateConversationDialog() {
  const [isOpen, setIsOpen] = useState(false);
  const [usernames, setUsernames] = useState<string[]>([]);
  const [created, setCreated] = useState<Conversation | null>(null);
  const queryClient = useQueryClient();
  const navigate = useNavigate();
  const { username: ownUsername } = useSigninUser();

  function handleUsernamesChange(next: string[]) {
    if (ownUsername && next.some((name) => name.toLowerCase() === ownUsername.toLowerCase())) {
      toast.error("You're automatically included — no need to add yourself");
      setUsernames(next.filter((name) => name.toLowerCase() !== ownUsername.toLowerCase()));
      return;
    }
    setUsernames(next);
  }

  const mutation = useMutation({
    mutationFn: () => createConversation({ participantUsernames: usernames }),
    onSuccess: (conversation) => {
      setCreated(conversation);
      void queryClient.invalidateQueries({ queryKey: queryKeys.conversationListAll });
    },
    onError: (error) => toast.error(getErrorMessage(error)),
  });

  function reset() {
    setUsernames([]);
    setCreated(null);
  }

  function handleOpenChange(open: boolean) {
    setIsOpen(open);
    if (!open) {
      reset();
    }
  }

  function copyPublicId() {
    if (!created) return;
    void navigator.clipboard.writeText(created.publicId);
    toast.success("Join code copied");
  }

  return (
    <Dialog open={isOpen} onOpenChange={handleOpenChange}>
      <Tooltip>
        <TooltipTrigger asChild>
          <DialogTrigger asChild>
            <Button variant="ghost" size="icon" aria-label="New conversation">
              <Plus className="size-4" />
            </Button>
          </DialogTrigger>
        </TooltipTrigger>
        <TooltipContent>New conversation</TooltipContent>
      </Tooltip>
      <DialogContent>
        {created ? (
          <>
            <DialogHeader>
              <DialogTitle>Conversation created</DialogTitle>
              <DialogDescription>
                Share this join code with others so they can join "{created.displayName}".
              </DialogDescription>
            </DialogHeader>
            <div className="relative flex items-center justify-center rounded-lg border p-3">
              <span className="font-mono text-lg tracking-widest">{created.publicId}</span>
              <Button
                variant="ghost"
                size="icon"
                onClick={copyPublicId}
                aria-label="Copy join code"
                className="absolute right-2"
              >
                <Copy className="size-4" />
              </Button>
            </div>
            <DialogFooter>
              <Button
                onClick={() => {
                  setIsOpen(false);
                  reset();
                  navigate(`/conversations/${created.id}`);
                }}
              >
                Open conversation
              </Button>
            </DialogFooter>
          </>
        ) : (
          <>
            <DialogHeader>
              <DialogTitle>Start a conversation</DialogTitle>
              <DialogDescription>
                Add one participant for a direct message, or more for a group.
              </DialogDescription>
            </DialogHeader>
            <div className="flex flex-col gap-2">
              <Label htmlFor="participant-usernames">Participants (by username)</Label>
              <UsernameChipInput
                usernames={usernames}
                onChange={handleUsernamesChange}
                disabled={mutation.isPending}
              />
            </div>
            <DialogFooter>
              <Button
                onClick={() => mutation.mutate()}
                disabled={usernames.length === 0 || mutation.isPending}
              >
                {mutation.isPending ? "Creating..." : "Create"}
              </Button>
            </DialogFooter>
          </>
        )}
      </DialogContent>
    </Dialog>
  );
}
