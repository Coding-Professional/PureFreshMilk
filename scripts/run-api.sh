#!/usr/bin/env bash
# Runs the API against the local PostgreSQL container (compose.yaml).
# Reads the database settings from the repository's .env file unless
# ConnectionStrings__MilkManager is already set in the environment.
#
#   bash scripts/run-api.sh
set -euo pipefail

root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
env_file="$root/.env"

# Reads KEY=value from .env without executing it as shell code.
env_value() {
  grep -E "^$1=" "$env_file" | tail -n 1 | cut -d= -f2- | tr -d '\r'
}

if [[ -z "${ConnectionStrings__MilkManager:-}" ]]; then
  if [[ ! -f "$env_file" ]]; then
    echo "No .env file found. Copy .env.example to .env and set POSTGRES_PASSWORD." >&2
    exit 1
  fi

  password="$(env_value POSTGRES_PASSWORD)"
  if [[ -z "$password" ]]; then
    echo "POSTGRES_PASSWORD is empty in .env." >&2
    exit 1
  fi

  database="$(env_value POSTGRES_DB)"
  user="$(env_value POSTGRES_USER)"
  port="$(env_value POSTGRES_PORT)"
  export ConnectionStrings__MilkManager="Host=localhost;Port=${port:-5432};Database=${database:-milkmanager};Username=${user:-milkmanager};Password=${password}"
fi

cd "$root/backend"
exec dotnet run --project src/MilkManager.Api --launch-profile http "$@"
