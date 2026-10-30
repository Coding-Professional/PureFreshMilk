import type { ApiHousehold } from "@/types/api";
import { apiGet, apiPut, type ApiRequestOptions } from "./api-client";

/** Rejects with a "household-not-set-up" problem (HTTP 404) until the household has been set up. */
export function getHousehold(options?: ApiRequestOptions): Promise<ApiHousehold> {
  return apiGet<ApiHousehold>("/household", options);
}

export interface HouseholdInput {
  name: string;
  /** An IANA id. Left out, the API keeps the current one (Asia/Kolkata for a new household). */
  timeZoneId?: string;
}

/** Creates the household the first time and updates it afterwards. */
export function setUpHousehold(
  input: HouseholdInput,
  options?: ApiRequestOptions,
): Promise<ApiHousehold> {
  return apiPut<ApiHousehold>("/household", input, options);
}
