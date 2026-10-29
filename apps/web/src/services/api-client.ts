/**
 * The single place the web app talks to the ASP.NET Core API (/api/v1).
 * Components never call fetch directly; they go through a service module.
 */

const API_PREFIX = "/api/v1";
const DEFAULT_TIMEOUT_MS = 5000;

export type ApiErrorKind = "not-configured" | "unreachable" | "http";

export class ApiError extends Error {
  constructor(
    message: string,
    readonly kind: ApiErrorKind,
    readonly status?: number,
    options?: ErrorOptions,
  ) {
    super(message, options);
    this.name = "ApiError";
  }
}

/** Base URL from NEXT_PUBLIC_API_BASE_URL without a trailing slash, or null when unset. */
export function getApiBaseUrl(): string | null {
  const value = process.env.NEXT_PUBLIC_API_BASE_URL?.trim();
  return value ? value.replace(/\/+$/, "") : null;
}

export interface ApiRequestOptions {
  signal?: AbortSignal;
  timeoutMs?: number;
}

export async function apiGet<T>(path: string, options: ApiRequestOptions = {}): Promise<T> {
  const baseUrl = getApiBaseUrl();
  if (!baseUrl) {
    throw new ApiError("NEXT_PUBLIC_API_BASE_URL is not set.", "not-configured");
  }

  const timeout = AbortSignal.timeout(options.timeoutMs ?? DEFAULT_TIMEOUT_MS);
  const signal = options.signal ? AbortSignal.any([options.signal, timeout]) : timeout;

  let response: Response;
  try {
    response = await fetch(`${baseUrl}${API_PREFIX}${path}`, {
      headers: { Accept: "application/json" },
      signal,
    });
  } catch (error) {
    throw new ApiError(`Could not reach the API at ${baseUrl}.`, "unreachable", undefined, {
      cause: error,
    });
  }

  if (!response.ok) {
    throw new ApiError(`The API responded with HTTP ${response.status}.`, "http", response.status);
  }

  return (await response.json()) as T;
}
