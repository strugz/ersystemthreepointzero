namespace ERFWeb.Infrastructure.Persistence;

/// <summary>
/// Raw SQL for the row-history trigger function and the triggers attached to the
/// important, high-churn tables. Applied from the initial migration (EF does not
/// generate trigger objects from the model). Kept here so it lives with the schema.
/// </summary>
public static class HistorySql
{
    public const string CreateFunction = """
        CREATE OR REPLACE FUNCTION fn_record_history() RETURNS trigger AS $$
        DECLARE
          v_user bigint := NULLIF(current_setting('app.current_user_id', true), '')::bigint;
        BEGIN
          IF (TG_OP = 'UPDATE') THEN
            IF to_jsonb(NEW) IS DISTINCT FROM to_jsonb(OLD) THEN
              INSERT INTO record_history(table_name,row_pk,operation,changed_by,old_data,new_data)
              VALUES (TG_TABLE_NAME, (to_jsonb(OLD)->>TG_ARGV[0]), 'U', v_user, to_jsonb(OLD), to_jsonb(NEW));
            END IF;
            RETURN NEW;
          ELSIF (TG_OP = 'DELETE') THEN
            INSERT INTO record_history(table_name,row_pk,operation,changed_by,old_data,new_data)
            VALUES (TG_TABLE_NAME, (to_jsonb(OLD)->>TG_ARGV[0]), 'D', v_user, to_jsonb(OLD), NULL);
            RETURN OLD;
          END IF;
          RETURN NULL;
        END;
        $$ LANGUAGE plpgsql;
        """;

    public const string CreateTriggers = """
        CREATE TRIGGER trg_hist_expense_reports  AFTER UPDATE OR DELETE ON expense_reports
          FOR EACH ROW EXECUTE FUNCTION fn_record_history('id');
        CREATE TRIGGER trg_hist_expense_lines    AFTER UPDATE OR DELETE ON expense_lines
          FOR EACH ROW EXECUTE FUNCTION fn_record_history('id');
        CREATE TRIGGER trg_hist_approval_steps   AFTER UPDATE OR DELETE ON approval_steps
          FOR EACH ROW EXECUTE FUNCTION fn_record_history('id');
        CREATE TRIGGER trg_hist_finance_tracking AFTER UPDATE OR DELETE ON finance_tracking
          FOR EACH ROW EXECUTE FUNCTION fn_record_history('report_id');
        CREATE TRIGGER trg_hist_cash_advance_lines AFTER UPDATE OR DELETE ON cash_advance_lines
          FOR EACH ROW EXECUTE FUNCTION fn_record_history('id');
        """;

    public const string DropAll = """
        DROP TRIGGER IF EXISTS trg_hist_cash_advance_lines ON cash_advance_lines;
        DROP TRIGGER IF EXISTS trg_hist_finance_tracking ON finance_tracking;
        DROP TRIGGER IF EXISTS trg_hist_approval_steps   ON approval_steps;
        DROP TRIGGER IF EXISTS trg_hist_expense_lines    ON expense_lines;
        DROP TRIGGER IF EXISTS trg_hist_expense_reports  ON expense_reports;
        DROP FUNCTION IF EXISTS fn_record_history();
        """;
}
