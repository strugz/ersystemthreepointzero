-- =====================================================================
--  ERFWeb — administration by the System Administrator alone (017_admin_only_authority.sql)
--  Dual approval is dropped: a role grant is Active as soon as an administrator saves it, scope and
--  rates are set by the System Administrator, and the Business Owner role no longer exists.
--  * Grants still waiting for a countersign are activated (audited as Admin.RoleActivatedByScript).
--  * Business Owner grants are deleted (audited as Admin.RoleRemovedByScript); the API no longer
--    knows the role, so a remaining row would fail every read of user_roles.
--  * user_roles.status now defaults to 'Active'. The countersign and bootstrap columns stay as history.
--  The number is 017 because 016 is reserved for the fare-modes branch; this script does not depend on it.
--  Idempotent: safe to re-run. Run after 001..015, BEFORE deploying the API that needs it.
-- =====================================================================
BEGIN;

INSERT INTO workflow_audit (event, actor_user_id, detail)
SELECT 'Admin.RoleActivatedByScript', NULL,
       jsonb_build_object('userId', user_id, 'role', role, 'script', '017_admin_only_authority')
FROM user_roles
WHERE status = 'PendingActivation';

UPDATE user_roles
SET status = 'Active'
WHERE status = 'PendingActivation';

INSERT INTO workflow_audit (event, actor_user_id, detail)
SELECT 'Admin.RoleRemovedByScript', NULL,
       jsonb_build_object('userId', user_id, 'role', role, 'status', status, 'script', '017_admin_only_authority')
FROM user_roles
WHERE role = 'BusinessOwner';

DELETE FROM user_roles WHERE role = 'BusinessOwner';

ALTER TABLE user_roles ALTER COLUMN status SET DEFAULT 'Active';

INSERT INTO schema_versions (version, name) VALUES (17, '017_admin_only_authority')
ON CONFLICT (version) DO NOTHING;

COMMIT;

-- =====================================================================
--  Rollback (manual; deploy the API without this change first). Activated grants and deleted
--  Business Owner grants are not restored: re-grant them by hand from the audit rows above.
--  BEGIN;
--  ALTER TABLE user_roles ALTER COLUMN status SET DEFAULT 'PendingActivation';
--  DELETE FROM schema_versions WHERE version = 17;
--  COMMIT;
-- =====================================================================
