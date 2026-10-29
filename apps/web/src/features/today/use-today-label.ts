"use client";

import { useSyncExternalStore } from "react";
import { formatLongDate } from "@/lib/dates";

const noSubscription = () => () => {};

/**
 * Today's date in the viewer's own time zone and locale. Returns null while
 * server rendering, because the server cannot know the viewer's "today".
 */
export function useTodayLabel(): string | null {
  return useSyncExternalStore(
    noSubscription,
    () => formatLongDate(new Date()),
    () => null,
  );
}
