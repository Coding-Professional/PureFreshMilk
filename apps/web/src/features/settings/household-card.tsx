"use client";

import { useCallback } from "react";
import { House, LoaderCircle } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { Notice } from "@/components/ui/notice";
import { HouseholdForm } from "@/features/household/household-form";
import { describeApiError } from "@/features/shared/api-error-message";
import { useResource } from "@/features/shared/use-resource";
import { hasProblemCode } from "@/services/api-client";
import { getHousehold } from "@/services/household-service";

/** Settings: the household's name and time zone. Also where a brand-new household is created. */
export function HouseholdCard() {
  const load = useCallback((signal: AbortSignal) => getHousehold({ signal }), []);
  const { state, refresh, retry } = useResource(load);

  const needsSetUp = state.kind === "failed" && hasProblemCode(state.error, "household-not-set-up");

  return (
    <Card className="p-5 sm:p-6">
      <div className="flex items-center gap-3">
        <span className="flex size-11 items-center justify-center rounded-2xl bg-accent-soft text-accent">
          <House aria-hidden className="size-5" />
        </span>
        <div>
          <h2 className="text-lg font-bold">Household</h2>
          <p className="text-sm text-ink-muted">
            {needsSetUp
              ? "Not set up yet. Everything else belongs to the household."
              : "Its name, and the time zone that decides what “today” means."}
          </p>
        </div>
      </div>

      <div className="mt-4">
        {state.kind === "loading" ? (
          <p role="status" className="flex items-center gap-2 text-sm text-ink-muted">
            <LoaderCircle aria-hidden className="size-4 animate-spin" />
            Loading the household…
          </p>
        ) : null}

        {state.kind === "failed" && !needsSetUp ? (
          <Notice
            title="Couldn't load the household"
            action={
              <Button variant="secondary" onClick={retry}>
                Try again
              </Button>
            }
          >
            <span role="alert">{describeApiError(state.error)}</span>
          </Notice>
        ) : null}

        {needsSetUp ? <HouseholdForm submitLabel="Create household" onSaved={retry} /> : null}

        {state.kind === "ready" ? (
          // Keyed by the saved values, so the form starts again from what the server now holds.
          <HouseholdForm
            key={`${state.data.name}|${state.data.timeZoneId}`}
            household={state.data}
            submitLabel="Save household"
            onSaved={refresh}
          />
        ) : null}
      </div>
    </Card>
  );
}
