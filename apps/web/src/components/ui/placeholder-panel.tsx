import type { LucideIcon } from "lucide-react";
import { Badge } from "./badge";
import { Card } from "./card";

/** Marks a screen whose real functionality is planned but not built yet. */
export function PlaceholderPanel({
  icon: Icon,
  title,
  description,
  planned,
}: {
  icon: LucideIcon;
  title: string;
  description: string;
  planned: readonly string[];
}) {
  return (
    <Card className="p-5 sm:p-7">
      <div className="flex flex-wrap items-center gap-3">
        <span className="flex size-11 items-center justify-center rounded-2xl bg-accent-soft text-accent">
          <Icon aria-hidden className="size-5" />
        </span>
        <Badge tone="planned">Placeholder</Badge>
      </div>
      <h2 className="mt-4 text-xl font-bold">{title}</h2>
      <p className="mt-1.5 max-w-prose text-ink-muted">{description}</p>
      <h3 className="mt-5 text-sm font-semibold text-ink">Planned for this screen</h3>
      <ul className="mt-2 grid gap-2">
        {planned.map((item) => (
          <li key={item} className="flex gap-2.5 text-ink-muted">
            <span
              aria-hidden
              className="mt-2 size-2 shrink-0 rounded-full border-2 border-dashed border-line-strong"
            />
            {item}
          </li>
        ))}
      </ul>
    </Card>
  );
}
