# 0003. The web app calls the API directly (CORS) until authentication is designed

- **Status:** Proposed
- **Date:** 2026-10-29
- **Deciders:** Project maintainer (pending review)

## Context

In development the web app runs on `http://localhost:3000` and the API on `http://localhost:5080`.
Browser calls between them are cross-origin. Authentication isn't designed yet, and the choice of
cookie sessions, tokens or a same-origin proxy affects how the two should be connected.

## Decision

For the foundation phase:

- The web app calls the API from the browser using `NEXT_PUBLIC_API_BASE_URL`, always through
  `apps/web/src/services`.
- The API allows only the origins listed in `Cors:AllowedOrigins` (Development:
  `http://localhost:3000`; no origins by default). Credentials are not allowed.
- Revisit this decision when authentication is designed.

## Consequences

- Simple to run and debug, and the web app still works with no API running.
- Moving to a same-origin setup later (for example a Next.js rewrite to the API) only changes the
  service layer's base URL handling and the CORS configuration.

## Alternatives considered

- **Next.js rewrites/proxy to the API now:** same-origin and cookie-friendly, but it would commit
  to a session model before authentication is designed.
- **Backend-for-frontend in Next.js:** would introduce a second backend, which is explicitly out of scope.
