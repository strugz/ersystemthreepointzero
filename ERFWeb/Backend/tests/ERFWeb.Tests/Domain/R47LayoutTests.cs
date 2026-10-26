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
            r => { var h = Assert.IsType<R47GroupHeaderRow>(r); Assert.True(h.ShowDate); Assert.Equal("SLMCGC Work w/: JSO", h.Text); },
            r => Assert.IsType<R47DetailRow>(r),
            r => Assert.IsType<R47DetailRow>(r),
            r => { var h = Assert.IsType<R47GroupHeaderRow>(r); Assert.False(h.ShowDate); Assert.Equal("MMC Work w/: RLN/JSO", h.Text); },
            r => Assert.IsType<R47DetailRow>(r),
            r => { var h = Assert.IsType<R47GroupHeaderRow>(r); Assert.True(h.ShowDate); Assert.Equal("MDMPI", h.Text); },
            r => Assert.IsType<R47DetailRow>(r));
    }

    [Theory]
    [InlineData("Jay C Abaoag", "JCA")]
    [InlineData("Jay C Abaoag, Romewel A Magdalita", "JCA/RAM")]
    [InlineData("Jay C Abaoag and Ma. Cristina Dela Cruz", "JCA/MCDC")]
    [InlineData("JSO", "JSO")]
    [InlineData("RLN/JSO", "RLN/JSO")]
    [InlineData("  ", "")]
    [InlineData(null, "")]
    public void WorkWithInitials_ShortensFullNamesToInitials(string? workWith, string expected) =>
        Assert.Equal(expected, R47Layout.WorkWithInitials(workWith));

    [Fact]
    public void BuildRows_PrintsWorkWithAsInitials()
    {
        var rows = R47Layout.BuildRows([Line(7, "US VETERANS EMBASSY OUT PATIENT CLINIC", "Jay C Abaoag", "Meals", ExpenseCategory.Meals, 200m, 1)]);

        var header = Assert.IsType<R47GroupHeaderRow>(rows[0]);
        Assert.Equal("US VETERANS EMBASSY OUT PATIENT CLINIC Work w/: JCA", header.Text);
    }

    // Wrap behaviour observed on the reference printout (MARER08OCT2026).
    [Theory]
    [InlineData("MDMPI", 1)]
    [InlineData("US VETERANS EMBASSY OUT PATIENT CLINIC", 2)]
    [InlineData("MCU-FDT Medical Foundation - GEM 3500 - (SN: 70060897) - 70060897", 2)]
    [InlineData("Alabang Medical Clinic - Almanza - DXH 520 - (SN: BF020105) - BF020105", 2)]
    public void LineCount_GroupHeaders_MatchReferencePrintout(string header, int expected) =>
        Assert.Equal(expected, R47TextMetrics.LineCount(header, R47TextMetrics.ParticularsCapacity));

    [Theory]
    [InlineData("Toll", 1)]
    [InlineData("SUCAT - NICHOLS (TOLL)", 1)]
    [InlineData("SUCAT - NICHOLS (TOLL) - MEDICARD INSTRUMENTS INVENTORY", 2)]
    [InlineData("SUCAT - NICHOLS (TOLL) - MEDICARD INSTRUMENTS INVENTORY - SLMCGC - AU480 - 2022054754 WORK WITH JSO", 3)]
    public void LineCount_Particulars_FollowTheParticularsTextWidth(string particulars, int expected) =>
        Assert.Equal(expected, R47TextMetrics.LineCount(particulars, R47TextMetrics.ParticularsCapacity));

    [Theory]
    [InlineData("00000247967", 1)]
    [InlineData("01000291926", 1)]
    [InlineData("5001000291926", 1)]
    [InlineData("SI-0001234567-89", 2)]   // wider than the column: breaks mid-token
    public void LineCount_InvoiceNumbers_FollowTheInvoiceColumnWidth(string invoice, int expected) =>
        Assert.Equal(expected, R47TextMetrics.LineCount(invoice, R47TextMetrics.InvoiceCapacity));

    [Theory]
    [InlineData(1, 13.3f)]
    [InlineData(2, 18.4f)]
    [InlineData(3, 26.6f)]
    public void RowHeight_MatchesReferencePrintout(int lines, float expected) =>
        Assert.Equal(expected, R47Layout.RowHeight(lines), precision: 2);

    /// <summary>Detail-area height holding exactly <paramref name="rows"/> single-line rows.</summary>
    private static float SingleRows(int rows) => rows * R47Layout.SingleRowHeight;

    [Fact]
    public void Paginate_RunningReconciliation_MatchesReferencePrintout()
    {
        // MUMER16AUG2026: page totals 1,601 / 912 / 1,050 / 904 / 250 with no cash advance.
        var rows = new List<R47Row> { Detail(1601m), Detail(912m), Detail(1050m), Detail(904m), Detail(250m) };

        var pages = R47Layout.Paginate(rows, cashAdvanceAmount: 0m, SingleRows(1));

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

        var pages = R47Layout.Paginate(rows, cashAdvanceAmount: 5000m, SingleRows(1));

        Assert.Equal((5000m, 3000m, 2000m), Rec(pages[0]));
        Assert.Equal((2000m, 1000m, 1000m), Rec(pages[1]));
    }

    [Fact]
    public void Paginate_FillsByHeight_AndNeverOrphansAGroupHeader()
    {
        // 14 single-line rows, then a group whose header wraps to 2 lines: 14 + header + first
        // detail is taller than 16 single rows, so the whole group moves to page 2.
        var rows = new List<R47Row>();
        for (var i = 0; i < 14; i++) rows.Add(i % 4 == 0 ? Header(17 + i / 4) : Detail(84m));
        rows.Add(Header(20, lines: 2));
        rows.Add(Detail(84m));
        rows.Add(Detail(65m));

        var pages = R47Layout.Paginate(rows, 0m, SingleRows(16));

        Assert.Equal(2, pages.Count);
        Assert.Equal(14, pages[0].Rows.Count);
        Assert.Equal(SingleRows(14), pages[0].UsedHeight, precision: 2);
        Assert.IsType<R47GroupHeaderRow>(pages[1].Rows[0]);
        Assert.Equal(R47Layout.RowHeight(2) + SingleRows(2), pages[1].UsedHeight, precision: 2);
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

        var pages = R47Layout.Paginate(rows, 0m, SingleRows(6));

        Assert.Equal(2, pages.Count);
        // page 1: header + 3 details + header + 1 detail = 6 single rows
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

        var pages = R47Layout.Paginate(rows, 0m, SingleRows(5));

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
        Assert.Equal(0f, pages[0].UsedHeight);
    }

    private static (decimal Carried, decimal Less, decimal Balance) Rec(R47Page p) =>
        (p.Reconciliation.CarriedBalance, p.Reconciliation.LessExpense, p.Reconciliation.BalanceDue);
}
