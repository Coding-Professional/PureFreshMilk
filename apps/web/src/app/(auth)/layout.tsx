import type { ReactNode } from "react";
import { Brand } from "@/components/layout/brand";
import { MAIN_CONTENT_ID, SkipLink } from "@/components/layout/skip-link";

export default function AuthLayout({ children }: Readonly<{ children: ReactNode }>) {
  return (
    <div className="flex min-h-dvh flex-col">
      <SkipLink />
      <header className="px-4 py-3 sm:px-6 lg:px-8">
        <Brand />
      </header>
      <main
        id={MAIN_CONTENT_ID}
        className="flex flex-1 justify-center px-4 pb-16 pt-4 sm:items-center sm:pt-0"
      >
        <div className="w-full max-w-md">{children}</div>
      </main>
    </div>
  );
}
