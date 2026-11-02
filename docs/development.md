# Local development

## Prerequisites

| Tool | Required | Tested with | Needed for |
| --- | --- | --- | --- |
| Node.js | 24 LTS (`.nvmrc`) | 24.17.0, npm 11.13.0 | Web app |
| .NET SDK | 10.0.100 or later 10.0.x (`backend/global.json`) | 10.0.303 | API and backend tests |
| Docker with Compose v2 | Any recent version | Docker 29.6, Compose 5.6 | Local PostgreSQL only (optional) |

Docker is **not** needed for front-end work: the web app runs without the API or a database.

Why these versions:

- **TypeScript 5.9**, not 7: `typescript-eslint` doesn't support TypeScript 7 yet.
- **ESLint 9**: `eslint-config-next` 16.4 depends on plugins that don't support ESLint 10 yet.
  npm shows a deprecation notice for ESLint 9; it goes away once those plugins support ESLint 10.

## Ports

| Service | URL |
| --- | --- |
| Web app (dev) | http://localhost:3000 |
| API | http://localhost:5080 (OpenAPI document in Development: `/openapi/v1.json`) |
| PostgreSQL | `localhost:5432` (bound to 127.0.0.1 only) |
| Web app during browser tests | http://localhost:3100 |

## 1. Web app only (fastest)

```bash
cd apps/web
npm ci
npm run dev
```

Open http://localhost:3000, then **Try the demo**. With no API address the Today and Recipients
screens show synthetic data, clearly labelled "Demo data", and save nothing. Settings says that the
household and price settings need the API.

To work on real data, run the database and the API (steps 2 and 3 below), copy
`apps/web/.env.example` to `apps/web/.env.local` (the default points at http://localhost:5080) and
restart `npm run dev`. Recipients and Settings (household and milk price) then use the API. If the
API cannot be reached they say so; they never fall back to demo data silently. Today still shows
demo data until daily deliveries are built.

## 2. Database (PostgreSQL in Docker)

```bash
cp .env.example .env              # then set POSTGRES_PASSWORD in .env
docker compose up -d --wait       # starts PostgreSQL and waits for its health check
docker compose ps                 # should show the service as "healthy"
```

Stop it with `docker compose down`. Add `--volumes` to delete the data as well.

## 3. API

The API needs a connection string and **refuses to start without one**. Pick one way to provide it.

**A. Helper script.** Reads `.env` and builds the connection string for you:

```bash
bash scripts/run-api.sh           # macOS / Linux / Git Bash
pwsh scripts/run-api.ps1          # Windows PowerShell 7 (or: powershell -File scripts\run-api.ps1)
```

**B. User secrets.** Use this when you run the API from an IDE. Secrets are stored outside the repository:

```bash
cd backend
dotnet user-secrets set "ConnectionStrings:PureFreshMilk" \
  "Host=localhost;Port=5432;Database=purefreshmilk;Username=purefreshmilk;Password=<your password>" \
  --project src/PureFreshMilk.Api
dotnet run --project src/PureFreshMilk.Api
```

**C. Environment variable.** `ConnectionStrings__PureFreshMilk=<connection string>`.

The database must have its tables before the API can serve data. On a new database, apply the
migrations once (see [Database migrations](#database-migrations)).

Check it is up:

```bash
curl http://localhost:5080/health/live     # Healthy — the process is running
curl http://localhost:5080/health/ready    # Healthy — PostgreSQL is reachable and every migration is applied
curl http://localhost:5080/api/v1/status   # {"service":"purefreshmilk-api","version":"0.1.0",...}
```

The API starts without a running database. Only `/health/ready` reports the database: it is
`Unhealthy` while PostgreSQL is down and also while migrations are still pending.

The data endpoints (`/api/v1/household`, `/recipients`, `/milk-rates`) read and write a household
without a login, because authentication is not built yet (ADR 0004).
They exist only in the Development and Testing environments, or when
`Access__AllowUnauthenticated=true` is set explicitly. In any other environment they are not
mapped and answer 404. Do not put a copy that stores real data on a network you do not trust.

## Environment variables

| Variable | Where | Required | Purpose |
| --- | --- | --- | --- |
| `POSTGRES_PASSWORD` | `.env` (root) | Yes, for Compose | Password for the local database container |
| `POSTGRES_DB`, `POSTGRES_USER`, `POSTGRES_PORT` | `.env` (root) | No | Default to `purefreshmilk`, `purefreshmilk`, `5432` |
| `ConnectionStrings__PureFreshMilk` | Environment or user secrets | Yes, for the API | Npgsql connection string |
| `ConnectionStrings__PureFreshMilkTests` | Environment | Only for the database tests | Connection string of a PostgreSQL server the tests may create and drop databases on (the Compose one). `scripts/check.*` builds it from `.env` |
| `Access__AllowUnauthenticated` | Environment | No | `true` serves the household, recipient and price endpoints without a login outside Development. Anything else leaves them off |
| `Cors__AllowedOrigins__0` (…`__1`, …) | Environment | No | Web origins allowed to call the API. Development allows `http://localhost:3000` |
| `NEXT_PUBLIC_API_BASE_URL` | `apps/web/.env.local` | No | API base URL for the web app. It is inlined into the browser bundle, so never put secrets in it |

Real `.env` files are git-ignored. Only `.env.example` files, which hold placeholders, are committed.

## Checks and tests

Run what CI runs:

```bash
bash scripts/check.sh            # backend format/build/tests + web format/lint/types/tests/build
bash scripts/check.sh --db       # …plus the database tests (start PostgreSQL first, see below)
bash scripts/check.sh --e2e      # …plus the Playwright browser tests
pwsh scripts/check.ps1 [-Database] [-E2E]    # the same on Windows
```

Or one at a time:

```bash
# Backend (from backend/)
dotnet format PureFreshMilk.slnx --verify-no-changes
dotnet build PureFreshMilk.slnx                 # warnings are errors
dotnet test --solution PureFreshMilk.slnx --filter-not-trait "Category=Database" --ignore-exit-code 8
# Microsoft.Testing.Platform (see global.json). The filter leaves out the tests that need PostgreSQL;
# one test project then has no tests left, which would otherwise end with exit code 8.

# Web (from apps/web/)
npm run format:check
npm run lint
npm run typecheck
npm test                                      # Vitest unit tests
npx playwright install chromium               # once
npm run test:e2e                              # builds, serves on :3100, runs browser tests
```

Backend test projects:

- `PureFreshMilk.Domain.Tests`: domain rules such as `Quantity`, `Recipient` and `RateSchedule`.
- `PureFreshMilk.Application.Tests`: the use cases, against an in-memory stand-in for the database.
- `PureFreshMilk.Api.Tests`: the HTTP pipeline in memory (status endpoint, the household, recipient
  and milk rate endpoints, health checks, Problem Details, CORS, fail-fast configuration). They need
  no database.
- `PureFreshMilk.Infrastructure.Tests`: the mapping, constraints and migrations against a real
  PostgreSQL. Every test is tagged `Category=Database`.
- `PureFreshMilk.ArchitectureTests`: the layer dependency rules.
- `PureFreshMilk.TestSupport`: shared fakes (a library, not a test project).

### Database tests

They create a throwaway database for each test class on the server named in
`ConnectionStrings__PureFreshMilkTests`, build it from the migrations, and drop it afterwards. They
fail, rather than skip, when that setting is missing, so they cannot quietly stop running.

```bash
docker compose up -d --wait
bash scripts/check.sh --db      # builds the connection string from .env (or: pwsh scripts/check.ps1 -Database)
# or by hand, from backend/:
export ConnectionStrings__PureFreshMilkTests="Host=localhost;Port=5432;Database=purefreshmilk;Username=purefreshmilk;Password=<your password>"
dotnet test --project tests/PureFreshMilk.Infrastructure.Tests
```

CI runs them in the `database` job, after applying the migrations to an empty database and before
starting the API for a smoke test (`scripts/smoke-api.sh`).

## Database migrations

The first migration is `AddHouseholdsRecipientsAndMilkRates`. Apply the migrations to a new
database before starting the API (it reports `/health/ready` as `Unhealthy` until they are applied):

```bash
cd backend
dotnet tool restore              # installs dotnet-ef from dotnet-tools.json
# The API host is used as the startup project, so a connection string must be configured (step 3).
dotnet ef database update \
  --project src/PureFreshMilk.Infrastructure \
  --startup-project src/PureFreshMilk.Api
```

To add a migration after changing the model:

```bash
dotnet ef migrations add <Name> \
  --project src/PureFreshMilk.Infrastructure \
  --startup-project src/PureFreshMilk.Api \
  --output-dir Persistence/Migrations
```

Use the `dotnet-ef` version pinned by `dotnet tool restore`, and keep it in step with the EF Core
package version. Migration files are generated code: `.editorconfig` marks them so that
`dotnet format` leaves them alone. A test fails if the model has changes that no migration covers.

## Troubleshooting

- **`Connection string 'PureFreshMilk' is not configured`**: provide it using one of the options in step 3.
- **`required variable POSTGRES_PASSWORD is missing a value`**: create `.env` from `.env.example`
  and set a password.
- **`/health/ready` returns `Unhealthy`**: the database isn't running, the credentials don't
  match, or the migrations have not been applied. Check `docker compose ps` and your `.env`, then
  run `dotnet ef database update` (see [Database migrations](#database-migrations)).
- **The data endpoints answer 404**: outside Development and Testing they are off unless
  `Access__AllowUnauthenticated=true` is set. See step 3.
- **The Recipients page says it can't reach the API**: start the API, check
  `NEXT_PUBLIC_API_BASE_URL` in `apps/web/.env.local` (it is read when the dev server starts), and
  that the web origin is allowed by `Cors__AllowedOrigins`.
- **Port already in use** (for example 5432, by a local PostgreSQL service): change `POSTGRES_PORT` in `.env`
  (`scripts/run-api.*` and `scripts/check.* --db` read it), run the web app with
  `npm run dev -- --port 3001`, or edit `backend/src/PureFreshMilk.Api/Properties/launchSettings.json`.
  If you change the web port, also allow the new origin in the API's CORS settings.
- **Settings shows "No API address is set"**: create `apps/web/.env.local` from `.env.example` and
  restart the dev server.
