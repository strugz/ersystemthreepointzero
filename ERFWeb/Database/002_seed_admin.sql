-- =====================================================================
--  ERFWeb — initial seed (002_seed_admin.sql)
--  Creates one department and an Admin login so you can sign in.
--  Login:  username = admin   password = ChangeMe!123   (CHANGE IT after first login)
--  Idempotent: safe to re-run.
-- =====================================================================
BEGIN;

INSERT INTO departments (code, name)
VALUES ('IMS', 'Information Management Systems')
ON CONFLICT (code) DO NOTHING;

INSERT INTO users (username, password_hash, full_name, position, department_id, role, is_active)
VALUES (
  'admin',
  '100000.lYUOEyU5tdswjcHo7VyOwQ==.mCgxznWS9G7DVKuW1AFS7Rm4K28XTedhLK5h9zvnr88=',
  'System Administrator',
  'Administrator',
  (SELECT id FROM departments WHERE code = 'IMS'),
  'Admin',
  true
)
ON CONFLICT (username) DO NOTHING;

COMMIT;
