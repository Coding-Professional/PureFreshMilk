import type { Metadata } from "next";
import { CalendarOff } from "lucide-react";
import { PageHeader } from "@/components/layout/page-header";
import { PlaceholderPanel } from "@/components/ui/placeholder-panel";
import { ApiStatusCard } from "@/features/settings/api-status-card";
import { HouseholdSettings } from "@/features/settings/household-settings";

export const metadata: Metadata = { title: "Settings" };

export default function SettingsPage() {
  return (
    <>
      <PageHeader title="Settings" />
      <div className="grid gap-4">
        <HouseholdSettings />
        <PlaceholderPanel
          icon={CalendarOff}
          title="Delivery days and pauses aren't built yet"
          description="This is where days without deliveries, such as a trip away, will be managed."
          planned={["Delivery days and pauses"]}
        />
        <ApiStatusCard />
      </div>
    </>
  );
}
