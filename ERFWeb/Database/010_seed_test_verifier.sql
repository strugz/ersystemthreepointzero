-- =====================================================================
--  ERFWeb — OPTIONAL, development and UAT only (010_seed_test_verifier.sql)
--  Adds a test Expense Verifier so the Phase 6 flow can be walked through:
--    username = verifier1   password = Test!1234 (same as the 004 test users)
--  Do NOT run in production: real verifiers are granted through the administration console and
--  countersigned. Requires 004 (it copies employee1's password hash). Idempotent.
-- =====================================================================
BEGIN;

INSERT INTO users (username, password_hash, full_name, position, department_id, is_active)
SELECT 'verifier1',
       (SELECT password_hash FROM users WHERE username = 'employee1'),
       'Test Verifier',
       'Administrator Department',
       (SELECT id FROM departments WHERE code = 'IMS'),
       true
WHERE NOT EXISTS (SELECT 1 FROM users WHERE username = 'verifier1')
  AND EXISTS (SELECT 1 FROM users WHERE username = 'employee1');

INSERT INTO user_roles (user_id, role, status, granted_at, countersigned_at, is_bootstrap, note)
SELECT u.id, 'ExpenseVerifier', 'Active', now(), now(), true, 'Test verifier seeded by 010'
FROM users u
WHERE u.username = 'verifier1'
ON CONFLICT (user_id, role) DO NOTHING;

COMMIT;
