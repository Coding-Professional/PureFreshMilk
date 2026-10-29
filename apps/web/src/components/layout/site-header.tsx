import Link from "next/link";
import { ButtonLink } from "@/components/ui/button";
import { Brand } from "./brand";

/** Header for public pages (landing, login, sign-up). */
export function SiteHeader() {
  return (
    <header className="mx-auto flex w-full max-w-6xl items-center justify-between gap-3 px-4 py-3 sm:px-6 lg:px-8">
      <Brand />
      <nav aria-label="Account" className="flex items-center gap-1 sm:gap-2">
        <Link
          href="/login"
          className="hidden min-h-11 items-center rounded-full px-4 font-semibold text-ink hover:bg-ink/5 sm:inline-flex"
        >
          Log in
        </Link>
        <ButtonLink href="/app/today">Try the demo</ButtonLink>
      </nav>
    </header>
  );
}
