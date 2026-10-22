-- =====================================================================
--  ERFWeb — Phase 8.3: forced password change and schema versions (015_security_and_schema_versions.sql)
--  * users.must_change_password: set when an administrator sets a password (new account, reset);
--    the user must choose their own before using anything else. Existing accounts start at false.
--  * schema_versions: one row per Database/NNN script run. /api/health compares the highest number
--    with the one the API needs and reports "schema_outdated" (503) while the database is behind, so
--    a deploy made before its script rolls back. Scripts 001-014 are recorded here retroactively;
--    from now on every script ends by inserting its own row.
--  * Sign-in lockout reuses the existing login_security table (no change).
--  Idempotent: safe to re-run. Run after 001..014, BEFORE deploying the API that needs it.
-- =====================================================================
BEGIN;

ALTER TABLE users
  ADD COLUMN IF NOT EXISTS must_change_password boolean NOT NULL DEFAULT false;

CREATE TABLE IF NOT EXISTS schema_versions (
  version     integer PRIMARY KEY,
  name        character varying(200) NOT NULL,
  applied_at  timestamp with time zone NOT NULL DEFAULT now()
);

INSERT INTO schema_versions (version, name) VALUES
  (1,  '001_initial_schema'),
  (2,  '002_seed_admin'),
  (3,  '003_reference_series'),
  (4,  '004_seed_test_users'),
  (5,  '005_cash_advance_lines'),
  (6,  '006_user_roles_and_administration'),
  (7,  '007_scope_and_rate_profiles'),
  (8,  '008_approvals_and_finance'),
  (9,  '009_verification_requests'),
  (10, '010_seed_test_verifier'),
  (11, '011_sms_deliveries'),
  (12, '012_sms_deliveries_report'),
  (13, '013_approval_delegations'),
  (14, '014_email_deliveries'),
  (15, '015_security_and_schema_versions')
ON CONFLICT (version) DO NOTHING;

COMMIT;

-- =====================================================================
--  Rollback (manual; deploy the API without Phase 8.3 first, or its health check reports
--  schema_outdated):
--  BEGIN;
--  DROP TABLE IF EXISTS schema_versions;
--  ALTER TABLE users DROP COLUMN IF EXISTS must_change_password;
--  COMMIT;
-- =====================================================================
