"use client";

import { useCallback, useState } from "react";
import { LoaderCircle, Plus, UsersRound } from "lucide-react";
import { PageHeader } from "@/components/layout/page-header";
import { DemoBadge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { Notice } from "@/components/ui/notice";
import { HouseholdForm } from "@/features/household/household-form";
import { describeApiError } from "@/features/shared/api-error-message";
import { useResource } from "@/features/shared/use-resource";
import { DEMO_RECIPIENTS } from "@/lib/demo-household";
import { getApiBaseUrl, hasProblemCode } from "@/services/api-client";
import { getHousehold } from "@/services/household-service";
import { archiveRecipient, listRecipients, unarchiveRecipient } from "@/services/recipient-service";
import type { ApiRecipient } from "@/types/api";
import { DemoRecipientList } from "./demo-recipient-list";
import { RecipientForm } from "./recipient-form";
import { RecipientRow } from "./recipient-row";

const DESCRIPTION =
  "Everyone in the household who gets a daily delivery, with their usual quantity.";

/**
 * The Recipients page. With no API address it shows the synthetic demo household, clearly labelled.
 * With one, it works on the household's real recipients and never falls back to demo data silently.
 */
export function RecipientsScreen() {
  return getApiBaseUrl() ? <ConnectedRecipients /> : <DemoRecipients />;
}

function DemoRecipients() {
  return (
    <>
      <PageHeader title="Recipients" description={DESCRIPTION} actions={<DemoBadge />} />
      <div className="grid gap-4">
        <DemoRecipientList recipients={DEMO_RECIPIENTS} />
        <Notice title="Demo data: this app is not connected to an API">
          These are sample recipients and nothing here is saved. To manage your own, run the API and
          point the web app at it (see docs/development.md).
        </Notice>
      </div>
    </>
  );
}

/** What the page is showing besides the list: nothing, the add form, or a form or question for one recipient. */
type Panel =
  | { kind: "closed" }
  | { kind: "add" }
  | { kind: "edit"; id: string }
  | { kind: "confirm-archive"; id: string };

function ConnectedRecipients() {
  const [includeArchived, setIncludeArchived] = useState(false);
  const load = useCallback(
    async (signal: AbortSignal) => {
      const [household, recipients] = await Promise.all([
        getHousehold({ signal }),
        listRecipients({ includeArchived, signal }),
      ]);
      return { household, recipients };
    },
    [includeArchived],
  );
  const { state, refresh, retry } = useResource(load);

  const [panel, setPanel] = useState<Panel>({ kind: "closed" });
  const [busyId, setBusyId] = useState<string | null>(null);
  const [actionError, setActionError] = useState<string | null>(null);

  async function run(id: string, action: () => Promise<unknown>) {
    setBusyId(id);
    setActionError(null);
    try {
      await action();
      setPanel({ kind: "closed" });
      refresh();
    } catch (error) {
      setActionError(describeApiError(error));
    } finally {
      setBusyId(null);
    }
  }

  if (state.kind === "loading") {
    return (
      <>
        <PageHeader title="Recipients" description={DESCRIPTION} />
        <p role="status" className="flex items-center gap-2 text-ink-muted">
          <LoaderCircle aria-hidden className="size-4 animate-spin" />
          Loading recipients…
        </p>
      </>
    );
  }

  if (state.kind === "failed") {
    if (hasProblemCode(state.error, "household-not-set-up")) {
      return (
        <>
          <PageHeader title="Recipients" description={DESCRIPTION} />
          <Card className="p-5 sm:p-6">
            <h2 className="text-lg font-bold">Set up your household first</h2>
            <p className="mt-1 mb-4 max-w-prose text-ink-muted">
              Recipients, prices and deliveries all belong to a household. Name yours to begin.
            </p>
            <HouseholdForm submitLabel="Create household" onSaved={retry} />
          </Card>
        </>
      );
    }

    return (
      <>
        <PageHeader title="Recipients" description={DESCRIPTION} />
        <Notice
          title="Couldn't load recipients"
          action={
            <Button variant="secondary" onClick={retry}>
              Try again
            </Button>
          }
        >
          <span role="alert">{describeApiError(state.error)}</span>
        </Notice>
      </>
    );
  }

  const { household, recipients } = state.data;
  const editing = panel.kind === "edit" ? panel.id : null;
  const confirming = panel.kind === "confirm-archive" ? panel.id : null;

  return (
    <>
      <PageHeader
        title="Recipients"
        description={DESCRIPTION}
        actions={
          <Button onClick={() => setPanel({ kind: "add" })} disabled={panel.kind === "add"}>
            <Plus aria-hidden />
            Add recipient
          </Button>
        }
      />

      <div className="grid gap-4">
        {actionError ? (
          <Notice title="That didn't work">
            <span role="alert">{actionError}</span>
          </Notice>
        ) : null}

        {panel.kind === "add" ? (
          <Card className="p-5 sm:p-6">
            <h2 className="mb-4 text-lg font-bold">Add a recipient</h2>
            <RecipientForm
              today={household.today}
              onSaved={() => {
                setPanel({ kind: "closed" });
                refresh();
              }}
              onCancel={() => setPanel({ kind: "closed" })}
            />
          </Card>
        ) : null}

        {recipients.length === 0 ? (
          <EmptyRecipients
            archivedShown={includeArchived}
            onAdd={() => setPanel({ kind: "add" })}
          />
        ) : (
          <Card className="p-2 sm:p-3">
            <ul className="divide-y divide-line">
              {recipients.map((recipient) => (
                <li key={recipient.id}>
                  {editing === recipient.id ? (
                    <div className="p-3">
                      <h2 className="mb-4 text-lg font-bold">Edit {recipient.name}</h2>
                      <RecipientForm
                        recipient={recipient}
                        today={household.today}
                        onSaved={() => {
                          setPanel({ kind: "closed" });
                          refresh();
                        }}
                        onCancel={() => setPanel({ kind: "closed" })}
                      />
                    </div>
                  ) : (
                    <>
                      <RecipientRow
                        recipient={recipient}
                        today={household.today}
                        busy={busyId === recipient.id}
                        onEdit={() => setPanel({ kind: "edit", id: recipient.id })}
                        onArchive={() => setPanel({ kind: "confirm-archive", id: recipient.id })}
                        onRestore={() => run(recipient.id, () => unarchiveRecipient(recipient.id))}
                      />
                      {confirming === recipient.id ? (
                        <ArchiveQuestion
                          recipient={recipient}
                          busy={busyId === recipient.id}
                          onConfirm={() => run(recipient.id, () => archiveRecipient(recipient.id))}
                          onCancel={() => setPanel({ kind: "closed" })}
                        />
                      ) : null}
                    </>
                  )}
                </li>
              ))}
            </ul>
          </Card>
        )}

        <label className="flex min-h-11 items-center gap-3 text-sm font-semibold">
          <input
            type="checkbox"
            className="size-5 accent-accent"
            checked={includeArchived}
            onChange={(event) => setIncludeArchived(event.target.checked)}
          />
          Show archived recipients
        </label>
      </div>
    </>
  );
}

function EmptyRecipients({ archivedShown, onAdd }: { archivedShown: boolean; onAdd: () => void }) {
  return (
    <Card className="p-5 sm:p-7">
      <span className="flex size-11 items-center justify-center rounded-2xl bg-accent-soft text-accent">
        <UsersRound aria-hidden className="size-5" />
      </span>
      <h2 className="mt-4 text-xl font-bold">No recipients yet</h2>
      <p className="mt-1.5 max-w-prose text-ink-muted">
        {archivedShown
          ? "Nobody has been added to this household."
          : "Add each person who gets milk, with how much they usually get a day. Deliveries are recorded against this list."}
      </p>
      <Button className="mt-4" onClick={onAdd}>
        <Plus aria-hidden />
        Add the first recipient
      </Button>
    </Card>
  );
}

function ArchiveQuestion({
  recipient,
  busy,
  onConfirm,
  onCancel,
}: {
  recipient: ApiRecipient;
  busy: boolean;
  onConfirm: () => void;
  onCancel: () => void;
}) {
  return (
    <div
      role="group"
      aria-label={`Archive ${recipient.name}?`}
      className="mx-3 mb-3 rounded-2xl bg-canvas p-4"
    >
      <p className="font-semibold">Archive {recipient.name}?</p>
      <p className="mt-1 text-sm text-ink-muted">
        No more deliveries are expected from today, or from tomorrow if they only started today.
        Their history is kept, and you can restore them later.
      </p>
      <div className="mt-3 flex flex-wrap gap-2">
        <Button onClick={onConfirm} disabled={busy}>
          {busy ? "Archiving…" : `Archive ${recipient.name}`}
        </Button>
        <Button variant="ghost" onClick={onCancel} disabled={busy}>
          Keep
        </Button>
      </div>
    </div>
  );
}
