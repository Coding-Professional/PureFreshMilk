import type { ApiItems, ApiMilkRate } from "@/types/api";
import { apiDelete, apiGet, apiPost, hasProblemCode, type ApiRequestOptions } from "./api-client";

/** Every rate of the household, newest effective date first. */
export async function listMilkRates(options?: ApiRequestOptions): Promise<ApiMilkRate[]> {
  const result = await apiGet<ApiItems<ApiMilkRate>>("/milk-rates", options);
  return result.items;
}

/**
 * The rate in force on `on` (YYYY-MM-DD; default: today in the household's time zone), decided by the server.
 * `null` means no price has started yet: that is not the same as a price of zero.
 */
export async function getMilkRateInEffect(
  on?: string,
  options?: ApiRequestOptions,
): Promise<ApiMilkRate | null> {
  const query = on ? `?on=${encodeURIComponent(on)}` : "";

  try {
    return await apiGet<ApiMilkRate>(`/milk-rates/in-effect${query}`, options);
  } catch (error) {
    if (hasProblemCode(error, "no-rate-in-effect")) return null;
    throw error;
  }
}

export interface MilkRateInput {
  /** YYYY-MM-DD, the first day the price applies. It may be in the past. */
  effectiveFrom: string;
  /** The price of one litre, in whole paise. */
  pricePerLitrePaise: number;
}

export function addMilkRate(
  input: MilkRateInput,
  options?: ApiRequestOptions,
): Promise<ApiMilkRate> {
  return apiPost<ApiMilkRate>("/milk-rates", input, options);
}

export function deleteMilkRate(id: string, options?: ApiRequestOptions): Promise<void> {
  return apiDelete(`/milk-rates/${encodeURIComponent(id)}`, options);
}
