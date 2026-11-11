using ERFWeb.Domain.Reporting;
using Xunit;

namespace ERFWeb.Tests.Domain;

public class DailyMealClaimsBuilderTests
{
    private static readonly DateOnly Mon = new(2026, 11, 9);
    private static readonly DateOnly Tue = new(2026, 11, 10);

    private static MealClaimLine Line(long employee, string name, DateOnly date, decimal amount, string? reference, params string?[] meals) =>
        new(employee, name, "Sales", reference, date, meals, amount);

    [Theory]
    [InlineData("Breakfast", MealKind.Breakfast)]
    [InlineData("lunch", MealKind.Lunch)]
    [InlineData("DINNER", MealKind.Dinner)]
    [InlineData("OT Meal", MealKind.OtMeal)]
    [InlineData("OTMeal", MealKind.OtMeal)]
    [InlineData("Snack", MealKind.Other)]
    [InlineData(null, MealKind.Other)]
    public void Kind_NormalisesMealTypes(string? type, MealKind kind) => Assert.Equal(kind, DailyMealClaimsBuilder.Kind(type));

    [Fact]
    public void GroupsByDayThenEmployee()
    {
        var claims = DailyMealClaimsBuilder.Build(
        [
            Line(2, "Zed", Mon, 150m, "ERF-2026-000002", "Lunch"),
            Line(1, "Ana", Tue, 100m, "ERF-2026-000001", "Breakfast"),
            Line(1, "Ana", Mon, 150m, "ERF-2026-000001", "Lunch", "Dinner"),
            Line(1, "Ana", Mon, 80m, "ERF-2026-000003", "OT Meal"),
        ]);

        Assert.Equal([Mon, Tue], claims.Days.Select(d => d.Date));
        var monday = claims.Days[0];
        Assert.Equal(["Ana", "Zed"], monday.Employees.Select(e => e.EmployeeName));
        Assert.Equal(2, monday.EmployeeCount);
        Assert.Equal(2, monday.Lunch);
        Assert.Equal(1, monday.Dinner);
        Assert.Equal(1, monday.OtMeal);
        Assert.Equal(380m, monday.Amount);

        var ana = monday.Employees[0];
        Assert.True(ana.Lunch && ana.Dinner && ana.OtMeal);
        Assert.False(ana.Breakfast || ana.Other);
        Assert.Equal(2, ana.LineCount);
        Assert.Equal(["ERF-2026-000001", "ERF-2026-000003"], ana.ReferenceNos);

        Assert.Equal(3, claims.EmployeeDays);
        Assert.Equal(480m, claims.Total);
    }

    [Fact]
    public void LineWithoutMealItems_CountsAsOther()
    {
        var claims = DailyMealClaimsBuilder.Build([Line(1, "Ana", Mon, 200m, null)]);

        var row = claims.Days.Single().Employees.Single();
        Assert.True(row.Other);
        Assert.Empty(row.ReferenceNos);
        Assert.Equal(1, claims.Days[0].Other);
    }

    [Fact]
    public void NoLines_NoDays()
    {
        var claims = DailyMealClaimsBuilder.Build([]);

        Assert.Empty(claims.Days);
        Assert.Equal(0m, claims.Total);
    }
}
