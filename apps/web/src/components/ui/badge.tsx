import type { ComponentProps } from "react";
import { cn } from "@/lib/cn";

type BadgeTone = "neutral" | "accent" | "demo" | "planned";

const tones: Record<BadgeTone, string> = {
  neutral: "border-line bg-canvas text-ink-muted",
  accent: "border-accent/20 bg-accent-soft text-accent-ink",
  // Demo data must always be obvious: a solid, high-contrast label.
  demo: "border-ink bg-ink text-white",
  planned: "border-dashed border-line-strong bg-surface text-ink-muted",
};

export function Badge({
  tone = "neutral",
  className,
  ...props
}: ComponentProps<"span"> & { tone?: BadgeTone }) {
  return (
    <span
      className={cn(
        "inline-flex items-center gap-1.5 whitespace-nowrap rounded-full border px-2.5 py-0.5 text-xs font-semibold",
        tones[tone],
        className,
      )}
      {...props}
    />
  );
}

/** The label every piece of synthetic data carries. */
export function DemoBadge({ className }: { className?: string }) {
  return (
    <Badge tone="demo" className={className}>
      <span aria-hidden className="size-1.5 rounded-full bg-accent-soft" />
      Demo data
    </Badge>
  );
}
