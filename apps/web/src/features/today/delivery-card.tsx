"use client";

import { useId } from "react";
import { Check, Minus, Plus, X, type LucideIcon } from "lucide-react";
import { Button } from "@/components/ui/button";
import { RecipientMarker } from "@/components/ui/recipient-marker";
import { cn } from "@/lib/cn";
import { formatQuantity } from "@/lib/quantity";
import { MAX_DELIVERY_ML, QUANTITY_STEP_ML, type TodayRow } from "./today-state";

interface DeliveryCardProps {
  row: TodayRow;
  onDeliver: () => void;
  onMiss: () => void;
  onClear: () => void;
  onAdjust: (deltaMl: number) => void;
}

/** One recipient's delivery for today: delivered or missed, and how much arrived. */
export function DeliveryCard({ row, onDeliver, onMiss, onClear, onAdjust }: DeliveryCardProps) {
  const { recipient, marker, entry } = row;
  const headingId = useId();

  return (
    <li>
      <article
        aria-labelledby={headingId}
        className="rounded-card border border-line bg-surface p-4 shadow-card sm:flex sm:flex-wrap sm:items-center sm:gap-x-4 sm:p-5"
      >
        <div className="flex min-w-0 items-center gap-3 sm:flex-1">
          <RecipientMarker marker={marker} status={entry.status} />
          <div className="min-w-0">
            <h3 id={headingId} className="truncate text-lg font-bold leading-tight">
              {recipient.name}
            </h3>
            <p className="text-sm text-ink-muted">
              Usually {formatQuantity(recipient.expectedMl)} a day
            </p>
          </div>
        </div>

        <div className="mt-4 grid grid-cols-2 gap-2 sm:mt-0 sm:w-72">
          <StatusToggle
            label="Delivered"
            icon={Check}
            tone="delivered"
            recipientName={recipient.name}
            pressed={entry.status === "delivered"}
            onClick={entry.status === "delivered" ? onClear : onDeliver}
          />
          <StatusToggle
            label="Missed"
            icon={X}
            tone="missed"
            recipientName={recipient.name}
            pressed={entry.status === "missed"}
            onClick={entry.status === "missed" ? onClear : onMiss}
          />
        </div>

        {entry.status === "delivered" ? (
          <QuantityStepper
            recipientName={recipient.name}
            deliveredMl={entry.deliveredMl}
            differenceMl={row.differenceMl}
            onAdjust={onAdjust}
          />
        ) : null}
      </article>
    </li>
  );
}

const pressedStyles = {
  delivered: "border-accent bg-accent text-white",
  missed: "border-missed/60 bg-missed-soft text-missed-ink",
} as const;

function StatusToggle({
  label,
  icon: Icon,
  tone,
  recipientName,
  pressed,
  onClick,
}: {
  label: string;
  icon: LucideIcon;
  tone: keyof typeof pressedStyles;
  recipientName: string;
  pressed: boolean;
  onClick: () => void;
}) {
  return (
    <button
      type="button"
      // The visible label starts the accessible name (WCAG 2.5.3); the name adds whose delivery it is.
      aria-label={`${label} for ${recipientName}`}
      aria-pressed={pressed}
      onClick={onClick}
      className={cn(
        "inline-flex min-h-12 items-center justify-center gap-2 rounded-xl border-2 px-3 font-semibold transition-colors",
        pressed ? pressedStyles[tone] : "border-line bg-surface text-ink hover:border-line-strong",
      )}
    >
      <Icon aria-hidden className="size-5 shrink-0" />
      {label}
    </button>
  );
}

function QuantityStepper({
  recipientName,
  deliveredMl,
  differenceMl,
  onAdjust,
}: {
  recipientName: string;
  deliveredMl: number;
  differenceMl: number;
  onAdjust: (deltaMl: number) => void;
}) {
  const step = formatQuantity(QUANTITY_STEP_ML);

  return (
    <div className="mt-3 rounded-xl bg-canvas px-3 py-2.5 sm:basis-full">
      <div
        role="group"
        aria-label={`Quantity received by ${recipientName}`}
        className="flex flex-wrap items-center justify-between gap-2"
      >
        <span aria-hidden className="text-sm font-semibold">
          Quantity received
        </span>
        <div className="flex items-center gap-1">
          <Button
            variant="secondary"
            size="icon"
            aria-label={`${step} less`}
            disabled={deliveredMl <= QUANTITY_STEP_ML}
            onClick={() => onAdjust(-QUANTITY_STEP_ML)}
          >
            <Minus aria-hidden />
          </Button>
          <output className="min-w-[4.5rem] text-center font-display text-lg font-bold tabular-nums">
            {formatQuantity(deliveredMl)}
          </output>
          <Button
            variant="secondary"
            size="icon"
            aria-label={`${step} more`}
            disabled={deliveredMl >= MAX_DELIVERY_ML}
            onClick={() => onAdjust(QUANTITY_STEP_ML)}
          >
            <Plus aria-hidden />
          </Button>
        </div>
      </div>
      {differenceMl !== 0 ? (
        <p className="mt-1.5 text-sm text-ink-muted">
          {differenceMl > 0
            ? `${formatQuantity(differenceMl)} more than usual`
            : `${formatQuantity(-differenceMl)} less than usual`}
        </p>
      ) : null}
    </div>
  );
}
