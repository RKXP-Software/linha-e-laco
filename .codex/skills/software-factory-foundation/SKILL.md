---
name: software-factory-foundation
description: Establish and extend web applications for this software factory using React, ASP.NET Core on Linux, Vercel, and Supabase.
---

# Software Factory Foundation

Use this as the default technical foundation when creating or evolving an application for this software factory, unless the user explicitly chooses something else.

## Baseline

- Frontend: React.js.
- Backend: ASP.NET Core targeting .NET 10, designed to run on Linux.
- Web publication: Vercel.
- Managed backend services: Supabase.
- API hosting: Railway on Linux, packaged and deployed with Docker.
- Store each product in its own top-level folder under apps/<product-slug>/ in this repository; its main application, back-office, API, and product-specific assets stay within that product folder.
- Every user-facing application includes a separate, parallel React back-office application for authorized operational management.
- Each product has its own dedicated database. Its primary application and its back-office may access that product database through authorized backend services, but no other product may share it.
- All user-facing and back-office interfaces must be responsive and usable across phone, tablet, and desktop viewports.

## Architecture decisions

- Keep the React client and .NET API as separately deployable components when both are needed.
- Keep the public application and its back-office as separate deployable frontends, with distinct routes, access controls, and environment configuration. The back-office supports user administration, metrics, and other product-specific operational functions.
- Treat Supabase as the default choice for PostgreSQL, authentication, storage, and realtime capabilities. Keep the service-role key server-only; never expose it in React or public Vercel variables.
- Provision an isolated Supabase PostgreSQL database for each product. Do not make cross-product queries, foreign keys, or shared tables; use an explicit integration only when the user requests one.
- Deploy each React frontend (main application and back-office) as its own Vercel project. Deploy the ASP.NET Core API as a Docker container on Railway, configured to listen on 0.0.0.0 and Railway's PORT environment variable. Do not use Vercel container functions as the default API host.
- Configure credentials and URLs through environment variables. Commit neither secrets nor local environment files.
- Build responsive layouts mobile-first and verify practical navigation, content visibility, and touch targets at phone, tablet, and desktop widths.
- Before creating a live Supabase project, connecting a Vercel account, deploying, or choosing paid infrastructure, obtain the user's authorization and the necessary account context.

## When details are missing

Make ordinary implementation choices that are compatible with this baseline. Ask the user only when a choice materially affects product scope, cost, a third-party account, data ownership, or production deployment.