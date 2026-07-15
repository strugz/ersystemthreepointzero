using ERSystem.Web.Application.Features.ManagerApprovals;
using ERSystem.Web.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace ERSystem.Web.Tests.Unit;

public sealed class LegacyExpenseMappingTests
{
    [Theory]
    [InlineData(nameof(ExpenseDetailEntity.PerDiem), "ExpensePerdiem")]
    [InlineData(nameof(ExpenseDetailEntity.InvoiceNumber), "ExpenseInvoice")]
    [InlineData(nameof(ExpenseDetailEntity.Multiplier), "ExpenseMultiplier")]
    [InlineData(nameof(ExpenseDetailEntity.ExpenseType), "ExpenseType")]
    [InlineData(nameof(ExpenseDetailEntity.VatAmount), "VatAmount")]
    [InlineData(nameof(ExpenseDetailEntity.Status), "ExpenseStatus")]
    [InlineData(nameof(ExpenseDetailEntity.WorkWith), "WorkWith")]
    [InlineData(nameof(ExpenseDetailEntity.ServiceNumber), "ServiceNumber")]
    [InlineData(nameof(ExpenseDetailEntity.Instrument), "Instrument")]
    [InlineData(nameof(ExpenseDetailEntity.SerialNumber), "SerialNumber")]
    [InlineData(nameof(ExpenseDetailEntity.MinusDays), "MDays")]
    [InlineData(nameof(ExpenseDetailEntity.TotalDays), "TotDays")]
    [InlineData(nameof(ExpenseDetailEntity.Computation), "Computation")]
    public void Maps_manager_review_fields_to_the_legacy_expense_table(string propertyName, string columnName)
    {
        using var context = CreateContext();
        var entityType = context.Model.FindEntityType(typeof(ExpenseDetailEntity));
        var property = entityType?.FindProperty(propertyName);
        var table = StoreObjectIdentifier.Table("tbExpenseDetails", null);

        Assert.NotNull(property);
        Assert.Equal(columnName, property.GetColumnName(table));
    }

    [Fact]
    public void Manager_expense_contract_keeps_vat_nullable_and_exposes_business_fields()
    {
        var properties = typeof(ExpenseLineDto).GetProperties().ToDictionary(property => property.Name);

        Assert.Equal(typeof(decimal?), properties[nameof(ExpenseLineDto.VatAmount)].PropertyType);
        Assert.Equal(typeof(bool), properties[nameof(ExpenseLineDto.IsPerDiem)].PropertyType);
        Assert.Contains(nameof(ExpenseLineDto.InvoiceNumber), properties.Keys);
        Assert.Contains(nameof(ExpenseLineDto.Computation), properties.Keys);
    }

    private static LegacyErDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<LegacyErDbContext>()
            .UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=ERSystemModelInspection;Trusted_Connection=True")
            .Options;
        return new LegacyErDbContext(options);
    }
}
