import type { ComponentProps } from "react";
import { cn } from "@/lib/cn";

interface SelectFieldProps extends Omit<ComponentProps<"select">, "id"> {
  id: string;
  label: string;
  hint?: string;
  /** What is wrong with the value. Shown under the field and announced with it. */
  error?: string;
  options: readonly { value: string; label: string }[];
}

export function SelectField({
  id,
  label,
  hint,
  error,
  options,
  className,
  ...selectProps
}: SelectFieldProps) {
  const hintId = hint ? `${id}-hint` : undefined;
  const errorId = error ? `${id}-error` : undefined;

  return (
    <div className={cn("grid gap-1.5", className)}>
      <label htmlFor={id} className="text-sm font-semibold text-ink">
        {label}
      </label>
      <select
        id={id}
        aria-describedby={[hintId, errorId].filter(Boolean).join(" ") || undefined}
        aria-invalid={error ? true : undefined}
        className={cn(
          "min-h-12 w-full rounded-xl border bg-surface px-3 text-base text-ink",
          error ? "border-missed" : "border-line-strong",
          "focus-visible:border-accent disabled:cursor-not-allowed disabled:bg-canvas disabled:text-ink-muted",
        )}
        {...selectProps}
      >
        {options.map((option) => (
          <option key={option.value} value={option.value}>
            {option.label}
          </option>
        ))}
      </select>
      {hint ? (
        <p id={hintId} className="text-sm text-ink-muted">
          {hint}
        </p>
      ) : null}
      {error ? (
        <p id={errorId} className="text-sm font-semibold text-missed-ink">
          {error}
        </p>
      ) : null}
    </div>
  );
}
