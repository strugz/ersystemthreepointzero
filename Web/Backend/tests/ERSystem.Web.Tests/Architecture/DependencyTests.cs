using ERSystem.Web.Application.Common;
using ERSystem.Web.Domain.Common;
using ERSystem.Web.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ERSystem.Web.Tests.Architecture;

public sealed class DependencyTests
{
    [Fact]
    public void Domain_does_not_reference_application_infrastructure_or_api()
    {
        var references = typeof(ApprovalSequence).Assembly.GetReferencedAssemblies().Select(x => x.Name).ToArray();
        Assert.DoesNotContain("ERSystem.Web.Application", references);
        Assert.DoesNotContain("ERSystem.Web.Infrastructure", references);
        Assert.DoesNotContain("ERSystem.Web.Api", references);
    }

    [Fact]
    public void Application_does_not_reference_infrastructure_or_api()
    {
        var references = typeof(PagedRequest).Assembly.GetReferencedAssemblies().Select(x => x.Name).ToArray();
        Assert.DoesNotContain("ERSystem.Web.Infrastructure", references);
        Assert.DoesNotContain("ERSystem.Web.Api", references);
    }

    [Fact]
    public void Infrastructure_contains_the_database_contexts()
    {
        Assert.Equal("ERSystem.Web.Infrastructure", typeof(LegacyErDbContext).Assembly.GetName().Name);
        Assert.Equal("ERSystem.Web.Infrastructure", typeof(WebWorkflowDbContext).Assembly.GetName().Name);
    }

    [Fact]
    public void Legacy_context_maps_the_dedicated_approval_transaction_table()
    {
        var options = new DbContextOptionsBuilder<LegacyErDbContext>()
            .UseSqlServer("Server=(local);Database=ERSystemModelOnly;Trusted_Connection=True;TrustServerCertificate=True")
            .Options;
        using var context = new LegacyErDbContext(options);
        var entity = context.Model.FindEntityType(typeof(ReportApprovalTransactionEntity));

        Assert.NotNull(entity);
        Assert.Equal("tbReportApprovalTransaction", entity.GetTableName());
        Assert.True(entity.FindProperty(nameof(ReportApprovalTransactionEntity.RowVersion))!.IsConcurrencyToken);
    }
}
