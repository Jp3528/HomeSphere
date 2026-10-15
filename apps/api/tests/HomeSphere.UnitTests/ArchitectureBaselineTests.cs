namespace HomeSphere.UnitTests;

public class ArchitectureBaselineTests
{
    [Fact]
    public void Architecture_ShouldReferenceDomainAndApplication()
    {
        // Act
        var domainAssembly = typeof(HomeSphere.Domain.Class1).Assembly;
        var applicationAssembly = typeof(HomeSphere.Application.Class1).Assembly;

        // Assert
        Assert.NotNull(domainAssembly);
        Assert.NotNull(applicationAssembly);
    }
}
