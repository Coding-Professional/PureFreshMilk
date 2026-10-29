#!/usr/bin/env bash
# Runs the same checks as CI (.github/workflows/ci.yml), stopping at the first failure.
# Expects dependencies to be installed (npm ci in apps/web).
#
#   bash scripts/check.sh          # backend + web checks
#   bash scripts/check.sh --e2e    # also the Playwright browser tests
set -euo pipefail

root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
step() { printf '\n==> %s\n' "$*"; }

step "Backend: restore, format, build, test"
cd "$root/backend"
dotnet restore MilkManager.slnx
dotnet format MilkManager.slnx --verify-no-changes --no-restore
dotnet build MilkManager.slnx --configuration Release --no-restore
dotnet test --solution MilkManager.slnx --configuration Release --no-build

step "Web: format, lint, types, unit tests, build"
cd "$root/apps/web"
npm run format:check
npm run lint
npm run typecheck
npm test
npm run build

if [[ "${1:-}" == "--e2e" ]]; then
  step "Web: browser tests"
  # CI=1 makes Playwright serve the build above instead of building again.
  CI=1 npm run test:e2e
fi

step "All checks passed"
