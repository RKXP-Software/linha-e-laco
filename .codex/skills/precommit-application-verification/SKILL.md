---
name: precommit-application-verification
description: Create and run focused unit tests plus verification for changed software-factory applications before a Git commit. Use for implementation work in apps/; not for documentation-only commits.
---

# Precommit Application Verification

Use this skill immediately before committing changes that affect an application. The goal is to distinguish code that compiles from code whose behavior is covered by focused automated tests.

## Scope the verification

- Inspect `git status`, the staged diff, and the changed application folders first.
- For this factory, a product lives in `apps/<slug>/`; React applications normally live in `web/` and `backoffice/`, while the ASP.NET Core API lives below `api/`.
- Test the changed components and their direct integrations. Do not claim unrelated applications were tested.

## Unit-test requirement

For every change to application behavior, identify the business rules, validation paths, state transitions, calculations, or user actions it changes.

- Add or update focused unit tests for those behaviors in the same change. Do not accept a build as a substitute for unit tests.
- For .NET APIs, use the project's test framework or add a conventional xUnit test project when none exists. Keep database and network calls outside unit tests through fakes, test doubles, or extracted pure logic.
- For React applications, use the existing test runner or add Vitest and React Testing Library when none exists. Cover important rendered states and the user interaction that triggers the changed behavior; mock HTTP at the boundary.
- When a change is truly not unit-testable without disproportionate infrastructure, explain why, test it at the appropriate level, and obtain user direction before committing without unit coverage.

## Required gate

Before committing application changes:

1. Run `git diff --check` for whitespace errors.
2. Run the focused unit tests added or affected by the change, then the project's complete unit-test suite.
3. Build every changed deployable component:
   - `dotnet build` for changed .NET projects.
   - `npm run build` for each changed React application.
4. Exercise the user-visible actions or API endpoints changed in the request. Prefer a local or dedicated test environment. Check both success and expected validation/error behavior where practical.
5. Review the staged file list and diff for secrets, local `.env` files, generated artifacts, and accidental unrelated changes. Never commit provider passwords, connection strings, secret keys, or local state.

## Database and deployment boundaries

- Database schema changes require a migration file under the product's `supabase/migrations/` folder and a review of how the API will use it.
- Never use a production database as the default test target. If live credentials, provider access, a migration, or a deployed environment is required to complete verification, obtain the user's authorization and report the exact result.
- A successful local build or unit test does not prove a Vercel or Railway deployment updated. Treat deployment verification as a separate step: confirm the deployed commit, health endpoint, and relevant client/API interaction.

## Commit decision

Commit only after the applicable unit tests, builds, and checks pass. If a required check cannot run, either fix the blocker or clearly tell the user which behavior remains unverified before asking whether to proceed with a limited commit. In the final report, list the tests and flows verified plus any remaining production action.