-- =====================================================================
--  ERFWeb — the audit trail is append-only in the database itself (021_workflow_audit_append_only.sql)
--  Spec 9.9: no screen, endpoint or role may edit, redact or purge an audit record. The API has never
--  done so; from this script on, PostgreSQL refuses it too:
--  * UPDATE or DELETE of a workflow_audit row, and TRUNCATE of the table, raise an error.
--  * The only way past it is a maintenance script that opts in for its own transaction with
--      SET LOCAL erfweb.allow_audit_purge = 'on';
--    (Database/maintenance/reset_*.sql and delete_users_keep_admin.sql do, and record that they ran.)
--    The API never sets it.
--  Inserting stays as it was. Idempotent: safe to re-run. The API does not depend on it, so it may be
--  run before or after a deploy.
-- =====================================================================
BEGIN;

CREATE OR REPLACE FUNCTION workflow_audit_append_only() RETURNS trigger
LANGUAGE plpgsql AS $$
BEGIN
  IF coalesce(current_setting('erfweb.allow_audit_purge', true), '') = 'on' THEN
    RETURN CASE WHEN TG_OP = 'DELETE' THEN OLD ELSE NEW END;
  END IF;
  RAISE EXCEPTION 'workflow_audit is append-only: % is not allowed (spec 9.9)', TG_OP
    USING ERRCODE = 'insufficient_privilege';
END;
$$;

DROP TRIGGER IF EXISTS trg_workflow_audit_no_change ON workflow_audit;
CREATE TRIGGER trg_workflow_audit_no_change
  BEFORE UPDATE OR DELETE ON workflow_audit
  FOR EACH ROW EXECUTE FUNCTION workflow_audit_append_only();

DROP TRIGGER IF EXISTS trg_workflow_audit_no_truncate ON workflow_audit;
CREATE TRIGGER trg_workflow_audit_no_truncate
  BEFORE TRUNCATE ON workflow_audit
  FOR EACH STATEMENT EXECUTE FUNCTION workflow_audit_append_only();

INSERT INTO schema_versions (version, name) VALUES (21, '021_workflow_audit_append_only')
ON CONFLICT (version) DO NOTHING;

COMMIT;

-- =====================================================================
--  Rollback (manual). Makes the audit trail editable again.
--  BEGIN;
--  DROP TRIGGER IF EXISTS trg_workflow_audit_no_truncate ON workflow_audit;
--  DROP TRIGGER IF EXISTS trg_workflow_audit_no_change ON workflow_audit;
--  DROP FUNCTION IF EXISTS workflow_audit_append_only();
--  DELETE FROM schema_versions WHERE version = 21;
--  COMMIT;
-- =====================================================================
