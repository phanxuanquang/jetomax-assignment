import { Avatar, AvatarFallback } from "@/components/ui/avatar";
import { avatarColorForSeed } from "@/lib/avatarColor";

/** Presentational only — given a display name, renders an initial on a deterministically-colored circle. */
export function UserAvatar({
  name,
  size = "default",
  className,
}: {
  name: string;
  size?: "sm" | "default" | "lg";
  className?: string;
}) {
  const initial = name.trim().charAt(0).toUpperCase() || "?";

  return (
    <Avatar size={size} className={className}>
      <AvatarFallback
        style={{ backgroundColor: avatarColorForSeed(name), color: "white" }}
        className="font-medium"
      >
        {initial}
      </AvatarFallback>
    </Avatar>
  );
}
