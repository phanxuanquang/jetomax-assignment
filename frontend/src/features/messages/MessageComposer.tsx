import { useRef, useState, type KeyboardEvent } from "react";
import { ImagePlus, Loader2, Send } from "lucide-react";
import { toast } from "sonner";
import { Button } from "@/components/ui/button";
import { Textarea } from "@/components/ui/textarea";
import { useHub } from "@/features/realtime/HubProvider";
import { uploadConversationImage } from "@/lib/supabase/storage";
import { getErrorMessage } from "@/lib/api/client";

export function MessageComposer({
  conversationId,
  disabled,
}: {
  conversationId: string;
  disabled?: boolean;
}) {
  const { sendTextMessage, sendImageMessage } = useHub();
  const [text, setText] = useState("");
  const [isSending, setIsSending] = useState(false);
  const [isUploadingImage, setIsUploadingImage] = useState(false);
  const fileInputRef = useRef<HTMLInputElement>(null);

  const isBusy = isSending || isUploadingImage || disabled;

  async function handleSendText() {
    const trimmed = text.trim();
    if (!trimmed || isBusy) return;

    setIsSending(true);
    try {
      await sendTextMessage(conversationId, trimmed);
      setText("");
    } catch (error) {
      toast.error(getErrorMessage(error));
    } finally {
      setIsSending(false);
    }
  }

  function handleKeyDown(event: KeyboardEvent<HTMLTextAreaElement>) {
    if (event.key === "Enter" && !event.shiftKey) {
      event.preventDefault();
      void handleSendText();
    }
  }

  async function handleFileSelected(event: React.ChangeEvent<HTMLInputElement>) {
    const file = event.target.files?.[0];
    event.target.value = "";
    if (!file) return;

    setIsUploadingImage(true);
    try {
      const storedPath = await uploadConversationImage(conversationId, file);
      await sendImageMessage(conversationId, storedPath);
    } catch (error) {
      toast.error(getErrorMessage(error));
    } finally {
      setIsUploadingImage(false);
    }
  }

  return (
    <div className="flex items-center gap-2 border-t p-3">
      <input
        ref={fileInputRef}
        type="file"
        accept="image/*"
        className="hidden"
        onChange={handleFileSelected}
      />
      <div className="bg-muted flex flex-1 items-center gap-1 rounded-3xl p-1 pl-2">
        <Button
          type="button"
          variant="ghost"
          size="icon"
          className="shrink-0 rounded-full"
          onClick={() => fileInputRef.current?.click()}
          disabled={isBusy}
          aria-label="Send an image"
        >
          {isUploadingImage ? (
            <Loader2 className="size-4 animate-spin" />
          ) : (
            <ImagePlus className="size-4" />
          )}
        </Button>
        <Textarea
          value={text}
          onChange={(event) => setText(event.target.value)}
          onKeyDown={handleKeyDown}
          placeholder={disabled ? "This conversation is read-only" : "Message"}
          disabled={isBusy}
          rows={1}
          className="max-h-32 min-h-8 flex-1 resize-none border-none bg-transparent px-1 py-1.5 shadow-none focus-visible:ring-0 dark:bg-transparent"
        />
      </div>
      <Button
        type="button"
        size="icon"
        className="shrink-0 rounded-full"
        onClick={handleSendText}
        disabled={isBusy || text.trim().length === 0}
        aria-label="Send message"
      >
        <Send className="size-4" />
      </Button>
    </div>
  );
}
