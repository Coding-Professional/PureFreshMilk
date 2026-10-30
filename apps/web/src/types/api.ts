/**
 * Shapes of API responses. Each mirrors a response DTO in PureFreshMilk.Api;
 * keep them in sync by hand (there is deliberately no shared .NET/TypeScript package).
 */

import type { Recipient } from "@/types/household";

/** GET /api/v1/status — PureFreshMilk.Api.Features.Status.StatusResponse */
export interface ApiStatusResponse {
  service: string;
  version: string;
  /** ISO 8601 timestamp. */
  serverTime: string;
}

/**
 * An error body: RFC 9457 Problem Details plus the `code` extension.
 * Every error the API raises on purpose has one; a 500 may not.
 */
export interface ApiProblem {
  status?: number;
  title?: string;
  detail?: string;
  /** Stable, machine-readable, for example "recipient-name-taken" or "household-not-set-up". */
  code?: string;
  /** Present on validation errors (400): input field name → messages. */
  errors?: Record<string, string[]>;
}

/** Every list endpoint answers with an object holding an `items` array. */
export interface ApiItems<T> {
  items: T[];
}

/** GET and PUT /api/v1/household — PureFreshMilk.Api.Features.Households.HouseholdResponse */
export interface ApiHousehold {
  id: string;
  name: string;
  /** An IANA time zone id, for example "Asia/Kolkata". */
  timeZoneId: string;
  /** The calendar date now, in the household's time zone, as YYYY-MM-DD. The server decides it. */
  today: string;
}

/** /api/v1/recipients — PureFreshMilk.Api.Features.Recipients.RecipientResponse */
export interface ApiRecipient extends Recipient {
  /** First day a delivery is expected, YYYY-MM-DD. */
  startsOn: string;
  /** First day without deliveries, YYYY-MM-DD, or null while the recipient is not archived. */
  archivedFrom: string | null;
}

/** /api/v1/milk-rates — PureFreshMilk.Api.Features.MilkRates.MilkRateResponse */
export interface ApiMilkRate {
  id: string;
  /** First day this price applies, YYYY-MM-DD. */
  effectiveFrom: string;
  /** The price of one litre, in whole paise. */
  pricePerLitrePaise: number;
}
