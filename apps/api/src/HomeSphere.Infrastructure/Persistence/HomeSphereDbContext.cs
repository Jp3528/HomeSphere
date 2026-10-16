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
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<StockReservation> StockReservations => Set<StockReservation>();
    public DbSet<OrderStatusHistory> OrderStatusHistories => Set<OrderStatusHistory>();
    public DbSet<PaymentAttempt> PaymentAttempts => Set<PaymentAttempt>();

    public async Task<IApplicationDbTransaction?> BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (Database.IsRelational())
        {
            var tx = await Database.BeginTransactionAsync(cancellationToken);
            return new ApplicationDbTransaction(tx);
        }
        return null;
    }

    private class ApplicationDbTransaction : IApplicationDbTransaction
    {
        private readonly Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction _tx;

        public ApplicationDbTransaction(Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction tx)
        {
            _tx = tx;
        }

        public Task CommitAsync(CancellationToken cancellationToken = default) => _tx.CommitAsync(cancellationToken);
        public Task RollbackAsync(CancellationToken cancellationToken = default) => _tx.RollbackAsync(cancellationToken);
        public ValueTask DisposeAsync() => _tx.DisposeAsync();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(HomeSphereDbContext).Assembly);
    }
}
