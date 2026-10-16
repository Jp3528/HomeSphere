using HomeSphere.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HomeSphere.Infrastructure.Persistence.Configurations;

public class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.ToTable("order_items");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.Id)
            .HasColumnName("id");

        builder.Property(i => i.OrderId)
            .HasColumnName("order_id")
            .IsRequired();

        builder.Property(i => i.ProductVariantId)
            .HasColumnName("product_variant_id")
            .IsRequired();

        builder.Property(i => i.SkuSnapshot)
            .HasColumnName("sku_snapshot")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(i => i.ProductNameSnapshot)
            .HasColumnName("product_name_snapshot")
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(i => i.UnitPrice)
            .HasColumnName("unit_price")
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(i => i.Quantity)
            .HasColumnName("quantity")
            .IsRequired();

        builder.Property(i => i.TotalPrice)
            .HasColumnName("total_price")
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(i => i.AttributesJsonSnapshot)
            .HasColumnName("attributes_json_snapshot")
            .HasColumnType("text");

        builder.Property(i => i.ImageUrlSnapshot)
            .HasColumnName("image_url_snapshot")
            .HasMaxLength(1000);

        builder.Property(i => i.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        builder.Property(i => i.UpdatedAtUtc)
            .HasColumnName("updated_at_utc");

        builder.HasOne(i => i.ProductVariant)
            .WithMany()
            .HasForeignKey(i => i.ProductVariantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(i => i.OrderId);
        builder.HasIndex(i => i.ProductVariantId);
    }
}
