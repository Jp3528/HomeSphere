using HomeSphere.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HomeSphere.Infrastructure.Persistence.Configurations;

public class StockReservationConfiguration : IEntityTypeConfiguration<StockReservation>
{
    public void Configure(EntityTypeBuilder<StockReservation> builder)
    {
        builder.ToTable("stock_reservations");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Id)
            .HasColumnName("id");

        builder.Property(r => r.OrderId)
            .HasColumnName("order_id")
            .IsRequired();

        builder.Property(r => r.ProductVariantId)
            .HasColumnName("product_variant_id")
            .IsRequired();

        builder.Property(r => r.Quantity)
            .HasColumnName("quantity")
            .IsRequired();

        builder.Property(r => r.ExpiresAtUtc)
            .HasColumnName("expires_at_utc")
            .IsRequired();

        builder.Property(r => r.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(r => r.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        builder.Property(r => r.UpdatedAtUtc)
            .HasColumnName("updated_at_utc");

        builder.HasOne(r => r.ProductVariant)
            .WithMany()
            .HasForeignKey(r => r.ProductVariantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(r => r.OrderId);
        builder.HasIndex(r => r.ProductVariantId);
        builder.HasIndex(r => new { r.Status, r.ExpiresAtUtc });
    }
}
