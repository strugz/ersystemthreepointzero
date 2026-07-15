using ERSystem.Web.Application.Common;
using ERSystem.Web.Domain.Common;
using ERSystem.Web.Infrastructure.Persistence;

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
}
