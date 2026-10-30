#!/usr/bin/env bash
# Smoke-tests the data endpoints of a running API against its real database: sets up the household, adds
# a recipient and a milk rate, and checks the error answers. CI runs it after the migrations are applied.
#
# It expects an EMPTY database and an API that serves the data endpoints (Development, or
# Access__AllowUnauthenticated=true). It leaves its data behind, so do not point it at a database you care about.
#
#   bash scripts/smoke-api.sh [base url]      # default: http://localhost:5080
set -euo pipefail

base="${1:-http://localhost:5080}/api/v1"
response="$(mktemp)"
trap 'rm -f "$response"' EXIT

status=""

# call METHOD PATH [JSON BODY]: sets $status and leaves the body in $response.
call() {
  local method="$1" path="$2" body="${3:-}"
  local args=(--silent --show-error --output "$response" --write-out '%{http_code}' --request "$method" "$base$path")
  [[ -n "$body" ]] && args+=(--header 'Content-Type: application/json' --data "$body")
  status="$(curl "${args[@]}")"
}

# expect STATUS TEXT STEP: the last call answered STATUS and its body contains TEXT (when TEXT is not empty).
expect() {
  local want="$1" text="$2" step="$3"
  if [[ "$status" != "$want" ]]; then
    echo "FAILED: $step: expected HTTP $want, got $status" >&2
    cat "$response" >&2
    echo >&2
    exit 1
  fi
  if [[ -n "$text" ]] && ! grep -q -F -- "$text" "$response"; then
    echo "FAILED: $step: the body does not contain $text" >&2
    cat "$response" >&2
    echo >&2
    exit 1
  fi
  echo "  ok  $step"
}

call GET /household
expect 404 'household-not-set-up' 'the household does not exist yet'

call PUT /household '{"name":"Smoke test household"}'
expect 201 '"timeZoneId":"Asia/Kolkata"' 'setting up the household creates it'

call POST /recipients '{"name":"Recipient A","expectedMl":1000,"startsOn":"2026-10-01"}'
expect 201 '"name":"Recipient A"' 'adding a recipient'
recipient_id="$(sed -n 's/.*"id":"\([^"]*\)".*/\1/p' "$response")"
[[ -n "$recipient_id" ]] || { echo "FAILED: no recipient id in the response" >&2; exit 1; }

call POST /recipients '{"name":"recipient a","expectedMl":500}'
expect 409 'recipient-name-taken' 'a duplicate name is a conflict, whatever its case'

call POST /recipients '{"name":" ","expectedMl":0}'
expect 400 'validation-failed' 'invalid input is a validation problem'

call GET /recipients
expect 200 '"name":"Recipient A"' 'listing the recipients'

call POST /milk-rates '{"effectiveFrom":"2026-10-01","pricePerLitrePaise":6250}'
expect 201 '"pricePerLitrePaise":6250' 'adding a milk rate'

call GET '/milk-rates/in-effect?on=2026-10-02'
expect 200 '"pricePerLitrePaise":6250' 'the rate in effect after it starts'

call GET '/milk-rates/in-effect?on=2026-09-30'
expect 404 'no-rate-in-effect' 'there is no price before the first rate'

call POST "/recipients/$recipient_id/archive"
expect 200 '"archivedFrom":"' 'archiving a recipient'

call DELETE "/recipients/$recipient_id"
expect 405 '' 'a recipient cannot be deleted'

echo "Smoke test passed."
