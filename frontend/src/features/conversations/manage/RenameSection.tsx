import { useState } from "react";
import { toast } from "sonner";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { getErrorMessage } from "@/lib/api/client";
import { useRenameConversation } from "@/features/conversations/useConversationMutations";

const MAX_DISPLAY_NAME_LENGTH = 100;

export function RenameSection({
  conversationId,
  currentName,
}: {
  conversationId: string;
  currentName: string;
}) {
  const [displayName, setDisplayName] = useState(currentName);
  const mutation = useRenameConversation(conversationId);

  function handleSave() {
    const trimmed = displayName.trim();
    if (!trimmed || trimmed === currentName) return;
    mutation.mutate(trimmed, {
      onSuccess: () => toast.success("Conversation renamed"),
      onError: (error) => toast.error(getErrorMessage(error)),
    });
  }

  return (
    <section className="flex flex-col gap-2">
      <Label htmlFor="rename-input">Display name</Label>
      <div className="flex gap-2">
        <Input
          id="rename-input"
          value={displayName}
          onChange={(event) => setDisplayName(event.target.value.slice(0, MAX_DISPLAY_NAME_LENGTH))}
          maxLength={MAX_DISPLAY_NAME_LENGTH}
          disabled={mutation.isPending}
        />
        <Button
          onClick={handleSave}
          disabled={
            mutation.isPending || !displayName.trim() || displayName.trim() === currentName
          }
        >
          Save
        </Button>
      </div>
    </section>
  );
}
