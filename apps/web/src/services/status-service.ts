import type { ApiStatusResponse } from "@/types/api";
import { apiGet, type ApiRequestOptions } from "./api-client";

export function getApiStatus(options?: ApiRequestOptions): Promise<ApiStatusResponse> {
  return apiGet<ApiStatusResponse>("/status", options);
}
