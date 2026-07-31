import { Users } from "lucide-react";
import { Avatar, AvatarFallback } from "@/components/ui/avatar";
import { ResolvedUserAvatar } from "@/features/users/ResolvedUserAvatar";
import { useSession } from "@/features/auth/SessionContext";
import { avatarColorForSeed } from "@/lib/avatarColor";
import type { Conversation } from "@/types";

export function ConversationAvatar({
  conversation,
  size = "default",
}: {
  conversation: Conversation;
  size?: "sm" | "default" | "lg";
}) {
  const { user } = useSession();
  const isDirect = conversation.participantUserIds.length === 2;

  if (isDirect) {
    const otherUserId = conversation.participantUserIds.find((id) => id !== user?.id);
    if (otherUserId) {
      return <ResolvedUserAvatar userId={otherUserId} size={size} />;
    }
  }

  return (
    <Avatar size={size}>
      <AvatarFallback style={{ backgroundColor: avatarColorForSeed(conversation.id) }}>
        <Users className="size-1/2 text-white" />
      </AvatarFallback>
    </Avatar>
  );
}
