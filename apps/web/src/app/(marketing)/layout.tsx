import type { ReactNode } from "react";
import { SiteFooter } from "@/components/layout/site-footer";
import { SiteHeader } from "@/components/layout/site-header";
import { MAIN_CONTENT_ID, SkipLink } from "@/components/layout/skip-link";

export default function MarketingLayout({ children }: Readonly<{ children: ReactNode }>) {
  return (
    <div className="flex min-h-dvh flex-col">
      <SkipLink />
      <SiteHeader />
      <main id={MAIN_CONTENT_ID} className="flex-1">
        {children}
      </main>
      <SiteFooter />
    </div>
  );
}
