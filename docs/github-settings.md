# GitHub settings for maintainers

Some protections live in the repository's GitHub settings, not in files. Files in this repository
cannot turn them on, and this page does not record whether they are on. Use the checklist at the
end to see what is left. GitHub renames menu entries now and then; if a label differs, search the
settings page for the feature name.

## Dependabot

`.github/dependabot.yml` configures weekly version updates for the web app (npm), the backend
(NuGet) and the GitHub Actions in `.github/workflows`. It starts working once it is on `main`.
Check **Insights → Dependency graph → Dependabot** afterwards for configuration errors.

Turn on the parts that are settings, in **Settings → Advanced Security** (labelled **Code security**
in some versions):

1. **Dependency graph**: Enable.
2. **Dependabot alerts**: Enable.
3. **Dependabot security updates**: Enable.

Things to know:

- Each ecosystem opens at most 5 pull requests. Updates to `next`/`eslint-config-next`, to the React
  packages, and to the EF Core / ASP.NET Core / Extensions packages arrive as one grouped pull
  request each. Commit messages start with `chore(deps):`.
- TypeScript and ESLint major versions are ignored on purpose; minor and patch updates still arrive.
- The backend builds with warnings as errors and NuGet audit is on. A newly published advisory for
  a package we use fails the build until the package is updated, which the security-update pull
  request does for you.

## CodeQL code scanning

Use GitHub's **default setup**. Do not add a custom `codeql.yml` for now:

- Nothing here needs custom queries, path filters or build commands. Both stacks build with the
  plain commands CI already uses.
- Default setup has no workflow file to maintain or update.
- The repository is public, so scanning on GitHub-hosted runners is free.

Steps:

1. Open **Settings → Advanced Security**.
2. Under **Code scanning**, find **CodeQL analysis** and choose **Set up → Default**.
3. Check that the languages are **Actions**, **C#** and **JavaScript / TypeScript**.
4. Leave **Query suite** on **Default** and the runner on **Standard GitHub runner**.
5. Choose **Enable CodeQL**.

Results appear under **Security → Code scanning**. Switch to an advanced workflow only if you later
need custom queries, path filters, a manual build, or a self-hosted runner.

## Private vulnerability reporting

`SECURITY.md` tells reporters to use GitHub Security Advisories. That only works once you enable it:

1. Open **Settings → Advanced Security**.
2. Next to **Private vulnerability reporting**, choose **Enable**.
3. Check it: open
   <https://github.com/Coding-Professional/PureFreshMilk/security/advisories/new> in a private
   window. You should see the report form.
4. To get notified, choose **Watch → Custom → Security alerts** on the repository.

Until it is enabled, the fallback paragraph in `SECURITY.md` applies.

## Ruleset for `main`

The ruleset blocks force pushes and deletion of `main` and requires the three CI checks. It does
not require review approvals, because there is a single maintainer.

1. Open **Settings → Rules → Rulesets → New ruleset → New branch ruleset**.
2. **Ruleset name**: `Protect main`. **Enforcement status**: **Active**.
3. **Bypass list**: leave it empty.
4. **Target branches**: **Add target → Include default branch**.
5. Under **Branch rules**, tick:
   - **Restrict deletions**
   - **Block force pushes**
   - **Require status checks to pass**, then **Add checks** and add these three. Pick them from
     the list instead of typing; the dash is an em dash (U+2014):
     - `Web — format, lint, types, tests, build`
     - `API — format, build, tests`
     - `Database — Compose PostgreSQL + API readiness`
   - Leave **Require branches to be up to date before merging** unticked.
6. Leave every review option unticked. Choose **Create**.

Checks are matched by name. If a job's `name:` changes in `.github/workflows/ci.yml`, update the
ruleset in the same change, or merges wait for a check that no longer exists.

CI runs on pull requests and on pushes to `main` only. A commit pushed straight to `main` has no
passing checks yet, so GitHub rejects it. In practice, changes reach `main` through a pull request
with no required approvals. If you want that to be explicit, you can also tick **Require a pull
request before merging** with **Required approvals** set to `0`.

### Emergency recovery

For an ordinary mistake, fix forward with a revert pull request. Do not force-push.

For a real emergency, such as publishing a secret or the wrong history, keep the exception
deliberate instead of giving anyone a standing bypass:

1. **Settings → Rules → Rulesets → Protect main → Enforcement status → Disabled**, then **Update**.
2. Rewrite `main` with a lease, naming the commit you expect it to be on:
   `git push --force-with-lease=main:<expected-sha> origin <branch>:main`
3. Set **Enforcement status** back to **Active** immediately.

A bypass actor, even one limited to repository admins, could force-push at any time without these
steps. Rotate any exposed secret; rewriting history does not remove it from clones and forks.

## Code of Conduct contact

This is not a GitHub setting. `CODE_OF_CONDUCT.md` has no private contact for conduct reports yet.
Pick a private contact you control and monitor, replace the paragraph marked
`MAINTAINER ACTION REQUIRED` in that file, and delete the comment. Do not use private vulnerability
reporting for conduct reports.

## Checklist

- [ ] Dependency graph, Dependabot alerts and Dependabot security updates enabled
- [ ] `.github/dependabot.yml` on `main`, and **Insights → Dependency graph → Dependabot** shows no errors
- [ ] CodeQL default setup enabled for Actions, C# and JavaScript / TypeScript
- [ ] Private vulnerability reporting enabled and verified in a private window
- [ ] `Protect main` ruleset active with the three status checks
- [ ] Code of Conduct contact published
