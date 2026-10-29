import { DemoBadge } from "@/components/ui/badge";
import { Card } from "@/components/ui/card";
import { RecipientMarker } from "@/components/ui/recipient-marker";
import { DEMO_RECIPIENTS } from "@/lib/demo-household";
import { formatQuantity } from "@/lib/quantity";
import { DayMeasure } from "@/features/today/day-measure";
import {
  createTodayState,
  summarizeDay,
  todayReducer,
  todayRows,
  type TodayAction,
  type TodayRow,
} from "@/features/today/today-state";

// A mid-morning snapshot built with the same state logic as the Today screen.
const previewActions: TodayAction[] = [
  { type: "deliver", recipientId: "demo-a" },
  { type: "deliver", recipientId: "demo-b" },
];
const previewState = previewActions.reduce(todayReducer, createTodayState(DEMO_RECIPIENTS));

/** Static, non-interactive picture of the Today screen for the landing page. */
export function TodayPreview() {
  const rows = todayRows(previewState);
  const summary = summarizeDay(previewState);

  return (
    <figure aria-label="Preview of the Today screen with demo data">
      <Card className="p-5 sm:p-6">
        <div className="flex items-center justify-between gap-3">
          <p className="font-display text-lg font-bold">Today</p>
          <DemoBadge />
        </div>
        <p className="mt-4 flex flex-wrap items-baseline gap-x-2 font-display">
          <span className="text-4xl font-extrabold tracking-tight tabular-nums">
            {formatQuantity(summary.deliveredMl)}
          </span>
          <span className="font-semibold text-ink-muted">
            of {formatQuantity(summary.expectedMl)} delivered
          </span>
        </p>
        <DayMeasure className="mt-4" rows={rows} summary={summary} animateOnMount />
        <ul className="mt-3 divide-y divide-line">
          {rows.map((row) => (
            <li key={row.recipient.id} className="flex items-center gap-3 py-2.5">
              <RecipientMarker
                marker={row.marker}
                status={row.entry.status}
                className="size-9 text-base"
              />
              <span className="min-w-0 flex-1 truncate font-semibold">{row.recipient.name}</span>
              <span className="text-sm text-ink-muted">{statusText(row)}</span>
            </li>
          ))}
        </ul>
      </Card>
    </figure>
  );
}

function statusText({ entry }: TodayRow): string {
  switch (entry.status) {
    case "delivered":
      return `${formatQuantity(entry.deliveredMl)} delivered`;
    case "missed":
      return "Missed";
    case "pending":
      return "Not marked yet";
  }
}
