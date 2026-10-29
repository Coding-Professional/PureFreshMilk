# Milk Manager — web app

Next.js (App Router) front end for Milk Manager. See the [root README](../../README.md), the
[architecture overview](../../docs/architecture/overview.md#frontend) and
[local development](../../docs/development.md).

```bash
npm ci
npm run dev            # http://localhost:3000
npm run lint           # ESLint (warnings fail)
npm run typecheck      # route types + tsc --noEmit
npm run format:check   # Prettier
npm test               # Vitest unit tests (tests/unit)
npm run test:e2e       # Playwright browser tests (tests/e2e); run `npx playwright install chromium` once
```

The app needs no backend to run. The Today screen uses synthetic demo data from
`src/lib/demo-household.ts` and saves nothing. Set `NEXT_PUBLIC_API_BASE_URL` in `.env.local`
(see `.env.example`) to let Settings check the API connection.
