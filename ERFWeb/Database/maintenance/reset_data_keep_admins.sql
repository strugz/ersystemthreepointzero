-- =====================================================================
--  ERFWeb: delete all data and start over, keeping only the 'admin' login so you can sign in.
--  Take a pg_dump first; there is no rollback.
--
--  What is deleted, and what stays:
--  * Reports and everything under them (lines, meal and fare items, steps, finance tracking,
--    cash advance lines, verification requests, receipts, reminders, SMS, email)   deleted
--  * reference_series (ERF-YYYY counters)                     emptied: the next ERF is ERF-YYYY-000001
--  * Approval routes, delegations, scope, rate profiles, old employee_rates       deleted
--  * Every user except 'admin', and their roles and sign-in lockouts              deleted
--  * 'admin'                                                  kept, with its own roles (re-marked as
--                                                             bootstrap, grantor fields cleared), its
--                                                             department, no cost center
--  * Departments other than admin's, cost centers, sites, app_settings,
--    department_signatories, currencies other than PHP                            deleted
--  * PHP currency                                             kept, made the active default
--  * fare_modes                                               kept: the 016 list is master data that
--                                                             the line editor needs
--  * workflow_audit, record_history                           emptied (the deletes above write
--                                                             history rows; they go too), then one
--                                                             Data.ResetByScript event is added
--  * schema_versions                                          kept, so /api/health stays green
--
--  If 'admin' does not exist the script stops before deleting anything.
--  Run as one transaction; safe to re-run.
-- =====================================================================
BEGIN;

DO $$
BEGIN
  IF NOT EXISTS (SELECT 1 FROM users WHERE username = 'admin') THEN
    RAISE EXCEPTION 'No ''admin'' user found; nothing deleted.';
  END IF;
END $$;

-- ---------- Reports (children first) ----------
DELETE FROM verification_requests;
DELETE FROM expense_meal_items;
DELETE FROM expense_transport_items;
DELETE FROM receipt_attachments;
DELETE FROM reminder_deliveries;
DELETE FROM sms_deliveries;
DELETE FROM email_deliveries;
DELETE FROM cash_advance_lines;
DELETE FROM finance_tracking;
DELETE FROM approval_steps;
DELETE FROM expense_lines;
DELETE FROM expense_reports;
DELETE FROM reference_series;

-- ---------- Routing, scope and rates ----------
DELETE FROM approval_delegations;
DELETE FROM approval_routes;
DELETE FROM employee_rates;
DELETE FROM scope_assignment_members;
DELETE FROM scope_assignments;
DELETE FROM rate_profile_assignments;
DELETE FROM rate_profile_lines;
DELETE FROM rate_profiles;

-- ---------- Settings and master data ----------
DELETE FROM login_security;
DELETE FROM department_signatories;
DELETE FROM app_settings;
DELETE FROM sites;
DELETE FROM currencies WHERE code <> 'PHP';
UPDATE currencies SET is_default = true, is_active = true WHERE code = 'PHP';

-- ---------- Users: keep only admin ----------
DELETE FROM user_roles WHERE user_id <> (SELECT id FROM users WHERE username = 'admin');
UPDATE user_roles
SET granted_by_user_id = NULL, countersigned_by_user_id = NULL, revoked_by_user_id = NULL, is_bootstrap = true;
DELETE FROM users WHERE username <> 'admin';
UPDATE users SET cost_center_id = NULL;
DELETE FROM cost_centers;
DELETE FROM departments WHERE id IS DISTINCT FROM (SELECT department_id FROM users WHERE username = 'admin');

-- ---------- Trail ----------
DELETE FROM workflow_audit;
DELETE FROM record_history;

INSERT INTO workflow_audit (event, actor_user_id, detail)
VALUES ('Data.ResetByScript', NULL, jsonb_build_object('script', 'maintenance/reset_data_keep_admins'));

COMMIT;

-- Check afterwards (users 1, reports 0, profiles 0, currencies 1):
-- SELECT (SELECT count(*) FROM users) AS users, (SELECT count(*) FROM expense_reports) AS reports,
--        (SELECT count(*) FROM rate_profiles) AS profiles, (SELECT count(*) FROM currencies) AS currencies;
