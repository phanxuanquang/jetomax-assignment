import { useState } from "react";
import { MoreHorizontal, ShieldCheck, UserMinus } from "lucide-react";
import { toast } from "sonner";
import { Button } from "@/components/ui/button";
import { Label } from "@/components/ui/label";
import { Badge } from "@/components/ui/badge";
import {
  DropdownMenu,
  DropdownMenuContent,
  DropdownMenuItem,
  DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import { UsernameChipInput } from "@/components/UsernameChipInput";
import { useUsername } from "@/features/users/useUsername";
import { getErrorMessage } from "@/lib/api/client";
import {
  useAddParticipants,
  useRemoveParticipants,
  useTransferOwnership,
} from "@/features/conversations/useConversationMutations";

function ParticipantRow({
  userId,
  isOwner,
  onTransfer,
  onRemove,
  isBusy,
}: {
  userId: string;
  isOwner: boolean;
  onTransfer: (username: string) => void;
  onRemove: (username: string) => void;
  isBusy: boolean;
}) {
  const { username, displayName } = useUsername(userId);

  return (
    <li className="flex items-center justify-between gap-2 rounded-md border px-3 py-1.5">
      <span className="text-sm">
        {displayName}
        {isOwner && (
          <Badge variant="outline" className="ml-2">
            Owner
          </Badge>
        )}
      </span>
      {!isOwner && username && (
        <DropdownMenu>
          <DropdownMenuTrigger asChild>
            <Button
              variant="ghost"
              size="icon"
              className="size-6"
              disabled={isBusy}
              aria-label={`Actions for ${username}`}
            >
              <MoreHorizontal className="size-4" />
            </Button>
          </DropdownMenuTrigger>
          <DropdownMenuContent align="end">
            <DropdownMenuItem onClick={() => onTransfer(username)}>
              <ShieldCheck className="size-4" />
              Set as owner
            </DropdownMenuItem>
            <DropdownMenuItem variant="destructive" onClick={() => onRemove(username)}>
              <UserMinus className="size-4" />
              Remove
            </DropdownMenuItem>
          </DropdownMenuContent>
        </DropdownMenu>
      )}
    </li>
  );
}

export function ParticipantsSection({
  conversationId,
  participantUserIds,
  ownerId,
}: {
  conversationId: string;
  participantUserIds: string[];
  ownerId: string | null;
}) {
  const [newUsernames, setNewUsernames] = useState<string[]>([]);
  const addMutation = useAddParticipants(conversationId);
  const removeMutation = useRemoveParticipants(conversationId);
  const transferMutation = useTransferOwnership(conversationId);

  function handleAdd() {
    if (newUsernames.length === 0) return;
    addMutation.mutate(newUsernames, {
      onSuccess: () => {
        toast.success("Participants added");
        setNewUsernames([]);
      },
      onError: (error) => toast.error(getErrorMessage(error)),
    });
  }

  function handleRemove(username: string) {
    removeMutation.mutate([username], {
      onSuccess: () => toast.success(`Removed @${username}`),
      onError: (error) => toast.error(getErrorMessage(error)),
    });
  }

  function handleTransfer(username: string) {
    transferMutation.mutate(username, {
      onSuccess: () => toast.success(`Ownership transferred to @${username}`),
      onError: (error) => toast.error(getErrorMessage(error)),
    });
  }

  const isBusy = removeMutation.isPending || transferMutation.isPending;

  return (
    <section className="flex flex-col gap-3">
      <div>
        <Label>Participants</Label>
        <ul className="mt-2 flex flex-col gap-1.5">
          {participantUserIds.map((userId) => (
            <ParticipantRow
              key={userId}
              userId={userId}
              isOwner={userId === ownerId}
              onTransfer={handleTransfer}
              onRemove={handleRemove}
              isBusy={isBusy}
            />
          ))}
        </ul>
      </div>
      <div className="flex flex-col gap-2">
        <Label htmlFor="add-participants">Add participants</Label>
        <UsernameChipInput
          usernames={newUsernames}
          onChange={setNewUsernames}
          disabled={addMutation.isPending}
        />
        <Button
          onClick={handleAdd}
          disabled={addMutation.isPending || newUsernames.length === 0}
          className="self-start"
        >
          Add
        </Button>
      </div>
    </section>
  );
}
