-- =====================================================================
--  ERFWeb — link a notification SMS to its expense report (012_sms_deliveries_report.sql)
--  * sms_deliveries.report_id: the report a follow-up SMS is about. With a report, the claimant and
--    the report's Level 1 and Level 2 signatories are texted (whoever has a mobile number).
--    NULL for an SMS sent to a user without a report. Set to NULL if the report is ever deleted.
--  Idempotent: safe to re-run. Run after 011.
-- =====================================================================
BEGIN;

ALTER TABLE sms_deliveries
  ADD COLUMN IF NOT EXISTS report_id uuid;

DO $$
BEGIN
  IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fk_sms_deliveries_expense_reports_report_id') THEN
    ALTER TABLE sms_deliveries
      ADD CONSTRAINT fk_sms_deliveries_expense_reports_report_id
      FOREIGN KEY (report_id) REFERENCES expense_reports (id) ON DELETE SET NULL;
  END IF;
END $$;

CREATE INDEX IF NOT EXISTS ix_sms_deliveries_report_id ON sms_deliveries (report_id);

COMMIT;

-- =====================================================================
--  Rollback (manual; deploy the API without report-linked SMS first):
--  BEGIN;
--  ALTER TABLE sms_deliveries DROP COLUMN IF EXISTS report_id;
--  COMMIT;
-- =====================================================================
