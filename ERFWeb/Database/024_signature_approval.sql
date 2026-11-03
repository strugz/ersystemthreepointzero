-- =====================================================================
--  ERFWeb — signature images need Finance approval before the R47 prints them (024_signature_approval.sql)
--  The R47 signature rule (Rules page, Reports.R47SignatureMode) chooses between printing approved
--  signature images with an ELECTRONIC SIGNATURE watermark, or status only ("Electronically approved"
--  with the date and time, no images). Either way an image prints only after Finance approves it.
--  * users.signature_uploaded_at: when the image on file was loaded; a new upload resets the approval.
--  * users.signature_approved_at / signature_approved_by_user_id: Finance's approval of that image.
--  * Signatures already on file are marked approved (approved_by left empty), so R47s keep printing
--    them after the deploy. To make Finance review them all instead, leave out the UPDATE below.
--  Idempotent: safe to re-run. Run after 001..023, BEFORE deploying the API that needs it.
-- =====================================================================
BEGIN;

ALTER TABLE users ADD COLUMN IF NOT EXISTS signature_uploaded_at timestamptz NULL;
ALTER TABLE users ADD COLUMN IF NOT EXISTS signature_approved_at timestamptz NULL;
ALTER TABLE users ADD COLUMN IF NOT EXISTS signature_approved_by_user_id bigint NULL;

DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_constraint WHERE conname = 'fk_users_signature_approved_by') THEN
        ALTER TABLE users ADD CONSTRAINT fk_users_signature_approved_by
            FOREIGN KEY (signature_approved_by_user_id) REFERENCES users (id) ON DELETE SET NULL;
    END IF;
END $$;

-- Existing signatures: approved as they stand. Only rows never touched by this script are updated.
UPDATE users
SET signature_uploaded_at = now(),
    signature_approved_at = now()
WHERE signature IS NOT NULL
  AND length(signature) > 0
  AND signature_uploaded_at IS NULL;

INSERT INTO schema_versions (version, name) VALUES (24, '024_signature_approval')
ON CONFLICT (version) DO NOTHING;

COMMIT;

-- =====================================================================
--  Rollback (manual; deploy the API without this change first).
--  BEGIN;
--  ALTER TABLE users DROP CONSTRAINT IF EXISTS fk_users_signature_approved_by;
--  ALTER TABLE users DROP COLUMN IF EXISTS signature_approved_by_user_id;
--  ALTER TABLE users DROP COLUMN IF EXISTS signature_approved_at;
--  ALTER TABLE users DROP COLUMN IF EXISTS signature_uploaded_at;
--  DELETE FROM schema_versions WHERE version = 24;
--  COMMIT;
-- =====================================================================
