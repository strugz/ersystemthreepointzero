-- =====================================================================
--  ERFWeb — link an expense line to the CRM activity it came from (020_expense_line_crm_activity.sql)
--  * expense_lines.crm_activity_id: nullable; the CRM activityId (ITI-... itinerary visit or SR-...
--    service report) the employee picked to pre-fill the line. Lines typed by hand stay null.
--  * One activity may fill several lines of the same report (a meal and a fare from one visit), but the
--    API refuses it on a line of another of the employee's reports unless that report was Rejected.
--    The rule spans reports and statuses, so it is checked by the API, not by a unique index.
--  Idempotent: safe to re-run. Run after 001..019, BEFORE deploying the API that needs it.
-- =====================================================================
BEGIN;

ALTER TABLE expense_lines ADD COLUMN IF NOT EXISTS crm_activity_id varchar(50);

CREATE INDEX IF NOT EXISTS ix_expense_lines_crm_activity_id ON expense_lines (crm_activity_id)
    WHERE crm_activity_id IS NOT NULL;

INSERT INTO schema_versions (version, name) VALUES (20, '020_expense_line_crm_activity')
ON CONFLICT (version) DO NOTHING;

COMMIT;

-- =====================================================================
--  Rollback (manual; deploy the API without this change first). Drops every line's CRM link.
--  BEGIN;
--  DROP INDEX IF EXISTS ix_expense_lines_crm_activity_id;
--  ALTER TABLE expense_lines DROP COLUMN IF EXISTS crm_activity_id;
--  DELETE FROM schema_versions WHERE version = 20;
--  COMMIT;
-- =====================================================================
