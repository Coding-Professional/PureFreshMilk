import type { Metadata } from "next";
import { House } from "lucide-react";
import { PageHeader } from "@/components/layout/page-header";
import { PlaceholderPanel } from "@/components/ui/placeholder-panel";
import { ApiStatusCard } from "@/features/settings/api-status-card";

export const metadata: Metadata = { title: "Settings" };

export default function SettingsPage() {
  return (
    <>
      <PageHeader title="Settings" />
      <div className="grid gap-4">
        <PlaceholderPanel
          icon={House}
          title="Household settings aren't built yet"
          description="This is where your household's details and milk prices will be managed."
          planned={[
            "Household name and members",
            "Milk price per litre",
            "Delivery days and pauses",
          ]}
        />
        <ApiStatusCard />
      </div>
    </>
  );
}
