using HomeSphere.Infrastructure.Security;

namespace HomeSphere.UnitTests.Security;

public class PasswordHasherTests
{
    private readonly PasswordHasher _hasher = new();

    [Fact]
    public void HashPassword_ShouldGenerateNonEmptyFormattedHash()
    {
        // Act
        var hash = _hasher.HashPassword("SecretP@ssword123");

        // Assert
        Assert.False(string.IsNullOrWhiteSpace(hash));
        var parts = hash.Split('.');
        Assert.Equal(3, parts.Length); // iterations.salt.hash
        Assert.Equal("100000", parts[0]);
    }

    [Fact]
    public void VerifyPassword_WithCorrectPassword_ShouldReturnTrue()
    {
        // Arrange
        const string password = "MySecurePassword2026!";
        var hash = _hasher.HashPassword(password);

        // Act
        var isValid = _hasher.VerifyPassword(password, hash);

        // Assert
        Assert.True(isValid);
    }

    [Fact]
    public void VerifyPassword_WithWrongPassword_ShouldReturnFalse()
    {
        // Arrange
        var hash = _hasher.HashPassword("CorrectPassword");

        // Act
        var isValid = _hasher.VerifyPassword("WrongPassword", hash);

        // Assert
        Assert.False(isValid);
    }

    [Fact]
    public void HashPassword_SamePasswordMultipleTimes_ShouldUseDifferentSalts()
    {
        // Arrange
        const string password = "SamePassword123";

        // Act
        var hash1 = _hasher.HashPassword(password);
        var hash2 = _hasher.HashPassword(password);

        // Assert
        Assert.NotEqual(hash1, hash2);
        Assert.True(_hasher.VerifyPassword(password, hash1));
        Assert.True(_hasher.VerifyPassword(password, hash2));
    }
}
