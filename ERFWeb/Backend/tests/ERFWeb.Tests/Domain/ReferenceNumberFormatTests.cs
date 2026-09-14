using ERFWeb.Domain.Expenses;
using Xunit;

namespace ERFWeb.Tests.Domain;

public class ReferenceNumberFormatTests
{
    [Theory]
    [InlineData("ERF", 2026, 142, 6, "ERF-2026-000142")]
    [InlineData("erf", 2026, 1, 6, "ERF-2026-000001")]
    [InlineData(" ERF ", 2027, 1234567, 6, "ERF-2027-1234567")]
    [InlineData("MDMPI", 2026, 7, 4, "MDMPI-2026-0007")]
    public void Format_ProducesPrefixYearPaddedSequence(string prefix, int year, int seq, int digits, string expected) =>
        Assert.Equal(expected, ReferenceNumberFormat.Format(prefix, year, seq, digits));

    [Fact]
    public void SeriesKey_IsPrefixAndYear() =>
        Assert.Equal("ERF-2026", ReferenceNumberFormat.SeriesKey("erf", 2026));

    [Fact]
    public void Format_RejectsSequenceBelowOne() =>
        Assert.Throws<ArgumentOutOfRangeException>(() => ReferenceNumberFormat.Format("ERF", 2026, 0, 6));

    [Theory]
    [InlineData(2)]
    [InlineData(10)]
    public void Format_RejectsDigitsOutsideRange(int digits) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => ReferenceNumberFormat.Format("ERF", 2026, 1, digits));

    [Fact]
    public void Format_RejectsBlankPrefix() =>
        Assert.Throws<ArgumentException>(() => ReferenceNumberFormat.Format("  ", 2026, 1, 6));

    [Fact]
    public void Format_SortsAsText_WithinAYear()
    {
        var a = ReferenceNumberFormat.Format("ERF", 2026, 9, 6);
        var b = ReferenceNumberFormat.Format("ERF", 2026, 10, 6);
        Assert.True(string.CompareOrdinal(a, b) < 0);
    }
}
