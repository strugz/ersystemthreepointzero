-- =====================================================================
--  ERFWeb: sample cost center for the IMS department, for trying out the Budgets page.
--  Needs 022_cost_center_budgets.sql first. Adds:
--  * cost center CC-IMS "Information Management Systems", linked to department IMS
--  * its overall budget for 2026: 500,000.00 (change v_amount / v_year below)
--  Skipped when IMS already has an active cost center (a department has at most one); the budget
--  is then set on that cost center instead. The same can be done from Master data -> Cost centers,
--  which also writes an Admin.MasterDataChanged audit row; this script writes none.
--  Run as one transaction; safe to re-run.
-- =====================================================================
BEGIN;

DO $$
DECLARE
  v_year   integer       := 2026;
  v_amount numeric(14,2) := 500000.00;
  v_dept   bigint;
  v_cc     bigint;
BEGIN
  SELECT id INTO v_dept FROM departments WHERE code = 'IMS';
  IF v_dept IS NULL THEN
    RAISE EXCEPTION 'Department IMS not found.';
  END IF;

  SELECT id INTO v_cc FROM cost_centers WHERE department_id = v_dept AND is_active ORDER BY id LIMIT 1;
  IF v_cc IS NULL THEN
    INSERT INTO cost_centers (code, name, department_id, is_active)
    VALUES ('CC-IMS', 'Information Management Systems', v_dept, true)
    ON CONFLICT (code) DO UPDATE SET department_id = EXCLUDED.department_id, is_active = true
    RETURNING id INTO v_cc;
  END IF;

  INSERT INTO cost_center_budgets (cost_center_id, budget_year, amount, updated_at)
  VALUES (v_cc, v_year, v_amount, now())
  ON CONFLICT (cost_center_id, budget_year) DO UPDATE SET amount = EXCLUDED.amount, updated_at = now();

  RAISE NOTICE 'Cost center % has a % budget of %.', v_cc, v_year, v_amount;
END $$;

COMMIT;

-- =====================================================================
--  Undo (manual). Only while no other budgets were set on CC-IMS that you want to keep.
--  BEGIN;
--  DELETE FROM cost_center_budgets WHERE cost_center_id = (SELECT id FROM cost_centers WHERE code = 'CC-IMS');
--  DELETE FROM cost_centers WHERE code = 'CC-IMS';
--  COMMIT;
-- =====================================================================
