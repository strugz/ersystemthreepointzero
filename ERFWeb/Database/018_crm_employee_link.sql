-- =====================================================================
--  ERFWeb — link an account to its CRM employee (018_crm_employee_link.sql)
--  The CRM holds no employee email, so an ERFWeb user is tied to the CRM by the CRM employee ID
--  (e.g. 'CNT-001'), which the System Administrator picks from the CRM employee lookup. Later CRM
--  calls (the activities an employee can claim) take this ID.
--  * users.crm_employee_id: nullable; an existing account stays unlinked until an admin links it.
--  * One CRM employee links to at most one ERFWeb account (unique index; NULLs do not collide).
--  Idempotent: safe to re-run. Run after 001..017, BEFORE deploying the API that needs it.
-- =====================================================================
BEGIN;

ALTER TABLE users ADD COLUMN IF NOT EXISTS crm_employee_id varchar(50);

CREATE UNIQUE INDEX IF NOT EXISTS ix_users_crm_employee_id ON users (crm_employee_id);

INSERT INTO schema_versions (version, name) VALUES (18, '018_crm_employee_link')
ON CONFLICT (version) DO NOTHING;

COMMIT;

-- =====================================================================
--  Rollback (manual; deploy the API without this change first). Drops every CRM link.
--  BEGIN;
--  DROP INDEX IF EXISTS ix_users_crm_employee_id;
--  ALTER TABLE users DROP COLUMN IF EXISTS crm_employee_id;
--  DELETE FROM schema_versions WHERE version = 18;
--  COMMIT;
-- =====================================================================
