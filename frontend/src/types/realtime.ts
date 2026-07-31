export type MemberChangeAction = "Added" | "Left";

export interface MemberChangedEvent {
  conversationId: string;
  userId: string;
  action: MemberChangeAction;
}
