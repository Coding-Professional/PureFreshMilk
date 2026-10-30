# 0004. Domain model for households, recipients, milk rates and daily deliveries

- **Status:** Proposed
- **Date:** 2026-10-30
- **Deciders:** Project maintainer (pending review)

## Context

The foundation stores nothing. The first working slice persists four things: a household, the
recipients who get milk, the price of milk over time, and one delivery record per recipient per day.
Monthly bills and payments come later. They are designed here, but not built, so that the tables
created now never have to change shape when billing arrives.

What makes these choices hard to reverse:

- Money and dates end up in every bill. Changing how they are represented later means migrating
  history that people rely on.
- [ADR 0002](0002-quantities-in-whole-millilitres.md) fixed quantities as whole millilitres and left
  money to a later decision. This is that decision.
- Authentication and household isolation come later, but every table created now must already be
  ready for them.
- The Today prototype (`apps/web/src/features/today`) already defines vocabulary: a recipient has an
  `expectedMl`, an entry has a status and a `deliveredMl`.

The design details live in two companion documents so that this record stays about decisions:

- [Domain model](../domain-model.md): entities, tables, constraints and the invariants that need tests.
- [Slice 1 plan](../../plans/slice-1-recipients-and-deliveries.md): API endpoints, the two pull
  requests, and the questions that still need a decision.

## Decision

The maintainer proposed seven decisions. Each was checked against the code and the prototype. All
seven stand; five need changes to be safe, and those changes are what this record adds.

| # | Proposed decision | Verdict |
| --- | --- | --- |
| 1 | Money as whole paise (`long`) | Confirmed, with a rounding rule |
| 2 | Milk rates with an effective-from date | Confirmed, but prices are derived, not stored on deliveries |
| 3 | One delivery per recipient per day, delivered or missed | Confirmed |
| 4 | Recipients are archived, never deleted | Confirmed, but with dates instead of a flag |
| 5 | `HouseholdId` on every aggregate | Confirmed, but a column alone is not isolation |
| 6 | `DateOnly` in the household's time zone | Confirmed |
| 7 | Bills calculated on demand, then frozen | Confirmed, with the ledger rules spelled out |

### 1. Money is whole paise, rounded once per bill line

- Money is a signed `long` of **paise** (PostgreSQL `bigint`). The only currency is INR; a second
  currency would need its own decision. Names carry the unit, as with millilitres:
  `price_per_litre_paise` in the database and `pricePerLitrePaise` in JSON.
- **Change.** Prices are per litre and quantities are millilitres, so an amount is not automatically
  a whole number of paise: 250 ml at ₹62.50 a litre is 1562.5 paise. The rule is
  `amount = round_half_up(millilitres × paise_per_litre ÷ 1000)`, applied **once per bill line**
  (one recipient at one rate), never per delivery.
- Why per line: 30 days of 250 ml at ₹62.50 is 7,500 ml. One rounding gives 46,875 paise, the figure
  a calculator gives for 7.5 L × ₹62.50. Rounding each delivery gives 30 × 1,563 = 46,890 paise, which
  overstates the bill by 15 paise and cannot be reproduced by hand.
- Bounds keep the arithmetic safe in `long`: a price is 1 to 1,000,000 paise a litre (₹10,000) and a
  delivery is at most 20,000 ml. Arithmetic is `checked`, as in `Quantity`.
- Quantities stay non-negative. Money may be negative, because adjustments can be.

### 2. Rates are effective-dated, and prices are derived

- A rate is a price per litre with an **effective-from** date. A delivery is priced at the rate with
  the latest effective-from on or before its delivery date. There is one rate per household per
  effective-from date.
- **Change.** Deliveries store no price and no amount. A delivery records what arrived; its price is
  a policy applied later. Storing the price on the delivery would either leave early deliveries
  unpriced when the rate is entered late (the normal case: the price rise is announced, and entered a
  few days afterwards) or force stored prices to be rewritten.
- So "rate changes never rewrite past amounts" holds for **finalised** bills (decision 7). Amounts
  for months that are not finalised always follow the current rate table, including a back-dated
  rate, and the UI must label them as not final. Once a finalised bill covers a date, the rate that
  governs that date can no longer be changed or deleted.
- A date before the first rate has no price. Deliveries can still be recorded, and finalising a
  month that has delivered milk on an unpriced date is refused rather than billed at zero.
- One price series per household. Whether recipients can have their own price is an open question.

### 3. One delivery per recipient per day

- Confirmed as proposed: unique per recipient and calendar day; status `delivered` or `missed`; the
  actual quantity is stored. A day with no record is **not recorded**, never assumed delivered.
- The default quantity is **copied** into the row when the delivery is recorded. A later change to the
  recipient's usual quantity does not alter history.
- Added rules: `delivered` needs a quantity of at least 1 ml; `missed` has quantity 0; a date after
  the household's today is rejected; the recipient must be active on that date (decision 4).
- "Not recorded" is the absence of a row, not a `pending` row. A pending row would have to be created
  for every recipient every day, and a job that did not run would look like a decision. The API
  reports it as `notRecorded`; the prototype's `pending` is renamed to match.
- Whether one record a day is enough (a morning and an evening delivery) is an open question.

### 4. Recipients are archived with dates, never deleted

- Confirmed: no hard delete, no DELETE endpoint, and the foreign key from deliveries is `RESTRICT`.
- **Change.** A recipient has a required `starts_on` and an optional `archived_from` (the first day
  without deliveries). The recipient is active on date *d* when `starts_on ≤ d < archived_from`.
  "Not recorded" only means something on days when a delivery was expected. A flag cannot say that a
  recipient added on 20 November owed nothing before then, and someone who starts using the app
  mid-month needs to enter earlier days.
- Names are unique among active recipients, ignoring case, so a double tap cannot create the same
  person twice and the Today list is unambiguous. An archived name can be reused.
- Archiving is not erasure. If a person asks for their name to be removed, the name is replaced and
  the billing history stays.

### 5. Every aggregate carries `HouseholdId`, and the database enforces it

- Confirmed: every household-owned table has a `NOT NULL household_id`, and it is the first column
  of that table's indexes.
- **Change.** A column alone does not stop a delivery pointing at another household's recipient.
  Child tables use composite foreign keys, `(household_id, recipient_id)` into
  `recipients (household_id, id)`. In the Domain, ids are typed (`HouseholdId`, `RecipientId`) so one
  cannot be passed where the other is expected.
- Until authentication exists there is no current household. Slice 1 runs in **single-household
  mode** behind an `ICurrentHousehold` abstraction in Application. URLs carry no household id, so
  the later change (a claims-based implementation plus an EF Core global filter) alters no API route.
- Single-household mode reads and writes without a login. It is therefore proposed that the data
  endpoints are not mapped outside Development and Testing unless a setting turns them on.

### 6. Dates are `DateOnly` in the household's time zone

- Confirmed: `DateOnly` in the domain, `date` in PostgreSQL, `YYYY-MM-DD` in JSON. Default time zone
  `Asia/Kolkata`, stored as an IANA id on the household.
- The **server** decides what "today" is, from `TimeProvider` and the household's zone. The web app
  never sends a "today" it computed itself: a phone abroad would record against the wrong day.
- A month is a calendar month in the household's zone. Instants such as `created_at` are `timestamptz`
  in UTC.
- Time zone ids need time zone data at runtime. Slim container images can lack it, so a test must
  fail early if `Asia/Kolkata` does not resolve.

### 7. Bills are calculated on demand until finalised, then frozen

- Confirmed: no bill is stored until it is finalised. A finalised bill is an immutable snapshot, and a
  later correction is an adjustment, never an edit.
- Planned shape, not built in slice 1: a finalised bill stores its lines (the recipient's name as it
  was, millilitres, price per litre, amount; one line per recipient and rate) and its totals.
- A balance is never a stored number. It is derived: finalised bills, plus adjustments, minus payments.
  Adjustments are signed and carry a reason; an opening balance is an adjustment. Payments are
  positive and are voided rather than deleted.
- Deliveries inside a finalised month become read-only. A correction goes in as an adjustment.
  Voiding or reopening a bill is deliberately not designed yet.

### Further decisions

- **`Delivery` is its own aggregate**, not a child of `Recipient`: it grows without bound, and
  loading a recipient must not load years of deliveries. `Household`, `Recipient`, `MilkRate` and
  `Delivery` are separate roots. Rules that span rows live in a pure Domain type (`RateSchedule`) or
  in an Application handler.
- **Identifiers** are UUIDv7 (`Guid.CreateVersion7`), created in the application and held in typed
  wrappers. They are ordered by time, so inserts stay index-friendly, and they cannot be guessed.
- **Status** is stored as text with a `CHECK` constraint, not a native PostgreSQL enum (awkward to
  migrate) and not a number (unreadable in SQL).
- **Names for units** follow ADR 0002: `expected_ml`, `delivered_ml`, `…_paise`.

## Consequences

- Totals stay exact and agree between the server and any client. Billing adds tables; it does not
  change the four created in slice 1.
- Past months stay stable once finalised, and a late price change still reaches months that are open.
- The database refuses cross-household references, so isolation does not depend on every query being
  written correctly.
- Pricing needs a rate lookup wherever an amount is shown. The tables are tiny, so this costs little.
- The Today screen has to handle "not recorded" as a real state, and the unrecorded days of a month
  have to be shown before it is finalised.
- Composite keys, typed ids and a generated column make the EF Core configuration longer than a
  basic model.
- **Risks to manage:** single-household mode has no login, so slice 1 is for local use only; there is
  no backup for the local Docker volume, and `docker compose down --volumes` deletes real data; time
  zone data may be missing in some container images.
- **Follow-up work:** the two pull requests in the slice plan; a billing ADR (rounding of the monthly
  total, the lock policy for finalised months); delivery pauses; authentication and household
  isolation.

## Alternatives considered

- **Money as decimal rupees or floating point.** The same objection as in ADR 0002: every boundary
  between .NET, JSON and JavaScript must keep the decimal exact. Floating point drifts.
- **Rounding each delivery, or storing a price per millilitre.** Per-delivery rounding overstates
  bills and cannot be checked with a calculator. A price in thousandths of a paisa is exact but
  unnatural to enter and read.
- **A price stored on every delivery.** Simple to read, but wrong when a rate is entered late and
  duplicated across thousands of rows.
- **Rates with a from and a to date.** Ranges can overlap or leave gaps. An effective-from date
  cannot.
- **A `pending` delivery row for every recipient and day.** Needs a job to create it, and turns a
  missed job into false data.
- **An `is_archived` flag, or deleting recipients with cascading deletes.** A flag loses when the
  recipient was active. Cascades destroy billing history.
- **Only a `household_id` column plus a query filter.** One missed filter leaks data. PostgreSQL
  row-level security is a good extra layer later, but it needs a per-request session setting and a
  database role that does not own the tables. A schema or database per household is far too heavy.
- **`DateTime` or `DateTimeOffset` for dates, or a "today" sent by the browser.** Midnight
  conventions and zone conversions cause off-by-one days.
- **Always recomputing bills, never freezing them.** History would change whenever a rate does.
- **A stored running balance.** It cannot be audited and would be exposed to lost updates.
- **Editable finalised bills with an audit log.** More machinery than adjustments, and a weaker
  guarantee.
- **Database-generated ids (identity columns).** Predictable, and unable to create a record without
  a round trip.
