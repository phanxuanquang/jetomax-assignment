export interface Conversation {
  id: string;
  publicId: string;
  displayName: string;
  ownerId: string | null;
  isReadonly: boolean;
  createdTime: string;
  lastMessageTime: string | null;
  participantUserIds: string[];
}

export interface CreateConversationRequest {
  participantUsernames: string[];
}

export interface JoinConversationRequest {
  publicId: string;
}

export interface RenameConversationRequest {
  displayName: string;
}

export interface SetReadonlyRequest {
  isReadonly: boolean;
}

export interface TransferOwnershipRequest {
  newOwnerUsername: string;
}

export interface ParticipantsRequest {
  usernames: string[];
}

export type LeaveMode = "delete" | "freeze";

export interface LeaveConversationRequest {
  mode?: LeaveMode;
}
