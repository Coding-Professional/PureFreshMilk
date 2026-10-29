import type { Metadata } from "next";
import { History } from "lucide-react";
import { PageHeader } from "@/components/layout/page-header";
import { PlaceholderPanel } from "@/components/ui/placeholder-panel";

export const metadata: Metadata = { title: "History" };

export default function HistoryPage() {
  return (
    <>
      <PageHeader title="History" />
      <PlaceholderPanel
        icon={History}
        title="History isn't built yet"
        description="Past days will appear here once deliveries are saved to your household's record."
        planned={[
          "Every recorded day, newest first",
          "Payments shown alongside deliveries",
          "The balance outstanding at the end of each month",
        ]}
      />
    </>
  );
}
