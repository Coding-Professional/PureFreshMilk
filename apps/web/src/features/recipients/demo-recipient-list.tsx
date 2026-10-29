import { Card } from "@/components/ui/card";
import { RecipientMarker } from "@/components/ui/recipient-marker";
import { formatQuantity } from "@/lib/quantity";
import { recipientMarker } from "@/lib/recipient-marker";
import type { Recipient } from "@/types/household";

/** Read-only list of the demo household. Editing arrives with persistence. */
export function DemoRecipientList({ recipients }: { recipients: readonly Recipient[] }) {
  return (
    <Card className="p-2 sm:p-3">
      <ul className="divide-y divide-line">
        {recipients.map((recipient) => (
          <li key={recipient.id} className="flex items-center gap-3 px-3 py-3">
            <RecipientMarker marker={recipientMarker(recipient.name)} />
            <span className="min-w-0 flex-1 truncate font-semibold">{recipient.name}</span>
            <span className="text-sm text-ink-muted">
              {formatQuantity(recipient.expectedMl)} a day
            </span>
          </li>
        ))}
      </ul>
    </Card>
  );
}
