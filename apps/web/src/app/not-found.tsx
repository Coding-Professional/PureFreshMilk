import { Brand } from "@/components/layout/brand";
import { ButtonLink } from "@/components/ui/button";

export default function NotFound() {
  return (
    <main className="mx-auto flex min-h-dvh max-w-md flex-col justify-center px-4 py-16">
      <Brand />
      <h1 className="mt-10 text-3xl font-extrabold">This page doesn&apos;t exist</h1>
      <p className="mt-2 text-ink-muted">Check the address, or go back to the start.</p>
      <div className="mt-6 flex flex-wrap gap-3">
        <ButtonLink href="/">Go to the home page</ButtonLink>
        <ButtonLink href="/app/today" variant="secondary">
          Open Today
        </ButtonLink>
      </div>
    </main>
  );
}
