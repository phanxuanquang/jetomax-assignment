import { toast } from "sonner";
import { Label } from "@/components/ui/label";
import { Switch } from "@/components/ui/switch";
import { getErrorMessage } from "@/lib/api/client";
import { useSetConversationReadonly } from "@/features/conversations/useConversationMutations";

export function ReadonlySection({
  conversationId,
  isReadonly,
}: {
  conversationId: string;
  isReadonly: boolean;
}) {
  const mutation = useSetConversationReadonly(conversationId);

  function handleToggle(checked: boolean) {
    mutation.mutate(checked, {
      onError: (error) => toast.error(getErrorMessage(error)),
    });
  }

  return (
    <section className="flex items-center justify-between gap-2">
      <div className="flex flex-col gap-0.5">
        <Label htmlFor="readonly-toggle">Read-only</Label>
        <p className="text-muted-foreground text-xs">Only you can send messages.</p>
      </div>
      <Switch
        id="readonly-toggle"
        checked={isReadonly}
        onCheckedChange={handleToggle}
        disabled={mutation.isPending}
      />
    </section>
  );
}
