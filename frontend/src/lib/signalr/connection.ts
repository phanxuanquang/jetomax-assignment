import * as signalR from "@microsoft/signalr";
import { getAccessToken } from "@/lib/supabase/client";
import { resolveApiBaseUrl } from "@/lib/apiBaseUrl";

export function createChatHubConnection(): signalR.HubConnection {
  const hubUrl = `${resolveApiBaseUrl()}/hub/chat`;

  return new signalR.HubConnectionBuilder()
    .withUrl(hubUrl, {
      accessTokenFactory: async () => (await getAccessToken()) ?? "",
    })
    .withAutomaticReconnect()
    .build();
}
