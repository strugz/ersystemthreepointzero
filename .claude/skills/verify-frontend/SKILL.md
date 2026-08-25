---
name: verify-frontend
description: Verify the ER System Vue web client (Web/Frontend/ersystem-web-client) by running npm ci, ESLint, Vitest, and the production Vite build. Use after changing anything under Web/Frontend/, or when asked to lint, type-check, test, or build the web client / SPA / Vue app.
---

# Verify the ER System web frontend

Runs the frontend verification sequence required by `Web/Frontend/AGENTS.md`. Report honestly which steps ran and which were blocked — never claim success for a step that did not execute.

## Commands

Run these from the repository root, in order. Stop at the first failure and report it. `--prefix` is used deliberately so no `cd` is needed.

```bash
npm --prefix "Web/Frontend/ersystem-web-client" ci
```

```bash
npm --prefix "Web/Frontend/ersystem-web-client" run lint
```

```bash
npm --prefix "Web/Frontend/ersystem-web-client" run test
```

```bash
npm --prefix "Web/Frontend/ersystem-web-client" run build
```

## Rules

- **Use `npm ci`, never `npm install`.** `AGENTS.md` requires reproducible installs from `package-lock.json`. If `npm ci` fails because the lockfile is out of sync, report that as the finding — do not "fix" it by switching to `npm install`.
- **`npm run build` already runs `vue-tsc --noEmit`** (`build` is `vue-tsc --noEmit && vite build`). A separate `npm run type-check` is redundant when you are building. Run `type-check` alone only when you want types checked without a build.
- **Node 20.19 or later is required** (`package.json` engines). Verified working: Node 24.18.0, which is also what `render.yaml` pins for deployment.
- Do not edit `node_modules/`, `dist/`, or `package-lock.json` by hand. Lockfile changes come from an intentional dependency change only.

## Scope of what this proves

Known-good baseline: **ESLint clean, 20 test files / 65 tests passed, Vite build succeeds.** A drop in the test count is a regression, not a flake.

`npm run test` is Vitest unit and component coverage. It does **not** exercise the API. For a change that crosses the API/frontend boundary, also run `/verify-web` and check the request/response contract, permission handling, error and empty states, row-version handling, and antiforgery behavior on both sides together.

For UI work, `AGENTS.md` also expects manual exercise of loading, empty, error, permission-denied, stale-data, keyboard, and responsive states for the affected workflow. Say so if you have not done that rather than implying the build passing covers it.

## If a step cannot run

State the limitation plainly and continue with the steps that can run. Common cases:

- `npm` or Node missing, or Node older than 20.19 → report the version found.
- Network unavailable → `npm ci` cannot restore; the remaining steps will fail for a missing `node_modules` and that is the cause, not a code defect.
