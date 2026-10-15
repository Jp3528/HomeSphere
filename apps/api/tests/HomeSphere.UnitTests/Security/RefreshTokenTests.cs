using HomeSphere.Domain.Entities;

namespace HomeSphere.UnitTests.Security;

public class RefreshTokenTests
{
    [Fact]
    public void RefreshToken_WhenNewAndUnexpired_ShouldBeActive()
    {
        // Arrange
        var token = new RefreshToken
        {
            Token = "sample-token-abc",
            CreatedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(7)
        };

        // Assert
        Assert.True(token.IsActive);
        Assert.False(token.IsExpired);
        Assert.False(token.IsRevoked);
    }

    [Fact]
    public void RefreshToken_WhenExpired_ShouldNotBeActive()
    {
        // Arrange
        var token = new RefreshToken
        {
            Token = "expired-token-xyz",
            CreatedAtUtc = DateTime.UtcNow.AddDays(-10),
            ExpiresAtUtc = DateTime.UtcNow.AddDays(-3)
        };

        // Assert
        Assert.False(token.IsActive);
        Assert.True(token.IsExpired);
    }

    [Fact]
    public void RefreshToken_WhenRevoked_ShouldNotBeActiveAndStoreReplacement()
    {
        // Arrange
        var token = new RefreshToken
        {
            Token = "token-to-revoke",
            CreatedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddDays(7)
        };

        // Act
        token.Revoke(replacedByToken: "new-token-123");

        // Assert
        Assert.False(token.IsActive);
        Assert.True(token.IsRevoked);
        Assert.NotNull(token.RevokedAtUtc);
        Assert.Equal("new-token-123", token.ReplacedByToken);
    }
}
