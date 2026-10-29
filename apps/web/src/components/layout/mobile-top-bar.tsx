"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import { cn } from "@/lib/cn";
import { Brand } from "./brand";
import { householdNav, isActivePath } from "./nav-items";

/** Phone header (below md): brand plus the household set-up screens. */
export function MobileTopBar() {
  const pathname = usePathname();

  return (
    <header className="sticky top-0 z-30 border-b border-line bg-canvas/90 backdrop-blur md:hidden">
      <div className="flex items-center justify-between gap-2 px-4 py-1.5">
        <Brand />
        <nav aria-label="Household">
          <ul className="flex items-center gap-1">
            {householdNav.map(({ href, label, icon: Icon }) => {
              const active = isActivePath(pathname, href);
              return (
                <li key={href}>
                  <Link
                    href={href}
                    aria-label={label}
                    aria-current={active ? "page" : undefined}
                    className={cn(
                      "flex size-11 items-center justify-center rounded-full transition-colors",
                      active ? "bg-accent-soft text-accent" : "text-ink-muted hover:bg-ink/5",
                    )}
                  >
                    <Icon aria-hidden className="size-5" />
                  </Link>
                </li>
              );
            })}
          </ul>
        </nav>
      </div>
    </header>
  );
}
