import Link from "next/link";
import { ArrowRight } from "lucide-react";
import { ButtonLink } from "@/components/ui/button";
import { TodayPreview } from "./today-preview";

export function Hero() {
  return (
    <section
      aria-labelledby="hero-heading"
      className="mx-auto grid w-full max-w-6xl items-center gap-10 px-4 pb-16 pt-8 sm:px-6 md:pt-14 lg:grid-cols-[minmax(0,1.05fr)_minmax(0,1fr)] lg:gap-14 lg:px-8 lg:pb-24"
    >
      <div>
        <h1
          id="hero-heading"
          className="text-[2.5rem] font-extrabold leading-[1.05] sm:text-6xl lg:text-[4.25rem]"
        >
          Every morning&apos;s milk, measured to the millilitre.
        </h1>
        <p className="mt-5 max-w-xl text-lg leading-relaxed text-ink-muted">
          PureFreshMilk replaces the pencil marks on the kitchen calendar. Record what arrived for
          each person at home and note the mornings the milk didn&apos;t come.
        </p>
        <div className="mt-8 flex flex-wrap items-center gap-3">
          <ButtonLink href="/app/today" size="lg">
            Try the Today screen
            <ArrowRight aria-hidden />
          </ButtonLink>
          <Link
            href="#how-it-works"
            className="inline-flex min-h-12 items-center rounded-full px-4 font-semibold text-ink hover:bg-ink/5"
          >
            How it works
          </Link>
        </div>
        <p className="mt-6 text-sm text-ink-muted">
          Free and open source. Monthly totals, bills and payments are next on the list.
        </p>
      </div>

      <div className="rounded-[2rem] bg-accent-soft p-3 sm:p-6 lg:p-8">
        <TodayPreview />
      </div>
    </section>
  );
}
