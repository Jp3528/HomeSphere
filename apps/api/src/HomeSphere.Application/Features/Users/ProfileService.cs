using HomeSphere.Application.Common.Interfaces;
using HomeSphere.Application.Features.Users.DTOs;
using HomeSphere.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace HomeSphere.Application.Features.Users;

public interface IProfileService
{
    Task<UserProfileDto> GetProfileAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<UserProfileDto> UpdateProfileAsync(Guid userId, UpdateProfileRequest request, CancellationToken cancellationToken = default);
}

public class ProfileService : IProfileService
{
    private readonly IApplicationDbContext _context;

    public ProfileService(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<UserProfileDto> GetProfileAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _context.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user == null)
        {
            throw new NotFoundException("User", userId);
        }

        var roles = user.UserRoles.Select(ur => ur.Role.Name).ToList();

        return new UserProfileDto(
            user.Id,
            user.Email,
            user.FirstName,
            user.LastName,
            user.FullName,
            user.PhoneNumber,
            roles,
            user.CreatedAtUtc
        );
    }

    public async Task<UserProfileDto> UpdateProfileAsync(Guid userId, UpdateProfileRequest request, CancellationToken cancellationToken = default)
    {
        var user = await _context.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user == null)
        {
            throw new NotFoundException("User", userId);
        }

        user.FirstName = request.FirstName.Trim();
        user.LastName = request.LastName.Trim();
        user.PhoneNumber = request.PhoneNumber?.Trim();
        user.UpdatedAtUtc = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        var roles = user.UserRoles.Select(ur => ur.Role.Name).ToList();

        return new UserProfileDto(
            user.Id,
            user.Email,
            user.FirstName,
            user.LastName,
            user.FullName,
            user.PhoneNumber,
            roles,
            user.CreatedAtUtc
        );
    }
}
