#!/usr/bin/env bash
# Applies the EF Core migrations to the local Compose database. It never deletes data.
set -euo pipefail

root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
env_file="$root/.env"

env_value() {
  grep -E "^$1=" "$env_file" | tail -n 1 | cut -d= -f2- | tr -d '\r'
}

if [[ -z "${ConnectionStrings__PureFreshMilk:-}" ]]; then
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
  export ConnectionStrings__PureFreshMilk="Host=localhost;Port=${port:-5432};Database=${database:-purefreshmilk};Username=${user:-purefreshmilk};Password=${password}"
fi

cd "$root/backend"
dotnet tool restore
dotnet ef database update --project src/PureFreshMilk.Infrastructure --startup-project src/PureFreshMilk.Api
