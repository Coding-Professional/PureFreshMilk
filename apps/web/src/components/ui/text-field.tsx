import type { ComponentProps } from "react";
import { cn } from "@/lib/cn";

interface TextFieldProps extends Omit<ComponentProps<"input">, "id"> {
  id: string;
  label: string;
  hint?: string;
}

export function TextField({ id, label, hint, className, ...inputProps }: TextFieldProps) {
  const hintId = hint ? `${id}-hint` : undefined;

  return (
    <div className={cn("grid gap-1.5", className)}>
      <label htmlFor={id} className="text-sm font-semibold text-ink">
        {label}
      </label>
      <input
        id={id}
        aria-describedby={hintId}
        className={cn(
          "min-h-12 w-full rounded-xl border border-line-strong bg-surface px-3.5 text-base text-ink",
          "placeholder:text-ink-muted focus-visible:border-accent",
          "disabled:cursor-not-allowed disabled:bg-canvas disabled:text-ink-muted",
        )}
        {...inputProps}
      />
      {hint ? (
        <p id={hintId} className="text-sm text-ink-muted">
          {hint}
        </p>
      ) : null}
    </div>
  );
}
