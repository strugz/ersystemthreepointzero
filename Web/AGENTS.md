# ER System Web Agent Guide

## Purpose

This guide applies only to files under `Web/`. The repository-root `AGENTS.md` remains authoritative for repository-wide rules and the legacy WinForms application. If the two guides conflict, follow the root guide.

For work under `Backend/`, also follow `Backend/AGENTS.md`. For work under `Frontend/`, also follow `Frontend/AGENTS.md`. These nested guides explain the current projects, files, workflows, placement decisions, and verification expectations in greater detail; this guide remains authoritative for rules shared across the whole web portal.

## Folder Ownership

- `Frontend/ersystem-web-client/` owns the Vue, TypeScript, and Vuetify single-page application.
- `Backend/src/ERSystem.Web.Api/` owns HTTP, cookies, antiforgery, policies, middleware, OpenAPI, and health endpoints.
- `Backend/src/ERSystem.Web.Application/` owns DTOs, validation, use-case interfaces, pagination, and orchestration contracts.
- `Backend/src/ERSystem.Web.Domain/` owns UI- and infrastructure-independent workflow rules and status values.
- `Backend/src/ERSystem.Web.Infrastructure/` owns EF Core mappings, SQL Server access, legacy credential compatibility, auditing, and external integrations.
- `Backend/tests/ERSystem.Web.Tests/` owns unit, architecture, and SQL Server integration tests.

## Frontend Architecture

Use Vue 3 Composition API with `<script setup lang="ts">` and feature-first folders.

```text
app -> views/layouts -> features -> shared
```

- `shared` must not import from features, views, layouts, or app.
- Features must not import directly from another feature. Route views coordinate multiple features.
- Keep API calls in typed feature API modules or the shared API client, never in templates or presentational components.
- Keep business workflow decisions out of shared UI components.
- Pinia is for session identity, permissions, and truly cross-route state. Keep form and queue state local.
- Search existing shared components and composables before creating a new table, dialog, status chip, notification, pagination, or formatting implementation.
- Reusable components must accept typed props, emit typed events, expose loading/disabled/error states, and use design tokens.

## Backend Architecture

Use Clean Architecture with feature-oriented application services and these dependencies:

```text
Api -> Application
Api -> Infrastructure
Infrastructure -> Application -> Domain
Infrastructure -> Domain
Domain -> nothing
```

- Do not reference EF Core, HTTP, configuration, or WinForms from Domain.
- Do not expose EF entities from API endpoints.
- Keep controllers thin; approval and Finance rules belong in application/infrastructure services.
- Do not introduce MediatR, base controllers, catch-all helpers, or generic repositories over EF Core.
- Extract shared methods only after two real call sites or when they enforce a cross-cutting safety rule.
- Use centralized pagination, row-version handling, current-user access, audit writing, and exception-to-ProblemDetails mapping.

## Authentication and Permissions

- Never log passwords, encrypted passwords, cookies, connection strings, receipt contents, or encryption keys.
- Load secrets from environment/IIS configuration. Do not commit production secrets.
- Frontend permission checks control display only. Enforce role and report-level authorization in the API.
- Protect cookie-authenticated mutations with antiforgery validation.

## API Standards

- Prefix routes with `/api` and use async operations with `CancellationToken`.
- Use request/response DTOs and RFC `ProblemDetails`.
- Default pagination is 25 rows; cap it at 100.
- Use ISO-8601 dates, UTC timestamps, and Base64 row versions.
- Return `409 Conflict` for stale, duplicate, or out-of-order workflow actions.

## EF Core and Database Safety

- `ER3.0.sql` is a live-schema reference. Do not modify or execute it wholesale during ordinary feature work.
- `LegacyErDbContext` maps legacy tables and owns no migrations.
- `WebWorkflowDbContext` owns only web security and audit tables.
- Do not run migrations automatically against production.
- Add reviewed, dated, forward SQL scripts under the repository-root `Database/` folder.
- Configure SQL Server compatibility level 100 and verify generated queries against it.
- Use transactions for approval and Finance mutations and concurrency tokens for shared desktop/web records.
- Never delete arbitrary file paths. Restrict server-side deletion to an allowlisted shared-storage root.

## Verification

Frontend, from `Web/Frontend/ersystem-web-client`:

```text
npm ci
npm run lint
npm run type-check
npm run test
npm run build
```

Backend, from `Web/Backend`:

```text
dotnet restore ERSystem.Web.sln
dotnet build ERSystem.Web.sln
dotnet test ERSystem.Web.sln
dotnet format ERSystem.Web.sln --verify-no-changes
```

If SQL Server or a supported Node runtime is unavailable, state the limitation instead of claiming verification.

## Prohibited Patterns

- No duplicate API clients, dialog frameworks, pagination implementations, status mappings, or notification systems.
- No business SQL or workflow logic in Vue components or API controllers.
- No client-supplied user IDs for authorization decisions.
- No silent catches, automatic destructive data cleanup, or migration-history rewrites.
- No dependencies from Domain back to Application, Infrastructure, API, or Frontend.
