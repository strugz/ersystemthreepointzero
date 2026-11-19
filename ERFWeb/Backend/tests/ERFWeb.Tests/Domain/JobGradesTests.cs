using ERFWeb.Domain.Administration;
using Xunit;

namespace ERFWeb.Tests.Domain;

public class JobGradesTests
{
    [Theory]
    [InlineData("Staff", "Staff")]
    [InlineData("specialist", "Specialist")]
    [InlineData("  SUPERVISOR ", "Supervisor")]
    public void Normalize_ReturnsTheStoredCode(string value, string expected)
    {
        Assert.Equal(expected, JobGrades.Normalize(value));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("Manager")]
    [InlineData("Specialists/Engineers")]
    public void Normalize_RefusesBlankAndUnknown(string? value)
    {
        Assert.Null(JobGrades.Normalize(value));
    }
}
