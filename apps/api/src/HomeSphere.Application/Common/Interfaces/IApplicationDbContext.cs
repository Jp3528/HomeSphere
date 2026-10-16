using HomeSphere.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HomeSphere.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<User> Users { get; }
    DbSet<Role> Roles { get; }
    DbSet<UserRole> UserRoles { get; }
    DbSet<RefreshToken> RefreshTokens { get; }
    DbSet<Address> Addresses { get; }
    DbSet<IdempotencyRecord> IdempotencyRecords { get; }
    DbSet<Category> Categories { get; }
    DbSet<Brand> Brands { get; }
    DbSet<Product> Products { get; }
    DbSet<ProductVariant> ProductVariants { get; }
    DbSet<ProductImage> ProductImages { get; }
    DbSet<RoomScene> RoomScenes { get; }
    DbSet<SceneHotspot> SceneHotspots { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
