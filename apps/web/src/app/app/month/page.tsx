import type { Metadata } from "next";
import { CalendarDays } from "lucide-react";
import { PageHeader } from "@/components/layout/page-header";
import { PlaceholderPanel } from "@/components/ui/placeholder-panel";

export const metadata: Metadata = { title: "This month" };

export default function MonthPage() {
  return (
    <>
      <PageHeader title="This month" />
      <PlaceholderPanel
        icon={CalendarDays}
        title="The monthly view isn't built yet"
        description="This screen will show the month as a calendar of delivered and missed days, with totals for each person."
        planned={[
          "A calendar of delivered and missed days",
          "Litres for each person this month",
          "The month's charges, calculated by the server",
        ]}
      />
    </>
  );
}
