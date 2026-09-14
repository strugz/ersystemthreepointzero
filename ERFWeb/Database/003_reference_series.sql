-- =====================================================================
--  ERFWeb — ERF reference number series (003_reference_series.sql)
--  One counter row per year, keyed "ERF-2026". The API increments it inside the
--  filing transaction and formats the number as ERF-2026-000142 (prefix and
--  padding come from the ReferenceNumber section of appsettings.json).
--  Numbers are issued on FIRST filing only, so deleted drafts leave no gaps.
--  Idempotent: safe to re-run.
-- =====================================================================
BEGIN;

CREATE TABLE IF NOT EXISTS reference_series (
  series_key  varchar(32) PRIMARY KEY,
  next_value  integer     NOT NULL DEFAULT 1,
  updated_at  timestamptz NOT NULL DEFAULT now()
);

COMMENT ON TABLE reference_series IS 'Yearly ERF reference number counters (PREFIX-YYYY -> next sequence).';

COMMIT;
