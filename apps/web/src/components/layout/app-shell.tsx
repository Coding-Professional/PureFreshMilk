import type { ReactNode } from "react";
import { AppSidebar } from "./app-sidebar";
import { MobileBottomNav } from "./mobile-bottom-nav";
import { MobileTopBar } from "./mobile-top-bar";
import { MAIN_CONTENT_ID, SkipLink } from "./skip-link";

/** Signed-in application frame: sidebar on md+, top bar and bottom tab bar on phones. */
export function AppShell({ children }: { children: ReactNode }) {
  return (
    <div className="flex min-h-dvh">
      <SkipLink />
      <AppSidebar />
      <div className="flex min-w-0 flex-1 flex-col">
        <MobileTopBar />
        <main
          id={MAIN_CONTENT_ID}
          className="mx-auto w-full max-w-5xl flex-1 px-4 pb-28 pt-5 sm:px-6 md:px-8 md:pb-12 md:pt-8 lg:px-10"
        >
          {children}
        </main>
      </div>
      <MobileBottomNav />
    </div>
  );
}
