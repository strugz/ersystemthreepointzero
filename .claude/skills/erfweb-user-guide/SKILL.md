---
name: erfweb-user-guide
description: Write or refresh a step-by-step ERFWeb end-user guide for one role (Employee filing, Signatory, Finance, Expense Verifier, Administrator) as a shared Claude Doc, with exact labels from the Vue code and screenshots captured from the live site in the built-in browser pane. Use when asked for an ERFWeb user guide, filing guide, signatory/approver guide, how-to for staff, or to add or retake screenshots in one of those guides. ERFWeb only; for other apps use the generic app-user-guide skill.
---

# ERFWeb role guide

Produces a short, click-by-click guide for one ERFWeb role. The reference is the **ERFWeb Employee Filing Guide** (Claude Doc `https://claude.ai/artifact/SCUXpSnVU7XdwZbPnsRp6q`, exported to `ERFWeb/Document/ERFWeb Employee Filing Guide.docx` / `.pdf`). The other guides were made the same way; update these rather than starting new ones. The export caps pictures at 16 per file: a guide with more gets "[image: …]" placeholders, so add the missing pictures to the .docx and make that PDF from it with Word (done for the Filing guide on 2026-11-05):

| Guide | Claude Doc | Word/PDF in `ERFWeb/Document/` |
| --- | --- | --- |
| Employee Filing | `https://claude.ai/artifact/SCUXpSnVU7XdwZbPnsRp6q` | yes (exported 2026-11-05) |
| Signatory | `https://claude.ai/artifact/R2NtPN24LukQ1vEk7KPCTw` | yes (exported 2026-11-05) |
| Finance | `https://claude.ai/artifact/JCVhLYTgSZAsdhqPVchvak` | yes (exported 2026-11-05) |
| Expense Verifier | `https://claude.ai/artifact/BHwSkTQT1iUFr1eoQPvcui` | yes (exported 2026-11-05) |
| Administrator | `https://claude.ai/artifact/2Yx5f5VuTMBTELwd99YKCC` | yes (exported 2026-11-05) |

The staff rollout deck ("ERFWeb for Staff", `https://claude.ai/artifact/63jFBoX1vyS5FXgEsnPuBF`) points people to these guides; keep its wording in step when a guide changes a label.

This is documentation work only: no repo source changes. ERFWeb only — never the desktop app or `Web/`.

## 1. One guide per role, doc first

One role per guide; do not mix roles (a combined "everyone" guide was tried first and replaced). Name it `ERFWeb <Role> Guide` (filing: `ERFWeb Employee Filing Guide`).

The turn's first tool call creates the Claude Doc skeleton (title, date, byline, one `pending` block per section), then `open` it with the Artifact tool. Fill sections as you go, posting a one-line progress note per section.

Employee filing outline (reuse its shape for other roles):

1. **Before you start** — sign-in, first-time **Change password** rules, what the admin must set up first (signature, approvers, scope/rates).
2. **Step 1: Create the report** — report type cards, purpose, dates.
3. **Step 2: Add expense lines** — categories, meals, fare legs, VAT, R47 column.
4. **Step 3: Record cash advances (Liquidation only)**.
5. **Step 4: Attach receipts**.
6. **Step 5: Check the list and file** — the **Before you file** checklist and the confirmation.
7. **After filing** — status table (status / meaning / what you do), Reopen rule, notifications.
8. **Common problems** — table (what you see / what to do).

Style: numbered steps, UI labels in **bold** exactly as on screen, one short sentence per step, tables for status and troubleshooting. Plain English for non-technical staff.

## 2. Take labels from the code, not the README

The README gives the shape; the screens give the words. Read the role's views and dialogs under `ERFWeb/Frontend/erfweb-client/src/` and grep for button text, `label="…"`, card titles, `snack.success(...)` messages and checklist `text:` items.

| Role | Start from |
| --- | --- |
| Employee filing | `views/reports/NewReportView.vue`, `ReportEditorView.vue`, `MyReportsView.vue`; `features/expenses/components/` (`ReportTypePicker`, `ReportHeaderForm`, `ExpenseLineDialog`, `CashAdvanceDialog`, `CashAdvancesCard`, `AttachmentsPanel`, `FilingChecklist`); `features/expenses/labels.ts` |
| Signatory | `views/approvals/ApprovalInboxView.vue`, `ReportReviewView.vue`; `features/approvals/components/`; `views/delegations/`, `features/delegations/components/` |
| Finance | `views/finance/FinanceQueueView.vue`, `features/finance/components/ReceiptsDialog.vue`, `views/reports/ExpenseSummaryView.vue`; raising a verification is the **Verify a line** button on the review page (`features/verification/components/VerificationCard.vue`, `VerificationActionDialog.vue`) |
| Expense Verifier | `views/verification/VerificationQueueView.vue`; `features/verification/components/`, `features/verification/labels.ts` (finding options and hints) |
| Administrator | `views/admin/`, `features/admin/components/` (`UserForm`, `RoleGrantsCard`, `SignatureUpload`, `ResetPasswordDialog`, `SettingsForm`, `ReminderStatusCard`), `views/entitlements/`, `features/entitlements/components/` |
| Budgets (Signatory, Finance) | `views/reports/CostCenterBudgetView.vue` |
| Navigation | `layouts/AuthenticatedLayout.vue` (menu titles per role) |

Watch for labels that change with state and say so in the guide. Known case: the editor button reads **Add expense line** on an empty report and **Add line** once lines exist.

## 3. Screenshots from the live site

Ask the user before starting: screenshots need a signed-in session, and walking a workflow writes real records.

**Sign-in.** Use the built-in browser pane: `preview_start {url: "https://ersystemthreepointzero.onrender.com"}`, then `show_pane` so the user can see it. The user signs in; never type a password on the live site. Signing in in their own Chrome or Brave does not sign in the pane, and Claude in Chrome did not connect (Brave never did), so don't spend turns on it. Check the pane yourself before continuing:

```js
({ url: location.href, h1: document.querySelector('h1,.page-title')?.textContent?.trim() ?? null })
```

Better, ask the API who is signed in and with which roles (read-only): `fetch('https://sr.mdmpi.com.ph/erf/auth/me', {credentials:'include'})` → `fullName`, `username`, `roles` (401 = nobody). Users often say "signed in" after signing in somewhere else, so check every time. Never create accounts or set passwords on the live site yourself; when a role has no account, point to the seeded test users (`004` has `finance1`, `010` has `verifier1`; passwords are in those scripts' headers, never repeat them in chat) or ask the user to create one as `admin`.

`/login?redirect=…` with h1 "Sign in" means not signed in yet. The h1 greeting ("Good day, Jay") names the account. The pane keeps the last session, so it may still be signed in as someone from an earlier walkthrough (it was still Ma. Agnes G Santos after the signatory guide): when the name is not the role you need, ask the user to sign out and back in; don't sign out yourself. If it is a real person rather than a test account (`employee1`, `manager1`, …), tell the user the screenshots will show real names and approvers.

**Check the test data before using it.** Earlier walkthroughs move records on, so open **My reports** and confirm the record you plan to use still exists and is still a draft. Never save into a draft you did not create in this session; it may be the user's real work.

**Capturing.**
- Keep `resize_window {preset: "desktop"}`. Emulating 1280×800 renders shrunken, blurry shots.
- The pane can change width between actions, so click by `ref` from `find`, not coordinates, for anything that saves.
- Date fields: set with `form_input` (`YYYY-MM-DD`); typing does not fill them.
- Vuetify text fields: click, `ctrl+a`, then `type`. `form_input` leaves the floating label overlapping the value, which looks broken in a screenshot.
- To leave a field, press `Tab` or click the dialog title. Never batch a click on **Close** or ✕ to blur a field: it closes the window and loses the form. If `Tab` leaves a focus ring on the next control, click the title before the screenshot.
- **Prefer fill-and-Cancel.** A dialog screenshot rarely needs the save: fill the window, capture it, click **Cancel**, then confirm with `read_network_requests` (no request to the lines endpoint) that nothing was written. Save only when the next screenshot needs the saved state.
- One open **Add expense line** window can serve several shots: switch the category tile and the typed Particulars, Location and Amount stay, so only the differences need retyping.
- Match the theme of the guide's existing screenshots (they are dark mode; the header toggle reads "Switch to light mode" when dark).
- If a save seems not to happen, check `read_network_requests` before retrying.
- Show the interesting state: a filled form beats an empty one; an open dropdown (fare **Mode** list) can be its own shot.
- Screenshots land in this session's `tool-results` folder. Copy each one right after taking it into `<scratchpad>/shots/NN-name.jpg` (`01-sign-in`, `02-dashboard`, `03-new-report`, `04-editor-new`, `05-cash-advance`, `06-line-meals`, `07-fare-modes`, `08-line-transport`, `09-checklist-receipts`, `10-file-confirm`, `11-my-reports`).

**Irreversible actions.** Never click **File report**, Approve, Return, Reject or any other workflow action on the live site without the user's explicit yes in chat for that action: filing assigns a real ERF number and texts/emails real approvers. Default: open the confirmation window, screenshot it, click **Cancel**, and confirm the status did not change. Creating a draft is fine after telling the user; report what test data was left behind (type, amounts, reference numbers) and ask whether to delete it.

Even with the user's yes, the session's safety check may refuse Claude's click on the final **File report** (and likely Approve, Return, Reject). Do not retry or work around it: leave the confirmation window open in the pane and ask the user to click it. A test draft that links CRM activities holds them: the employee cannot claim those activities on a real report until the draft is deleted, so link CRM activities only for the screenshot and delete the draft afterwards (or use a typed line).

Screens that need data the live system may not have: **Approve** on the R47 page needs a report waiting for that signatory; **Clear for payment** needs Receipts received; the verifier's **Pick up** / **Record your finding** need an open verification request. Check with the API before planning those shots and ask the user before creating the data, since each step texts and emails real people.

Data that worked for the filing walkthrough: Liquidation of Cash Advance, 1–15 Oct; advance ₱5,000, Check Voucher, `CV-TEST-001`; Meals breakfast ₱350 at MDMPI (type the amount when the account has no meal rates); Transportation, Grab, SUCAT → NICHOLS, ₱285, then use the legs-total button to fill Amount; Toll, *Toll - SLEX Alabang to Calamba*, CALMC, ₱125 × **Multiplier** 2; Parking, *Parking - St. Luke's BGC*, SLMC BGC, ₱80 with OR `OR-TEST-0042` and VAT ₱8.57 (Toll and Parking were captured fill-and-Cancel).

State of the live data as of 2026-10-27: ERF-2026-000001 and ERF-2026-000002 are ALEXANDER H ANCLOTE's real approved reports in the Finance queue (receipts not yet received); never act on them. ERF-2026-000003 is a test report (ROMEWEL A MAGDALITA, Reimbursement, one ₱80 Parking line, purpose "TEST - guide screenshot, please return") filed to Ma. Agnes G Santos for the Approve-on-R47 shots; the user said they would close it themselves. ROMEWEL's Revolving Fund draft for Oct 12–23 is real; don't touch it. Accounts used for screenshots: ROMEWEL A MAGDALITA (Employee, later also Expense Verifier), Ma. Agnes G Santos (Level 1 and 2), Jay C Abaoag (Finance), `admin`.

**Into the doc.** Upload each file with `Artifact {url: <doc>, asset: true, file_path: …}` one at a time (`file_paths` batch upload is refused from the scratchpad). Register each returned asset id as a blob (`create`, object `blob`, payload `{asset}` — several can go in one `batch`), then insert `![alt text](blob/<id>)` after the step it illustrates. Alt text says what the screen shows.

Screens not captured get one line in the guide pointing to the nearest screenshot and are listed in the final message (for filing: receipt upload, which needs the OS file picker).

## Changing an existing guide

Asked to add or expand part of a guide that already exists (for example "a guide for Meals, Transportation, Toll and Parking"):
1. Read the doc first: the outline, then `view` + `sinceRev` from your last known rev. Other sessions and the user edit these guides, so the content may already be there; Step 2 of the filing guide already had a section per category before it was asked for.
2. Check what is there against the code and say what was already correct. Add only what is missing (a quick-reference table, a rule that was never stated, a screenshot), and remove summaries the new sections make redundant.
3. Before each later write, `read` with `sinceRev` again; the user's edits win.
4. The `.docx` / `.pdf` in `ERFWeb/Document/` go stale after any change: say so, and re-export only when asked.

## 4. Finish

- Re-read the doc against what the walkthrough showed and fix any label or behaviour it got wrong (e.g. no meal rate → amount stays ₱0).
- Word/PDF copies for handing out are exported from the Claude Doc to `ERFWeb/Document/ERFWeb <Role> Guide.docx` and `.pdf`. Do this only when the user asks; nothing is committed unless they ask.
- Final message: doc link, screenshot count, what is not covered, which account the screenshots show, test data left in the live system, and the Changed files / What happens next sections from `CLAUDE.md` (normally "no repo changes; only the shared doc changed").
