# Milk Manager

Milk Manager keeps an exact record of a household's daily milk deliveries: who received how much,
and which mornings the milk didn't come. Monthly totals, charges, payments and outstanding
balances come next.

> **Project status: foundation.** The repository structure, architecture and tooling are in place.
> The **Today** screen is a working visual prototype on **synthetic demo data**. Nothing is saved
> yet, and billing, accounts and persistence are not implemented.
> See [what is not implemented yet](docs/architecture/overview.md#what-is-not-implemented-yet).

## Tech stack

| Part | Technology |
| --- | --- |
| Web app (`apps/web`) | Next.js 16 (App Router), React 19, TypeScript 5.9 (strict), Tailwind CSS 4, Lucide icons |
| API (`backend/`) | ASP.NET Core on .NET 10, Onion Architecture (Domain / Application / Infrastructure / Api) |
| Database | PostgreSQL 18 with EF Core 10 (Npgsql) |
| Tests | xUnit v3, Vitest + Testing Library, Playwright + axe |
| CI | GitHub Actions |

## Repository layout

```text
apps/web/                 Next.js web app
backend/                  ASP.NET Core solution (MilkManager.slnx)
  src/MilkManager.Domain/           Entities, value objects, business rules
  src/MilkManager.Application/      Use cases and the interfaces they need
  src/MilkManager.Infrastructure/   PostgreSQL / EF Core persistence
  src/MilkManager.Api/              HTTP endpoints, contracts, composition root
  tests/                            Domain, API and architecture tests
docs/                     Architecture overview, decision records, local development guide
scripts/                  Local check and run helpers (Bash and PowerShell)
.github/                  CI workflow, issue and pull request templates
compose.yaml              Local PostgreSQL
```

## Getting started

Prerequisites: **Node.js 24 LTS** for the web app. For the API you also need the
**.NET SDK 10.0.100+**, plus **Docker** for the local database. Full details are in
[docs/development.md](docs/development.md).

**Web app only.** This needs no Docker, .NET or database:

```bash
cd apps/web
npm ci
npm run dev          # http://localhost:3000
```

**Full stack:**

```bash
cp .env.example .env                 # set POSTGRES_PASSWORD
docker compose up -d --wait          # PostgreSQL on localhost:5432
bash scripts/run-api.sh              # API on http://localhost:5080 (or: pwsh scripts/run-api.ps1)
cd apps/web && cp .env.example .env.local && npm run dev
```

**Run every check CI runs:** `bash scripts/check.sh --e2e` (or `pwsh scripts/check.ps1 -E2E`).

## Documentation

- [Architecture overview](docs/architecture/overview.md): layers, dependency rules, API boundaries, front-end structure
- [Architecture decision records](docs/architecture/decisions/README.md)
- [Local development](docs/development.md): setup, environment variables, tests, migrations, troubleshooting

## Contributing

Contributions are welcome. Read [CONTRIBUTING.md](CONTRIBUTING.md) and follow the
[Code of Conduct](CODE_OF_CONDUCT.md). Report security issues privately as described in
[SECURITY.md](SECURITY.md), not in public issues.

## License

[MIT](LICENSE)
