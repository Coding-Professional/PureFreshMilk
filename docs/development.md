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

Open http://localhost:3000, then **Try the demo**. The Today screen runs on synthetic demo data
and saves nothing.

To let **Settings → API connection** reach a running API, copy `apps/web/.env.example` to
`apps/web/.env.local` (the default points at http://localhost:5080) and restart `npm run dev`.

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
dotnet user-secrets set "ConnectionStrings:MilkManager" \
  "Host=localhost;Port=5432;Database=milkmanager;Username=milkmanager;Password=<your password>" \
  --project src/MilkManager.Api
dotnet run --project src/MilkManager.Api
```

**C. Environment variable.** `ConnectionStrings__MilkManager=<connection string>`.

Check it is up:

```bash
curl http://localhost:5080/health/live     # Healthy — the process is running
curl http://localhost:5080/health/ready    # Healthy — PostgreSQL is reachable (Unhealthy if not)
curl http://localhost:5080/api/v1/status   # {"service":"milk-manager-api","version":"0.1.0",...}
```

The API starts without a running database. Only `/health/ready` reports the database.

## Environment variables

| Variable | Where | Required | Purpose |
| --- | --- | --- | --- |
| `POSTGRES_PASSWORD` | `.env` (root) | Yes, for Compose | Password for the local database container |
| `POSTGRES_DB`, `POSTGRES_USER`, `POSTGRES_PORT` | `.env` (root) | No | Default to `milkmanager`, `milkmanager`, `5432` |
| `ConnectionStrings__MilkManager` | Environment or user secrets | Yes, for the API | Npgsql connection string |
| `Cors__AllowedOrigins__0` (…`__1`, …) | Environment | No | Web origins allowed to call the API. Development allows `http://localhost:3000` |
| `NEXT_PUBLIC_API_BASE_URL` | `apps/web/.env.local` | No | API base URL for the web app. It is inlined into the browser bundle, so never put secrets in it |

Real `.env` files are git-ignored. Only `.env.example` files, which hold placeholders, are committed.

## Checks and tests

Run what CI runs:

```bash
bash scripts/check.sh            # backend format/build/tests + web format/lint/types/tests/build
bash scripts/check.sh --e2e      # …plus the Playwright browser tests
pwsh scripts/check.ps1 [-E2E]    # the same on Windows
```

Or one at a time:

```bash
# Backend (from backend/)
dotnet format MilkManager.slnx --verify-no-changes
dotnet build MilkManager.slnx                 # warnings are errors
dotnet test --solution MilkManager.slnx       # Microsoft.Testing.Platform (see global.json)

# Web (from apps/web/)
npm run format:check
npm run lint
npm run typecheck
npm test                                      # Vitest unit tests
npx playwright install chromium               # once
npm run test:e2e                              # builds, serves on :3100, runs browser tests
```

Backend test projects:

- `MilkManager.Domain.Tests`: domain rules such as `Quantity`.
- `MilkManager.Api.Tests`: the HTTP pipeline in memory (status endpoint, health checks, Problem
  Details, CORS, fail-fast configuration). They need no database.
- `MilkManager.ArchitectureTests`: the layer dependency rules.

## Database migrations

There are no migrations yet; the first one arrives with the first persisted entity. The tooling is
ready:

```bash
cd backend
dotnet tool restore              # installs dotnet-ef from dotnet-tools.json
# The API host is used as the startup project, so a connection string must be configured (step 3).
dotnet ef migrations add <Name> \
  --project src/MilkManager.Infrastructure \
  --startup-project src/MilkManager.Api \
  --output-dir Persistence/Migrations
dotnet ef database update \
  --project src/MilkManager.Infrastructure \
  --startup-project src/MilkManager.Api
```

## Troubleshooting

- **`Connection string 'MilkManager' is not configured`**: provide it using one of the options in step 3.
- **`required variable POSTGRES_PASSWORD is missing a value`**: create `.env` from `.env.example`
  and set a password.
- **`/health/ready` returns `Unhealthy`**: the database isn't running or the credentials don't
  match. Check `docker compose ps` and your `.env`.
- **Port already in use**: change `POSTGRES_PORT` in `.env`, run the web app with
  `npm run dev -- --port 3001`, or edit `backend/src/MilkManager.Api/Properties/launchSettings.json`.
  If you change the web port, also allow the new origin in the API's CORS settings.
- **Settings shows "No API address is set"**: create `apps/web/.env.local` from `.env.example` and
  restart the dev server.
