import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { RecipientMarker } from "@/components/ui/recipient-marker";
import { formatIsoDate } from "@/lib/dates";
import { formatQuantity } from "@/lib/quantity";
import { recipientMarker } from "@/lib/recipient-marker";
import type { ApiRecipient } from "@/types/api";

/** One recipient in the list: who, their usual quantity, when they start or stop, and what can be done. */
export function RecipientRow({
  recipient,
  today,
  busy,
  onEdit,
  onArchive,
  onRestore,
}: {
  recipient: ApiRecipient;
  /** The household's calendar date now, YYYY-MM-DD, from the server. */
  today: string;
  busy: boolean;
  onEdit: () => void;
  onArchive: () => void;
  onRestore: () => void;
}) {
  // YYYY-MM-DD dates sort as text, so they can be compared without turning them into Date objects.
  const archived = recipient.archivedFrom !== null && recipient.archivedFrom <= today;
  const startsLater = recipient.startsOn > today;

  return (
    <div className="flex flex-wrap items-center gap-x-3 gap-y-2 px-3 py-3">
      <RecipientMarker marker={recipientMarker(recipient.name)} />
      <div className="min-w-0 flex-1 basis-40">
        <p className="truncate font-semibold">{recipient.name}</p>
        <p className="text-sm text-ink-muted">
          {formatQuantity(recipient.expectedMl)} a day
          {startsLater ? ` · starts ${formatIsoDate(recipient.startsOn)}` : null}
        </p>
        {recipient.archivedFrom !== null ? (
          <Badge tone={archived ? "planned" : "neutral"} className="mt-1">
            {archived ? "Archived" : "Stops"} from {formatIsoDate(recipient.archivedFrom)}
          </Badge>
        ) : null}
      </div>
      <div className="flex flex-wrap gap-2">
        <Button
          variant="secondary"
          onClick={onEdit}
          disabled={busy}
          aria-label={`Edit ${recipient.name}`}
        >
          Edit
        </Button>
        {recipient.archivedFrom !== null ? (
          // Archived: bring them back. Archiving scheduled for later: cancel it.
          <Button
            variant="secondary"
            onClick={onRestore}
            disabled={busy}
            aria-label={`${archived ? "Restore" : "Keep"} ${recipient.name}`}
          >
            {archived ? "Restore" : "Keep"}
          </Button>
        ) : (
          <Button
            variant="ghost"
            onClick={onArchive}
            disabled={busy}
            aria-label={`Archive ${recipient.name}`}
          >
            Archive
          </Button>
        )}
      </div>
    </div>
  );
}
