"use client";

import { useCallback, useState } from "react";
import Link from "next/link";
import { LoaderCircle } from "lucide-react";
import { PageHeader } from "@/components/layout/page-header";
import { Button } from "@/components/ui/button";
import { Notice } from "@/components/ui/notice";
import { describeApiError } from "@/features/shared/api-error-message";
import { useResource } from "@/features/shared/use-resource";
import { formatIsoDate } from "@/lib/dates";
import { recipientMarker } from "@/lib/recipient-marker";
import { hasProblemCode } from "@/services/api-client";
import {
  clearDelivery,
  deliverRemaining,
  getDaySheet,
  recordDelivery,
} from "@/services/delivery-service";
import type { ApiDayRow, ApiDaySheet } from "@/types/api";
import { DeliveryCard } from "./delivery-card";
import { SUMMARY_HEADING_ID, TodaySummary } from "./today-summary";
import type { DaySummary, TodayRow } from "./today-state";

/** Real day sheet. The API owns the saved totals; local changes are shown while a request is pending. */
export function ConnectedDayScreen({ date }: { date: string }) {
  const load = useCallback((signal: AbortSignal) => getDaySheet(date, { signal }), [date]);
  const { state, refresh, retry } = useResource(load);
  const [optimistic, setOptimistic] = useState<{ base: ApiDaySheet; sheet: ApiDaySheet } | null>(
    null,
  );
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const title = date === "today" ? "Today" : "Day record";
  if (state.kind === "loading")
    return (
      <>
        <PageHeader title={title} />
        <p role="status" className="flex items-center gap-2 text-ink-muted">
          <LoaderCircle aria-hidden className="size-4 animate-spin" />
          Loading deliveries…
        </p>
      </>
    );
  if (state.kind === "failed")
    return (
      <>
        <PageHeader title={title} />
        {hasProblemCode(state.error, "household-not-set-up") ? (
          <Notice title="Set up your household first">
            Start on the{" "}
            <Link href="/app/recipients" className="font-semibold text-accent underline">
              Recipients page
            </Link>
            , then come back to record deliveries.
          </Notice>
        ) : (
          <Notice
            title="Couldn't load deliveries"
            action={
              <Button variant="secondary" onClick={retry}>
                Try again
              </Button>
            }
          >
            <span role="alert">{describeApiError(state.error)}</span>
          </Notice>
        )}
      </>
    );

  const baseSheet = state.data;
  const sheet = optimistic?.base === baseSheet ? optimistic.sheet : baseSheet;
  const rows: TodayRow[] = sheet.rows.map((row) => ({
    recipient: { id: row.recipientId, name: row.name, expectedMl: row.expectedMl },
    marker: recipientMarker(row.name),
    entry: {
      recipientId: row.recipientId,
      status: row.status === "notRecorded" ? "pending" : row.status,
      deliveredMl: row.deliveredMl,
    },
    differenceMl: row.status === "delivered" ? row.deliveredMl - row.expectedMl : 0,
  }));
  const summary: DaySummary = {
    expectedMl: sheet.totals.expectedMl,
    deliveredMl: sheet.totals.deliveredMl,
    counts: {
      delivered: sheet.totals.delivered,
      missed: sheet.totals.missed,
      pending: sheet.totals.notRecorded,
    },
  };

  async function change(
    recipient: ApiDayRow,
    status: "delivered" | "missed" | "notRecorded",
    ml?: number,
  ) {
    if (busy) return;
    const previous = sheet;
    setBusy(true);
    setError(null);
    setOptimistic({
      base: baseSheet,
      sheet: changeRow(
        previous,
        recipient.recipientId,
        status,
        ml ?? (status === "delivered" ? recipient.expectedMl : 0),
      ),
    });
    try {
      if (status === "notRecorded") await clearDelivery(recipient.recipientId, previous.date);
      else
        await recordDelivery(
          recipient.recipientId,
          previous.date,
          status,
          status === "delivered" ? ml : undefined,
        );
      refresh();
    } catch (cause) {
      setOptimistic({ base: baseSheet, sheet: previous });
      setError(describeApiError(cause));
    } finally {
      setBusy(false);
    }
  }

  async function markRemaining() {
    if (busy) return;
    const previous = sheet;
    setBusy(true);
    setError(null);
    let next = previous;
    for (const row of previous.rows.filter((item) => item.status === "notRecorded"))
      next = changeRow(next, row.recipientId, "delivered", row.expectedMl);
    setOptimistic({ base: baseSheet, sheet: next });
    try {
      setOptimistic({ base: baseSheet, sheet: await deliverRemaining(previous.date) });
      refresh();
    } catch (cause) {
      setOptimistic({ base: baseSheet, sheet: previous });
      setError(describeApiError(cause));
    } finally {
      setBusy(false);
    }
  }

  return (
    <>
      <PageHeader
        eyebrow={formatIsoDate(sheet.date)}
        title={sheet.isToday ? "Today" : "Day record"}
        actions={
          !sheet.isToday ? (
            <Link href="/app/history" className="text-sm font-semibold text-accent hover:underline">
              Back to History
            </Link>
          ) : undefined
        }
      />
      {error ? (
        <Notice
          title="Couldn't save that change"
          action={
            <Button variant="secondary" onClick={refresh}>
              Reload day
            </Button>
          }
        >
          <span role="alert">{error}</span>
        </Notice>
      ) : null}
      {rows.length === 0 ? (
        <Notice title="No recipients expected">
          Add recipients on the{" "}
          <Link href="/app/recipients" className="font-semibold text-accent underline">
            Recipients page
          </Link>{" "}
          to start recording deliveries.
        </Notice>
      ) : (
        <div className="grid gap-6 lg:grid-cols-[minmax(0,1fr)_21rem] lg:items-start">
          <section
            aria-labelledby={SUMMARY_HEADING_ID}
            className="lg:sticky lg:top-8 lg:col-start-2 lg:row-start-1"
          >
            <TodaySummary
              rows={rows}
              summary={summary}
              onDeliverRemaining={markRemaining}
              busy={busy}
              heading={sheet.isToday ? "Today's measure" : "Day's measure"}
            />
          </section>
          <section aria-labelledby="deliveries-heading" className="lg:col-start-1 lg:row-start-1">
            <h2 id="deliveries-heading" className="mb-3 text-base font-bold">
              Deliveries
            </h2>
            <ul className="grid gap-3" aria-busy={busy}>
              {rows.map((row) => {
                const source = sheet.rows.find((item) => item.recipientId === row.recipient.id)!;
                return (
                  <DeliveryCard
                    key={row.recipient.id}
                    row={row}
                    disabled={busy}
                    onDeliver={() =>
                      change(source, row.entry.status === "delivered" ? "notRecorded" : "delivered")
                    }
                    onMiss={() =>
                      change(source, row.entry.status === "missed" ? "notRecorded" : "missed")
                    }
                    onClear={() => change(source, "notRecorded")}
                    onAdjust={(deltaMl) =>
                      change(source, "delivered", row.entry.deliveredMl + deltaMl)
                    }
                  />
                );
              })}
            </ul>
          </section>
        </div>
      )}
    </>
  );
}

function changeRow(
  sheet: ApiDaySheet,
  id: string,
  status: ApiDayRow["status"],
  ml: number,
): ApiDaySheet {
  const rows = sheet.rows.map((row) =>
    row.recipientId === id ? { ...row, status, deliveredMl: ml } : row,
  );
  return {
    ...sheet,
    rows,
    totals: {
      expectedMl: rows.reduce((sum, row) => sum + row.expectedMl, 0),
      deliveredMl: rows.reduce((sum, row) => sum + row.deliveredMl, 0),
      delivered: rows.filter((row) => row.status === "delivered").length,
      missed: rows.filter((row) => row.status === "missed").length,
      notRecorded: rows.filter((row) => row.status === "notRecorded").length,
    },
  };
}
