import { apiClient } from "./client";
import type { UserMeta } from "@/types";

export async function fetchUserByIdOrUsername(idOrUsername: string): Promise<UserMeta> {
  const { data } = await apiClient.get<UserMeta>(
    `/api/users/${encodeURIComponent(idOrUsername)}`,
  );
  return data;
}
