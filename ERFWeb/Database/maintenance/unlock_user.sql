-- =====================================================================
--  ERFWeb: let ONE account sign in again (admin included, e.g. when nobody can open the Users page).
--  * login_security row of that username is deleted (clears failed attempts and any lockout).
--  * reactivate = true also sets users.is_active = true (a deactivated account cannot sign in).
--  * The password stays unless new_password_hash is filled in. Then it is set to that PBKDF2 hash and
--    users.must_change_password = true, so the user signs in with that password and must choose a new one.
--    Get a hash of a password you know from an account that already uses it:
--      SELECT password_hash FROM users WHERE username = '<account whose password you know>';
--    Never put a plain password or a hash in this file in git (the repository is public).
--  * delete_reports = true (default false) also deletes every expense report this user filed or drafted
--    (expense_reports.employee_user_id), any status, with everything tied to it, the same tables
--    as reset_reports.sql: lines and their meal/fare items, verification requests, receipts and
--    documents (stored in the row), approval steps, finance tracking, cash advance lines, reminder,
--    email and SMS delivery rows, and the reports' workflow_audit rows. NO ROLLBACK: take a pg_dump first.
--    Kept: other people's reports (Work With tags naming this user stay), record_history, ERF numbers
--    already used (reference_series is not reset).
--  Roles, scope and rates stay. One Users.UnlockedByScript event with the counts is added.
--
--  Usage: set the values in the DECLARE block, then run the whole script
--  (pgAdmin Query Tool, or psql -d erms_db -f ...). Safe to re-run. The result is shown as a NOTICE.
--  Rollback: deleted reports only from the backup; a replaced password cannot be restored. The lockout
--    needs none (a new lock starts after the next failed attempts).
--    is_active: UPDATE users SET is_active = false WHERE username = '<username>';
-- =====================================================================
DO $$
DECLARE
  target_username   text    := 'CHANGE_ME';  -- username exactly as on the Users page
  reactivate        boolean := true;
  delete_reports    boolean := false;        -- true deletes ALL of this user's expense reports
  new_password_hash text    := '';           -- optional PBKDF2 hash (see the header); empty keeps the password

  target_id   bigint;
  was_active  boolean;
  had_lock    boolean;
  report_ids  uuid[];
  line_count  bigint := 0;
  file_count  bigint := 0;
BEGIN
  SELECT id, is_active INTO target_id, was_active FROM users WHERE username = target_username;
  IF target_id IS NULL THEN
    RAISE EXCEPTION 'No user with username "%". Nothing changed.', target_username;
  END IF;

  DELETE FROM login_security WHERE username = target_username;
  had_lock := FOUND;

  UPDATE users
  SET is_active            = CASE WHEN reactivate THEN true ELSE is_active END,
      password_hash        = CASE WHEN new_password_hash <> '' THEN new_password_hash ELSE password_hash END,
      must_change_password = CASE WHEN new_password_hash <> '' THEN true ELSE must_change_password END
  WHERE id = target_id;

  IF delete_reports THEN
    SELECT coalesce(array_agg(id), '{}') INTO report_ids FROM expense_reports WHERE employee_user_id = target_id;

    IF cardinality(report_ids) > 0 THEN
      SELECT count(*) INTO line_count FROM expense_lines WHERE report_id = ANY (report_ids);
      SELECT count(*) INTO file_count FROM receipt_attachments WHERE report_id = ANY (report_ids);

      -- Script 021 makes workflow_audit append-only; the reports' own audit rows are deleted on purpose.
      PERFORM set_config('erfweb.allow_audit_purge', 'on', true);

      DELETE FROM verification_requests WHERE report_id = ANY (report_ids);
      DELETE FROM expense_meal_items
        WHERE expense_line_id IN (SELECT id FROM expense_lines WHERE report_id = ANY (report_ids));
      DELETE FROM expense_transport_items
        WHERE expense_line_id IN (SELECT id FROM expense_lines WHERE report_id = ANY (report_ids));
      DELETE FROM expense_lines        WHERE report_id = ANY (report_ids);
      DELETE FROM receipt_attachments  WHERE report_id = ANY (report_ids);
      DELETE FROM reminder_deliveries  WHERE report_id = ANY (report_ids);
      DELETE FROM email_deliveries     WHERE report_id = ANY (report_ids);
      DELETE FROM sms_deliveries       WHERE report_id = ANY (report_ids);
      DELETE FROM cash_advance_lines   WHERE report_id = ANY (report_ids);
      DELETE FROM finance_tracking     WHERE report_id = ANY (report_ids);
      DELETE FROM approval_steps       WHERE report_id = ANY (report_ids);
      DELETE FROM workflow_audit       WHERE report_id = ANY (report_ids);
      DELETE FROM expense_reports      WHERE id = ANY (report_ids);
    END IF;
  END IF;

  INSERT INTO workflow_audit (event, actor_user_id, detail)
  VALUES ('Users.UnlockedByScript', NULL,
          jsonb_build_object('userId', target_id, 'lockCleared', had_lock,
                             'reactivated', reactivate AND NOT was_active, 'passwordReset', new_password_hash <> '',
                             'reportsDeleted', coalesce(cardinality(report_ids), 0),
                             'linesDeleted', line_count, 'attachmentsDeleted', file_count,
                             'script', 'maintenance/unlock_user'));

  RAISE NOTICE 'User "%": lockout cleared = %, reactivated = %, password replaced = %, reports deleted = % (lines %, attachments %).',
    target_username, had_lock, reactivate AND NOT was_active, new_password_hash <> '',
    coalesce(cardinality(report_ids), 0), line_count, file_count;
END $$;

-- Check afterwards (reports is 0 when delete_reports was true):
-- SELECT u.username, u.is_active, u.must_change_password, l.failed_attempts, l.lockout_until,
--        (SELECT count(*) FROM expense_reports r WHERE r.employee_user_id = u.id) AS reports
-- FROM users u LEFT JOIN login_security l ON l.username = u.username WHERE u.username = 'CHANGE_ME';
