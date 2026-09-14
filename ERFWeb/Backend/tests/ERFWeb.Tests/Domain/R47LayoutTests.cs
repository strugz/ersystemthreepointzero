using ERFWeb.Domain;
using ERFWeb.Domain.Reporting;
using Xunit;

namespace ERFWeb.Tests.Domain;

public class R47LayoutTests
{
    private static R47Line Line(int day, string location, string? workWith, string particulars, ExpenseCategory cat, decimal amount, int sort, string? invoice = null) =>
        new(new DateOnly(2026, 8, day), particulars, invoice, ExpenseType.Local, cat, amount, 0, amount, location, null, null, null, workWith, sort);

    private static R47DetailRow Detail(decimal amount, int lines = 1) =>
        new("x", null, new R47Buckets(0, 0, amount, 0, 0, 0, 0, 0, amount), lines);

    private static R47GroupHeaderRow Header(int day, int lines = 1) =>
        new(new DateOnly(2026, 8, day), true, "HDR", lines);

    [Fact]
    public void BuildRows_EmitsGroupHeaderPerDateLocation_DateShownOnce()
    {
        var lines = new[]
        {
            Line(20, "SLMCGC", "JSO", "SUCAT - NICHOLS (TOLL)", ExpenseCategory.Toll, 84m, 1),
            Line(20, "SLMCGC", "JSO", "PARKING", ExpenseCategory.Parking, 65m, 2),
            Line(20, "MMC", "RLN/JSO", "PARKING", ExpenseCategory.Parking, 210m, 3),
            Line(21, "MDMPI", null, "MEAL", ExpenseCategory.Meals, 250m, 4),
        };

        var rows = R47Layout.BuildRows(lines);

        Assert.Collection(rows,
            r => { var h = Assert.IsType<R47GroupHeaderRow>(r); Assert.True(h.ShowDate); Assert.Equal("SLMCGC JSO", h.Text); },
            r => Assert.IsType<R47DetailRow>(r),
            r => Assert.IsType<R47DetailRow>(r),
            r => { var h = Assert.IsType<R47GroupHeaderRow>(r); Assert.False(h.ShowDate); Assert.Equal("MMC RLN/JSO", h.Text); },
            r => Assert.IsType<R47DetailRow>(r),
            r => { var h = Assert.IsType<R47GroupHeaderRow>(r); Assert.True(h.ShowDate); Assert.Equal("MDMPI", h.Text); },
            r => Assert.IsType<R47DetailRow>(r));
    }

    // Wrap behaviour observed on the reference printout (MUMER16AUG2026, pages 2–5).
    [Theory]
    [InlineData("MDMPI Work With NONE", 1)]
    [InlineData("SLMCGC - AU480 - 2022054754 Work With JSO", 2)]
    [InlineData("MMC - DXI 800 - 603220 Work With RLN/JSO", 2)]
    [InlineData("AMCALMANZA - DXH 520 - BF020105 Work With NONE", 2)]
    [InlineData("LPCMC - ACCESS 2E - 571487 Work With NONE", 2)]
    public void LineCount_GroupHeaders_MatchReferencePrintout(string header, int expected) =>
        Assert.Equal(expected, R47TextMetrics.LineCount(header, R47TextMetrics.ParticularsCapacity, bold: true));

    [Theory]
    [InlineData("SUCAT - NICHOLS (TOLL)", 1)]
    [InlineData("SUCAT - NICHOLS (TOLL) - MEDICARD INSTRUMENTS INVENTORY", 2)]
    [InlineData("MEAL - MEDICARD INSTRUMENTS INVENTORY", 2)]
    [InlineData("ALABANG - NICHOLS (TOLL) - MEDICARD INSTRUMENTS INVENTORY", 3)]
    [InlineData("NICHOLS - FESTIVAL (TOLL) - MEDICARD INSTRUMENTS INVENTORY", 3)]
    public void LineCount_Particulars_MatchReferencePrintout(string particulars, int expected) =>
        Assert.Equal(expected, R47TextMetrics.LineCount(particulars, R47TextMetrics.ParticularsCapacity));

    [Theory]
    [InlineData("2667789054", 1)]
    [InlineData("35000123581-00", 1)]
    [InlineData("9BEUOTWB-0007", 2)]   // breaks mid-token on the JCAER printout
    public void LineCount_InvoiceNumbers_MatchReferencePrintout(string invoice, int expected) =>
        Assert.Equal(expected, R47TextMetrics.LineCount(invoice, R47TextMetrics.InvoiceCapacity));

    [Fact]
    public void Paginate_RunningReconciliation_MatchesReferencePrintout()
    {
        // MUMER16AUG2026: page totals 1,601 / 912 / 1,050 / 904 / 250 with no cash advance.
        var rows = new List<R47Row> { Detail(1601m), Detail(912m), Detail(1050m), Detail(904m), Detail(250m) };

        var pages = R47Layout.Paginate(rows, cashAdvanceAmount: 0m, linesPerPage: 1);

        Assert.Equal(5, pages.Count);
        Assert.Equal((0m, 1601m, -1601m), Rec(pages[0]));
        Assert.Equal((-1601m, 912m, -2513m), Rec(pages[1]));
        Assert.Equal((-2513m, 1050m, -3563m), Rec(pages[2]));
        Assert.Equal((-3563m, 904m, -4467m), Rec(pages[3]));
        Assert.Equal((-4467m, 250m, -4717m), Rec(pages[4]));
    }

    [Fact]
    public void Paginate_WithCashAdvance_CarriesAdvanceMinusPriorExpenses()
    {
        var rows = new List<R47Row> { Detail(3000m), Detail(1000m) };

        var pages = R47Layout.Paginate(rows, cashAdvanceAmount: 5000m, linesPerPage: 1);

        Assert.Equal((5000m, 3000m, 2000m), Rec(pages[0]));
        Assert.Equal((2000m, 1000m, 1000m), Rec(pages[1]));
    }

    [Fact]
    public void Paginate_FillsByLines_AndNeverOrphansAGroupHeader()
    {
        // Reference page 1: 14 single-line rows, then the 20-Aug group whose header wraps to 2 lines.
        // 14 + 2 + 1 > 16, so the whole group moves to page 2 and page 1 keeps 2 blank lines.
        var rows = new List<R47Row>();
        for (var i = 0; i < 14; i++) rows.Add(i % 4 == 0 ? Header(17 + i / 4) : Detail(84m));
        rows.Add(Header(20, lines: 2));
        rows.Add(Detail(84m));
        rows.Add(Detail(65m));

        var pages = R47Layout.Paginate(rows, 0m, linesPerPage: 16);

        Assert.Equal(2, pages.Count);
        Assert.Equal(14, pages[0].Rows.Count);
        Assert.Equal(14, pages[0].UsedLines);
        Assert.IsType<R47GroupHeaderRow>(pages[1].Rows[0]);
        Assert.Equal(4, pages[1].UsedLines);
    }

    [Fact]
    public void Paginate_SplitGroup_RepeatsHeaderWithDateOnContinuationPage()
    {
        // Reference pages 2→3: the 24-Aug MDMPI group starts at the bottom of page 2 and its header
        // is printed again (with the date) at the top of page 3.
        var rows = new List<R47Row>
        {
            new R47GroupHeaderRow(new DateOnly(2026, 8, 20), true, "SLMCGC Work With JSO", 1),
            Detail(84m), Detail(65m), Detail(210m),
            new R47GroupHeaderRow(new DateOnly(2026, 8, 24), true, "MDMPI Work With NONE", 1),
            Detail(84m), Detail(84m), Detail(250m),
        };

        var pages = R47Layout.Paginate(rows, 0m, linesPerPage: 6);

        Assert.Equal(2, pages.Count);
        // page 1: header + 3 details + header + 1 detail = 6 lines
        Assert.Equal(6, pages[0].Rows.Count);
        // page 2 opens with the repeated 24-Aug header, date shown
        var cont = Assert.IsType<R47GroupHeaderRow>(pages[1].Rows[0]);
        Assert.Equal("MDMPI Work With NONE", cont.Text);
        Assert.True(cont.ShowDate);
        Assert.Equal(new DateOnly(2026, 8, 24), cont.Date);
        Assert.Equal(2, pages[1].Rows.OfType<R47DetailRow>().Count());
        // totals follow the details actually printed on each page
        Assert.Equal(84m + 65m + 210m + 84m, pages[0].PageTotals.GrandTotal);
        Assert.Equal(84m + 250m, pages[1].PageTotals.GrandTotal);
    }

    [Fact]
    public void Paginate_FirstGroupOnEveryPage_ShowsItsDate()
    {
        var rows = new List<R47Row>
        {
            new R47GroupHeaderRow(new DateOnly(2026, 8, 26), true, "MDMPI Work With NONE", 1), Detail(84m),
            new R47GroupHeaderRow(new DateOnly(2026, 8, 26), false, "MEDICAC Work With RLN", 1), Detail(124m), Detail(90m),
            new R47GroupHeaderRow(new DateOnly(2026, 8, 26), false, "MEDLCTR Work With RLN", 1), Detail(118m),
        };

        var pages = R47Layout.Paginate(rows, 0m, linesPerPage: 5);

        Assert.Equal(2, pages.Count);
        var second = Assert.IsType<R47GroupHeaderRow>(pages[1].Rows[0]);
        Assert.Equal("MEDLCTR Work With RLN", second.Text);
        Assert.True(second.ShowDate);
    }

    [Fact]
    public void Paginate_EmptyReport_StillYieldsOnePage()
    {
        var pages = R47Layout.Paginate([], 0m);
        Assert.Single(pages);
        Assert.Empty(pages[0].Rows);
        Assert.Equal(0m, pages[0].PageTotals.GrandTotal);
        Assert.Equal(0, pages[0].UsedLines);
    }

    private static (decimal Carried, decimal Less, decimal Balance) Rec(R47Page p) =>
        (p.Reconciliation.CarriedBalance, p.Reconciliation.LessExpense, p.Reconciliation.BalanceDue);
}
