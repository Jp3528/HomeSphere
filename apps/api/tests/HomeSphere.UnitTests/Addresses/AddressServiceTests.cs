using HomeSphere.Application.Common.Interfaces;
using HomeSphere.Application.Features.Addresses;
using HomeSphere.Application.Features.Addresses.DTOs;
using HomeSphere.Domain.Entities;
using HomeSphere.Domain.Exceptions;
using HomeSphere.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HomeSphere.UnitTests.Addresses;

public class AddressServiceTests
{
    private readonly HomeSphereDbContext _dbContext;
    private readonly AddressService _service;

    public AddressServiceTests()
    {
        var options = new DbContextOptionsBuilder<HomeSphereDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new HomeSphereDbContext(options);
        _service = new AddressService(_dbContext);
    }

    [Fact]
    public async Task GetAddressById_WhenAddressBelongsToAnotherUser_ShouldThrowForbiddenAccessException()
    {
        // Arrange
        var userA = Guid.NewGuid();
        var userB = Guid.NewGuid();

        var addressOfUserB = new Address
        {
            Id = Guid.NewGuid(),
            UserId = userB,
            RecipientName = "User B",
            Line1 = "Street 123",
            City = "Lima",
            PhoneNumber = "999888777"
        };

        _dbContext.Addresses.Add(addressOfUserB);
        await _dbContext.SaveChangesAsync();

        // Act & Assert (User A attempts to access User B's address -> IDOR attack)
        var exception = await Assert.ThrowsAsync<ForbiddenAccessException>(() =>
            _service.GetAddressByIdAsync(addressOfUserB.Id, userA));

        Assert.Contains("permission", exception.Message);
    }

    [Fact]
    public async Task GetUserAddresses_ShouldOnlyReturnAddressesOfRequestedUser()
    {
        // Arrange
        var userA = Guid.NewGuid();
        var userB = Guid.NewGuid();

        _dbContext.Addresses.AddRange(
            new Address { Id = Guid.NewGuid(), UserId = userA, RecipientName = "User A 1", Line1 = "Street 1", City = "Lima", PhoneNumber = "111" },
            new Address { Id = Guid.NewGuid(), UserId = userA, RecipientName = "User A 2", Line1 = "Street 2", City = "Lima", PhoneNumber = "222" },
            new Address { Id = Guid.NewGuid(), UserId = userB, RecipientName = "User B 1", Line1 = "Street 3", City = "Cusco", PhoneNumber = "333" }
        );
        await _dbContext.SaveChangesAsync();

        // Act
        var userAAddresses = await _service.GetUserAddressesAsync(userA);

        // Assert
        Assert.Equal(2, userAAddresses.Count);
        Assert.All(userAAddresses, a => Assert.Equal(userA, a.UserId));
    }

    [Fact]
    public async Task CreateAddress_FirstAddress_ShouldBeAutomaticallyDefault()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var request = new CreateAddressRequest(
            Label: "Casa",
            RecipientName: "Maria Garcia",
            PhoneNumber: "912345678",
            Line1: "Av. Principal 456",
            Line2: "Dpto 301",
            District: "Miraflores",
            City: "Lima",
            Region: "Lima",
            Reference: "Frente al parque",
            IsDefault: false
        );

        // Act
        var created = await _service.CreateAddressAsync(userId, request);

        // Assert
        Assert.True(created.IsDefault);
        var inDb = await _dbContext.Addresses.FindAsync(created.Id);
        Assert.NotNull(inDb);
        Assert.True(inDb.IsDefault);
    }

    [Fact]
    public async Task DeleteAddress_BelongingToAnotherUser_ShouldThrowForbiddenAccessException()
    {
        // Arrange
        var userA = Guid.NewGuid();
        var userB = Guid.NewGuid();

        var addressOfUserB = new Address
        {
            Id = Guid.NewGuid(),
            UserId = userB,
            RecipientName = "User B",
            Line1 = "Street 123",
            City = "Lima",
            PhoneNumber = "999888777"
        };

        _dbContext.Addresses.Add(addressOfUserB);
        await _dbContext.SaveChangesAsync();

        // Act & Assert
        await Assert.ThrowsAsync<ForbiddenAccessException>(() =>
            _service.DeleteAddressAsync(addressOfUserB.Id, userA));
    }
}
