import type { ReactNode } from "react";

export function PageHeader({
  eyebrow,
  title,
  description,
  actions,
}: {
  eyebrow?: ReactNode;
  title: string;
  description?: string;
  actions?: ReactNode;
}) {
  return (
    <header className="mb-6 flex flex-wrap items-end justify-between gap-x-4 gap-y-3 md:mb-8">
      <div className="min-w-0">
        {eyebrow ? <p className="text-sm font-semibold text-ink-muted">{eyebrow}</p> : null}
        <h1 className="mt-0.5 text-3xl font-extrabold sm:text-4xl">{title}</h1>
        {description ? <p className="mt-2 max-w-prose text-ink-muted">{description}</p> : null}
      </div>
      {actions ? <div className="flex flex-wrap items-center gap-2">{actions}</div> : null}
    </header>
  );
}
