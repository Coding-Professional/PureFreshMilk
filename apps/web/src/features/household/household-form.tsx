"use client";

import { useState, type FormEvent } from "react";
import { Button } from "@/components/ui/button";
import { SelectField } from "@/components/ui/select-field";
import { TextField } from "@/components/ui/text-field";
import { describeApiError } from "@/features/shared/api-error-message";
import { DEFAULT_TIME_ZONE_ID, timeZoneOptions } from "@/lib/time-zones";
import { fieldErrors } from "@/services/api-client";
import { setUpHousehold } from "@/services/household-service";
import type { ApiHousehold } from "@/types/api";

/**
 * Creates the household (first run) or changes its name and time zone. The household has to exist before
 * anything else can be saved, because everything belongs to it.
 */
export function HouseholdForm({
  household,
  submitLabel,
  onSaved,
}: {
  /** The existing household, or undefined when it is being created. */
  household?: ApiHousehold;
  submitLabel: string;
  onSaved: (household: ApiHousehold) => void;
}) {
  const [name, setName] = useState(household?.name ?? "");
  const [timeZoneId, setTimeZoneId] = useState(household?.timeZoneId ?? DEFAULT_TIME_ZONE_ID);
  const [errors, setErrors] = useState<Record<string, string>>({});
  const [formError, setFormError] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setSaving(true);
    setErrors({});
    setFormError(null);

    try {
      onSaved(await setUpHousehold({ name, timeZoneId }));
    } catch (error) {
      const fields = fieldErrors(error);
      if (Object.keys(fields).length > 0) setErrors(fields);
      else setFormError(describeApiError(error));
    } finally {
      setSaving(false);
    }
  }

  return (
    <form noValidate onSubmit={submit} className="grid gap-4">
      <TextField
        id="household-name"
        label="Household name"
        hint="Only you see this, for example “Our home”."
        autoComplete="off"
        value={name}
        onChange={(event) => setName(event.target.value)}
        error={errors.name}
      />
      <SelectField
        id="household-time-zone"
        label="Time zone"
        hint="Decides what “today” means. Deliveries are recorded by calendar day in this time zone."
        value={timeZoneId}
        onChange={(event) => setTimeZoneId(event.target.value)}
        options={timeZoneOptions(household?.timeZoneId).map((zone) => ({
          value: zone.id,
          label: zone.label,
        }))}
        error={errors.timeZoneId}
      />
      {formError ? (
        <p role="alert" className="text-sm font-semibold text-missed-ink">
          {formError}
        </p>
      ) : null}
      <div>
        <Button type="submit" disabled={saving}>
          {saving ? "Saving…" : submitLabel}
        </Button>
      </div>
    </form>
  );
}
