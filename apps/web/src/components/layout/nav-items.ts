import { CalendarDays, History, Settings, Sun, Users, type LucideIcon } from "lucide-react";

export interface NavItem {
  href: string;
  label: string;
  icon: LucideIcon;
}

/** Daily screens: the mobile bottom bar and the top of the desktop sidebar. */
export const primaryNav: readonly NavItem[] = [
  { href: "/app/today", label: "Today", icon: Sun },
  { href: "/app/month", label: "This month", icon: CalendarDays },
  { href: "/app/history", label: "History", icon: History },
];

/** Set-up screens: the mobile top bar and the lower group of the desktop sidebar. */
export const householdNav: readonly NavItem[] = [
  { href: "/app/recipients", label: "Recipients", icon: Users },
  { href: "/app/settings", label: "Settings", icon: Settings },
];

export function isActivePath(pathname: string, href: string): boolean {
  return pathname === href || pathname.startsWith(`${href}/`);
}
