import { useState, type KeyboardEvent } from "react";
import { X } from "lucide-react";
import { Input } from "@/components/ui/input";
import { Badge } from "@/components/ui/badge";

interface UsernameChipInputProps {
  usernames: string[];
  onChange: (usernames: string[]) => void;
  placeholder?: string;
  disabled?: boolean;
}

/** Free-text username entry: type a username, press Enter or "," to add it as a chip. */
export function UsernameChipInput({
  usernames,
  onChange,
  placeholder = "Type a username and press Enter",
  disabled,
}: UsernameChipInputProps) {
  const [draft, setDraft] = useState("");

  function commitDraft() {
    const trimmed = draft.trim();
    if (trimmed && !usernames.includes(trimmed)) {
      onChange([...usernames, trimmed]);
    }
    setDraft("");
  }

  function handleKeyDown(event: KeyboardEvent<HTMLInputElement>) {
    if (event.key === "Enter" || event.key === ",") {
      event.preventDefault();
      commitDraft();
    } else if (event.key === "Backspace" && draft === "" && usernames.length > 0) {
      onChange(usernames.slice(0, -1));
    }
  }

  function removeUsername(username: string) {
    onChange(usernames.filter((existing) => existing !== username));
  }

  return (
    <div className="flex flex-col gap-2">
      <div className="flex flex-wrap gap-1.5">
        {usernames.map((username) => (
          <Badge key={username} variant="secondary" className="gap-1 pr-1">
            {username}
            <button
              type="button"
              onClick={() => removeUsername(username)}
              disabled={disabled}
              aria-label={`Remove ${username}`}
              className="rounded-full hover:bg-muted-foreground/20"
            >
              <X className="size-3" />
            </button>
          </Badge>
        ))}
      </div>
      <Input
        value={draft}
        onChange={(event) => setDraft(event.target.value)}
        onKeyDown={handleKeyDown}
        onBlur={commitDraft}
        placeholder={placeholder}
        disabled={disabled}
      />
    </div>
  );
}
