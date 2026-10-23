-- =====================================================================
--  ERFWeb: delete every rate profile so rates can be set up again from the Rates page.
--  Take a pg_dump first. The deleted lines and assignments stay readable in record_history
--  (the 007 history triggers log each deleted row), but there is no scripted rollback.
--
--  What connects to rate profiles, and what happens to it:
--  * rate_profile_assignments  -> deleted first (its FK to rate_profiles has no cascade)
--  * rate_profile_lines        -> deleted (would also cascade from rate_profiles)
--  * rate_profiles             -> deleted
--  * employee_rates            -> deleted: the old flat rates are no longer read, but re-running 007
--                                 would copy them back as "Migrated - <username>" profiles
--  * record_history            -> kept; gains one 'D' row per deleted line and assignment
--  * workflow_audit            -> kept (past Rates.* events stay); one Rates.ResetByScript event added
--  * expense_lines / expense_meal_items -> untouched: lines keep their own amounts and hold no
--                                 reference to a rate line, so filed ERFs do not change
--
--  Until new profiles are assigned, the line editor finds no rate for anyone (my-rates is empty).
--  Run as one transaction; safe to re-run.
-- =====================================================================
BEGIN;

INSERT INTO workflow_audit (event, actor_user_id, detail)
SELECT 'Rates.ResetByScript', NULL,
       jsonb_build_object(
         'profiles',    (SELECT count(*) FROM rate_profiles),
         'lines',       (SELECT count(*) FROM rate_profile_lines),
         'assignments', (SELECT count(*) FROM rate_profile_assignments),
         'employeeRates', (SELECT count(*) FROM employee_rates),
         'script', 'maintenance/reset_rate_profiles')
WHERE EXISTS (SELECT 1 FROM rate_profiles) OR EXISTS (SELECT 1 FROM employee_rates);

DELETE FROM rate_profile_assignments;
DELETE FROM rate_profile_lines;
DELETE FROM rate_profiles;
DELETE FROM employee_rates;

COMMIT;

-- Check afterwards (all four should be 0):
-- SELECT (SELECT count(*) FROM rate_profiles) AS profiles, (SELECT count(*) FROM rate_profile_lines) AS lines,
--        (SELECT count(*) FROM rate_profile_assignments) AS assignments, (SELECT count(*) FROM employee_rates) AS employee_rates;
