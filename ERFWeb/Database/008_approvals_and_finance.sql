-- =====================================================================
--  ERFWeb — Phase 5: approvals and Finance receipts (008_approvals_and_finance.sql)
--  * expense_lines keep the filed amount when the Level 1 approver lowers a line before the
--    first approval (amounts are locked after it, spec 9.6).
--  * finance_tracking gains clearance and follow-up columns. Its status now runs
--    Pending -> ReceiptsIncomplete -> ReceiptsReceived (one-time) -> ClearedForPayment.
--  * New text values (no schema change needed): expense_reports.status 'Rejected';
--    approval_steps.status 'Rejected' and 'Overridden'.
--  Idempotent: safe to re-run. Run after 001..007.
-- =====================================================================
BEGIN;

ALTER TABLE expense_lines ADD COLUMN IF NOT EXISTS original_amount       numeric(14,2);
ALTER TABLE expense_lines ADD COLUMN IF NOT EXISTS original_total_amount numeric(14,2);
ALTER TABLE expense_lines ADD COLUMN IF NOT EXISTS adjusted_by_user_id   bigint REFERENCES users (id);
ALTER TABLE expense_lines ADD COLUMN IF NOT EXISTS adjusted_at           timestamp with time zone;
ALTER TABLE expense_lines ADD COLUMN IF NOT EXISTS adjustment_reason     text;
CREATE INDEX IF NOT EXISTS ix_expense_lines_adjusted_by_user_id ON expense_lines (adjusted_by_user_id);

ALTER TABLE finance_tracking ADD COLUMN IF NOT EXISTS cleared_by_user_id bigint REFERENCES users (id);
ALTER TABLE finance_tracking ADD COLUMN IF NOT EXISTS cleared_at         timestamp with time zone;
ALTER TABLE finance_tracking ADD COLUMN IF NOT EXISTS follow_up_count    integer NOT NULL DEFAULT 0;
ALTER TABLE finance_tracking ADD COLUMN IF NOT EXISTS last_follow_up_at  timestamp with time zone;
ALTER TABLE finance_tracking ADD COLUMN IF NOT EXISTS created_at         timestamp with time zone NOT NULL DEFAULT now();
ALTER TABLE finance_tracking ADD COLUMN IF NOT EXISTS updated_at         timestamp with time zone NOT NULL DEFAULT now();
CREATE INDEX IF NOT EXISTS ix_finance_tracking_cleared_by_user_id  ON finance_tracking (cleared_by_user_id);
CREATE INDEX IF NOT EXISTS ix_finance_tracking_received_by_user_id ON finance_tracking (received_by_user_id);

-- received_by_user_id existed without a foreign key; add it once.
DO $$
BEGIN
  IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fk_finance_tracking_received_by') THEN
    ALTER TABLE finance_tracking
      ADD CONSTRAINT fk_finance_tracking_received_by FOREIGN KEY (received_by_user_id) REFERENCES users (id);
  END IF;
END $$;

-- Queue order and filter.
CREATE INDEX IF NOT EXISTS ix_finance_tracking_status ON finance_tracking (status);

COMMIT;

-- =====================================================================
--  Rollback (manual; roll back the Phase 5 build first):
--  BEGIN;
--  ALTER TABLE finance_tracking DROP CONSTRAINT IF EXISTS fk_finance_tracking_received_by;
--  ALTER TABLE finance_tracking DROP COLUMN IF EXISTS updated_at, DROP COLUMN IF EXISTS created_at,
--    DROP COLUMN IF EXISTS last_follow_up_at, DROP COLUMN IF EXISTS follow_up_count,
--    DROP COLUMN IF EXISTS cleared_at, DROP COLUMN IF EXISTS cleared_by_user_id;
--  ALTER TABLE expense_lines DROP COLUMN IF EXISTS adjustment_reason, DROP COLUMN IF EXISTS adjusted_at,
--    DROP COLUMN IF EXISTS adjusted_by_user_id, DROP COLUMN IF EXISTS original_total_amount,
--    DROP COLUMN IF EXISTS original_amount;
--  UPDATE finance_tracking SET status = 'Pending' WHERE status IN ('ReceiptsIncomplete', 'ClearedForPayment');
--  COMMIT;
-- =====================================================================
