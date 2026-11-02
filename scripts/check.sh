#!/usr/bin/env bash
# Runs the same checks as CI (.github/workflows/ci.yml), stopping at the first failure.
# Expects dependencies to be installed (npm ci in apps/web).
#
#   bash scripts/check.sh          # backend + web checks (the database tests are left out)
#   bash scripts/check.sh --db     # also the database tests; start PostgreSQL first: docker compose up -d --wait
#   bash scripts/check.sh --e2e    # also the Playwright browser tests
set -euo pipefail

run_db=false
run_e2e=false
for option in "$@"; do
  case "$option" in
    --db) run_db=true ;;
    --e2e) run_e2e=true ;;
    *) echo "Unknown option: $option (use --db and/or --e2e)" >&2; exit 2 ;;
  esac
done

root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
step() { printf '\n==> %s\n' "$*"; }

step "Backend: restore, format, build, test"
cd "$root/backend"
dotnet restore PureFreshMilk.slnx
dotnet format PureFreshMilk.slnx --verify-no-changes --no-restore
dotnet build PureFreshMilk.slnx --configuration Release --no-restore
# Tests that need PostgreSQL are tagged Category=Database. The test project that holds only those is left with
# no tests, which ends with exit code 8 ("zero tests ran").
dotnet test --solution PureFreshMilk.slnx --configuration Release --no-build --filter-not-trait "Category=Database" --ignore-exit-code 8

if [[ "$run_db" == true ]]; then
  step "Backend: database tests (PostgreSQL from compose.yaml)"
  if [[ -z "${ConnectionStrings__PureFreshMilkTests:-}" ]]; then
    env_file="$root/.env"
    if [[ ! -f "$env_file" ]]; then
      echo "No .env file found. Copy .env.example to .env and set POSTGRES_PASSWORD, or set ConnectionStrings__PureFreshMilkTests." >&2
      exit 1
    fi
    # Reads KEY=value from .env without executing it as shell code; $2 is the default for a missing key.
    env_value() {
      local value
      value="$(grep -E "^$1=" "$env_file" | tail -n 1 | cut -d= -f2- | tr -d '\r' || true)"
      echo "${value:-$2}"
    }
    password="$(env_value POSTGRES_PASSWORD '')"
    if [[ -z "$password" ]]; then
      echo "POSTGRES_PASSWORD is empty in .env." >&2
      exit 1
    fi
    export ConnectionStrings__PureFreshMilkTests="Host=localhost;Port=$(env_value POSTGRES_PORT 5432);Database=$(env_value POSTGRES_DB purefreshmilk);Username=$(env_value POSTGRES_USER purefreshmilk);Password=${password}"
  fi
  dotnet test --project tests/PureFreshMilk.Infrastructure.Tests --configuration Release --no-build
fi

step "Web: format, lint, types, unit tests, build"
cd "$root/apps/web"
npm run format:check
npm run lint
npm run typecheck
npm test
npm run build

if [[ "$run_e2e" == true ]]; then
  step "Web: browser tests"
  # CI=1 makes Playwright serve the build above instead of building again.
  CI=1 npm run test:e2e
fi

step "All checks passed"
