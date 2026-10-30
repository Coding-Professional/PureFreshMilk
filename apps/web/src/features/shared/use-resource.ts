"use client";

import { useCallback, useEffect, useState } from "react";

export type Resource<T> =
  { kind: "loading" } | { kind: "ready"; data: T } | { kind: "failed"; error: unknown };

/**
 * Loads something from the API and keeps it in step with what the screen does to it.
 *
 * `load` must be stable (wrap it in useCallback with the values it uses): a new function starts a new load.
 * - `refresh()` fetches again but keeps showing the current data, for use after a change was saved.
 * - `retry()` goes back to the loading state first, for use after a failure.
 */
export function useResource<T>(load: (signal: AbortSignal) => Promise<T>) {
  const [state, setState] = useState<Resource<T>>({ kind: "loading" });
  const [attempt, setAttempt] = useState(0);

  useEffect(() => {
    const controller = new AbortController();

    load(controller.signal)
      .then((data) => {
        if (!controller.signal.aborted) setState({ kind: "ready", data });
      })
      .catch((error: unknown) => {
        if (!controller.signal.aborted) setState({ kind: "failed", error });
      });

    return () => controller.abort();
  }, [load, attempt]);

  const refresh = useCallback(() => setAttempt((value) => value + 1), []);
  const retry = useCallback(() => {
    setState({ kind: "loading" });
    setAttempt((value) => value + 1);
  }, []);

  return { state, refresh, retry };
}
