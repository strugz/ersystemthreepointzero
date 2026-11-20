-- =====================================================================
--  ERFWeb — Work With tags as accounts (030_expense_line_work_with_user_ids.sql)
--  expense_lines.work_with_user_ids: the accounts a line's Work With names matched when it was saved (Finance
--  requirement 2: the tagged employee cannot claim the meal the filer claimed that day). Existing lines start
--  empty, so only lines saved after this release tag anyone.
--  Idempotent: safe to re-run. Run after 001..029, BEFORE deploying the API that needs it.
-- =====================================================================
BEGIN;

ALTER TABLE expense_lines ADD COLUMN IF NOT EXISTS work_with_user_ids bigint[] NOT NULL DEFAULT '{}';

CREATE INDEX IF NOT EXISTS ix_expense_lines_work_with_user_ids ON expense_lines USING gin (work_with_user_ids);

INSERT INTO schema_versions (version, name) VALUES (30, '030_expense_line_work_with_user_ids')
ON CONFLICT (version) DO NOTHING;

COMMIT;

-- =====================================================================
--  Rollback (manual; deploy the API without this change first).
--  BEGIN;
--  DROP INDEX IF EXISTS ix_expense_lines_work_with_user_ids;
--  ALTER TABLE expense_lines DROP COLUMN IF EXISTS work_with_user_ids;
--  DELETE FROM schema_versions WHERE version = 30;
--  COMMIT;
-- =====================================================================
