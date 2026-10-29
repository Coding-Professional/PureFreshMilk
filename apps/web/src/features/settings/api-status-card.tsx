"use client";

import { useEffect, useState } from "react";
import { LoaderCircle, PlugZap, RefreshCw } from "lucide-react";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { ApiError, getApiBaseUrl } from "@/services/api-client";
import { getApiStatus } from "@/services/status-service";
import type { ApiStatusResponse } from "@/types/api";

type CheckState =
  | { kind: "checking" }
  | { kind: "connected"; status: ApiStatusResponse }
  | { kind: "failed"; error: unknown };

/** Developer diagnostic: can the web app reach the ASP.NET Core API? */
export function ApiStatusCard() {
  const [state, setState] = useState<CheckState>({ kind: "checking" });
  const [attempt, setAttempt] = useState(0);

  useEffect(() => {
    const controller = new AbortController();
    getApiStatus({ signal: controller.signal })
      .then((status) => setState({ kind: "connected", status }))
      .catch((error: unknown) => {
        if (!controller.signal.aborted) setState({ kind: "failed", error });
      });
    return () => controller.abort();
  }, [attempt]);

  const checkAgain = () => {
    setState({ kind: "checking" });
    setAttempt((value) => value + 1);
  };

  return (
    <Card className="p-5 sm:p-6">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <div className="flex items-center gap-3">
          <span className="flex size-11 items-center justify-center rounded-2xl bg-canvas text-ink-muted">
            <PlugZap aria-hidden className="size-5" />
          </span>
          <div>
            <h2 className="text-lg font-bold">API connection</h2>
            <p className="text-sm text-ink-muted">For developers running the backend locally.</p>
          </div>
        </div>
        <Button variant="secondary" onClick={checkAgain} disabled={state.kind === "checking"}>
          <RefreshCw aria-hidden />
          Check again
        </Button>
      </div>

      <div aria-live="polite" className="mt-4 rounded-xl bg-canvas px-4 py-3 text-sm">
        <StatusMessage state={state} />
      </div>
    </Card>
  );
}

function StatusMessage({ state }: { state: CheckState }) {
  switch (state.kind) {
    case "checking":
      return (
        <p className="flex items-center gap-2 text-ink-muted">
          <LoaderCircle aria-hidden className="size-4 animate-spin" />
          Checking the API…
        </p>
      );
    case "connected":
      return (
        <p className="flex flex-wrap items-center gap-2">
          <Badge tone="accent">Connected</Badge>
          <span className="text-ink-muted">
            {state.status.service} {state.status.version} · server time{" "}
            {new Date(state.status.serverTime).toLocaleTimeString()}
          </span>
        </p>
      );
    case "failed":
      return <p className="text-ink-muted">{failureMessage(state.error)}</p>;
  }
}

function failureMessage(error: unknown): string {
  if (error instanceof ApiError) {
    switch (error.kind) {
      case "not-configured":
        return "No API address is set. Add NEXT_PUBLIC_API_BASE_URL to apps/web/.env.local, then restart the dev server.";
      case "unreachable":
        return `Can't reach the API at ${getApiBaseUrl()}. Start it (see docs/development.md), then check again. The demo screens work without it.`;
      case "http":
        return `The API answered with HTTP ${error.status}. Check the API logs.`;
    }
  }
  return "The API check failed unexpectedly. Check the browser console for details.";
}
