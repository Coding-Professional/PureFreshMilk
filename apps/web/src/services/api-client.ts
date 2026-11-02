/**
 * The single place the web app talks to the ASP.NET Core API (/api/v1).
 * Components never call fetch directly; they go through a service module.
 */

import type { ApiProblem } from "@/types/api";

const API_PREFIX = "/api/v1";
const DEFAULT_TIMEOUT_MS = 5000;

export type ApiErrorKind = "not-configured" | "unreachable" | "http";

interface ApiErrorOptions extends ErrorOptions {
  /** The Problem Details body of an error response, when the API sent one. */
  problem?: ApiProblem;
}

export class ApiError extends Error {
  readonly problem?: ApiProblem;

  constructor(
    message: string,
    readonly kind: ApiErrorKind,
    readonly status?: number,
    options?: ApiErrorOptions,
  ) {
    super(message, options);
    this.name = "ApiError";
    this.problem = options?.problem;
  }
}

/** Whether `error` is an API error answered with the given stable `code`, for example "recipient-name-taken". */
export function hasProblemCode(error: unknown, code: string): error is ApiError {
  return error instanceof ApiError && error.problem?.code === code;
}

/** The first validation message for each input field of a 400 response, keyed by the API's field name. */
export function fieldErrors(error: unknown): Record<string, string> {
  if (!(error instanceof ApiError) || !error.problem?.errors) return {};

  const messages: Record<string, string> = {};
  for (const [field, list] of Object.entries(error.problem.errors)) {
    const first = list[0];
    if (first) messages[field] = first;
  }
  return messages;
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

export function apiGet<T>(path: string, options: ApiRequestOptions = {}): Promise<T> {
  return request<T>("GET", path, undefined, options);
}

/** POST, with an optional JSON body. */
export function apiPost<T>(
  path: string,
  body?: unknown,
  options: ApiRequestOptions = {},
): Promise<T> {
  return request<T>("POST", path, body, options);
}

export function apiPut<T>(
  path: string,
  body: unknown,
  options: ApiRequestOptions = {},
): Promise<T> {
  return request<T>("PUT", path, body, options);
}

export async function apiDelete(path: string, options: ApiRequestOptions = {}): Promise<void> {
  await request<undefined>("DELETE", path, undefined, options);
}

async function request<T>(
  method: string,
  path: string,
  body: unknown,
  options: ApiRequestOptions,
): Promise<T> {
  const baseUrl = getApiBaseUrl();
  if (!baseUrl) {
    throw new ApiError("NEXT_PUBLIC_API_BASE_URL is not set.", "not-configured");
  }

  const timeout = AbortSignal.timeout(options.timeoutMs ?? DEFAULT_TIMEOUT_MS);
  const signal = options.signal ? AbortSignal.any([options.signal, timeout]) : timeout;

  const headers: Record<string, string> = { Accept: "application/json" };
  const init: RequestInit = { method, headers, signal };
  if (body !== undefined) {
    headers["Content-Type"] = "application/json";
    init.body = JSON.stringify(body);
  }

  let response: Response;
  try {
    response = await fetch(`${baseUrl}${API_PREFIX}${path}`, init);
  } catch (error) {
    throw new ApiError(`Could not reach the API at ${baseUrl}.`, "unreachable", undefined, {
      cause: error,
    });
  }

  if (!response.ok) {
    throw new ApiError(`The API responded with HTTP ${response.status}.`, "http", response.status, {
      problem: await readProblem(response),
    });
  }

  if (response.status === 204) return undefined as T;
  return (await response.json()) as T;
}

async function readProblem(response: Response): Promise<ApiProblem | undefined> {
  if (!response.headers.get("content-type")?.includes("json")) return undefined;

  try {
    return (await response.json()) as ApiProblem;
  } catch {
    return undefined;
  }
}
