## What this changes

<!-- One or two sentences. Link the issue it addresses, e.g. "Closes #12". -->

## Why

<!-- The problem or need behind the change. -->

## How it was tested

<!-- Commands you ran and what you checked by hand (screens, viewports, API calls). -->

## Checklist

- [ ] `scripts/check.sh` (or `scripts/check.ps1`) passes locally
- [ ] New behaviour has tests; UI changes were checked at 360px and on desktop
- [ ] Backend dependencies still point inwards (Api → Application/Infrastructure → Domain)
- [ ] Billing or balance calculations live in the backend, not in the web app or an endpoint
- [ ] No secrets, real personal data, local databases or build output are committed
- [ ] Docs updated (README, docs/, or an ADR for architectural decisions)
