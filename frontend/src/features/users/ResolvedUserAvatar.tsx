import { UserAvatar } from "@/components/UserAvatar";
import { useUsername } from "./useUsername";

/** Resolves a userId to a display name, then renders it as an avatar. */
export function ResolvedUserAvatar({
  userId,
  size,
  className,
}: {
  userId: string;
  size?: "sm" | "default" | "lg";
  className?: string;
}) {
  const { displayName } = useUsername(userId);
  return <UserAvatar name={displayName || "?"} size={size} className={className} />;
}
