-- =====================================================================
--  ERFWeb — an expense line may link several CRM activities (023_expense_line_crm_activities.sql)
--  A client visit can cover several service instruments, one CRM activity each; the employee ticks
--  them all for one line, and the line's service no., instrument and serial list them joined by " / ".
--  * expense_lines.crm_activity_ids: the CRM activityIds (ITI-/SR-) the line was pre-filled from, in
--    the order picked; empty for a line typed by hand. The one-report-per-activity rule still applies
--    to each ID and is checked by the API.
--  * Moves every expense_lines.crm_activity_id into the new column and clears it. The API no longer
--    reads that column; it stays (empty) so the running API keeps working until the new one is
--    deployed. This updates each linked line once, so trg_hist_expense_lines writes one
--    record_history row each and an editor open on such a line reloads after a 409. Run it when
--    nobody is filing, BEFORE deploying the API that needs it, and run it again right after the
--    deploy to move any link the old API saved in between.
--  Idempotent: safe to re-run. Run after 001..022.
-- =====================================================================
BEGIN;

ALTER TABLE expense_lines ADD COLUMN IF NOT EXISTS crm_activity_ids varchar(50)[] NOT NULL DEFAULT '{}';

CREATE INDEX IF NOT EXISTS ix_expense_lines_crm_activity_ids ON expense_lines USING gin (crm_activity_ids);

UPDATE expense_lines
SET crm_activity_ids = CASE
        WHEN crm_activity_id = ANY (crm_activity_ids) THEN crm_activity_ids
        ELSE array_append(crm_activity_ids, crm_activity_id)
    END,
    crm_activity_id = NULL
WHERE crm_activity_id IS NOT NULL;

INSERT INTO schema_versions (version, name) VALUES (23, '023_expense_line_crm_activities')
ON CONFLICT (version) DO NOTHING;

COMMIT;

-- =====================================================================
--  Rollback (manual; deploy the API without this change first). Keeps each line's first activity.
--  BEGIN;
--  UPDATE expense_lines SET crm_activity_id = crm_activity_ids[1] WHERE cardinality(crm_activity_ids) > 0;
--  DROP INDEX IF EXISTS ix_expense_lines_crm_activity_ids;
--  ALTER TABLE expense_lines DROP COLUMN IF EXISTS crm_activity_ids;
--  DELETE FROM schema_versions WHERE version = 23;
--  COMMIT;
-- =====================================================================
