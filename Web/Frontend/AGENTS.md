# ER System Web Frontend Agent Guide

## Purpose and Scope

This guide applies to all files under `Web/Frontend/`. It supplements the repository-root `AGENTS.md` and `Web/AGENTS.md`; follow all three guides. If instructions conflict, the repository-root guide is authoritative, followed by `Web/AGENTS.md`, then this frontend-specific guide.

The active frontend application is `ersystem-web-client/`, a Vue 3 single-page application for Manager expense-report approval and Finance physical-receipt tracking. It consumes the ASP.NET Core API under the same origin and is deployed into the API publish directory's `wwwroot` folder.

## Runtime and Tooling

- Node.js 20.19 or later is required.
- `package.json` and `package-lock.json` are the dependency and reproducible-install sources of truth.
- Vue 3 Composition API and `<script setup lang="ts">` are the component standard.
- Vuetify provides UI components and layout primitives.
- Pinia owns session identity and other truly cross-route state.
- Vue Router owns route loading and client-side navigation guards.
- Vite provides development and production builds.
- Vitest and Vue Test Utils provide unit and component testing.
- ESLint and `vue-tsc` enforce code quality and type safety.

Do not manually edit `node_modules/` or generated `dist/` content. Keep the lockfile synchronized with intentional dependency changes.

## Application Function

The frontend provides three connected experiences:

1. It signs users in with their existing ER System credentials and maintains a server-backed cookie session.
2. It gives Managers a filtered report queue, detailed expense/receipt review, ordered approval, and return-with-reason workflow.
3. It gives Finance users a filtered queue of fully approved reports and a one-time action for recording receipt of physical documents.

The frontend controls presentation and navigation only. Authentication, role enforcement, report-level authorization, workflow ordering, and concurrency remain authoritative in the API.

## Architecture and Import Direction

Preserve the feature-first dependency direction:

```text
app -> views/layouts -> features -> shared
```

- `app/` composes global plugins, routing, stores, and styles.
- `views/` are route-level coordinators that load data and combine feature/shared pieces.
- `layouts/` provide application shells around route views.
- `features/` own feature-specific API modules, types, forms, dialogs, and workflow UI.
- `shared/` owns reusable, feature-independent API infrastructure, components, composables, design tokens, types, validation, and formatting.

`shared` must not import from `features`, `views`, `layouts`, or `app`. Features must not directly import another feature. A route view may coordinate multiple features when a workflow genuinely crosses feature boundaries.

## Bootstrap and Application Shell

| File or area | Current function |
| --- | --- |
| `src/main.ts` | Creates the Vue application, installs Pinia, Router, and Vuetify, loads global styles, and mounts the root component. |
| `src/App.vue` | Provides the Vuetify application root, active route outlet, and one global snackbar host. |
| `src/app/plugins/vuetify.ts` | Configures the ER light theme, design-token colors, and consistent defaults for buttons, cards, text fields, selects, and textareas. |
| `src/app/styles/main.css` | Defines global typography, page shells, filter/detail layouts, labels, muted text, and mobile page spacing. |
| `src/layouts/AuthenticatedLayout.vue` | Provides authenticated navigation, role-specific menu items, current-user display, logout, app bar, responsive drawer, and nested route outlet. |
| `src/env.d.ts` | Supplies Vite/Vue TypeScript environment declarations. |

Keep `App.vue` and layouts focused on application composition. Feature fetching, workflow decisions, and dialog state belong lower in the appropriate feature or route view.

## Routing and Session Flow

### Router

`src/app/router/index.ts` defines lazy-loaded route views and the navigation guard.

| Route | View | Required state |
| --- | --- | --- |
| `/login` | `views/auth/LoginView.vue` | Guest route; authenticated users are redirected into the portal. |
| `/manager/reports` | `views/manager/ManagerReportsView.vue` | Authenticated user with `Manager` role. |
| `/manager/reports/:reportId` | `views/manager/ManagerReportDetailView.vue` | Authenticated user with `Manager` role; API performs report-level authorization. |
| `/finance/receipts` | `views/finance/FinanceReceiptsView.vue` | Authenticated user with `Finance` role. |
| `/finance/receipts/:reportId` | `views/finance/FinanceReceiptDetailView.vue` | Authenticated user with `Finance` role. |
| `/forbidden` | `views/ForbiddenView.vue` | Authenticated user without the required client-visible role. |

Unknown paths redirect to `/`. The root route selects the Manager queue first when a user has both roles, otherwise the Finance queue, otherwise the forbidden page.

Route metadata and navigation guards improve user experience; they are not security controls. Every protected API request must still be authorized by the backend.

### Session store

`src/app/stores/session.ts` owns:

- The current `AuthenticatedUser` or `null`.
- One-time session initialization through `GET /api/auth/me`.
- Login through the authentication feature API.
- Logout and local identity cleanup.
- The initialized flag used to avoid repeated session probes during routing.

Keep credentials out of Pinia persistence, local storage, session storage, URLs, and logs. The server authentication cookie is HttpOnly and must remain inaccessible to application code.

## Feature Responsibilities

### Authentication feature

| File | Current function |
| --- | --- |
| `src/features/auth/types.ts` | Defines login input and authenticated-user/role response types. |
| `src/features/auth/api.ts` | Calls `me`, `login`, and `logout`; refreshes the antiforgery token before and after identity changes and invalidates it after logout. |
| `src/features/auth/LoginForm.vue` | Validates username/password, prevents duplicate submission, calls the session store, displays API errors, and routes users to their role-appropriate start page. |
| `src/views/auth/LoginView.vue` | Supplies the login-page presentation shell around `LoginForm`. |

Do not store or echo passwords. Do not add a separate authentication client; extend `authApi` and the session store when the API contract changes.

### Manager approvals feature

| File | Current function |
| --- | --- |
| `src/features/manager-approvals/types.ts` | Mirrors Manager queue, detail, expense, cash advance, receipt attachment, approval trail, filter, action-result, and row-version API contracts. |
| `src/features/manager-approvals/api.ts` | Lists reports, loads detail, downloads authorized receipt blobs, and submits approve/return mutations. |
| `src/features/manager-approvals/ManagerApprovalDialogs.vue` | Owns approval confirmation and return-reason input, validation, reset behavior, loading state, and typed events. |
| `src/views/manager/ManagerReportsView.vue` | Coordinates filters, debounced search, server table state, paging/sorting, refresh, reset, and navigation to report detail. |
| `src/views/manager/ManagerReportDetailView.vue` | Loads full report detail, renders summary/expenses/cash advance/approval trail/receipt attachments, previews authorized image/PDF blobs, submits approval or return with the current row version, refreshes on `409`, and cleans up object URLs. |

Approval and return buttons are presentation affordances. Never reproduce approval-sequence or authorization decisions in the frontend as authoritative logic. Always send the latest server-provided row version and treat `409 Conflict` as a prompt to reload current state.

### Finance receipts feature

| File | Current function |
| --- | --- |
| `src/features/finance-receipts/types.ts` | Mirrors Finance queue, detail, filter, receipt state, receiver, remarks, and row-version API contracts. |
| `src/features/finance-receipts/api.ts` | Lists Finance reports, loads detail, and submits physical-receipt confirmation with normalized optional remarks and row version. |
| `src/features/finance-receipts/ReceiveReceiptsDialog.vue` | Confirms the permanent action, validates optional remarks to 1000 characters, resets on close, and emits typed submission events. |
| `src/views/finance/FinanceReceiptsView.vue` | Coordinates Finance filters, server paging/sorting, receipt-state display, refresh/reset, and detail navigation. |
| `src/views/finance/FinanceReceiptDetailView.vue` | Loads report and Finance tracking detail, displays cash/receipt state, submits one-time receipt confirmation, and reloads the latest record after success or `409`. |

The API decides whether a report is fully approved and whether receipts can still be marked received. The frontend must render the server result and handle conflicts; it must not force a local state transition after a failed request.

## Shared API Infrastructure

### `src/shared/api/client.ts`

The shared client is the only low-level `fetch` implementation for application API traffic. It currently:

- Sends cookies with every request by using `credentials: 'include'`.
- Fetches and caches the antiforgery request token.
- Adds `X-CSRF-TOKEN` to unsafe methods.
- Refreshes and retries exactly once when the API returns the stable `antiforgery_validation_failed` code.
- Invalidates the token after unauthorized responses and logout.
- Parses RFC ProblemDetails into `ApiError`.
- Handles `204 No Content` without JSON parsing.
- Downloads authorized blobs for attachments.
- Builds query strings while omitting empty values.

Do not add raw `fetch` calls in views or feature components. Extend `apiRequest`, `apiBlob`, or a focused shared helper when transport behavior must change, and add tests for authentication, antiforgery, error, and response-shape edge cases.

### Shared API types

`src/shared/types/api.ts` defines the common paged result, ProblemDetails fields, and sort direction. Feature types should match backend DTO property names and nullability. Do not use `any` to hide a contract mismatch.

## Shared Composables

| Composable | Current function |
| --- | --- |
| `useAntiforgery` | Exposes explicit antiforgery refresh through the shared API client. |
| `useAsyncAction` | Prevents duplicate submissions, exposes loading/error state, preserves the thrown error for caller-specific handling, and resets state after completion. |
| `useDialogState` | Provides a reusable local open/show/close state for simple dialogs. |
| `usePermissions` | Derives Manager and Finance display permissions from the session store. |
| `useServerTable` | Owns items, totals, loading/error, page size, sorting, filters, debounced search, stale-request suppression, and table option mapping. |
| `useSnackbar` | Provides the single shared notification state plus success/error helpers rendered by `AppSnackbarHost`. |

Use local component state for one-screen forms and dialogs. Add Pinia state only when it must survive navigation or be shared across routes. Add a shared composable only for feature-independent behavior with repeated call sites or a cross-cutting safety requirement.

## Shared Components

Search these components before creating new UI infrastructure:

| Component | Current function |
| --- | --- |
| `AppBreadcrumbs` | Typed Vuetify breadcrumb wrapper for route hierarchy. |
| `AppConfirmDialog` | Standard confirmation dialog with cancellation, loading, action text, and action color. |
| `AppDate` | Displays optional date-only values using the shared Philippine formatter. |
| `AppDateTime` | Displays optional timestamp values using the shared Philippine formatter. |
| `AppEmptyState` | Standard no-records illustration, title, and guidance. |
| `AppErrorAlert` | Standard closable, tonal error message. |
| `AppFilterBar` | Responsive card/grid container for list filters. |
| `AppFormDialog` | Standard form-dialog shell with cancel/submit controls, loading, and disabled state. |
| `AppLoadingOverlay` | Contained persistent spinner for loading detail regions. |
| `AppMoney` | Displays optional amounts as Philippine pesos. |
| `AppPageHeader` | Responsive title/subtitle/action header for route views. |
| `AppPermissionDenied` | Standard insufficient-role state. |
| `AppReceiptViewer` | Previews image or PDF object URLs and shows a warning for unsupported content types. |
| `AppServerTable` | Standard server-driven Vuetify table with typed update/click events, slot forwarding, loading, pagination, and empty state. |
| `AppSnackbarHost` | Renders the one global notification system with an accessible close action. |
| `AppStatusChip` | Converts centralized workflow statuses into consistent labels and colors. |

Reusable components must have typed props and events, avoid feature-specific workflow decisions, use slots where composition is needed, and expose relevant loading, disabled, empty, error, and accessibility states.

## Design, Formatting, and Validation

| File or area | Current function |
| --- | --- |
| `src/shared/design/tokens.ts` | Owns brand/semantic colors and centralized status label/color mappings used by Vuetify and status chips. |
| `src/shared/utils/format.ts` | Owns Philippine peso, date-only, and date-time formatting plus the common missing-value placeholder. |
| `src/shared/validation/rules.ts` | Owns reusable required and maximum-length Vuetify validation rules. |

Do not hardcode repeated colors, status labels, money/date formatting, or validation messages in templates. Extend the existing centralized source when the behavior is genuinely shared.

## Where New Frontend Code Belongs

| New responsibility | Location |
| --- | --- |
| Route definition, navigation guard, global plugin, global store, or application theme setup | Matching folder under `src/app/` |
| Authenticated or guest application shell | `src/layouts/` |
| Route-level screen that coordinates loading, features, and navigation | `src/views/<area>/` |
| Feature-specific form, dialog, API module, DTO type, or workflow UI | `src/features/<feature>/` |
| Low-level API transport, reusable component/composable, cross-feature type, formatting, validation, or design token | Focused folder under `src/shared/` |
| Unit or component test | `tests/`, named for the behavior under test |

Keep API modules beside their feature types. Prefer a small complete feature slice over large generic component frameworks. Do not move feature-specific rules into `shared` merely to reduce imports.

## UI and State Rules

- Use Vuetify layout, forms, tables, dialogs, navigation, alerts, chips, and responsive utilities consistently with the existing theme.
- Keep route views thin enough to read as workflow coordination. Extract a feature component or composable when presentation or state becomes reusable or obscures the route flow.
- Use typed props, emits, route params, filter models, request bodies, and API responses.
- Use `v-data-table-server` through `AppServerTable` for paginated queues.
- Debounce free-text search and ignore stale responses; preserve the existing `useServerTable` behavior.
- Prevent double submission and show loading state for all mutations.
- Reset dialog form state on close and keep validation limits consistent with API contracts.
- Include loading, error, empty, disabled, readonly, permission-denied, stale-data, and success states where applicable.
- Keep layouts usable at a 320-pixel minimum width and verify tables, filters, dialogs, navigation, and action bars on mobile and desktop.
- Provide visible labels, meaningful errors, keyboard focus, accessible names for icon-only buttons, sufficient contrast, and non-color status text.
- Revoke generated object URLs when closing receipt previews or unmounting the view.

## API, Authentication, and Error Rules

- Use feature API modules; never call endpoints directly from templates.
- Encode route path parameters and use `buildQuery` for filter/query values.
- Keep credentials in the secure cookie flow. Never store passwords or session cookies in browser storage.
- Fetch antiforgery tokens through the shared client and attach them to all unsafe requests.
- Treat `401` as an expired/invalid session, `403` as insufficient permission, `404` as missing/inaccessible data, `409` as stale or invalid workflow state, and ProblemDetails text as the user-facing API failure message when safe.
- On mutation `409`, reload current server state before allowing another action.
- Do not manufacture successful local status changes before the API confirms the mutation.
- Keep frontend role checks for visibility and navigation only; never describe them as authorization enforcement.

## Tests

Current tests cover:

| Test file | Current function |
| --- | --- |
| `tests/api-client.test.ts` | Verifies antiforgery refresh before/after login, invalidation after logout/401, one-time retry on the stable antiforgery error code, and no retry loop. |
| `tests/format.test.ts` | Verifies missing-value and Philippine peso formatting. |
| `tests/status-chip.test.ts` | Verifies centralized status presentation in the reusable status chip. |
| `tests/setup.ts` | Configures shared Vue Test Utils stubs. |

Add unit tests for API mapping, stores, composables, formatting, and validation. Add component tests for forms, dialogs, queues, conflict reloads, permission states, empty/error states, and accessibility-sensitive interactions. Mock feature API modules or `fetch`; unit tests must not call the real backend.

## Verification

From `Web/Frontend/ersystem-web-client` run the checks appropriate to the change:

```text
npm ci
npm run lint
npm run type-check
npm run test
npm run build
```

For API-contract changes, run the backend tests and build as well. Manually exercise affected routes with the API when practical, including session expiry, forbidden roles, empty results, validation failure, API failure, stale row versions, repeated clicks, mobile layout, keyboard operation, and receipt preview cleanup.

The Vite development server proxies `/api` and `/health` to the HTTPS backend target configured in `vite.config.ts`. Production must keep SPA and API on the same HTTPS origin unless authentication, antiforgery, cookie, CORS, and deployment design are deliberately changed together.

If the supported Node runtime, backend, SQL Server, or browser environment is unavailable, state which verification could not run. Do not claim end-to-end success from a frontend build alone.

## Prohibited Changes

- Do not add a second API client, authentication store, snackbar system, dialog framework, server-table implementation, status mapping, or formatting layer.
- Do not scatter raw `fetch` calls across views and components.
- Do not place business SQL, authorization decisions, approval sequencing, or Finance workflow state transitions in frontend code.
- Do not let `shared` import from feature, view, layout, or app code.
- Do not create direct feature-to-feature imports; coordinate them in a route view.
- Do not persist passwords, cookies, antiforgery tokens, sensitive report data, or receipt blobs in browser storage.
- Do not ignore `401`, `403`, or `409` responses or hide failures with silent catches.
- Do not use untyped API responses, broad `any`, or hardcoded duplicate business constants to work around contract differences.
- Do not commit `node_modules/`, `dist/`, coverage output, environment secrets, or machine-specific configuration.
