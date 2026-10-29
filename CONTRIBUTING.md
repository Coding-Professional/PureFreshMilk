# Contributing to PureFreshMilk

Thanks for helping. This guide covers how to propose changes and the rules that keep the code
base consistent. By taking part you agree to follow the [Code of Conduct](CODE_OF_CONDUCT.md).

## Before you start

- **Bugs and ideas:** open an issue using a template. For anything bigger than a small fix, discuss
  the approach in an issue before writing code.
- **Security problems:** never open a public issue. Follow [SECURITY.md](SECURITY.md).
- **Personal data:** never include real names, addresses, phone numbers or payment details in
  issues, fixtures, screenshots or test data. Use clearly synthetic data such as "Recipient A".

## Setting up

Follow [docs/development.md](docs/development.md). You can work on the web app with Node.js
alone, without Docker or .NET.

## Making a change

1. Fork the repository and create a branch from `main`, for example `feat/recipient-editing` or
   `fix/today-total`.
2. Keep pull requests focused: one change, with its tests and docs.
3. Write commit messages in the imperative ("Add recipient form"). Prefixes from
   [Conventional Commits](https://www.conventionalcommits.org/) (`feat:`, `fix:`, `docs:`) are welcome.
4. Run the checks before pushing:

   ```bash
   bash scripts/check.sh --e2e      # or: pwsh scripts/check.ps1 -E2E
   ```

5. Open a pull request and fill in the template. CI must pass before review.

## Architecture rules

The full picture is in [docs/architecture/overview.md](docs/architecture/overview.md). The rules
reviewers check most often:

**Backend**

- Dependencies point inwards: Api → Application/Infrastructure, Infrastructure → Application/Domain,
  Application → Domain. `MilkManager.ArchitectureTests` enforces this; don't weaken it to make a
  change compile.
- Business rules and every calculation involving quantities or money live in Domain or Application.
  Never put them in endpoints, and never in the web app.
- Endpoints use explicit request/response records. Don't serialise domain entities.
- Persistence types stay `internal` to Infrastructure.
- Warnings are errors, and `dotnet format --verify-no-changes` must pass.

**Web app**

- Route files in `src/app` stay thin; screens live in `src/features/<feature>`.
- Only `src/services` talks to the API. Components never call `fetch`.
- Put logic in plain TypeScript modules with unit tests, not inside components.
- Label demo data as demo data, and never present it as stored data.
- Use the design tokens in `src/app/globals.css` rather than raw colours.
- New UI must work at 360px wide with no horizontal scrolling, keep touch targets of at least
  44px, show visible focus and pass the axe checks in `tests/e2e`.

**Architectural changes** (a new project, a new dependency direction, a new external service,
a change to how billing or authentication works) need an
[architecture decision record](docs/architecture/decisions/README.md) in the same pull request.

## Tests

- Backend: xUnit v3. Put domain rules in `MilkManager.Domain.Tests` and the HTTP behaviour in
  `MilkManager.Api.Tests`.
- Web: Vitest + Testing Library for units and components; Playwright for routes, layout and
  accessibility.
- Bug fixes should come with a test that fails without the fix.

## License

By contributing, you agree that your contributions are licensed under the [MIT License](LICENSE).
