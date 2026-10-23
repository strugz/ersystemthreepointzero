-- =====================================================================
--  ERFWeb — per-employee R47 REVIEWED BY (019_user_r47_reviewed_by.sql)
--  REVIEWED BY on the R47 is chosen in this order: the employee's own value (this column), else the
--  department's (department_signatories.reviewer_initials), else the Rules setting
--  Reports.R47ReviewedBy (default FINANCE).
--  * users.r47_reviewed_by: nullable; existing accounts stay blank and follow their department.
--  Idempotent: safe to re-run. Run after 001..018, BEFORE deploying the API that needs it.
-- =====================================================================
BEGIN;

ALTER TABLE users ADD COLUMN IF NOT EXISTS r47_reviewed_by varchar(60);

INSERT INTO schema_versions (version, name) VALUES (19, '019_user_r47_reviewed_by')
ON CONFLICT (version) DO NOTHING;

COMMIT;

-- =====================================================================
--  Rollback (manual; deploy the API without this change first). Drops every per-employee value.
--  BEGIN;
--  ALTER TABLE users DROP COLUMN IF EXISTS r47_reviewed_by;
--  DELETE FROM schema_versions WHERE version = 19;
--  COMMIT;
-- =====================================================================
