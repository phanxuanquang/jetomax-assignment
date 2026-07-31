import { useState } from "react";
import { useMutation } from "@tanstack/react-query";
import { Sparkles, Loader2 } from "lucide-react";
import { Button } from "@/components/ui/button";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from "@/components/ui/dialog";
import { requestConversationSummary } from "@/lib/api/conversations";
import { getErrorMessage } from "@/lib/api/client";

export function SummaryDialog({ conversationId }: { conversationId: string }) {
  const [isOpen, setIsOpen] = useState(false);
  const mutation = useMutation({
    mutationFn: () => requestConversationSummary(conversationId),
  });

  function handleOpenChange(open: boolean) {
    setIsOpen(open);
    if (open) {
      mutation.mutate();
    } else {
      mutation.reset();
    }
  }

  return (
    <Dialog open={isOpen} onOpenChange={handleOpenChange}>
      <DialogTrigger asChild>
        <Button variant="ghost" size="icon" aria-label="Summarize conversation">
          <Sparkles className="size-4" />
        </Button>
      </DialogTrigger>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>Conversation summary</DialogTitle>
          <DialogDescription>An AI-generated summary of everything sent so far.</DialogDescription>
        </DialogHeader>
        {mutation.isPending && (
          <div className="flex items-center justify-center py-8">
            <Loader2 className="text-muted-foreground size-6 animate-spin" aria-hidden="true" />
          </div>
        )}
        {mutation.isError && (
          <p className="text-destructive text-sm">{getErrorMessage(mutation.error)}</p>
        )}
        {mutation.isSuccess && (
          <p className="text-sm whitespace-pre-wrap">{mutation.data}</p>
        )}
      </DialogContent>
    </Dialog>
  );
}
