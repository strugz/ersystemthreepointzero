-- =====================================================================
--  ERFWeb — reports charged to another department (032_multi_department_reports.sql)
--  users.multi_department_enabled: the System Administrator lets this employee charge a report to one of
--  users.other_department_ids as well as to the main department (users.department_id). Off for everyone until set.
--  expense_reports.charged_department_id: the department the employee picked for the report; null = the main
--  department. Filing routes the approval by it and snapshots it into expense_reports.department_id as before.
--  Existing reports keep null, so they behave exactly as today.
--  Idempotent: safe to re-run. Run after 001..031, BEFORE deploying the API that needs it.
-- =====================================================================
BEGIN;

ALTER TABLE users ADD COLUMN IF NOT EXISTS multi_department_enabled boolean NOT NULL DEFAULT false;
ALTER TABLE users ADD COLUMN IF NOT EXISTS other_department_ids bigint[] NOT NULL DEFAULT '{}';

ALTER TABLE expense_reports ADD COLUMN IF NOT EXISTS charged_department_id bigint NULL;

DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fk_expense_reports_departments_charged_department_id') THEN
        ALTER TABLE expense_reports
            ADD CONSTRAINT fk_expense_reports_departments_charged_department_id
            FOREIGN KEY (charged_department_id) REFERENCES departments (id) ON DELETE RESTRICT;
    END IF;
END $$;

CREATE INDEX IF NOT EXISTS ix_expense_reports_charged_department_id ON expense_reports (charged_department_id);

INSERT INTO schema_versions (version, name) VALUES (32, '032_multi_department_reports')
ON CONFLICT (version) DO NOTHING;

COMMIT;

-- =====================================================================
--  Rollback (manual; deploy the API without this change first).
--  BEGIN;
--  DROP INDEX IF EXISTS ix_expense_reports_charged_department_id;
--  ALTER TABLE expense_reports DROP CONSTRAINT IF EXISTS fk_expense_reports_departments_charged_department_id;
--  ALTER TABLE expense_reports DROP COLUMN IF EXISTS charged_department_id;
--  ALTER TABLE users DROP COLUMN IF EXISTS other_department_ids;
--  ALTER TABLE users DROP COLUMN IF EXISTS multi_department_enabled;
--  DELETE FROM schema_versions WHERE version = 32;
--  COMMIT;
-- =====================================================================
