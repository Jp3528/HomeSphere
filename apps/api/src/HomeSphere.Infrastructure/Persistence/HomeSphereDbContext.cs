using HomeSphere.Application.Common.Interfaces;
using HomeSphere.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HomeSphere.Infrastructure.Persistence;

public class HomeSphereDbContext : DbContext, IApplicationDbContext
{
    public HomeSphereDbContext(DbContextOptions<HomeSphereDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Address> Addresses => Set<Address>();
    public DbSet<IdempotencyRecord> IdempotencyRecords => Set<IdempotencyRecord>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Brand> Brands => Set<Brand>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductVariant> ProductVariants => Set<ProductVariant>();
    public DbSet<ProductImage> ProductImages => Set<ProductImage>();
    public DbSet<RoomScene> RoomScenes => Set<RoomScene>();
    public DbSet<SceneHotspot> SceneHotspots => Set<SceneHotspot>();
    public DbSet<Cart> Carts => Set<Cart>();
    public DbSet<CartItem> CartItems => Set<CartItem>();
    public DbSet<Coupon> Coupons => Set<Coupon>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(HomeSphereDbContext).Assembly);
    }
}
