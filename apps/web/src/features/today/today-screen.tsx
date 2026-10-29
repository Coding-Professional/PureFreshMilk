"use client";

import { useReducer } from "react";
import { RotateCcw } from "lucide-react";
import { PageHeader } from "@/components/layout/page-header";
import { DemoBadge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import type { Recipient } from "@/types/household";
import { DeliveryCard } from "./delivery-card";
import { SUMMARY_HEADING_ID, TodaySummary } from "./today-summary";
import { createTodayState, summarizeDay, todayReducer, todayRows } from "./today-state";
import { useTodayLabel } from "./use-today-label";

/**
 * Visual prototype of the Today screen. State lives only in this component:
 * nothing is sent to the API and a reload starts the day over.
 */
export function TodayScreen({ recipients }: { recipients: readonly Recipient[] }) {
  const [state, dispatch] = useReducer(todayReducer, recipients, createTodayState);
  const dateLabel = useTodayLabel();
  const rows = todayRows(state);
  const summary = summarizeDay(state);

  return (
    <>
      <PageHeader
        // Fixed height: the date only appears after hydration, so reserve its line.
        eyebrow={<span className="inline-block min-h-5">{dateLabel}</span>}
        title="Today"
        actions={<DemoBadge />}
      />

      <div className="mb-6 flex items-center justify-between gap-3 rounded-2xl border border-line bg-surface py-1 pl-4 pr-1">
        <p className="text-sm text-ink-muted">
          Sample household<span className="hidden sm:inline">, demo data only</span>. Nothing here
          is saved.
        </p>
        <Button variant="ghost" onClick={() => dispatch({ type: "reset" })}>
          <RotateCcw aria-hidden />
          Start over
        </Button>
      </div>

      <div className="grid gap-6 lg:grid-cols-[minmax(0,1fr)_21rem] lg:items-start">
        <section
          aria-labelledby={SUMMARY_HEADING_ID}
          className="lg:sticky lg:top-8 lg:col-start-2 lg:row-start-1"
        >
          <TodaySummary
            rows={rows}
            summary={summary}
            onDeliverRemaining={() => dispatch({ type: "deliverRemaining" })}
          />
        </section>

        <section aria-labelledby="deliveries-heading" className="lg:col-start-1 lg:row-start-1">
          <h2 id="deliveries-heading" className="mb-3 text-base font-bold">
            Deliveries
          </h2>
          <ul className="grid gap-3">
            {rows.map((row) => {
              const recipientId = row.recipient.id;
              return (
                <DeliveryCard
                  key={recipientId}
                  row={row}
                  onDeliver={() => dispatch({ type: "deliver", recipientId })}
                  onMiss={() => dispatch({ type: "miss", recipientId })}
                  onClear={() => dispatch({ type: "clear", recipientId })}
                  onAdjust={(deltaMl) => dispatch({ type: "adjust", recipientId, deltaMl })}
                />
              );
            })}
          </ul>
        </section>
      </div>
    </>
  );
}
