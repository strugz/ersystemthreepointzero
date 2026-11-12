-- =====================================================================
--  ERFWeb — document type on attachments, Finance rules phase 4 (026_attachment_document_type.sql)
--  Sponsorship and Demo expense lines (new categories; no change needed, categories are stored as text)
--  and Representation lines need a document attached to the report before the line can be added: an
--  approved Activity Request Form for Sponsorship, an ISO-registered attendance sheet for Demo, the food
--  or meal receipt for Representation. The Rules page (Finance rules) switches each rule on or off.
--  * receipt_attachments.document_type: Receipt, ActivityRequestForm, IsoAttendanceSheet or FoodReceipt;
--    NULL (every file uploaded before this script) counts as Receipt.
--  Idempotent: safe to re-run. Run after 001..025, BEFORE deploying the API that needs it.
-- =====================================================================
BEGIN;

ALTER TABLE receipt_attachments ADD COLUMN IF NOT EXISTS document_type varchar(32) NULL;

INSERT INTO schema_versions (version, name) VALUES (26, '026_attachment_document_type')
ON CONFLICT (version) DO NOTHING;

COMMIT;

-- =====================================================================
--  Rollback (manual; deploy the API without this change first).
--  BEGIN;
--  ALTER TABLE receipt_attachments DROP COLUMN IF EXISTS document_type;
--  DELETE FROM schema_versions WHERE version = 26;
--  COMMIT;
-- =====================================================================
