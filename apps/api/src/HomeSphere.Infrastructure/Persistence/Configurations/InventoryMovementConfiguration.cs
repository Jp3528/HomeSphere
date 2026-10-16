using HomeSphere.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HomeSphere.Infrastructure.Persistence.Configurations;

public class InventoryMovementConfiguration : IEntityTypeConfiguration<InventoryMovement>
{
    public void Configure(EntityTypeBuilder<InventoryMovement> builder)
    {
        builder.ToTable("inventory_movements");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.ProductVariantId).IsRequired();
        builder.Property(m => m.Type).IsRequired();
        builder.Property(m => m.QuantityDelta).IsRequired();
        builder.Property(m => m.PreviousStock).IsRequired();
        builder.Property(m => m.NewStock).IsRequired();
        builder.Property(m => m.Reason).HasMaxLength(500).IsRequired();
        builder.Property(m => m.ReferenceType).HasMaxLength(50);
        builder.Property(m => m.ReferenceId).HasMaxLength(100);
        builder.Property(m => m.Actor).HasMaxLength(256);
        builder.Property(m => m.CreatedAtUtc).IsRequired();

        builder.HasIndex(m => m.ProductVariantId);
        builder.HasIndex(m => m.CreatedAtUtc);

        builder.HasOne(m => m.ProductVariant)
            .WithMany()
            .HasForeignKey(m => m.ProductVariantId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
