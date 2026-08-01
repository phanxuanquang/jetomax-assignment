import { apiClient } from "./client";
import type { UserMeta } from "@/types";

export async function fetchUserByIdOrUsername(idOrUsername: string): Promise<UserMeta> {
  const { data } = await apiClient.get<UserMeta>(
    `/api/users/${encodeURIComponent(idOrUsername)}`,
  );
  return data;
}

export async function fetchSigninUserMeta(): Promise<UserMeta> {
  const { data } = await apiClient.get<UserMeta>("/api/users/me");
  return data;
}
