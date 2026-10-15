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
    public DbSet<IdempotencyRecord> IdempotencyRecords => Set<IdempotencyRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(HomeSphereDbContext).Assembly);
    }
}
