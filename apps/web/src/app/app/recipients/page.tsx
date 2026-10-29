import type { Metadata } from "next";
import { PageHeader } from "@/components/layout/page-header";
import { DemoBadge } from "@/components/ui/badge";
import { Notice } from "@/components/ui/notice";
import { DemoRecipientList } from "@/features/recipients/demo-recipient-list";
import { DEMO_RECIPIENTS } from "@/lib/demo-household";

export const metadata: Metadata = { title: "Recipients" };

export default function RecipientsPage() {
  return (
    <>
      <PageHeader
        title="Recipients"
        description="Everyone in the household who gets a daily delivery, with their usual quantity."
        actions={<DemoBadge />}
      />
      <div className="grid gap-4">
        <DemoRecipientList recipients={DEMO_RECIPIENTS} />
        <Notice title="Adding and editing recipients isn't built yet">
          These are the demo household&apos;s recipients. Managing your own arrives once data is
          saved.
        </Notice>
      </div>
    </>
  );
}
