using ERFWeb.Domain.Expenses;
using Xunit;

namespace ERFWeb.Tests.Domain;

public class VehicleParticularsTests
{
    [Theory]
    [InlineData(" abc  1234 ", "ABC 1234")]
    [InlineData("NBC1234", "NBC1234")]
    [InlineData("  ", null)]
    public void NormalizePlate_UpperCasesAndSingleSpaces(string plate, string? expected) =>
        Assert.Equal(expected, VehicleParticulars.NormalizePlate(plate));

    [Theory]
    [InlineData(" Client visit - Toyota Vios - ABC 1234", "Client visit")]
    [InlineData("A - B - Toyota Vios - ABC 1234", "A - B")]
    [InlineData("Client visit", "Client visit")]
    [InlineData(null, "")]
    public void Reason_IsEverythingBeforeModelAndPlate(string? particulars, string expected) =>
        Assert.Equal(expected, VehicleParticulars.Reason(particulars));

    [Fact]
    public void Vehicle_BothOrNeither()
    {
        Assert.Empty(VehicleParticulars.VehicleProblems("Toyota Vios", "ABC 1234"));
        Assert.Empty(VehicleParticulars.VehicleProblems(null, " "));
        Assert.Equal(["plateNo"], VehicleParticulars.VehicleProblems("Toyota Vios", null).Keys);
        Assert.Equal(["model"], VehicleParticulars.VehicleProblems("", "ABC 1234").Keys);
    }

    [Fact]
    public void Vehicle_RefusesTheSeparatorAndLongValues()
    {
        Assert.Equal(["model"], VehicleParticulars.VehicleProblems("Toyota - Vios", "ABC 1234").Keys);
        Assert.Equal(["plateNo"], VehicleParticulars.VehicleProblems("Toyota Vios", new string('A', VehicleParticulars.MaxPlateLength + 1)).Keys);
        Assert.Equal(["model"], VehicleParticulars.VehicleProblems(new string('A', VehicleParticulars.MaxModelLength + 1), "ABC 1234").Keys);
    }
}
