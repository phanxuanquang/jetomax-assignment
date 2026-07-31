import { useState } from "react";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { LogIn } from "lucide-react";
import { toast } from "sonner";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
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
import { joinConversation } from "@/lib/api/conversations";
import { getErrorMessage } from "@/lib/api/client";
import { queryKeys } from "@/lib/query/keys";

export function JoinConversationDialog() {
  const [isOpen, setIsOpen] = useState(false);
  const [publicId, setPublicId] = useState("");
  const queryClient = useQueryClient();

  const mutation = useMutation({
    mutationFn: () => joinConversation({ publicId: publicId.trim() }),
    onSuccess: () => {
      toast.success("Joined conversation");
      void queryClient.invalidateQueries({ queryKey: queryKeys.conversationListAll });
      setIsOpen(false);
      setPublicId("");
    },
    onError: (error) => toast.error(getErrorMessage(error)),
  });

  return (
    <Dialog
      open={isOpen}
      onOpenChange={(open) => {
        setIsOpen(open);
        if (!open) setPublicId("");
      }}
    >
      <Tooltip>
        <TooltipTrigger asChild>
          <DialogTrigger asChild>
            <Button variant="ghost" size="icon" aria-label="Join conversation">
              <LogIn className="size-4" />
            </Button>
          </DialogTrigger>
        </TooltipTrigger>
        <TooltipContent>Join by code</TooltipContent>
      </Tooltip>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>Join a conversation</DialogTitle>
          <DialogDescription>Enter the 6-character code someone shared with you.</DialogDescription>
        </DialogHeader>
        <div className="flex flex-col gap-2">
          <Label htmlFor="public-id">Join code</Label>
          <Input
            id="public-id"
            value={publicId}
            onChange={(event) => setPublicId(event.target.value.toUpperCase())}
            placeholder="ABC123"
            maxLength={6}
            className="font-mono tracking-widest"
            disabled={mutation.isPending}
          />
        </div>
        <DialogFooter>
          <Button
            onClick={() => mutation.mutate()}
            disabled={publicId.trim().length === 0 || mutation.isPending}
          >
            {mutation.isPending ? "Joining..." : "Join"}
          </Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
