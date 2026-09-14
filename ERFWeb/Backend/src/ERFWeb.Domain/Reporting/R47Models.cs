namespace ERFWeb.Domain.Reporting;

/// <summary>
/// One expense line as the R47 report needs it. Framework-free input so the
/// bucketing/pagination rules can be unit-tested without EF entities.
/// </summary>
public sealed record R47Line(
    DateOnly TransactionDate,
    string Particulars,
    string? InvoiceOrNo,
    ExpenseType ExpenseType,
    ExpenseCategory Category,
    decimal Amount,
    decimal VatAmount,
    decimal TotalAmount,
    string? Location,
    string? Instrument,
    string? SerialNumber,
    string? ServiceNumber,
    string? WorkWith,
    int SortOrder);

/// <summary>Amounts of one line spread into the R47 columns.</summary>
public readonly record struct R47Buckets(
    decimal Meals,
    decimal Transportation,
    decimal Toll,
    decimal Parking,
    decimal Others,
    decimal Representation,
    decimal Oth,
    decimal Vat,
    decimal GrandTotal)
{
    /// <summary>The LOCAL TRAVEL "TOTAL" column: sum of the five local sub-columns.</summary>
    public decimal LocalTotal => Meals + Transportation + Toll + Parking + Others;

    public static R47Buckets Zero => default;

    public R47Buckets Add(R47Buckets o) => new(
        Meals + o.Meals, Transportation + o.Transportation, Toll + o.Toll, Parking + o.Parking,
        Others + o.Others, Representation + o.Representation, Oth + o.Oth, Vat + o.Vat, GrandTotal + o.GrandTotal);
}

/// <summary>A printable row; <see cref="Lines"/> is its estimated printed height in text lines.</summary>
public abstract record R47Row(int Lines);

/// <summary>Group header: shows the date (first group of that date, or first group on a page) and the bold location + Work With text, verbatim.</summary>
public sealed record R47GroupHeaderRow(DateOnly Date, bool ShowDate, string Text, int Lines = 1) : R47Row(Lines);

/// <summary>Detail line: particulars (indented), invoice/OR, and bucketed amounts.</summary>
public sealed record R47DetailRow(string Particulars, string? InvoiceOrNo, R47Buckets Buckets, int Lines = 1) : R47Row(Lines);

/// <summary>Everything one rendered page needs: its rows, this page's totals, and the cash-advance block.</summary>
public sealed record R47Page(
    int PageNumber,
    IReadOnlyList<R47Row> Rows,
    R47Buckets PageTotals,
    R47Reconciliation Reconciliation,
    int UsedLines = 0);

/// <summary>
/// The per-page cash-advance block. Mirrors the legacy report's running behaviour:
/// the "Cash Advance" row carries the balance from prior pages, "Less Expense" is this
/// page's expenses, and "Bal Due" is the remainder (negative = due to employee).
/// </summary>
public readonly record struct R47Reconciliation(decimal CarriedBalance, decimal LessExpense, decimal BalanceDue);
