-- =====================================================================
--  ERFWeb — form code check on attendance sheets, Finance rules phase 4 (027_attachment_form_code_check.sql)
--  A Demo line needs the ISO attendance sheet, form R94 (code set on the Rules page). On upload the API
--  searches the PDF's text for the code; a photo, a scan or a PDF without it is accepted only when the
--  employee confirms it is the form.
--  * receipt_attachments.form_code_check: Found (code in the PDF text) or Confirmed (employee confirmed);
--    NULL for other documents and for attendance sheets uploaded before this script, which no longer
--    count for a Demo line until uploaded again.
--  Idempotent: safe to re-run. Run after 001..026, BEFORE deploying the API that needs it.
-- =====================================================================
BEGIN;

ALTER TABLE receipt_attachments ADD COLUMN IF NOT EXISTS form_code_check varchar(16) NULL;

INSERT INTO schema_versions (version, name) VALUES (27, '027_attachment_form_code_check')
ON CONFLICT (version) DO NOTHING;

COMMIT;

-- =====================================================================
--  Rollback (manual; deploy the API without this change first).
--  BEGIN;
--  ALTER TABLE receipt_attachments DROP COLUMN IF EXISTS form_code_check;
--  DELETE FROM schema_versions WHERE version = 27;
--  COMMIT;
-- =====================================================================
