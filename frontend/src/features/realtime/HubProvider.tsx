import {
  createContext,
  useContext,
  useEffect,
  useRef,
  useState,
  type ReactNode,
} from "react";
import * as signalR from "@microsoft/signalr";
import { useQueryClient } from "@tanstack/react-query";
import { useLocation, useNavigate } from "react-router-dom";
import { createChatHubConnection } from "@/lib/signalr/connection";
import { useSession } from "@/features/auth/SessionContext";
import type { Message, MemberChangedEvent } from "@/types";
import {
  bumpConversationLastMessageTime,
  removeConversationFromCache,
  invalidateConversationLists,
  upsertMessageInCache,
} from "./cachePatchers";

interface HubContextValue {
  sendTextMessage: (conversationId: string, text: string) => Promise<void>;
  sendImageMessage: (conversationId: string, imageUrl: string) => Promise<void>;
  isConnected: boolean;
}

const HubContext = createContext<HubContextValue | undefined>(undefined);

export function HubProvider({ children }: { children: ReactNode }) {
  const { session } = useSession();
  const queryClient = useQueryClient();
  const navigate = useNavigate();
  const location = useLocation();
  const connectionRef = useRef<signalR.HubConnection | null>(null);
  const locationRef = useRef(location.pathname);
  locationRef.current = location.pathname;
  const [isReady, setIsReady] = useState(false);

  const userId = session?.user.id;

  useEffect(() => {
    if (!userId) {
      setIsReady(false);
      return;
    }

    const connection = createChatHubConnection();
    connectionRef.current = connection;

    connection.on("NewMessage", (message: Message) => {
      upsertMessageInCache(queryClient, message);
      bumpConversationLastMessageTime(queryClient, message.conversationId, message.sentAt);
    });

    connection.on(
      "MemberChanged",
      (conversationId: string, changedUserId: string, action: MemberChangedEvent["action"]) => {
        if (changedUserId === userId && action === "Left") {
          removeConversationFromCache(queryClient, conversationId);
          if (locationRef.current === `/conversations/${conversationId}`) {
            navigate("/", { replace: true });
          }
          return;
        }
        void invalidateConversationLists(queryClient);
      },
    );

    connection.onreconnected(() => {
      void queryClient.invalidateQueries({
        predicate: (query) =>
          query.queryKey[0] === "conversationList" || query.queryKey[0] === "conversation",
      });
    });

    connection.start().then(
      () => setIsReady(true),
      (error) => console.error("SignalR connection failed to start", error),
    );

    return () => {
      setIsReady(false);
      connectionRef.current = null;
      void connection.stop();
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [userId]);

  async function invoke(method: string, ...args: unknown[]) {
    const connection = connectionRef.current;
    if (!connection || connection.state !== signalR.HubConnectionState.Connected) {
      throw new Error("Not connected. Check your network and try again.");
    }
    await connection.invoke(method, ...args);
  }

  const value: HubContextValue = {
    sendTextMessage: (conversationId, text) => invoke("SendMessage", conversationId, text),
    sendImageMessage: (conversationId, imageUrl) => invoke("SendImage", conversationId, imageUrl),
    isConnected: isReady,
  };

  return <HubContext.Provider value={value}>{children}</HubContext.Provider>;
}

export function useHub(): HubContextValue {
  const context = useContext(HubContext);
  if (!context) {
    throw new Error("useHub must be used within a HubProvider");
  }
  return context;
}
