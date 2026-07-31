import axios from "axios";
import { getAccessToken } from "@/lib/supabase/client";
import { resolveApiBaseUrl } from "@/lib/apiBaseUrl";
import { isApiError, type ApiError } from "@/types";

export const apiClient = axios.create({
  baseURL: resolveApiBaseUrl(),
});

apiClient.interceptors.request.use(async (config) => {
  const token = await getAccessToken();
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

apiClient.interceptors.response.use(
  (response) => response,
  (error) => {
    if (axios.isAxiosError(error) && isApiError(error.response?.data)) {
      return Promise.reject(error.response!.data as ApiError);
    }
    return Promise.reject(error);
  },
);

/** Human-readable message for anything thrown out of an apiClient call. */
export function getErrorMessage(error: unknown): string {
  if (isApiError(error)) {
    return error.message;
  }
  if (error instanceof Error) {
    return error.message;
  }
  return "Something went wrong. Please try again.";
}
