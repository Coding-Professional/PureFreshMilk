"use client";

import { useEffect, useRef, useState, type FormEvent } from "react";
import { Button } from "@/components/ui/button";
import { TextField } from "@/components/ui/text-field";
import { describeApiError } from "@/features/shared/api-error-message";
import { formatQuantity } from "@/lib/quantity";
import { fieldErrors, hasProblemCode } from "@/services/api-client";
import { addRecipient, updateRecipient } from "@/services/recipient-service";
import type { ApiRecipient } from "@/types/api";

const WHOLE_NUMBER = /^\d+$/;

/** Adds a recipient, or changes the name, usual quantity and start date of an existing one. */
export function RecipientForm({
  recipient,
  today,
  onSaved,
  onCancel,
}: {
  /** The recipient being edited, or undefined when adding. */
  recipient?: ApiRecipient;
  /** The household's calendar date now, YYYY-MM-DD, from the server. */
  today: string;
  onSaved: () => void;
  onCancel: () => void;
}) {
  const idPrefix = recipient ? `recipient-${recipient.id}` : "recipient-new";
  const nameRef = useRef<HTMLInputElement>(null);

  const [name, setName] = useState(recipient?.name ?? "");
  const [expectedMl, setExpectedMl] = useState(recipient ? String(recipient.expectedMl) : "");
  const [startsOn, setStartsOn] = useState(recipient?.startsOn ?? today);
  const [errors, setErrors] = useState<Record<string, string>>({});
  const [formError, setFormError] = useState<string | null>(null);
  const [saving, setSaving] = useState(false);

  // Move straight to the first field when the form opens.
  useEffect(() => nameRef.current?.focus(), []);

  const quantityHint = WHOLE_NUMBER.test(expectedMl.trim())
    ? `That is ${formatQuantity(Number(expectedMl))} a day.`
    : "In millilitres, for example 500 or 1000.";

  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setFormError(null);

    const problems: Record<string, string> = {};
    if (!WHOLE_NUMBER.test(expectedMl.trim())) {
      problems.expectedMl = "Enter a whole number of millilitres, for example 500.";
    }
    if (startsOn === "") problems.startsOn = "Choose the first day a delivery is expected.";
    if (Object.keys(problems).length > 0) {
      setErrors(problems);
      return;
    }

    setErrors({});
    setSaving(true);
    try {
      const input = { name, expectedMl: Number(expectedMl.trim()), startsOn };
      if (recipient) await updateRecipient(recipient.id, input);
      else await addRecipient(input);
      onSaved();
    } catch (error) {
      const fields = fieldErrors(error);
      if (hasProblemCode(error, "recipient-name-taken")) {
        setErrors({ name: "Another recipient is already called that. Use a different name." });
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
    <form
      noValidate
      onSubmit={submit}
      aria-label={recipient ? `Edit ${recipient.name}` : "Add a recipient"}
      className="grid gap-4"
    >
      <TextField
        ref={nameRef}
        id={`${idPrefix}-name`}
        label="Name"
        autoComplete="off"
        value={name}
        onChange={(event) => setName(event.target.value)}
        error={errors.name}
      />
      <TextField
        id={`${idPrefix}-quantity`}
        label="Usual daily quantity (ml)"
        hint={quantityHint}
        inputMode="numeric"
        autoComplete="off"
        value={expectedMl}
        onChange={(event) => setExpectedMl(event.target.value)}
        error={errors.expectedMl}
      />
      <TextField
        id={`${idPrefix}-starts-on`}
        label="First delivery on"
        hint="Leave it as today unless this person already gets milk."
        type="date"
        value={startsOn}
        onChange={(event) => setStartsOn(event.target.value)}
        error={errors.startsOn}
      />
      {formError ? (
        <p role="alert" className="text-sm font-semibold text-missed-ink">
          {formError}
        </p>
      ) : null}
      <div className="flex flex-wrap gap-2">
        <Button type="submit" disabled={saving}>
          {saving ? "Saving…" : "Save recipient"}
        </Button>
        <Button variant="ghost" onClick={onCancel} disabled={saving}>
          Cancel
        </Button>
      </div>
    </form>
  );
}
