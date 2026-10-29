import { cn } from "@/lib/cn";
import type { DeliveryStatus } from "@/types/household";

const statusStyles: Record<DeliveryStatus | "none", string> = {
  none: "border-line bg-canvas text-ink",
  pending: "border-dashed border-line-strong bg-surface text-ink-muted",
  delivered: "border-accent bg-accent text-white",
  missed: "hatch-missed border-missed/50 bg-missed-soft text-missed-ink",
};

/**
 * A recipient's letter in a circle. With a status it uses the same marks as the
 * day's measure: solid teal = delivered, hatched amber = missed, dashed = not marked yet.
 * Decorative: always pair it with the recipient's name in text.
 */
export function RecipientMarker({
  marker,
  status,
  className,
}: {
  marker: string;
  status?: DeliveryStatus;
  className?: string;
}) {
  return (
    <span
      aria-hidden
      className={cn(
        "flex size-11 shrink-0 items-center justify-center rounded-full border-2 font-display text-lg font-bold transition-colors",
        statusStyles[status ?? "none"],
        className,
      )}
    >
      {marker}
    </span>
  );
}
