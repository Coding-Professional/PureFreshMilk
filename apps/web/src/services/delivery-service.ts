import type { ApiDaySheet, ApiDaySummary, ApiItems } from "@/types/api";
import { apiDelete, apiGet, apiPost, apiPut, type ApiRequestOptions } from "./api-client";

export function getDaySheet(date: string, options?: ApiRequestOptions): Promise<ApiDaySheet> {
  return apiGet<ApiDaySheet>(`/day-sheets/${encodeURIComponent(date)}`, options);
}

export function recordDelivery(
  recipientId: string,
  date: string,
  status: "delivered" | "missed",
  deliveredMl?: number,
  options?: ApiRequestOptions,
): Promise<unknown> {
  return apiPut(
    `/recipients/${encodeURIComponent(recipientId)}/deliveries/${encodeURIComponent(date)}`,
    { status, ...(deliveredMl === undefined ? {} : { deliveredMl }) },
    options,
  );
}

export function clearDelivery(
  recipientId: string,
  date: string,
  options?: ApiRequestOptions,
): Promise<void> {
  return apiDelete(
    `/recipients/${encodeURIComponent(recipientId)}/deliveries/${encodeURIComponent(date)}`,
    options,
  );
}

export function deliverRemaining(date: string, options?: ApiRequestOptions): Promise<ApiDaySheet> {
  return apiPost<ApiDaySheet>(
    `/day-sheets/${encodeURIComponent(date)}/deliver-remaining`,
    undefined,
    options,
  );
}

export async function listDaySummaries(
  from: string,
  to: string,
  options?: ApiRequestOptions,
): Promise<ApiDaySummary[]> {
  const response = await apiGet<ApiItems<ApiDaySummary>>(
    `/day-summaries?from=${encodeURIComponent(from)}&to=${encodeURIComponent(to)}`,
    options,
  );
  return response.items;
}
