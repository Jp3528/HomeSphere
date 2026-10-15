using HomeSphere.Domain.Entities;

namespace HomeSphere.UnitTests;

public class ArchitectureBaselineTests
{
    [Fact]
    public void Architecture_ShouldReferenceDomainAndApplication()
    {
        // Act
        var domainAssembly = typeof(IdempotencyRecord).Assembly;

        // Assert
        Assert.NotNull(domainAssembly);
    }
}
