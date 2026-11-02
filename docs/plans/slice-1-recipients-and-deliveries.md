# Slice 1 plan: recipients, milk rates and daily deliveries

> **Status: PR A is built (see [PR A as built](#pr-a-as-built)); PR B is not.** The model it builds is
> described in [Domain model](../architecture/domain-model.md) and justified in
> [ADR 0004](../architecture/decisions/0004-domain-model-for-household-deliveries.md).

Slice 1 is the first time PureFreshMilk stores anything. It replaces the demo data behind the
Recipients and Today screens with a real household, in two pull requests:

| PR | Delivers | Replaces demo data on |
| --- | --- | --- |
| A | Household, recipients and milk rates, end to end | Recipients, Settings |
| B | Daily deliveries and history, end to end | Today, History |

Out of scope for the slice: monthly bills, payments, authentication, multiple households, delivery
pauses, deployment. Money appears only as the milk price a household enters.

## API

All routes are under `/api/v1`. They follow the conventions already in
[the architecture overview](../architecture/overview.md): feature folders, thin endpoints, explicit
request and response records, RFC 9457 Problem Details.

### Conventions

| Topic | Rule |
| --- | --- |
| Names | camelCase JSON. Units are in the name: `expectedMl`, `deliveredMl`, `pricePerLitrePaise`. |
| Dates and times | A date is `"2026-10-30"`. An instant is ISO 8601 in UTC. Never a `Date` built from a date-only string in the browser. |
| Enums | Camel-case strings (`"delivered"`), never numbers. |
| Ids | UUID strings. |
| Lists | An object with an `items` array, so fields can be added later without breaking clients. |
| Errors | `400` for input that is invalid, `404` for something that does not exist, `409` for a clash with stored data. Problem Details with an extra `code` member that clients can branch on (for example `recipient-name-taken`). No stack traces. |
| Retries | `PUT` and `DELETE` are idempotent, so a flaky phone connection can safely retry them. |
| Household | Not in any URL. The server resolves it ([single-household mode](#questions-to-decide-before-pr-a)). |
| Authentication | None yet. Adding it later changes no route. |

### Household and recipients (PR A)

| Method and path | What it does | Success | Errors |
| --- | --- | --- | --- |
| `GET /household` | Reads the household, including `today` in its time zone. | 200 | 404 `household-not-set-up` |
| `PUT /household` | Creates the household the first time, then updates it. Body: `name`, `timeZoneId`. | 201, then 200 | 400 |
| `GET /recipients?includeArchived=false` | Lists recipients, ordered by name. | 200 | |
| `POST /recipients` | Adds a recipient. Body: `name`, `expectedMl`, optional `startsOn` (default: today). | 201 and `Location` | 400, 409 `recipient-name-taken` |
| `GET /recipients/{id}` | Reads one. | 200 | 404 |
| `PUT /recipients/{id}` | Replaces the editable fields: `name`, `expectedMl`, `startsOn`. | 200 | 400, 404, 409 |
| `POST /recipients/{id}/archive` | Archives from `archivedFrom` (default: the later of today and the day after the latest delivery). | 200 | 400, 404, 409 `recipient-already-archived` |
| `POST /recipients/{id}/unarchive` | Clears the archive date. | 200 | 404, 409 `recipient-name-taken` |

There is deliberately no `DELETE /recipients/{id}`.

```json
{
  "id": "0199f3a2-7c1e-7b52-9d0a-3f1c2a4b5c60",
  "name": "Recipient A",
  "expectedMl": 1000,
  "startsOn": "2026-10-01",
  "archivedFrom": null
}
```

### Milk rates (PR A)

| Method and path | What it does | Success | Errors |
| --- | --- | --- | --- |
| `GET /milk-rates` | Lists rates, newest effective date first. | 200 | |
| `GET /milk-rates/in-effect?on=2026-10-30` | The rate in force on a date (default: today). | 200 | 404 `no-rate-in-effect` |
| `POST /milk-rates` | Adds a rate. Body: `effectiveFrom`, `pricePerLitrePaise`. | 201 | 400, 409 `rate-date-taken` |
| `PUT /milk-rates/{id}` | Changes a rate. | 200 | 400, 404, 409 |
| `DELETE /milk-rates/{id}` | Removes a rate. | 204 | 404 |

```json
{ "id": "0199f3a2-8d4f-7e10-a6b3-1c9d0e7f2a11", "effectiveFrom": "2026-10-01", "pricePerLitrePaise": 6250 }
```

Once billing exists, `PUT` and `DELETE` also answer `409 period-finalised` for a rate that governs a
finalised month.

### Day sheets and history (PR B)

`{date}` is `YYYY-MM-DD` or the word `today`, which the server resolves in the household's zone.

| Method and path | What it does | Success | Errors |
| --- | --- | --- | --- |
| `GET /day-sheets/{date}` | One row per recipient active that day, with its state and the day's totals. | 200 | 400 `future-date` |
| `PUT /recipients/{recipientId}/deliveries/{date}` | Records a delivery. Body: `status`, optional `deliveredMl` (default: the usual quantity). | 201 new, 200 replaced | 400, 404, 409 `recipient-not-active` |
| `DELETE /recipients/{recipientId}/deliveries/{date}` | Clears the record; the day returns to `notRecorded`. | 204 | 404 (recipient) |
| `POST /day-sheets/{date}/deliver-remaining` | Records every `notRecorded` recipient as delivered at the usual quantity, in one transaction. Returns the day sheet. | 200 | 400 `future-date` |
| `GET /day-summaries?from=2026-10-01&to=2026-10-31` | One line of totals per day, newest first. At most 366 days. | 200 | 400 |

`PUT` and `DELETE` also answer `409 period-finalised` once billing exists.

```json
{
  "date": "2026-10-30",
  "isToday": true,
  "rows": [
    { "recipientId": "0199f3a2-7c1e-7b52-9d0a-3f1c2a4b5c60", "name": "Recipient A", "expectedMl": 1000, "status": "delivered", "deliveredMl": 1000 },
    { "recipientId": "0199f3a2-7c9a-7d33-8e41-6b2f9a0c1d72", "name": "Recipient B", "expectedMl": 500, "status": "notRecorded", "deliveredMl": 0 }
  ],
  "totals": { "expectedMl": 1500, "deliveredMl": 1000, "delivered": 1, "missed": 0, "notRecorded": 1 }
}
```

The server computes the totals. `features/today/today-state.ts` does that arithmetic on the client
today only because nothing is stored; PR B removes it.

## Test strategy

- **No new dependencies.** Fakes are hand-written; PostgreSQL is the Compose container that CI
  already starts.
- **`dotnet test` stays database-free by default.** Tests that need PostgreSQL carry
  `[Trait("Category", "Database")]`, live in `PureFreshMilk.Infrastructure.Tests`, and are excluded
  from the `api` job and from `scripts/check.*` with
  `--filter-not-trait "Category=Database" --ignore-exit-code 8` (the second option because the test
  project that holds only those tests then runs none, which Microsoft.Testing.Platform reports as exit
  code 8). The `database` job runs them. A database test never skips silently: with no connection string it fails and says how
  to start PostgreSQL.
- **Each database test class gets a throwaway database**, created from the migrations and dropped
  afterwards, through `ConnectionStrings__PureFreshMilkTests`.
- **Contract tests for the API use in-memory repositories**, registered in
  `PureFreshMilkApiFactory`, so they need no database. The database behaviour is covered by the
  Infrastructure tests.
- **The `database` CI job gets more steps, not a new name.** The ruleset on `main` matches check
  names literally, so `Database — Compose PostgreSQL + API readiness` must not change.
- **Browser tests keep running without an API** (demo mode). Real-data flows are covered by Vitest
  with a stubbed `fetch` and by the API tests.

## PR A: household, recipients and milk rates

Branch `feat/households-recipients-rates`. Suggested commit order inside the pull request, so it can
be reviewed layer by layer: Domain with tests, Application with tests, Infrastructure with migration
and database tests, Api with contract tests, web app, CI and docs.

### Domain (`backend/src/PureFreshMilk.Domain/`)

New:

- `Households/HouseholdId.cs`, `Households/Household.cs`
- `Recipients/RecipientId.cs`, `Recipients/Recipient.cs`
- `Rates/MilkRateId.cs`, `Rates/PricePerLitre.cs`, `Rates/MilkRate.cs`, `Rates/RateSchedule.cs`

### Application (`backend/src/PureFreshMilk.Application/`)

New:

- `Common/NotFoundException.cs`, `Common/ConflictException.cs` (both carry a stable `Code`)
- `Persistence/IUnitOfWork.cs`
- `Households/ICurrentHousehold.cs`, `Households/IHouseholdRepository.cs`,
  `Households/HouseholdDetails.cs`, `Households/GetHouseholdHandler.cs`,
  `Households/SetUpHouseholdHandler.cs`
- `Recipients/IRecipientRepository.cs`, `Recipients/RecipientDetails.cs`,
  `Recipients/ListRecipientsHandler.cs`, `Recipients/GetRecipientHandler.cs`,
  `Recipients/AddRecipientHandler.cs`, `Recipients/UpdateRecipientHandler.cs`,
  `Recipients/ArchiveRecipientHandler.cs`, `Recipients/UnarchiveRecipientHandler.cs`
- `Rates/IMilkRateRepository.cs`, `Rates/MilkRateDetails.cs`, `Rates/ListMilkRatesHandler.cs`,
  `Rates/GetRateInEffectHandler.cs`, `Rates/AddMilkRateHandler.cs`,
  `Rates/UpdateMilkRateHandler.cs`, `Rates/DeleteMilkRateHandler.cs`

Edited: `ApplicationServiceCollectionExtensions.cs` (register the handlers).

### Infrastructure (`backend/src/PureFreshMilk.Infrastructure/`)

New:

- `Persistence/Configurations/HouseholdConfiguration.cs`, `RecipientConfiguration.cs`,
  `MilkRateConfiguration.cs`
- `Persistence/Repositories/HouseholdRepository.cs`, `RecipientRepository.cs`,
  `MilkRateRepository.cs`
- `Persistence/UnitOfWork.cs`
- `Persistence/MigrationsAppliedHealthCheck.cs` (optional: `/health/ready` fails while migrations are
  pending, instead of every request failing with "relation does not exist")
- `Households/SingleHouseholdProvider.cs` (the `ICurrentHousehold` for single-household mode)
- `Persistence/Migrations/<timestamp>_AddHouseholdsRecipientsAndMilkRates.cs`, its `.Designer.cs`,
  and `PureFreshMilkDbContextModelSnapshot.cs` (generated with `dotnet ef`, see
  [development.md](../development.md#database-migrations))

Edited: `Persistence/PureFreshMilkDbContext.cs` (sets, `ApplyConfigurationsFromAssembly`, converters
for typed ids), `InfrastructureServiceCollectionExtensions.cs`, `PureFreshMilk.Infrastructure.csproj`
(`InternalsVisibleTo` for the new test project; the context stays `internal`).

### Api (`backend/src/PureFreshMilk.Api/`)

New:

- `Features/Households/HouseholdEndpoints.cs`, `HouseholdResponse.cs`, `SetUpHouseholdRequest.cs`
- `Features/Recipients/RecipientEndpoints.cs`, `RecipientResponse.cs`, `RecipientRequests.cs`
- `Features/MilkRates/MilkRateEndpoints.cs`, `MilkRateResponse.cs`, `MilkRateRequests.cs`
- `Hosting/ApplicationExceptionHandler.cs` (maps `DomainException` to 400,
  `NotFoundException` to 404, `ConflictException` to 409, all as Problem Details)
- `Hosting/UnauthenticatedAccess.cs` (the guard described under the questions below)

Edited: `Program.cs` (map the endpoints, string enums, the guard), `appsettings.Development.json`
(`Access:AllowUnauthenticated`: `true`).

### Backend tests (`backend/tests/`)

New:

- `PureFreshMilk.Domain.Tests/Households/HouseholdTests.cs`,
  `Recipients/RecipientTests.cs`, `Rates/PricePerLitreTests.cs`, `Rates/MilkRateTests.cs`,
  `Rates/RateScheduleTests.cs`
- `PureFreshMilk.TestSupport/` (class library): in-memory repositories, `FixedTimeProvider`
- `PureFreshMilk.Application.Tests/` (project): `Households/…`, `Recipients/…`, `Rates/…` handler tests
- `PureFreshMilk.Infrastructure.Tests/` (project): `DatabaseFixture.cs`, `MigrationTests.cs`,
  `RecipientConstraintTests.cs`, `MilkRateConstraintTests.cs`, `RepositoryRoundTripTests.cs`
- `PureFreshMilk.Api.Tests/Features/Households/HouseholdEndpointTests.cs`,
  `Features/Recipients/RecipientEndpointTests.cs`, `Features/MilkRates/MilkRateEndpointTests.cs`,
  `Hosting/UnauthenticatedAccessTests.cs`

Edited: `PureFreshMilk.Api.Tests/PureFreshMilkApiFactory.cs`, `backend/PureFreshMilk.slnx` (three
projects). The invariants covered are marked **PR A** in the
[invariant tables](../architecture/domain-model.md#invariants-that-need-tests).

### Web app (`apps/web/`)

New:

- `src/services/household-service.ts`, `recipient-service.ts`, `milk-rate-service.ts`
- `src/lib/money.ts` (display only: 6250 paise → "₹62.50")
- `src/features/household/household-setup.tsx` (first-run form: name and time zone)
- `src/features/recipients/recipients-screen.tsx`, `recipient-form.tsx`, `recipient-row.tsx`
- `src/features/settings/milk-rate-card.tsx`
- `tests/unit/lib/money.test.ts`, `tests/unit/lib/dates.test.ts`,
  `tests/unit/services/recipient-service.test.ts`, `tests/unit/features/recipients-screen.test.tsx`,
  `tests/unit/features/milk-rate-card.test.tsx`

Edited:

- `src/services/api-client.ts` (adds POST, PUT and DELETE, and reads Problem Details into `ApiError`)
- `src/types/api.ts`, `src/types/household.ts` (add `startsOn` and `archivedFrom`)
- `src/lib/dates.ts` (ISO date helpers)
- `src/app/app/recipients/page.tsx`, `src/app/app/settings/page.tsx`
- `tests/unit/services/api-client.test.ts`

Behaviour:

- With no `NEXT_PUBLIC_API_BASE_URL`, the Recipients page stays in **demo mode** and keeps its "Demo
  data" badge.
- If the API is configured but unreachable, the page shows an error with a retry button. It never
  falls back to demo data silently.
- The price is edited in Settings, in the milk-rate card, with its history listed below.

### CI, scripts and docs

Edited: `.github/workflows/ci.yml` (the `api` job excludes database tests; the `database` job applies
migrations, runs them, and adds a curl smoke test with `Access__AllowUnauthenticated=true`; **no job
`name:` changes**), `scripts/check.sh` and `scripts/check.ps1` (exclude database tests; opt in with a
flag), `docs/development.md` (first migration, database tests, single-household mode),
`docs/architecture/overview.md` (what now exists), and ADR 0004's status once accepted.

### PR A as built

What differs from the plan above:

- **More than the minimum.** All routes in the PR A tables exist (including `GET /recipients/{id}`,
  unarchive, `GET /milk-rates/in-effect`, and changing and deleting a rate). `Money` (whole paise,
  checked arithmetic) was built here, not with billing, along with `PricePerLitre.CostOf`, which
  rounds half-up once per line.
- **Archive default.** With no date, a recipient is archived from today, or from the day after they
  start if that is later. A recipient is active for at least one day, so "from today" was refused for
  anyone who started today.
- **Single-household mode** is switched by the environment (Development and Testing) or
  `Access__AllowUnauthenticated=true`; `appsettings.Development.json` needed no change. Anything other
  than exactly `true` leaves the endpoints off.
- **Composite foreign keys** need a child table, so none exists yet. `recipients` carries the
  `UNIQUE (household_id, id)` key they will point at; they arrive with `deliveries` in PR B.
- **Web.** `ApiRecipient` lives in `types/api.ts` (extending `Recipient`) rather than changing
  `types/household.ts`. Settings offers a short list of time zones. The Playwright tests run against a
  build with no API address; the screens that use the API are covered by Vitest with mocked services,
  and the whole path was driven once in a browser against the real API and PostgreSQL.
- **Added to CI.** `scripts/smoke-api.sh`, run by the `database` job after the migrations are applied.
  The readiness check now also fails while migrations are pending.

## PR B: daily deliveries and history

Branch `feat/daily-deliveries`. Depends on PR A.

### Backend

New, in `backend/src/`:

- `PureFreshMilk.Domain/Deliveries/DeliveryId.cs`, `DeliveryStatus.cs`, `Delivery.cs`
- `PureFreshMilk.Application/Deliveries/IDeliveryRepository.cs`, `DaySheet.cs`, `DaySummary.cs`,
  `GetDaySheetHandler.cs`, `RecordDeliveryHandler.cs`, `ClearDeliveryHandler.cs`,
  `DeliverRemainingHandler.cs`, `ListDaySummariesHandler.cs`
- `PureFreshMilk.Infrastructure/Persistence/Configurations/DeliveryConfiguration.cs`,
  `Persistence/Repositories/DeliveryRepository.cs`,
  `Persistence/Migrations/<timestamp>_AddDeliveries.cs` and its designer
- `PureFreshMilk.Api/Features/Deliveries/DeliveryEndpoints.cs`, `DaySheetEndpoints.cs`,
  `DeliveryRequests.cs`, `DaySheetResponse.cs`, `DaySummaryResponse.cs`

Edited: `PureFreshMilkDbContext.cs` (model snapshot regenerated),
`ApplicationServiceCollectionExtensions.cs`, `InfrastructureServiceCollectionExtensions.cs`,
`Program.cs`, `UpdateRecipientHandler.cs` and `ArchiveRecipientHandler.cs` (rules R-6 and R-7, which
need deliveries to exist).

### Backend tests

New, in `backend/tests/`:

- `PureFreshMilk.Domain.Tests/Deliveries/DeliveryTests.cs`
- `PureFreshMilk.Application.Tests/Deliveries/…` (one file per handler)
- `PureFreshMilk.Infrastructure.Tests/DeliveryConstraintTests.cs`, `DeliveryConcurrencyTests.cs`
- `PureFreshMilk.Api.Tests/Features/Deliveries/DeliveryEndpointTests.cs`,
  `Features/Deliveries/DaySheetEndpointTests.cs`

Edited: the in-memory repositories in `PureFreshMilk.TestSupport/`. The invariants marked **PR B**
in the invariant tables are covered here.

### Web app

New:

- `src/services/delivery-service.ts`
- `src/features/history/history-screen.tsx`, `day-summary-row.tsx`
- `src/app/app/history/[date]/page.tsx` (a past day, using the same delivery cards as Today)
- `tests/unit/services/delivery-service.test.ts`, `tests/unit/features/history-screen.test.tsx`

Edited:

- `src/types/household.ts`, `src/types/api.ts` (`pending` becomes `notRecorded`; day sheet types)
- `src/features/today/today-screen.tsx`, `delivery-card.tsx`, `today-summary.tsx`, `day-measure.tsx`,
  `use-today-label.ts` (the date comes from the server)
- `src/features/today/today-state.ts` (shrinks to optimistic updates and rollback; the totals
  arithmetic is removed)
- `src/app/app/today/page.tsx`, `src/app/app/history/page.tsx`
- `tests/unit/features/today-state.test.ts`, `tests/unit/features/today-screen.test.tsx`,
  `tests/e2e/today.spec.ts` (still runs in demo mode)

Behaviour:

- Taps update the screen immediately and are sent in the background. A failed request restores the
  previous state and says so. Because `PUT` is idempotent, a retry is safe.
- A past day opens from History and reuses the same cards.
- Demo mode stays available when no API is configured.

### Docs

Edited: `docs/architecture/overview.md`, `docs/development.md`, and the status of ADR 0004.

## Questions to decide before PR A

These change what PR A builds. Each has a recommendation, so a plain "yes" is enough.

1. **One delivery a day, or morning and evening?**
   Recommended: one record per recipient per day, holding the day's total. If you need two, the
   recipient's usual quantity and the delivery key must carry a slot, and that has to be in PR A's
   migration instead of being added later.
2. **One price for the whole household, or a price per recipient? One kind of milk?**
   Recommended: one price series for the household, one kind of milk. If prices can differ per
   recipient, `milk_rates` gets a nullable `recipient_id` now (empty means "everyone"), which is
   cheap today and awkward later.
3. **Single-household mode with no login.** Until authentication exists, the API serves one
   household to anyone who can reach it. Recommended: the data endpoints are mapped only in
   Development and Testing, or when `Access__AllowUnauthenticated=true` is set explicitly, so a
   deployed copy cannot expose data by accident. The alternative is to build authentication before
   slice 1, which delays everything, or to leave the endpoints always on, which is unsafe to deploy.
4. **Recipient dates.** Recommended: a required `starts_on` and an optional `archived_from`
   (decision 4 in the ADR). The alternative is a plain archived flag, which cannot tell whether a
   recipient was expected on a given day, and which makes "unrecorded days" meaningless.

## Questions that can wait

Before PR B:

5. **How far back can a day be edited?** Recommended: any past day in the recipient's active window,
   until that month is finalised. The alternative is a limit such as the last 60 days.
6. **Does a missed delivery need a reason?** Recommended: no.

Before billing starts:

7. **Who is billed: the household as a whole, or each recipient separately?** Recommended: the
   household, with a line per recipient. Separate bills would add a recipient to the bill's key.
8. **Is the monthly total rounded to whole rupees, or kept to the paisa?** This depends on how your
   milkman bills. The model supports either.
9. **Finalised months: block changes to deliveries, or create an adjustment automatically?**
   Recommended: block, and correct with an adjustment that has a reason.
10. **Pauses and non-delivery days** (travel, festivals, weekdays only). The Settings screen already
    lists "Delivery days and pauses". Without them, every such day is a "not recorded" warning.
11. **A request to remove someone's data.** Recommended: replace the name and keep the billing
    history. Decide before real people's names are stored for long.

## Defaults assumed

These were chosen without asking. Say so if any is wrong.

- Recipients hold only a name, a usual quantity and dates. No phone number, address or photo.
- A quantity is at most 20 L per delivery in the domain. The Today screen still steps in 250 ml
  and stops at 5 L.
- A price is at most ₹10,000 a litre.
- Recipient names are unique among active recipients, ignoring case.
- Migrations are applied explicitly (`dotnet ef database update`), never automatically at start-up.
- Errors map to 400, 404 and 409 only.
- Demo mode stays whenever the web app has no API address.

## Risks

- **No login.** Slice 1 is for local use. Do not put it on the internet before authentication
  exists (question 3).
- **Real data, no backup.** The database lives in a local Docker volume. `docker compose down
  --volumes` deletes it. Once real deliveries are entered, someone has to decide how they are backed
  up.
- **Time zone data.** `Asia/Kolkata` needs time zone data on the machine. Slim container images may
  not have it, which is why H-4 is a test.
- **A bad connection at the door.** The Today screen is used on a phone, standing at the gate.
  Optimistic updates with a clear failure message are the minimum; saving taps while offline is out of
  scope and will be asked for.
- **Review size.** PR A touches every layer. The commit order above is there so it can be reviewed in
  pieces; if it is still too large, the natural split is backend first, then web.
- **Tool version.** Generate migrations with the `dotnet-ef` version pinned in
  `backend/dotnet-tools.json` (`dotnet tool restore`), and keep it in step with the EF Core package
  version, so the generated migration files do not depend on who ran the command.
