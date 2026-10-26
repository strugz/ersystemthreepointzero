-- =====================================================================
--  ERFWeb: delete every expense report (draft, filed, approved or paid) and everything tied to it,
--  so filing can start again. Users, roles, scope, rate profiles, delegations, master data and
--  settings are kept. Take a pg_dump first; there is no rollback.
--
--  What connects to a report, and what happens to it:
--  * expense_lines -> expense_meal_items, expense_transport_items (fare legs)   deleted
--  * approval_steps, finance_tracking, cash_advance_lines                       deleted
--    (the old cash_advances table was dropped by 005; its history rows are cleared below)
--  * verification_requests                                                      deleted
--  * receipt_attachments (receipts are stored in the row as bytea, not on disk) deleted
--  * reminder_deliveries, email_deliveries                                      deleted
--  * sms_deliveries with a report                                               deleted (the FK would only
--                                                                               null them); other SMS rows are kept
--  * workflow_audit rows with a report_id                                       deleted; other audit rows are kept,
--                                                                               plus one Reports.ResetByScript event
--  * record_history rows of the report tables                                   deleted, including the 'D' rows this
--                                                                               script's own deletes write
--  * reference_series (ERF-YYYY counters)                                       emptied, so the next filed report
--                                                                               is ERF-YYYY-000001 again
--  Run as one transaction; safe to re-run.
-- =====================================================================
BEGIN;

-- Script 021 makes workflow_audit append-only; this maintenance script deletes audit rows on purpose.
SET LOCAL erfweb.allow_audit_purge = 'on';

INSERT INTO workflow_audit (event, actor_user_id, detail)
SELECT 'Reports.ResetByScript', NULL,
       jsonb_build_object(
         'reports',     (SELECT count(*) FROM expense_reports),
         'lines',       (SELECT count(*) FROM expense_lines),
         'attachments', (SELECT count(*) FROM receipt_attachments),
         'script', 'maintenance/reset_reports')
WHERE EXISTS (SELECT 1 FROM expense_reports);

-- Children first, so the intent is explicit; each would also cascade from expense_reports.
DELETE FROM verification_requests;
DELETE FROM expense_meal_items;
DELETE FROM expense_transport_items;
DELETE FROM expense_lines;
DELETE FROM receipt_attachments;
DELETE FROM reminder_deliveries;
DELETE FROM email_deliveries;
DELETE FROM sms_deliveries WHERE report_id IS NOT NULL;
DELETE FROM cash_advance_lines;
DELETE FROM finance_tracking;
DELETE FROM approval_steps;
DELETE FROM expense_reports;

DELETE FROM workflow_audit WHERE report_id IS NOT NULL;
DELETE FROM record_history
WHERE table_name IN ('expense_reports', 'expense_lines', 'approval_steps', 'finance_tracking',
                     'cash_advances', 'cash_advance_lines', 'verification_requests');
DELETE FROM reference_series;

COMMIT;

-- Check afterwards (all should be 0):
-- SELECT (SELECT count(*) FROM expense_reports) AS reports, (SELECT count(*) FROM expense_lines) AS lines,
--        (SELECT count(*) FROM receipt_attachments) AS attachments, (SELECT count(*) FROM approval_steps) AS steps,
--        (SELECT count(*) FROM reference_series) AS series;
