# 0001. Monorepo with a Next.js web app and an Onion-architecture ASP.NET Core API

- **Status:** Proposed
- **Date:** 2026-10-29
- **Deciders:** Project maintainer (pending review)

## Context

PureFreshMilk needs a responsive web front end and a backend that will own money calculations
(charges, payments, balances) and household-level data isolation. The project is small and
open source, so contributors should be able to understand and run everything from one clone.
Billing rules must stay testable and independent of the web framework and database.

## Decision

- One repository containing `apps/web` (Next.js, App Router, TypeScript) and `backend/`
  (ASP.NET Core on .NET 10), plus shared docs, CI and Compose files.
- The backend uses Onion Architecture with exactly four projects: Domain, Application,
  Infrastructure and Api. Dependencies point inwards (Api → Application/Infrastructure;
  Infrastructure → Application/Domain; Application → Domain).
- The rule is enforced by project references, by keeping persistence types `internal` to
  Infrastructure, and by architecture tests that run in CI.
- No JavaScript workspace tooling at the root: there is a single JavaScript package (`apps/web`).

## Consequences

- Business rules can be unit-tested without ASP.NET Core or a database.
- Endpoints can't reach EF Core directly, which keeps billing logic out of controllers.
- Contributors need both Node.js and the .NET SDK for full-stack work, but the web app runs on
  its own with Node.js only.
- The four-project split adds a little ceremony to small features; this is accepted in
  exchange for clear boundaries around money.

## Alternatives considered

- **Separate repositories** for web and API: harder to keep contracts and CI in step for a small team.
- **Next.js API routes or server actions as the backend:** would split business logic across two
  runtimes and contradict a single, server-authoritative billing implementation.
- **Vertical-slice single project:** less ceremony, but nothing would stop persistence or HTTP
  concerns from leaking into billing rules.
- **Microservices:** unjustified operational cost at this size.
