-- ERFWeb: delete all data, keep only the 'admin' login. Take a pg_dump first; there is no rollback.
-- Run top to bottom (children before parents). schema_versions is kept on purpose.

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
DELETE FROM approval_delegations;
DELETE FROM approval_routes;
DELETE FROM employee_rates;
DELETE FROM scope_assignment_members;
DELETE FROM scope_assignments;
DELETE FROM rate_profile_assignments;
DELETE FROM rate_profile_lines;
DELETE FROM rate_profiles;
DELETE FROM login_security;
DELETE FROM workflow_audit;
DELETE FROM department_signatories;
DELETE FROM app_settings;
DELETE FROM sites;
DELETE FROM currencies WHERE code <> 'PHP';
DELETE FROM user_roles WHERE user_id <> (SELECT id FROM users WHERE username = 'admin');
UPDATE user_roles SET granted_by_user_id = NULL, countersigned_by_user_id = NULL, revoked_by_user_id = NULL, is_bootstrap = true;
DELETE FROM users WHERE username <> 'admin';
UPDATE users SET cost_center_id = NULL;
DELETE FROM cost_centers;
DELETE FROM departments WHERE id <> (SELECT department_id FROM users WHERE username = 'admin');
DELETE FROM record_history;
