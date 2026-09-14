using System.Reflection;
using Xunit;

namespace ERFWeb.Tests.Architecture;

/// <summary>Guards the Clean Architecture dependency direction.</summary>
public class DependencyTests
{
    [Fact]
    public void Domain_DoesNotReference_OtherLayers()
    {
        var referenced = typeof(ERFWeb.Domain.ReportStatus).Assembly
            .GetReferencedAssemblies()
            .Select(a => a.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        Assert.DoesNotContain("ERFWeb.Application", referenced);
        Assert.DoesNotContain("ERFWeb.Infrastructure", referenced);
        Assert.DoesNotContain("ERFWeb.Api", referenced);
    }

    [Fact]
    public void Application_DoesNotReference_InfrastructureOrApi()
    {
        var referenced = typeof(ERFWeb.Application.Common.PagedRequest).Assembly
            .GetReferencedAssemblies()
            .Select(a => a.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        Assert.DoesNotContain("ERFWeb.Infrastructure", referenced);
        Assert.DoesNotContain("ERFWeb.Api", referenced);
    }
}
