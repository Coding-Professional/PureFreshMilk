"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import { cn } from "@/lib/cn";
import { Brand } from "./brand";
import { householdNav, isActivePath, primaryNav, type NavItem } from "./nav-items";

/** Desktop and tablet navigation (md and up). */
export function AppSidebar() {
  const pathname = usePathname();

  return (
    <aside className="sticky top-0 hidden h-dvh w-60 shrink-0 flex-col border-r border-line bg-surface/70 px-3 py-5 md:flex lg:w-64">
      <Brand className="px-2" />

      <nav aria-label="Main" className="mt-8 grid gap-6">
        <SidebarGroup items={primaryNav} pathname={pathname} />
        <div>
          <p className="px-3 pb-1.5 text-xs font-semibold text-ink-muted">Household</p>
          <SidebarGroup items={householdNav} pathname={pathname} />
        </div>
      </nav>

      <p className="mt-auto rounded-2xl bg-canvas px-3 py-3 text-xs leading-relaxed text-ink-muted">
        <span className="font-semibold text-ink">Preview build.</span> Screens use synthetic demo
        data and nothing is saved yet.
      </p>
    </aside>
  );
}

function SidebarGroup({ items, pathname }: { items: readonly NavItem[]; pathname: string }) {
  return (
    <ul className="grid gap-1">
      {items.map(({ href, label, icon: Icon }) => {
        const active = isActivePath(pathname, href);
        return (
          <li key={href}>
            <Link
              href={href}
              aria-current={active ? "page" : undefined}
              className={cn(
                "flex min-h-11 items-center gap-3 rounded-xl px-3 font-semibold transition-colors",
                active
                  ? "bg-accent-soft text-accent-ink"
                  : "text-ink-muted hover:bg-ink/5 hover:text-ink",
              )}
            >
              <Icon aria-hidden className={cn("size-5", active && "text-accent")} />
              {label}
            </Link>
          </li>
        );
      })}
    </ul>
  );
}
