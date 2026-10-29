import { Badge } from "@/components/ui/badge";

const capabilities = [
  {
    title: "Daily deliveries",
    body: "Mark each person's milk as delivered, with the quantity that actually arrived.",
    ready: true,
  },
  {
    title: "Missed mornings",
    body: "A missed day is recorded as missed, not left blank, so the month is never in doubt.",
    ready: true,
  },
  {
    title: "Monthly totals",
    body: "Litres per person for the month, worked out from the daily record.",
    ready: false,
  },
  {
    title: "Charges, payments and balances",
    body: "What the month cost at your rate, what has been paid, and what is still owed.",
    ready: false,
  },
];

export function ProjectScope() {
  return (
    <section aria-labelledby="scope-heading" className="border-t border-line">
      <div className="mx-auto grid w-full max-w-6xl gap-10 px-4 py-16 sm:px-6 lg:grid-cols-[minmax(0,0.8fr)_minmax(0,1.2fr)] lg:px-8 lg:py-20">
        <div>
          <h2 id="scope-heading" className="text-3xl font-extrabold sm:text-4xl">
            What&apos;s ready, and what&apos;s next
          </h2>
          <p className="mt-4 max-w-md leading-relaxed text-ink-muted">
            This preview covers the daily record. Money comes next, worked out on the server so
            every screen shows the same figures.
          </p>
        </div>
        <ul className="grid gap-3 sm:grid-cols-2">
          {capabilities.map((item) => (
            <li key={item.title} className="rounded-card border border-line bg-surface p-5">
              <Badge tone={item.ready ? "accent" : "planned"}>
                {item.ready ? "In the demo" : "Planned"}
              </Badge>
              <h3 className="mt-3 text-lg font-bold">{item.title}</h3>
              <p className="mt-1.5 text-ink-muted">{item.body}</p>
            </li>
          ))}
        </ul>
      </div>
    </section>
  );
}
