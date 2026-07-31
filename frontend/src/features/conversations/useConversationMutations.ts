import { useMutation, useQueryClient } from "@tanstack/react-query";
import {
  addParticipants,
  leaveConversation,
  removeParticipants,
  renameConversation,
  setConversationReadonly,
  transferOwnership,
} from "@/lib/api/conversations";
import { queryKeys } from "@/lib/query/keys";
import { removeConversationFromCache } from "@/features/realtime/cachePatchers";
import type { LeaveConversationRequest } from "@/types";

function useInvalidateConversationsOnSuccess() {
  const queryClient = useQueryClient();
  return () => queryClient.invalidateQueries({ queryKey: queryKeys.conversationListAll });
}

export function useRenameConversation(conversationId: string) {
  const invalidate = useInvalidateConversationsOnSuccess();
  return useMutation({
    mutationFn: (displayName: string) => renameConversation(conversationId, { displayName }),
    onSuccess: invalidate,
  });
}

export function useSetConversationReadonly(conversationId: string) {
  const invalidate = useInvalidateConversationsOnSuccess();
  return useMutation({
    mutationFn: (isReadonly: boolean) => setConversationReadonly(conversationId, { isReadonly }),
    onSuccess: invalidate,
  });
}

export function useTransferOwnership(conversationId: string) {
  const invalidate = useInvalidateConversationsOnSuccess();
  return useMutation({
    mutationFn: (newOwnerUsername: string) =>
      transferOwnership(conversationId, { newOwnerUsername }),
    onSuccess: invalidate,
  });
}

export function useAddParticipants(conversationId: string) {
  const invalidate = useInvalidateConversationsOnSuccess();
  return useMutation({
    mutationFn: (usernames: string[]) => addParticipants(conversationId, { usernames }),
    onSuccess: invalidate,
  });
}

export function useRemoveParticipants(conversationId: string) {
  const invalidate = useInvalidateConversationsOnSuccess();
  return useMutation({
    mutationFn: (usernames: string[]) => removeParticipants(conversationId, { usernames }),
    onSuccess: invalidate,
  });
}

export function useLeaveConversation(conversationId: string) {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (request: LeaveConversationRequest) => leaveConversation(conversationId, request),
    onSuccess: () => removeConversationFromCache(queryClient, conversationId),
  });
}
