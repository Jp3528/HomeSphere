using HomeSphere.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HomeSphere.Infrastructure.Persistence.Configurations;

public class ProductVariantConfiguration : IEntityTypeConfiguration<ProductVariant>
{
    public void Configure(EntityTypeBuilder<ProductVariant> builder)
    {
        builder.ToTable("product_variants");

        builder.HasKey(v => v.Id);

        builder.Property(v => v.Id)
            .HasColumnName("id");

        builder.Property(v => v.ProductId)
            .HasColumnName("product_id")
            .IsRequired();

        builder.Property(v => v.Sku)
            .HasColumnName("sku")
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(v => v.Name)
            .HasColumnName("name")
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(v => v.Price)
            .HasColumnName("price")
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(v => v.StockQuantity)
            .HasColumnName("stock_quantity")
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(v => v.Barcode)
            .HasColumnName("barcode")
            .HasMaxLength(64);

        builder.Property(v => v.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true)
            .IsRequired();

        builder.Property(v => v.AttributesJson)
            .HasColumnName("attributes_json");

        builder.Property(v => v.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        builder.Property(v => v.UpdatedAtUtc)
            .HasColumnName("updated_at_utc");

        builder.HasIndex(v => v.Sku)
            .IsUnique()
            .HasDatabaseName("ix_product_variants_sku");

        builder.HasIndex(v => v.ProductId)
            .HasDatabaseName("ix_product_variants_product_id");

        builder.HasOne(v => v.Product)
            .WithMany(p => p.Variants)
            .HasForeignKey(v => v.ProductId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
