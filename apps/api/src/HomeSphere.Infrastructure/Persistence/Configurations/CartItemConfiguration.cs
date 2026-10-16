using HomeSphere.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HomeSphere.Infrastructure.Persistence.Configurations;

public class CartItemConfiguration : IEntityTypeConfiguration<CartItem>
{
    public void Configure(EntityTypeBuilder<CartItem> builder)
    {
        builder.ToTable("cart_items");

        builder.HasKey(ci => ci.Id);

        builder.Property(ci => ci.Id)
            .HasColumnName("id");

        builder.Property(ci => ci.CartId)
            .HasColumnName("cart_id")
            .IsRequired();

        builder.Property(ci => ci.ProductVariantId)
            .HasColumnName("product_variant_id")
            .IsRequired();

        builder.Property(ci => ci.Quantity)
            .HasColumnName("quantity")
            .IsRequired();

        builder.Property(ci => ci.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        builder.Property(ci => ci.UpdatedAtUtc)
            .HasColumnName("updated_at_utc");

        builder.HasOne(ci => ci.Cart)
            .WithMany(c => c.Items)
            .HasForeignKey(ci => ci.CartId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ci => ci.ProductVariant)
            .WithMany()
            .HasForeignKey(ci => ci.ProductVariantId)
            .OnDelete(DeleteBehavior.Cascade);

        // Ensure each variant is present at most once in a given cart
        builder.HasIndex(ci => new { ci.CartId, ci.ProductVariantId })
            .IsUnique();
    }
}
