import type { ApiItems, ApiRecipient } from "@/types/api";
import { apiGet, apiPost, apiPut, type ApiRequestOptions } from "./api-client";

export interface ListRecipientsOptions extends ApiRequestOptions {
  /** Also return recipients who have been archived. */
  includeArchived?: boolean;
}

/** The household's recipients, ordered by name by the server. */
export async function listRecipients({
  includeArchived = false,
  ...options
}: ListRecipientsOptions = {}): Promise<ApiRecipient[]> {
  const query = includeArchived ? "?includeArchived=true" : "";
  const result = await apiGet<ApiItems<ApiRecipient>>(`/recipients${query}`, options);
  return result.items;
}

export interface RecipientInput {
  name: string;
  /** Usual daily quantity, in whole millilitres. */
  expectedMl: number;
  /** YYYY-MM-DD, the first day a delivery is expected. */
  startsOn: string;
}

export function addRecipient(
  input: RecipientInput,
  options?: ApiRequestOptions,
): Promise<ApiRecipient> {
  return apiPost<ApiRecipient>("/recipients", input, options);
}

export function updateRecipient(
  id: string,
  input: RecipientInput,
  options?: ApiRequestOptions,
): Promise<ApiRecipient> {
  return apiPut<ApiRecipient>(`/recipients/${encodeURIComponent(id)}`, input, options);
}

/** Stops deliveries from `archivedFrom` (YYYY-MM-DD; default: today). The recipient's history is kept. */
export function archiveRecipient(
  id: string,
  archivedFrom?: string,
  options?: ApiRequestOptions,
): Promise<ApiRecipient> {
  return apiPost<ApiRecipient>(
    `/recipients/${encodeURIComponent(id)}/archive`,
    archivedFrom ? { archivedFrom } : undefined,
    options,
  );
}

export function unarchiveRecipient(id: string, options?: ApiRequestOptions): Promise<ApiRecipient> {
  return apiPost<ApiRecipient>(
    `/recipients/${encodeURIComponent(id)}/unarchive`,
    undefined,
    options,
  );
}
