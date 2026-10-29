import { cn } from "@/lib/cn";
import { formatQuantitySpoken, MILLILITRES_PER_LITRE } from "@/lib/quantity";
import type { DeliveryStatus } from "@/types/household";
import type { DaySummary, TodayRow } from "./today-state";

/**
 * The day's measure: one bar, graduated like a measuring jug, split into a slot per
 * recipient. Solid teal = delivered, hatched amber = missed, dashed = not marked yet.
 * Purely presentational — it draws the rows and summary it is given.
 */
export function DayMeasure({
  rows,
  summary,
  animateOnMount = false,
  className,
}: {
  rows: readonly TodayRow[];
  summary: DaySummary;
  animateOnMount?: boolean;
  className?: string;
}) {
  const slots = layoutSlots(rows);
  const scaleMl = Math.max(1, slots.at(-1)?.endMl ?? 0);
  const percent = (ml: number) => `${(ml / scaleMl) * 100}%`;

  return (
    <div className={className}>
      <div role="img" aria-label={describe(rows, summary)} className="relative h-12">
        {slots.map(({ row, startMl, slotMl }) => {
          const { status, deliveredMl } = row.entry;
          const fill = status === "delivered" ? deliveredMl / slotMl : 0;
          const usualMark = row.differenceMl > 0 ? row.recipient.expectedMl / slotMl : null;

          return (
            <div
              key={row.recipient.id}
              className="absolute inset-y-0 px-px transition-[left,width] duration-500 ease-out"
              style={{ left: percent(startMl), width: percent(slotMl) }}
            >
              <div
                className={cn(
                  "relative flex h-full items-center overflow-hidden rounded-[0.625rem] border-2 pl-2.5 transition-colors",
                  slotStyles[status],
                )}
              >
                {status === "delivered" ? (
                  <span
                    className={cn(
                      "absolute inset-y-0 left-0 origin-left bg-accent transition-[width] duration-500 ease-out",
                      animateOnMount && "animate-measure-fill",
                    )}
                    style={{ width: `${fill * 100}%` }}
                  />
                ) : null}
                {usualMark !== null ? (
                  <span
                    className="absolute inset-y-2 w-0.5 rounded-full bg-white/80"
                    style={{ left: `${usualMark * 100}%` }}
                  />
                ) : null}
                <span
                  className={cn("relative font-display text-sm font-bold", markerStyles[status])}
                >
                  {row.marker}
                </span>
              </div>
            </div>
          );
        })}
      </div>

      <div aria-hidden className="relative mt-1.5 h-7">
        {graduations(scaleMl).map((tick, index, all) => (
          <span key={tick.ml} className="absolute top-0" style={{ left: percent(tick.ml) }}>
            <span
              className={cn(
                "block w-px -translate-x-1/2 bg-line-strong",
                tick.major ? "h-2.5" : "h-1.5",
              )}
            />
            {tick.label ? (
              <span
                className={cn(
                  "absolute top-3 whitespace-nowrap text-[0.6875rem] font-semibold tabular-nums text-ink-muted",
                  index === 0
                    ? "left-0"
                    : index === all.length - 1
                      ? "right-0"
                      : "-translate-x-1/2",
                )}
              >
                {tick.label}
              </span>
            ) : null}
          </span>
        ))}
      </div>
    </div>
  );
}

const slotStyles: Record<DeliveryStatus, string> = {
  delivered: "border-accent bg-accent-soft",
  missed: "hatch-missed border-missed/50 bg-missed-soft",
  pending: "border-dashed border-line-strong bg-surface",
};

const markerStyles: Record<DeliveryStatus, string> = {
  delivered: "text-white",
  missed: "text-missed-ink",
  pending: "text-ink-muted",
};

interface Slot {
  row: TodayRow;
  startMl: number;
  slotMl: number;
  endMl: number;
}

/** Each recipient gets room for their usual quantity, or more if more was delivered. */
function layoutSlots(rows: readonly TodayRow[]): Slot[] {
  let cursor = 0;
  return rows.map((row) => {
    const slotMl = Math.max(row.recipient.expectedMl, row.entry.deliveredMl, 1);
    const slot = { row, startMl: cursor, slotMl, endMl: cursor + slotMl };
    cursor = slot.endMl;
    return slot;
  });
}

const litres = new Intl.NumberFormat("en", { maximumFractionDigits: 2 });

/** Jug-style graduations: a mark every 250 ml, labels every 500 ml, unit on the last label. */
function graduations(scaleMl: number) {
  const minorStep = scaleMl <= 4000 ? 250 : 500;
  const labelStep = scaleMl <= 3000 ? 500 : 1000;

  const ticks: { ml: number; major: boolean; label?: string }[] = [];
  for (let ml = 0; ml <= scaleMl; ml += minorStep) {
    const labelled = ml % labelStep === 0;
    ticks.push({
      ml,
      major: ml % 500 === 0,
      label: labelled ? litres.format(ml / MILLILITRES_PER_LITRE) : undefined,
    });
  }

  const lastLabelled = ticks.findLast((tick) => tick.label !== undefined);
  if (lastLabelled && lastLabelled.ml > 0) lastLabelled.label = `${lastLabelled.label} L`;
  return ticks;
}

function describe(rows: readonly TodayRow[], summary: DaySummary): string {
  const total = `${formatQuantitySpoken(summary.deliveredMl)} of ${formatQuantitySpoken(summary.expectedMl)} delivered.`;
  const perRecipient = rows.map(({ recipient, entry }) => {
    switch (entry.status) {
      case "delivered":
        return `${recipient.name}: delivered, ${formatQuantitySpoken(entry.deliveredMl)}.`;
      case "missed":
        return `${recipient.name}: missed.`;
      case "pending":
        return `${recipient.name}: not marked yet.`;
    }
  });
  return [total, ...perRecipient].join(" ");
}
