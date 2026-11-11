-- =====================================================================
--  ERFWeb — weekly processing cutoff, Finance rules phase 1 (025_filing_cutoff.sql)
--  Finance processes reports every Friday. The Rules page (Finance rules card, app_settings row
--  Business.FinanceRules, no script needed) sets the weekly cutoff (default Wednesday 12:00) and whether a
--  report filed after it is only noticed or also flagged for Finance.
--  * expense_reports.filed_late: true when the report was last filed after the cutoff while the rule
--    "Flag for Finance" was on. Set at each filing; existing reports stay false.
--  Idempotent: safe to re-run. Run after 001..024, BEFORE deploying the API that needs it.
-- =====================================================================
BEGIN;

ALTER TABLE expense_reports ADD COLUMN IF NOT EXISTS filed_late boolean NOT NULL DEFAULT false;

INSERT INTO schema_versions (version, name) VALUES (25, '025_filing_cutoff')
ON CONFLICT (version) DO NOTHING;

COMMIT;

-- =====================================================================
--  Rollback (manual; deploy the API without this change first).
--  BEGIN;
--  ALTER TABLE expense_reports DROP COLUMN IF EXISTS filed_late;
--  DELETE FROM schema_versions WHERE version = 25;
--  COMMIT;
-- =====================================================================
