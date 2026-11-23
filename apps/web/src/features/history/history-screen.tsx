"use client";

import { useCallback } from "react";
import Link from "next/link";
import { LoaderCircle } from "lucide-react";
import { PageHeader } from "@/components/layout/page-header";
import { DemoBadge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { Notice } from "@/components/ui/notice";
import { describeApiError } from "@/features/shared/api-error-message";
import { useResource } from "@/features/shared/use-resource";
import { formatIsoDate } from "@/lib/dates";
import { formatQuantity } from "@/lib/quantity";
import { getApiBaseUrl } from "@/services/api-client";
import { hasProblemCode } from "@/services/api-client";
import { listDaySummaries } from "@/services/delivery-service";
import { getHousehold } from "@/services/household-service";

export function HistoryScreen() {
  return getApiBaseUrl() ? <ConnectedHistory /> : <DemoHistory />;
}

function DemoHistory() {
  return (
    <>
      <PageHeader title="History" actions={<DemoBadge />} />
      <Notice title="History needs saved deliveries">
        The Today demo resets on reload. Connect the API to record days and see them here.
      </Notice>
    </>
  );
}

function ConnectedHistory() {
  const load = useCallback(async (signal: AbortSignal) => {
    const household = await getHousehold({ signal });
    const end = new Date(`${household.today}T00:00:00Z`);
    end.setUTCDate(end.getUTCDate() - 29);
    const from = end.toISOString().slice(0, 10);
    return { summaries: await listDaySummaries(from, household.today, { signal }) };
  }, []);
  const { state, retry } = useResource(load);

  if (state.kind === "loading")
    return (
      <>
        <PageHeader title="History" />
        <p role="status" className="flex items-center gap-2 text-ink-muted">
          <LoaderCircle aria-hidden className="size-4 animate-spin" />
          Loading history…
        </p>
      </>
    );
  if (state.kind === "failed")
    return (
      <>
        <PageHeader title="History" />
        {hasProblemCode(state.error, "household-not-set-up") ? (
          <Notice title="Set up your household first">
            Start on the{" "}
            <Link href="/app/recipients" className="font-semibold text-accent underline">
              Recipients page
            </Link>
            , then your delivery history will appear here.
          </Notice>
        ) : (
          <Notice
            title="Couldn't load history"
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

  return (
    <>
      <PageHeader
        title="History"
        description="The last 30 days, newest first. Open a day to review or correct deliveries."
      />
      <Card className="p-2 sm:p-3">
        <ul className="divide-y divide-line">
          {state.data.summaries.map(({ date, totals }) => (
            <li key={date}>
              <Link
                href={`/app/history/${date}`}
                className="flex min-h-16 flex-wrap items-center justify-between gap-2 rounded-xl px-3 py-3 transition-colors hover:bg-canvas focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-accent"
              >
                <span className="font-semibold">{formatIsoDate(date)}</span>
                <span className="text-sm text-ink-muted">
                  {totals.expectedMl === 0
                    ? "No deliveries expected"
                    : totals.delivered + totals.missed === 0
                      ? "Not recorded"
                      : `${formatQuantity(totals.deliveredMl)} delivered · ${totals.delivered} delivered · ${totals.missed} missed`}
                  {totals.notRecorded > 0 ? ` · ${totals.notRecorded} to mark` : ""}
                </span>
              </Link>
            </li>
          ))}
        </ul>
      </Card>
    </>
  );
}
