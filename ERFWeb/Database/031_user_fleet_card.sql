-- =====================================================================
--  ERFWeb — fleet card holders (031_user_fleet_card.sql)
--  users.has_fleet_card: the employee has a company fleet card. With the Finance rule "Fleet card" on, such an
--  employee claims Gas only for one of the reasons on the Rules page (the card could not be used). The System
--  Administrator sets it on the user form. False for everyone until set.
--  Idempotent: safe to re-run. Run after 001..030, BEFORE deploying the API that needs it.
-- =====================================================================
BEGIN;

ALTER TABLE users ADD COLUMN IF NOT EXISTS has_fleet_card boolean NOT NULL DEFAULT false;

INSERT INTO schema_versions (version, name) VALUES (31, '031_user_fleet_card')
ON CONFLICT (version) DO NOTHING;

COMMIT;

-- =====================================================================
--  Rollback (manual; deploy the API without this change first).
--  BEGIN;
--  ALTER TABLE users DROP COLUMN IF EXISTS has_fleet_card;
--  DELETE FROM schema_versions WHERE version = 31;
--  COMMIT;
-- =====================================================================
