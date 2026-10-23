-- =====================================================================
--  ERFWeb: delete every user except the 'admin' login, together with every row that points at
--  them, so accounts can be set up again from the Users page. Departments, cost centers, sites,
--  currencies, fare modes, rate profiles, settings and reference_series are kept.
--  Take a pg_dump first; there is no rollback.
--
--  What connects to a user, and what happens to it ("doomed" = a user being deleted):
--  * expense_reports filed by a doomed user, or that a doomed user touched anywhere: approver
--    or acted_by on a step, adjusted a line, received or cleared it in Finance, uploaded a
--    receipt, or took part in a verification request. Those columns are NOT NULL or would point
--    at nothing, so the whole report goes, with everything under it:
--      expense_lines -> expense_meal_items, expense_transport_items         deleted
--      approval_steps, finance_tracking, cash_advance_lines                 deleted
--      verification_requests, receipt_attachments                           deleted
--      reminder_deliveries, email_deliveries, sms_deliveries of the report  deleted
--      workflow_audit rows of the report                                    deleted
--    A report filed by admin is deleted too if a doomed user approved or handled it.
--  * sms_deliveries   sender or recipient doomed                              deleted
--  * email_deliveries sender doomed                                           deleted
--  * approval_routes  employee or approver doomed                             deleted
--  * approval_delegations signatory, delegate or creator doomed               deleted
--                     (approval_steps.delegation_id cleared first); cancelled_by doomed -> NULL
--  * scope_assignments holder doomed                                          deleted (members cascade)
--                     created_by doomed -> reassigned to admin (deleted if admin is the holder)
--  * scope_assignment_members user doomed                                     deleted
--  * employee_rates   user doomed                                             deleted
--  * rate_profile_assignments Individual target doomed                        deleted
--                     assigned_by doomed -> reassigned to admin (deleted if admin is the target)
--  * rate_profiles.created_by, rate_profile_lines.changed_by doomed           reassigned to admin,
--                     so the company's rates survive
--  * user_roles       user doomed                                             deleted
--                     granted_by / countersigned_by / revoked_by doomed -> NULL
--  * app_settings.updated_by doomed                                           NULL
--  * login_security   lockout rows of doomed usernames                        deleted
--  * users            doomed                                                  deleted
--  * workflow_audit (other rows) and record_history                           kept as the trail; they
--                     hold user ids without a foreign key. One Users.DeletedByScript event is added.
--
--  If the API ever adds a new foreign key to users that is not handled here, the final
--  DELETE FROM users fails and the whole transaction rolls back; nothing is half-deleted.
--  Run as one transaction; safe to re-run.
-- =====================================================================
BEGIN;

DO $$
BEGIN
  IF NOT EXISTS (SELECT 1 FROM users WHERE username = 'admin') THEN
    RAISE EXCEPTION 'No ''admin'' user found; nothing deleted.';
  END IF;
END $$;

CREATE TEMP TABLE kept_users ON COMMIT DROP AS
SELECT id, username FROM users WHERE username = 'admin';

CREATE TEMP TABLE doomed_users ON COMMIT DROP AS
SELECT id, username FROM users WHERE id NOT IN (SELECT id FROM kept_users);

CREATE TEMP TABLE doomed_reports ON COMMIT DROP AS
SELECT r.id FROM expense_reports r
WHERE r.employee_user_id IN (SELECT id FROM doomed_users)
   OR EXISTS (SELECT 1 FROM approval_steps s WHERE s.report_id = r.id
              AND (s.approver_user_id IN (SELECT id FROM doomed_users)
                   OR s.acted_by_user_id IN (SELECT id FROM doomed_users)))
   OR EXISTS (SELECT 1 FROM expense_lines l WHERE l.report_id = r.id
              AND l.adjusted_by_user_id IN (SELECT id FROM doomed_users))
   OR EXISTS (SELECT 1 FROM finance_tracking f WHERE f.report_id = r.id
              AND (f.received_by_user_id IN (SELECT id FROM doomed_users)
                   OR f.cleared_by_user_id IN (SELECT id FROM doomed_users)))
   OR EXISTS (SELECT 1 FROM receipt_attachments a WHERE a.report_id = r.id
              AND a.uploaded_by_user_id IN (SELECT id FROM doomed_users))
   OR EXISTS (SELECT 1 FROM verification_requests v WHERE v.report_id = r.id
              AND (v.requested_by_user_id IN (SELECT id FROM doomed_users)
                   OR v.verifier_user_id  IN (SELECT id FROM doomed_users)
                   OR v.query_to_user_id  IN (SELECT id FROM doomed_users)
                   OR v.queried_by_user_id IN (SELECT id FROM doomed_users)
                   OR v.decided_by_user_id IN (SELECT id FROM doomed_users)));

INSERT INTO workflow_audit (event, actor_user_id, detail)
SELECT 'Users.DeletedByScript', NULL,
       jsonb_build_object(
         'users',   (SELECT count(*) FROM doomed_users),
         'reports', (SELECT count(*) FROM doomed_reports),
         'script',  'maintenance/delete_users_keep_admin')
WHERE EXISTS (SELECT 1 FROM doomed_users);

-- ---------- Reports the doomed users filed or handled (children first) ----------
DELETE FROM verification_requests WHERE report_id IN (SELECT id FROM doomed_reports);
DELETE FROM expense_meal_items
WHERE expense_line_id IN (SELECT id FROM expense_lines WHERE report_id IN (SELECT id FROM doomed_reports));
DELETE FROM expense_transport_items
WHERE expense_line_id IN (SELECT id FROM expense_lines WHERE report_id IN (SELECT id FROM doomed_reports));
DELETE FROM expense_lines       WHERE report_id IN (SELECT id FROM doomed_reports);
DELETE FROM receipt_attachments WHERE report_id IN (SELECT id FROM doomed_reports);
DELETE FROM reminder_deliveries WHERE report_id IN (SELECT id FROM doomed_reports);
DELETE FROM email_deliveries    WHERE report_id IN (SELECT id FROM doomed_reports);
DELETE FROM sms_deliveries      WHERE report_id IN (SELECT id FROM doomed_reports);
DELETE FROM cash_advance_lines  WHERE report_id IN (SELECT id FROM doomed_reports);
DELETE FROM finance_tracking    WHERE report_id IN (SELECT id FROM doomed_reports);
DELETE FROM approval_steps      WHERE report_id IN (SELECT id FROM doomed_reports);
DELETE FROM expense_reports     WHERE id        IN (SELECT id FROM doomed_reports);
DELETE FROM workflow_audit      WHERE report_id IN (SELECT id FROM doomed_reports);

-- ---------- Notifications sent by or to them ----------
DELETE FROM sms_deliveries
WHERE sender_user_id    IN (SELECT id FROM doomed_users)
   OR recipient_user_id IN (SELECT id FROM doomed_users);
DELETE FROM email_deliveries WHERE sender_user_id IN (SELECT id FROM doomed_users);

-- ---------- Approval routing and delegations ----------
DELETE FROM approval_routes
WHERE employee_user_id IN (SELECT id FROM doomed_users)
   OR approver_user_id IN (SELECT id FROM doomed_users);

UPDATE approval_steps SET delegation_id = NULL
WHERE delegation_id IN (SELECT id FROM approval_delegations
                        WHERE signatory_user_id  IN (SELECT id FROM doomed_users)
                           OR delegate_user_id   IN (SELECT id FROM doomed_users)
                           OR created_by_user_id IN (SELECT id FROM doomed_users));
DELETE FROM approval_delegations
WHERE signatory_user_id  IN (SELECT id FROM doomed_users)
   OR delegate_user_id   IN (SELECT id FROM doomed_users)
   OR created_by_user_id IN (SELECT id FROM doomed_users);
UPDATE approval_delegations SET cancelled_by_user_id = NULL
WHERE cancelled_by_user_id IN (SELECT id FROM doomed_users);

-- ---------- Scope ----------
DELETE FROM scope_assignment_members WHERE user_id IN (SELECT id FROM doomed_users);
DELETE FROM scope_assignments
WHERE holder_user_id IN (SELECT id FROM doomed_users)
   OR (created_by_user_id IN (SELECT id FROM doomed_users)
       AND holder_user_id IN (SELECT id FROM kept_users));          -- would break ck_scope_no_self_scope
UPDATE scope_assignments SET created_by_user_id = (SELECT id FROM kept_users)
WHERE created_by_user_id IN (SELECT id FROM doomed_users);

-- ---------- Rates (profiles are company data: keep them, re-own them to admin) ----------
DELETE FROM employee_rates WHERE user_id IN (SELECT id FROM doomed_users);
DELETE FROM rate_profile_assignments
WHERE user_id IN (SELECT id FROM doomed_users)
   OR (assigned_by_user_id IN (SELECT id FROM doomed_users)
       AND user_id IN (SELECT id FROM kept_users));                 -- would break ck_rate_assign_no_self_rate
UPDATE rate_profile_assignments SET assigned_by_user_id = (SELECT id FROM kept_users)
WHERE assigned_by_user_id IN (SELECT id FROM doomed_users);
UPDATE rate_profile_lines SET changed_by_user_id = (SELECT id FROM kept_users)
WHERE changed_by_user_id IN (SELECT id FROM doomed_users);
UPDATE rate_profiles SET created_by_user_id = (SELECT id FROM kept_users)
WHERE created_by_user_id IN (SELECT id FROM doomed_users);

-- ---------- Roles, settings, sign-in lockout ----------
DELETE FROM user_roles WHERE user_id IN (SELECT id FROM doomed_users);
UPDATE user_roles SET granted_by_user_id = NULL
WHERE granted_by_user_id IN (SELECT id FROM doomed_users);
UPDATE user_roles SET countersigned_by_user_id = NULL
WHERE countersigned_by_user_id IN (SELECT id FROM doomed_users);
UPDATE user_roles SET revoked_by_user_id = NULL
WHERE revoked_by_user_id IN (SELECT id FROM doomed_users);
UPDATE app_settings SET updated_by_user_id = NULL
WHERE updated_by_user_id IN (SELECT id FROM doomed_users);
DELETE FROM login_security WHERE username IN (SELECT username FROM doomed_users);

-- ---------- The users themselves ----------
DELETE FROM users WHERE id IN (SELECT id FROM doomed_users);

COMMIT;

-- Check afterwards (users should be 1):
-- SELECT count(*) AS users FROM users;
-- Every foreign key to users in the live database; each table listed should appear in this script:
-- SELECT conrelid::regclass AS table_name, conname
-- FROM pg_constraint WHERE contype = 'f' AND confrelid = 'users'::regclass ORDER BY 1;
