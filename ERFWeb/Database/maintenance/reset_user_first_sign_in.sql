-- =====================================================================
--  ERFWeb: put every account except 'admin' back to its first-sign-in state, as if just created.
--  * users.must_change_password = true: at the next sign-in each user must choose a new password
--    before using anything else. Their current password stays and is what they sign in with.
--  * login_security rows of those usernames are deleted (clears failed attempts and any lockout).
--  Nothing else changes: roles, scope, rates, routes, delegations, reports and audit stay.
--  One Users.FirstSignInResetByScript event with the count is added.
--
--  Usage: run the whole script (pgAdmin Query Tool, or psql -d erms_db -f ...). Safe to re-run.
--  Rollback (only the flag; lockout counters are not restored):
--    UPDATE users SET must_change_password = false WHERE username <> 'admin';
-- =====================================================================
BEGIN;

INSERT INTO workflow_audit (event, actor_user_id, detail)
SELECT 'Users.FirstSignInResetByScript', NULL,
       jsonb_build_object('users', count(*), 'script', 'maintenance/reset_user_first_sign_in')
FROM users
WHERE username <> 'admin';

UPDATE users SET must_change_password = true WHERE username <> 'admin';
DELETE FROM login_security WHERE username <> 'admin';

COMMIT;

-- Check afterwards (only admin should be false):
-- SELECT username, must_change_password FROM users ORDER BY username;
