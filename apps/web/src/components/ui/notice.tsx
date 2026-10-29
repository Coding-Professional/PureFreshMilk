import type { ReactNode } from "react";
import { Info } from "lucide-react";
import { cn } from "@/lib/cn";

/** A short, non-urgent message that explains the state of the page. */
export function Notice({
  title,
  children,
  action,
  className,
}: {
  title?: string;
  children: ReactNode;
  action?: ReactNode;
  className?: string;
}) {
  return (
    <div
      className={cn(
        "flex gap-3 rounded-2xl border border-line bg-surface px-4 py-3 text-sm",
        className,
      )}
    >
      <Info aria-hidden className="mt-0.5 size-4 shrink-0 text-accent" />
      <div className="flex min-w-0 flex-1 flex-wrap items-center justify-between gap-x-4 gap-y-2">
        <div className="min-w-0 flex-1 basis-48 text-ink-muted">
          {title ? <p className="font-semibold text-ink">{title}</p> : null}
          <div>{children}</div>
        </div>
        {action ? <div className="shrink-0">{action}</div> : null}
      </div>
    </div>
  );
}
