/**
 * Shapes of API responses. Each mirrors a response DTO in PureFreshMilk.Api;
 * keep them in sync by hand (there is deliberately no shared .NET/TypeScript package).
 */

/** GET /api/v1/status — PureFreshMilk.Api.Features.Status.StatusResponse */
export interface ApiStatusResponse {
  service: string;
  version: string;
  /** ISO 8601 timestamp. */
  serverTime: string;
}
