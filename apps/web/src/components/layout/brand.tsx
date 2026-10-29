import Link from "next/link";
import { cn } from "@/lib/cn";

/** A milk bottle filled to a line: the same "measure" idea as the Today screen. */
export function BrandMark({ className }: { className?: string }) {
  return (
    <svg viewBox="0 0 32 32" aria-hidden className={cn("size-8 shrink-0", className)}>
      <rect width="32" height="32" rx="8" className="fill-accent" />
      <path
        d="M13 6.5h6M13.6 6.5v3.1l-2.8 3.9a3 3 0 0 0-.5 1.7V24a2 2 0 0 0 2 2h7.4a2 2 0 0 0 2-2v-8.8a3 3 0 0 0-.5-1.7l-2.8-3.9V6.5"
        fill="none"
        stroke="#fff"
        strokeWidth="2"
        strokeLinecap="round"
        strokeLinejoin="round"
      />
      <path d="M10.3 17.2h11.4V24a2 2 0 0 1-2 2h-7.4a2 2 0 0 1-2-2z" fill="#fff" />
    </svg>
  );
}

export function Brand({ href = "/", className }: { href?: string; className?: string }) {
  return (
    <Link
      href={href}
      className={cn("inline-flex min-h-11 items-center gap-2.5 rounded-lg", className)}
    >
      <BrandMark />
      <span className="font-display text-lg font-bold tracking-tight text-ink">PureFreshMilk</span>
    </Link>
  );
}
