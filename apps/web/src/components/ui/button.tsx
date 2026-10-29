import Link from "next/link";
import type { ComponentProps } from "react";
import { cn } from "@/lib/cn";

type ButtonVariant = "primary" | "secondary" | "ghost";
type ButtonSize = "md" | "lg" | "icon";

interface ButtonStyleProps {
  variant?: ButtonVariant;
  size?: ButtonSize;
  className?: string;
}

const base =
  "inline-flex shrink-0 select-none items-center justify-center gap-2 rounded-full font-semibold transition-colors " +
  "disabled:cursor-not-allowed disabled:opacity-50 [&_svg]:size-[1.125rem] [&_svg]:shrink-0";

const variants: Record<ButtonVariant, string> = {
  primary: "bg-accent text-white hover:bg-accent-hover",
  secondary:
    "border border-line-strong bg-surface text-ink hover:border-ink-muted disabled:hover:border-line-strong",
  ghost: "text-ink hover:bg-ink/5",
};

// Every size keeps a touch target of at least 44px.
const sizes: Record<ButtonSize, string> = {
  md: "min-h-11 px-4 text-[0.9375rem]",
  lg: "min-h-12 px-6 text-base",
  icon: "size-11",
};

function buttonStyles({ variant = "primary", size = "md", className }: ButtonStyleProps = {}) {
  return cn(base, variants[variant], sizes[size], className);
}

export function Button({
  variant,
  size,
  className,
  type = "button",
  ...props
}: ComponentProps<"button"> & ButtonStyleProps) {
  return <button type={type} className={buttonStyles({ variant, size, className })} {...props} />;
}

export function ButtonLink({
  variant,
  size,
  className,
  ...props
}: ComponentProps<typeof Link> & ButtonStyleProps) {
  return <Link className={buttonStyles({ variant, size, className })} {...props} />;
}
