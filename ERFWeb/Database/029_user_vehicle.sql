-- =====================================================================
--  ERFWeb — the employee's issued vehicle (029_user_vehicle.sql)
--  users.vehicle_model and users.vehicle_plate_no: the vehicle a Gas or Vehicle emergency purchase line is
--  for. The employee saves it from the line dialog's prompt; new Gas and Vehicle emergency purchase lines then
--  fill in the model and plate number. Both NULL until saved.
--  Idempotent: safe to re-run. Run after 001..028, BEFORE deploying the API that needs it.
-- =====================================================================
BEGIN;

ALTER TABLE users ADD COLUMN IF NOT EXISTS vehicle_model    character varying(60) NULL;
ALTER TABLE users ADD COLUMN IF NOT EXISTS vehicle_plate_no character varying(20) NULL;

INSERT INTO schema_versions (version, name) VALUES (29, '029_user_vehicle')
ON CONFLICT (version) DO NOTHING;

COMMIT;

-- =====================================================================
--  Rollback (manual; deploy the API without this change first).
--  BEGIN;
--  ALTER TABLE users DROP COLUMN IF EXISTS vehicle_plate_no;
--  ALTER TABLE users DROP COLUMN IF EXISTS vehicle_model;
--  DELETE FROM schema_versions WHERE version = 29;
--  COMMIT;
-- =====================================================================
