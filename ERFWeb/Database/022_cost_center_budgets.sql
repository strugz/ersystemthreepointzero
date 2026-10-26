-- =====================================================================
--  ERFWeb — yearly cost center budgets (022_cost_center_budgets.sql)
--  A cost center is one department's spending code with one overall budget per calendar year.
--  It is for monitoring only: signatories who cover the department and Finance compare the budget
--  with what was spent. Nothing is blocked when a department goes over.
--  * cost_center_budgets: one amount per cost center per year, set by the System Administrator.
--  * expense_reports.department_id: the employee's department when the report was (re-)filed, so
--    moving an employee to another department later never moves their past spending. The cost
--    center is looked up from that department, so budgets set after filing still count it.
--  * Backfill: every report already filed gets its employee's current department. This updates
--    those rows once, so trg_hist_expense_reports writes one record_history row each (changed_by
--    empty) and an editor open on such a report reloads after a 409. Run it when nobody is filing.
--  Idempotent: safe to re-run. Run after 001..021, BEFORE deploying the API that needs it.
-- =====================================================================
BEGIN;

CREATE TABLE IF NOT EXISTS cost_center_budgets (
  cost_center_id     bigint        NOT NULL REFERENCES cost_centers (id),
  budget_year        integer       NOT NULL CHECK (budget_year BETWEEN 2000 AND 2100),
  amount             numeric(14,2) NOT NULL CHECK (amount >= 0),
  updated_by_user_id bigint        REFERENCES users (id),
  updated_at         timestamptz   NOT NULL DEFAULT now(),
  PRIMARY KEY (cost_center_id, budget_year)
);

ALTER TABLE expense_reports ADD COLUMN IF NOT EXISTS department_id bigint REFERENCES departments (id);
CREATE INDEX IF NOT EXISTS ix_expense_reports_department_id ON expense_reports (department_id);

UPDATE expense_reports r
SET department_id = u.department_id
FROM users u
WHERE u.id = r.employee_user_id
  AND r.filed_at IS NOT NULL
  AND r.department_id IS NULL
  AND u.department_id IS NOT NULL;

INSERT INTO schema_versions (version, name) VALUES (22, '022_cost_center_budgets')
ON CONFLICT (version) DO NOTHING;

COMMIT;

-- =====================================================================
--  Rollback (manual; deploy the API without this change first). Drops every budget.
--  BEGIN;
--  DROP INDEX IF EXISTS ix_expense_reports_department_id;
--  ALTER TABLE expense_reports DROP COLUMN IF EXISTS department_id;
--  DROP TABLE IF EXISTS cost_center_budgets;
--  DELETE FROM schema_versions WHERE version = 22;
--  COMMIT;
-- =====================================================================
