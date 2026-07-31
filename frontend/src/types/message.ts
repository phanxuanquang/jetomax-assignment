export type MessageType = "Text" | "Image";

export interface Message {
  id: string;
  conversationId: string;
  senderUserId: string;
  type: MessageType;
  repliesToMessageId: string | null;
  sentAt: string;
  content: string | null;
  imageUrl: string | null;
  caption: string | null;
}
