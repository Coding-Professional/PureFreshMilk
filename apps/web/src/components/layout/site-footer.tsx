import Link from "next/link";
import { BrandMark } from "./brand";

const links = [
  { href: "/app/today", label: "Today (demo)" },
  { href: "/login", label: "Log in" },
  { href: "/signup", label: "Sign up" },
];

export function SiteFooter() {
  return (
    <footer className="border-t border-line">
      <div className="mx-auto flex w-full max-w-6xl flex-col gap-4 px-4 py-8 text-sm text-ink-muted sm:flex-row sm:items-center sm:justify-between sm:px-6 lg:px-8">
        <div className="flex items-center gap-2.5">
          <BrandMark className="size-6" />
          <p>
            PureFreshMilk is open source under the MIT License. Demo data on this site is synthetic.
          </p>
        </div>
        <nav aria-label="Footer">
          <ul className="flex flex-wrap gap-x-1 gap-y-1">
            {links.map((link) => (
              <li key={link.href}>
                <Link
                  href={link.href}
                  className="inline-flex min-h-11 items-center rounded-full px-3 font-semibold text-ink hover:bg-ink/5"
                >
                  {link.label}
                </Link>
              </li>
            ))}
          </ul>
        </nav>
      </div>
    </footer>
  );
}
