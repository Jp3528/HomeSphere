using HomeSphere.Application.Common.Interfaces;
using HomeSphere.Application.Features.Addresses.DTOs;
using HomeSphere.Domain.Entities;
using HomeSphere.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace HomeSphere.Application.Features.Addresses;

public interface IAddressService
{
    Task<IReadOnlyList<AddressDto>> GetUserAddressesAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<AddressDto> GetAddressByIdAsync(Guid addressId, Guid userId, CancellationToken cancellationToken = default);
    Task<AddressDto> CreateAddressAsync(Guid userId, CreateAddressRequest request, CancellationToken cancellationToken = default);
    Task<AddressDto> UpdateAddressAsync(Guid addressId, Guid userId, UpdateAddressRequest request, CancellationToken cancellationToken = default);
    Task DeleteAddressAsync(Guid addressId, Guid userId, CancellationToken cancellationToken = default);
    Task SetDefaultAddressAsync(Guid addressId, Guid userId, CancellationToken cancellationToken = default);
}

public class AddressService : IAddressService
{
    private readonly IApplicationDbContext _context;

    public AddressService(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<AddressDto>> GetUserAddressesAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var addresses = await _context.Addresses
            .AsNoTracking()
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.IsDefault)
            .ThenByDescending(a => a.CreatedAtUtc)
            .Select(a => MapToDto(a))
            .ToListAsync(cancellationToken);

        return addresses;
    }

    public async Task<AddressDto> GetAddressByIdAsync(Guid addressId, Guid userId, CancellationToken cancellationToken = default)
    {
        var address = await _context.Addresses
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == addressId, cancellationToken);

        if (address == null)
        {
            throw new NotFoundException("Address", addressId);
        }

        // IDOR Protection: verify ownership
        if (address.UserId != userId)
        {
            throw new ForbiddenAccessException("You do not have permission to view this address.");
        }

        return MapToDto(address);
    }

    public async Task<AddressDto> CreateAddressAsync(Guid userId, CreateAddressRequest request, CancellationToken cancellationToken = default)
    {
        var isFirstAddress = !await _context.Addresses.AnyAsync(a => a.UserId == userId, cancellationToken);
        var shouldBeDefault = request.IsDefault || isFirstAddress;

        if (shouldBeDefault)
        {
            await ResetDefaultAddressesAsync(userId, cancellationToken);
        }

        var address = new Address
        {
            UserId = userId,
            Label = request.Label.Trim(),
            RecipientName = request.RecipientName.Trim(),
            PhoneNumber = request.PhoneNumber.Trim(),
            Line1 = request.Line1.Trim(),
            Line2 = request.Line2?.Trim(),
            District = request.District.Trim(),
            City = request.City.Trim(),
            Region = request.Region.Trim(),
            Reference = request.Reference?.Trim(),
            IsDefault = shouldBeDefault,
            CreatedAtUtc = DateTime.UtcNow
        };

        _context.Addresses.Add(address);
        await _context.SaveChangesAsync(cancellationToken);

        return MapToDto(address);
    }

    public async Task<AddressDto> UpdateAddressAsync(Guid addressId, Guid userId, UpdateAddressRequest request, CancellationToken cancellationToken = default)
    {
        var address = await _context.Addresses
            .FirstOrDefaultAsync(a => a.Id == addressId, cancellationToken);

        if (address == null)
        {
            throw new NotFoundException("Address", addressId);
        }

        // IDOR Protection: verify ownership
        if (address.UserId != userId)
        {
            throw new ForbiddenAccessException("You do not have permission to modify this address.");
        }

        if (request.IsDefault && !address.IsDefault)
        {
            await ResetDefaultAddressesAsync(userId, cancellationToken);
        }

        address.Label = request.Label.Trim();
        address.RecipientName = request.RecipientName.Trim();
        address.PhoneNumber = request.PhoneNumber.Trim();
        address.Line1 = request.Line1.Trim();
        address.Line2 = request.Line2?.Trim();
        address.District = request.District.Trim();
        address.City = request.City.Trim();
        address.Region = request.Region.Trim();
        address.Reference = request.Reference?.Trim();
        address.IsDefault = request.IsDefault;
        address.UpdatedAtUtc = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return MapToDto(address);
    }

    public async Task DeleteAddressAsync(Guid addressId, Guid userId, CancellationToken cancellationToken = default)
    {
        var address = await _context.Addresses
            .FirstOrDefaultAsync(a => a.Id == addressId, cancellationToken);

        if (address == null)
        {
            throw new NotFoundException("Address", addressId);
        }

        // IDOR Protection: verify ownership
        if (address.UserId != userId)
        {
            throw new ForbiddenAccessException("You do not have permission to delete this address.");
        }

        var wasDefault = address.IsDefault;
        _context.Addresses.Remove(address);
        await _context.SaveChangesAsync(cancellationToken);

        // If default address was deleted, promote another address to default if available
        if (wasDefault)
        {
            var nextAddress = await _context.Addresses
                .Where(a => a.UserId == userId)
                .OrderByDescending(a => a.CreatedAtUtc)
                .FirstOrDefaultAsync(cancellationToken);

            if (nextAddress != null)
            {
                nextAddress.IsDefault = true;
                await _context.SaveChangesAsync(cancellationToken);
            }
        }
    }

    public async Task SetDefaultAddressAsync(Guid addressId, Guid userId, CancellationToken cancellationToken = default)
    {
        var address = await _context.Addresses
            .FirstOrDefaultAsync(a => a.Id == addressId, cancellationToken);

        if (address == null)
        {
            throw new NotFoundException("Address", addressId);
        }

        if (address.UserId != userId)
        {
            throw new ForbiddenAccessException("You do not have permission to modify this address.");
        }

        await ResetDefaultAddressesAsync(userId, cancellationToken);
        address.IsDefault = true;
        address.UpdatedAtUtc = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task ResetDefaultAddressesAsync(Guid userId, CancellationToken cancellationToken)
    {
        var defaultAddresses = await _context.Addresses
            .Where(a => a.UserId == userId && a.IsDefault)
            .ToListAsync(cancellationToken);

        foreach (var addr in defaultAddresses)
        {
            addr.IsDefault = false;
        }
    }

    private static AddressDto MapToDto(Address a) => new(
        a.Id,
        a.UserId,
        a.Label,
        a.RecipientName,
        a.PhoneNumber,
        a.Line1,
        a.Line2,
        a.District,
        a.City,
        a.Region,
        a.Reference,
        a.IsDefault,
        a.CreatedAtUtc
    );
}
