import { useUsername } from "./useUsername";

/** Renders a resolved username for a userId, falling back to a shortened id while loading. */
export function Username({ userId }: { userId: string }) {
  const { displayName } = useUsername(userId);
  return <span>{displayName}</span>;
}
