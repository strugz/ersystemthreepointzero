using ERFWeb.Domain;
using ERFWeb.Domain.Reporting;
using Xunit;

namespace ERFWeb.Tests.Domain;

public class R47BucketingTests
{
    private static R47Line Line(ExpenseType type, ExpenseCategory cat, decimal amount, decimal vat = 0,
        string? location = "MDMPI", string? instrument = null, string? serial = null, string? service = null) =>
        new(new DateOnly(2026, 8, 17), "X", null, type, cat, amount, vat, amount + vat, location, instrument, serial, service, null, 1);

    [Theory]
    [InlineData(ExpenseCategory.Meals)]
    [InlineData(ExpenseCategory.Transportation)]
    [InlineData(ExpenseCategory.Toll)]
    [InlineData(ExpenseCategory.Parking)]
    [InlineData(ExpenseCategory.Others)]
    public void LocalLines_LandInMatchingLocalTravelColumn(ExpenseCategory category)
    {
        var b = R47Bucketing.Bucket(Line(ExpenseType.Local, category, 84m));

        var expected = category switch
        {
            ExpenseCategory.Meals => b.Meals,
            ExpenseCategory.Transportation => b.Transportation,
            ExpenseCategory.Toll => b.Toll,
            ExpenseCategory.Parking => b.Parking,
            _ => b.Others,
        };

        Assert.Equal(84m, expected);
        Assert.Equal(84m, b.LocalTotal);
        Assert.Equal(0m, b.Representation);
        Assert.Equal(0m, b.Oth);
        Assert.Equal(84m, b.GrandTotal);
    }

    [Fact]
    public void Representation_GoesToRepresentationColumn_NotLocalTravel()
    {
        var b = R47Bucketing.Bucket(Line(ExpenseType.Local, ExpenseCategory.Representation, 500m));
        Assert.Equal(500m, b.Representation);
        Assert.Equal(0m, b.LocalTotal);
    }

    [Fact]
    public void ForeignNonRepresentation_GoesToOthColumn()
    {
        var b = R47Bucketing.Bucket(Line(ExpenseType.Foreign, ExpenseCategory.Meals, 120m));
        Assert.Equal(120m, b.Oth);
        Assert.Equal(0m, b.LocalTotal);
    }

    [Fact]
    public void VatAndGrandTotal_PassThrough()
    {
        var b = R47Bucketing.Bucket(Line(ExpenseType.Local, ExpenseCategory.Toll, 100m, vat: 12m));
        Assert.Equal(12m, b.Vat);
        Assert.Equal(112m, b.GrandTotal);
    }

    [Fact]
    public void LocationText_UsesLocationOnly_WhenInstrumentDetailsMissingOrNA()
    {
        Assert.Equal("MDMPI", R47Bucketing.LocationText(Line(ExpenseType.Local, ExpenseCategory.Meals, 1m)));
        Assert.Equal("CALMC", R47Bucketing.LocationText(Line(ExpenseType.Local, ExpenseCategory.Meals, 1m, location: "CALMC", instrument: "N/A", serial: "N/A", service: "N/A")));
    }

    [Fact]
    public void LocationText_AppendsInstrumentAndSerial_WhenPresent()
    {
        var text = R47Bucketing.LocationText(Line(ExpenseType.Local, ExpenseCategory.Parking, 1m,
            location: "SLMCGC", instrument: "AU480", serial: "2022054754", service: "SR-1"));
        Assert.Equal("SLMCGC - AU480 - 2022054754", text);
    }
}
