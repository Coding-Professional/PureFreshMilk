# Security policy

## Supported versions

Milk Manager is pre-release (0.x). Only the latest commit on `main` receives security fixes.

## Reporting a vulnerability

**Please do not report security vulnerabilities through public issues, discussions or pull requests.**

Report them privately through GitHub's private vulnerability reporting: open the repository's
**Security** tab and choose **Report a vulnerability**.

Please include:

- what the issue is and where it is (file, endpoint or screen),
- steps to reproduce or a proof of concept,
- the impact you expect (for example, reading another household's data),
- any suggested fix.

Don't include real personal data in your report. Synthetic examples are enough.

## What to expect

This is a volunteer-maintained project. We aim to:

- acknowledge your report within **7 days**,
- confirm whether it is a vulnerability and share a plan within **30 days**,
- credit you when the fix is published, unless you would rather stay anonymous.

Please give us reasonable time to release a fix before you disclose anything publicly.

## Scope

In scope: code in this repository, including the web app, the API, and the configuration and
scripts. Of particular interest are household data isolation, authentication (once it exists),
and the integrity of billing calculations.

Out of scope: vulnerabilities in third-party dependencies with no demonstrated impact here
(report those upstream), and findings that need physical access to a user's device.

## Handling secrets

The repository must never contain secrets. Local credentials go in git-ignored `.env` files,
.NET user secrets or environment variables. If you find a committed secret, report it as above.
