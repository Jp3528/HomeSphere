using HomeSphere.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HomeSphere.Infrastructure.Persistence;

public class HomeSphereDbContext : DbContext
{
    public HomeSphereDbContext(DbContextOptions<HomeSphereDbContext> options)
        : base(options)
    {
    }

    public DbSet<IdempotencyRecord> IdempotencyRecords => Set<IdempotencyRecord>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(HomeSphereDbContext).Assembly);
    }
}
