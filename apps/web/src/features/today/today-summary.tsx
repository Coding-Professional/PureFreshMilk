import { CheckCheck } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { cn } from "@/lib/cn";
import { formatQuantity } from "@/lib/quantity";
import { DayMeasure } from "./day-measure";
import type { DaySummary, TodayRow } from "./today-state";

export const SUMMARY_HEADING_ID = "today-summary-heading";

export function TodaySummary({
  rows,
  summary,
  onDeliverRemaining,
}: {
  rows: readonly TodayRow[];
  summary: DaySummary;
  onDeliverRemaining: () => void;
}) {
  const remaining = summary.counts.pending;

  return (
    <Card className="p-5 sm:p-6">
      <h2 id={SUMMARY_HEADING_ID} className="text-base font-bold">
        Today&apos;s measure
      </h2>
      <p aria-live="polite" className="mt-2 flex flex-wrap items-baseline gap-x-2 font-display">
        <span className="text-5xl font-extrabold tracking-tight tabular-nums">
          {formatQuantity(summary.deliveredMl)}
        </span>
        <span className="text-lg font-semibold text-ink-muted">
          of {formatQuantity(summary.expectedMl)} delivered
        </span>
      </p>

      <DayMeasure className="mt-5" rows={rows} summary={summary} />
      <MeasureLegend counts={summary.counts} className="mt-3" />

      <Button className="mt-5 w-full" disabled={remaining === 0} onClick={onDeliverRemaining}>
        <CheckCheck aria-hidden />
        {remaining === 0 ? "Everyone is marked" : "Mark the rest as delivered"}
      </Button>
    </Card>
  );
}

/** Doubles as the measure's key: each count sits next to the mark it is drawn with. */
function MeasureLegend({
  counts,
  className,
}: {
  counts: DaySummary["counts"];
  className?: string;
}) {
  const items = [
    { label: "delivered", count: counts.delivered, swatch: "border-accent bg-accent" },
    {
      label: "missed",
      count: counts.missed,
      swatch: "hatch-missed border-missed/50 bg-missed-soft",
    },
    {
      label: "to mark",
      count: counts.pending,
      swatch: "border-dashed border-line-strong bg-surface",
    },
  ];

  return (
    <ul className={cn("flex flex-wrap gap-x-4 gap-y-1.5 text-sm text-ink-muted", className)}>
      {items.map((item) => (
        <li key={item.label} className="flex items-center gap-2">
          <span aria-hidden className={cn("size-3.5 rounded-[0.3rem] border-2", item.swatch)} />
          <span>
            <span className="font-bold text-ink tabular-nums">{item.count}</span> {item.label}
          </span>
        </li>
      ))}
    </ul>
  );
}
