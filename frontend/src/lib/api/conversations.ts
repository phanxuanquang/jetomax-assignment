import { apiClient } from "./client";
import type {
  Conversation,
  CreateConversationRequest,
  JoinConversationRequest,
  LeaveConversationRequest,
  Message,
  ParticipantsRequest,
  RenameConversationRequest,
  SetReadonlyRequest,
  TransferOwnershipRequest,
} from "@/types";

export async function fetchConversations(query: string): Promise<Conversation[]> {
  const { data } = await apiClient.get<Conversation[]>("/api/conversations", {
    params: query ? { q: query } : undefined,
  });
  return data;
}

export async function createConversation(
  request: CreateConversationRequest,
): Promise<Conversation> {
  const { data } = await apiClient.post<Conversation>("/api/conversations", request);
  return data;
}

export async function joinConversation(request: JoinConversationRequest): Promise<void> {
  await apiClient.post("/api/conversations/join", request);
}

export async function renameConversation(
  conversationId: string,
  request: RenameConversationRequest,
): Promise<void> {
  await apiClient.patch(`/api/conversations/${conversationId}/name`, request);
}

export async function setConversationReadonly(
  conversationId: string,
  request: SetReadonlyRequest,
): Promise<void> {
  await apiClient.patch(`/api/conversations/${conversationId}/readonly`, request);
}

export async function transferOwnership(
  conversationId: string,
  request: TransferOwnershipRequest,
): Promise<void> {
  await apiClient.post(`/api/conversations/${conversationId}/transfer`, request);
}

const DEFAULT_MESSAGE_PAGE_SIZE = 50;

export async function fetchMessages(
  conversationId: string,
  options?: { before?: string; limit?: number },
): Promise<Message[]> {
  const { data } = await apiClient.get<Message[]>(
    `/api/conversations/${conversationId}/messages`,
    {
      params: {
        before: options?.before,
        limit: options?.limit ?? DEFAULT_MESSAGE_PAGE_SIZE,
      },
    },
  );
  return data;
}

export async function addParticipants(
  conversationId: string,
  request: ParticipantsRequest,
): Promise<void> {
  await apiClient.post(`/api/conversations/${conversationId}/participants`, request);
}

export async function removeParticipants(
  conversationId: string,
  request: ParticipantsRequest,
): Promise<void> {
  await apiClient.delete(`/api/conversations/${conversationId}/participants`, {
    data: request,
  });
}

export async function leaveConversation(
  conversationId: string,
  request: LeaveConversationRequest,
): Promise<void> {
  await apiClient.post(`/api/conversations/${conversationId}/leave`, request);
}

export async function requestConversationSummary(conversationId: string): Promise<string> {
  const { data } = await apiClient.post<string>(`/api/conversations/${conversationId}/summary`);
  return data;
}
