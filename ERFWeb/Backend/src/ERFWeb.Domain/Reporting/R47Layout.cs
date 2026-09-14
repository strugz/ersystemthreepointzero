namespace ERFWeb.Domain.Reporting;

/// <summary>
/// Turns sorted lines into printable rows and pages, computing each page's totals and the
/// running cash-advance reconciliation exactly as the legacy Crystal report does.
/// </summary>
public static class R47Layout
{
    /// <summary>
    /// Height of the detail area, in text lines. A single-line row uses one; a wrapped
    /// particulars/invoice uses more. Calibrated against the reference printouts.
    /// </summary>
    public const int DefaultLinesPerPage = 16;

    /// <summary>
    /// Builds grid rows: a group header whenever the (date, location, work-with) key changes,
    /// with the date printed only on the first group of each date; then one detail row per line.
    /// Each row carries its estimated printed line count so pagination can fill pages by height.
    /// </summary>
    public static IReadOnlyList<R47Row> BuildRows(IEnumerable<R47Line> lines)
    {
        ArgumentNullException.ThrowIfNull(lines);

        var rows = new List<R47Row>();
        DateOnly? lastDate = null;
        string? lastKey = null;

        foreach (var line in lines.OrderBy(l => l.SortOrder))
        {
            // The header is exactly what the employee entered: the location, then the Work With text
            // verbatim when there is one. Nothing is added by the report.
            var location = R47Bucketing.LocationText(line);
            var headerText = string.IsNullOrWhiteSpace(line.WorkWith) ? location : $"{location} {line.WorkWith.Trim()}";
            var key = $"{line.TransactionDate:O}|{headerText}";

            if (key != lastKey)
            {
                var showDate = line.TransactionDate != lastDate;
                var headerLines = R47TextMetrics.LineCount(headerText, R47TextMetrics.ParticularsCapacity, bold: true);
                rows.Add(new R47GroupHeaderRow(line.TransactionDate, showDate, headerText, headerLines));
                lastKey = key;
                lastDate = line.TransactionDate;
            }

            var detailLines = Math.Max(
                R47TextMetrics.LineCount(line.Particulars, R47TextMetrics.ParticularsCapacity),
                R47TextMetrics.LineCount(line.InvoiceOrNo, R47TextMetrics.InvoiceCapacity));
            rows.Add(new R47DetailRow(line.Particulars, line.InvoiceOrNo, R47Bucketing.Bucket(line), detailLines));
        }

        return rows;
    }

    /// <summary>
    /// Fills pages by height the way Crystal does:
    /// <list type="bullet">
    /// <item>a page holds <paramref name="linesPerPage"/> text lines; wrapped rows count for more than one;</item>
    /// <item>a group header is never orphaned — if the header plus its first detail do not fit, the
    /// whole group starts on the next page;</item>
    /// <item>when a group is split across pages, its header (with the date) is repeated at the top of
    /// the continuation page;</item>
    /// <item>the first group on every page shows its date.</item>
    /// </list>
    /// Per page: carried = cashAdvance − Σ(prior pages' grand totals); lessExpense = this page's
    /// grand total; balanceDue = carried − lessExpense. A report with no lines still yields one page.
    /// </summary>
    public static IReadOnlyList<R47Page> Paginate(IReadOnlyList<R47Row> rows, decimal cashAdvanceAmount, int linesPerPage = DefaultLinesPerPage)
    {
        ArgumentNullException.ThrowIfNull(rows);
        if (linesPerPage < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(linesPerPage));
        }

        var pages = new List<R47Page>();
        var current = new List<R47Row>();
        var used = 0;
        var priorExpenses = 0m;
        R47GroupHeaderRow? openGroup = null;

        void Flush()
        {
            var totals = current.OfType<R47DetailRow>().Aggregate(R47Buckets.Zero, (acc, r) => acc.Add(r.Buckets));
            var carried = cashAdvanceAmount - priorExpenses;
            var less = totals.GrandTotal;
            pages.Add(new R47Page(pages.Count + 1, current, totals, new R47Reconciliation(carried, less, carried - less), used));
            priorExpenses += less;
            current = [];
            used = 0;
        }

        for (var i = 0; i < rows.Count; i++)
        {
            switch (rows[i])
            {
                case R47GroupHeaderRow header:
                    {
                        openGroup = header;
                        var firstDetail = i + 1 < rows.Count && rows[i + 1] is R47DetailRow d ? d.Lines : 0;
                        if (used > 0 && used + header.Lines + firstDetail > linesPerPage)
                        {
                            Flush();
                        }

                        current.Add(current.Count == 0 ? header with { ShowDate = true } : header);
                        used += header.Lines;
                        break;
                    }

                case R47DetailRow detail:
                    {
                        if (used > 0 && used + detail.Lines > linesPerPage)
                        {
                            Flush();
                            if (openGroup is not null && openGroup.Lines + detail.Lines <= linesPerPage)
                            {
                                current.Add(openGroup with { ShowDate = true });
                                used += openGroup.Lines;
                            }
                        }

                        current.Add(detail);
                        used += detail.Lines;
                        break;
                    }
            }
        }

        Flush();
        return pages;
    }
}
