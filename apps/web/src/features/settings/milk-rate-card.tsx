"use client";

import { useCallback, useState, type FormEvent } from "react";
import { IndianRupee, LoaderCircle } from "lucide-react";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";
import { Notice } from "@/components/ui/notice";
import { TextField } from "@/components/ui/text-field";
import { describeApiError } from "@/features/shared/api-error-message";
import { useResource } from "@/features/shared/use-resource";
import { formatIsoDate } from "@/lib/dates";
import { formatPaise, formatPricePerLitre, parseRupees } from "@/lib/money";
import { fieldErrors, hasProblemCode } from "@/services/api-client";
import { getHousehold } from "@/services/household-service";
import {
  addMilkRate,
  deleteMilkRate,
  getMilkRateInEffect,
  listMilkRates,
} from "@/services/milk-rate-service";
import type { ApiMilkRate } from "@/types/api";

/**
 * Settings: the price of a litre of milk, which changes over time. A new price has a first day; the days
 * before it keep the price they had. Which price is in force is decided by the server.
 */
export function MilkRateCard() {
  const load = useCallback(async (signal: AbortSignal) => {
    const [household, rates, inEffect] = await Promise.all([
      getHousehold({ signal }),
      listMilkRates({ signal }),
      getMilkRateInEffect(undefined, { signal }),
    ]);
    return { household, rates, inEffect };
  }, []);
  const { state, refresh, retry } = useResource(load);

  const needsHousehold =
    state.kind === "failed" && hasProblemCode(state.error, "household-not-set-up");

  return (
    <Card className="p-5 sm:p-6">
      <div className="flex items-center gap-3">
        <span className="flex size-11 items-center justify-center rounded-2xl bg-accent-soft text-accent">
          <IndianRupee aria-hidden className="size-5" />
        </span>
        <div>
          <h2 className="text-lg font-bold">Milk price</h2>
          <p className="text-sm text-ink-muted">
            What one litre costs. When the price changes, add the new price and the day it starts.
          </p>
        </div>
      </div>

      <div className="mt-4">
        {state.kind === "loading" ? (
          <p role="status" className="flex items-center gap-2 text-sm text-ink-muted">
            <LoaderCircle aria-hidden className="size-4 animate-spin" />
            Loading prices…
          </p>
        ) : null}

        {needsHousehold ? (
          <Notice title="Set up your household first">
            The price belongs to the household. Create it in the Household section above.
          </Notice>
        ) : null}

        {state.kind === "failed" && !needsHousehold ? (
          <Notice
            title="Couldn't load prices"
            action={
              <Button variant="secondary" onClick={retry}>
                Try again
              </Button>
            }
          >
            <span role="alert">{describeApiError(state.error)}</span>
          </Notice>
        ) : null}

        {state.kind === "ready" ? (
          <RatesPanel
            today={state.data.household.today}
            rates={state.data.rates}
            inEffect={state.data.inEffect}
            onChanged={refresh}
          />
        ) : null}
      </div>
    </Card>
  );
}

function RatesPanel({
  today,
  rates,
  inEffect,
  onChanged,
}: {
  today: string;
  rates: readonly ApiMilkRate[];
  inEffect: ApiMilkRate | null;
  onChanged: () => void;
}) {
  const [confirmingId, setConfirmingId] = useState<string | null>(null);
  const [busyId, setBusyId] = useState<string | null>(null);
  const [removeError, setRemoveError] = useState<string | null>(null);

  async function remove(rate: ApiMilkRate) {
    setBusyId(rate.id);
    setRemoveError(null);
    try {
      await deleteMilkRate(rate.id);
      setConfirmingId(null);
      onChanged();
    } catch (error) {
      setRemoveError(describeApiError(error));
    } finally {
      setBusyId(null);
    }
  }

  return (
    <div className="grid gap-5">
      <div aria-live="polite">
        {inEffect ? (
          <p className="font-display">
            <span className="text-3xl font-extrabold tabular-nums">
              {formatPricePerLitre(inEffect.pricePerLitrePaise)}
            </span>
            <span className="ml-2 text-ink-muted">
              since {formatIsoDate(inEffect.effectiveFrom)}
            </span>
          </p>
        ) : (
          <p className="text-ink-muted">
            No price is in force yet. Days before the first price have no price, and are never
            counted as free.
          </p>
        )}
      </div>

      {removeError ? (
        <Notice title="Couldn't remove the price">
          <span role="alert">{removeError}</span>
        </Notice>
      ) : null}

      {rates.length > 0 ? (
        <div>
          <h3 className="text-sm font-semibold">Price history</h3>
          <ul className="mt-2 divide-y divide-line rounded-2xl border border-line">
            {rates.map((rate) => (
              <li key={rate.id} className="px-3 py-3">
                <div className="flex flex-wrap items-center gap-x-3 gap-y-2">
                  <div className="min-w-0 flex-1 basis-40">
                    <p className="font-semibold tabular-nums">
                      {formatPricePerLitre(rate.pricePerLitrePaise)}
                    </p>
                    <p className="text-sm text-ink-muted">
                      from {formatIsoDate(rate.effectiveFrom)}
                    </p>
                  </div>
                  {inEffect?.id === rate.id ? <Badge tone="accent">In effect</Badge> : null}
                  <Button
                    variant="ghost"
                    onClick={() => setConfirmingId(rate.id)}
                    disabled={busyId === rate.id}
                    aria-label={`Remove the price from ${formatIsoDate(rate.effectiveFrom)}`}
                  >
                    Remove
                  </Button>
                </div>
                {confirmingId === rate.id ? (
                  <div
                    role="group"
                    aria-label="Confirm removing the price"
                    className="mt-3 rounded-2xl bg-canvas p-4"
                  >
                    <p className="font-semibold">
                      Remove {formatPaise(rate.pricePerLitrePaise)} from{" "}
                      {formatIsoDate(rate.effectiveFrom)}?
                    </p>
                    <p className="mt-1 text-sm text-ink-muted">
                      The days it covered use the earlier price, or have none if there is no earlier
                      one.
                    </p>
                    <div className="mt-3 flex flex-wrap gap-2">
                      <Button onClick={() => remove(rate)} disabled={busyId === rate.id}>
                        {busyId === rate.id ? "Removing…" : "Remove price"}
                      </Button>
                      <Button
                        variant="ghost"
                        onClick={() => setConfirmingId(null)}
                        disabled={busyId === rate.id}
                      >
                        Keep
                      </Button>
                    </div>
                  </div>
                ) : null}
              </li>
            ))}
          </ul>
        </div>
      ) : null}

      <AddRateForm today={today} onAdded={onChanged} />
    </div>
  );
}

function AddRateForm({ today, onAdded }: { today: string; onAdded: () => void }) {
  const [price, setPrice] = useState("");
  const [effectiveFrom, setEffectiveFrom] = useState(today);
  const [errors, setErrors] = useState<Record<string, string>>({});
  const [formError, setFormError] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setFormError(null);

    const parsed = parseRupees(price);
    const problems: Record<string, string> = {};
    if (!parsed.ok) problems.pricePerLitrePaise = parsed.message;
    if (effectiveFrom === "") problems.effectiveFrom = "Choose the first day this price applies.";
    if (!parsed.ok || Object.keys(problems).length > 0) {
      setErrors(problems);
      return;
    }

    setErrors({});
    setSaving(true);
    try {
      await addMilkRate({ effectiveFrom, pricePerLitrePaise: parsed.paise });
      setPrice("");
      setEffectiveFrom(today);
      onAdded();
    } catch (error) {
      const fields = fieldErrors(error);
      if (hasProblemCode(error, "rate-date-taken")) {
        setErrors({
          effectiveFrom:
            "A price already starts on that day. Remove it first, or choose another day.",
        });
      } else if (Object.keys(fields).length > 0) {
        setErrors(fields);
      } else {
        setFormError(describeApiError(error));
      }
    } finally {
      setSaving(false);
    }
  }

  return (
    <form noValidate onSubmit={submit} aria-label="Add a price" className="grid gap-4">
      <h3 className="text-sm font-semibold">Add a price</h3>
      <TextField
        id="rate-price"
        label="Price of one litre (₹)"
        hint="For example 62.50."
        inputMode="decimal"
        autoComplete="off"
        value={price}
        onChange={(event) => setPrice(event.target.value)}
        error={errors.pricePerLitrePaise}
      />
      <TextField
        id="rate-effective-from"
        label="Applies from"
        hint="Choose an earlier day if the price changed before you entered it."
        type="date"
        value={effectiveFrom}
        onChange={(event) => setEffectiveFrom(event.target.value)}
        error={errors.effectiveFrom}
      />
      {formError ? (
        <p role="alert" className="text-sm font-semibold text-missed-ink">
          {formError}
        </p>
      ) : null}
      <div>
        <Button type="submit" disabled={saving}>
          {saving ? "Adding…" : "Add price"}
        </Button>
      </div>
    </form>
  );
}
