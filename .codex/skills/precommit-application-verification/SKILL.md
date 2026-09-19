---
name: precommit-application-verification
description: Verify changed software-factory applications before creating a Git commit, including builds, affected behavior, and secret safety. Use for implementation work in apps/; do not use for documentation-only commits unless requested.
---

# Precommit Application Verification

Use this skill immediately before committing changes that affect an application. The goal is to distinguish code that compiles from a change that has been meaningfully checked.

## Scope the verification

- Inspect `git status`, the staged diff, and the changed application folders first.
- For this factory, a product lives in `apps/<slug>/`; React applications normally live in `web/` and `backoffice/`, while the ASP.NET Core API lives below `api/`.
- Test the changed components and their direct integrations. Do not claim unrelated applications were tested.

## Required gate

Before committing application changes:

1. Run `git diff --check` for whitespace errors.
2. Build every changed deployable component:
   - `dotnet build` for changed .NET projects.
   - `npm run build` for each changed React application.
3. Run existing automated tests when the affected project has them.
4. Exercise the user-visible actions or API endpoints changed in the request. Prefer a local or dedicated test environment. Check both success and expected validation/error behavior where practical.
5. Review the staged file list and diff for secrets, local `.env` files, generated artifacts, and accidental unrelated changes. Never commit provider passwords, connection strings, secret keys, or local state.

## Database and deployment boundaries

- Database schema changes require a migration file under the product's `supabase/migrations/` folder and a review of how the API will use it.
- Never use a production database as the default test target. If live credentials, provider access, a migration, or a deployed environment is required to complete verification, obtain the user's authorization and report the exact result.
- A successful local build does not prove a Vercel or Railway deployment updated. Treat deployment verification as a separate step: confirm the deployed commit, health endpoint, and relevant client/API interaction.

## Commit decision

Commit only after the applicable checks pass. If a required check cannot run, either fix the blocker or clearly tell the user which behavior remains unverified before asking whether to proceed with a limited commit. In the final report, list the commands/flows verified and any remaining production action.