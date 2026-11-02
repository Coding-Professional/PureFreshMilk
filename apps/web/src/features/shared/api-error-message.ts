import { ApiError, getApiBaseUrl } from "@/services/api-client";

/** A sentence a person can act on, for any failure of an API call. */
export function describeApiError(error: unknown): string {
  if (error instanceof ApiError) {
    switch (error.kind) {
      case "not-configured":
        return "No API address is set. Add NEXT_PUBLIC_API_BASE_URL to apps/web/.env.local, then restart the dev server.";
      case "unreachable":
        return `Can't reach the API at ${getApiBaseUrl()}. Start it (see docs/development.md), then try again.`;
      case "http":
        return (
          error.problem?.detail ??
          firstValidationMessage(error) ??
          `The API answered with HTTP ${error.status}. Check the API logs.`
        );
    }
  }
  return "Something went wrong. Check the browser console for details.";
}

/** A rejected request names the field it is about; when no form field can show that, say it in words. */
function firstValidationMessage(error: ApiError): string | undefined {
  return Object.values(error.problem?.errors ?? {})[0]?.[0];
}
