using HomeSphere.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HomeSphere.Infrastructure.Persistence.Configurations;

public class CouponConfiguration : IEntityTypeConfiguration<Coupon>
{
    public void Configure(EntityTypeBuilder<Coupon> builder)
    {
        builder.ToTable("coupons");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id)
            .HasColumnName("id");

        builder.Property(c => c.Code)
            .HasColumnName("code")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(c => c.Description)
            .HasColumnName("description")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(c => c.DiscountType)
            .HasColumnName("discount_type")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(c => c.DiscountValue)
            .HasColumnName("discount_value")
            .HasColumnType("decimal(10,2)")
            .IsRequired();

        builder.Property(c => c.MinOrderAmount)
            .HasColumnName("min_order_amount")
            .HasColumnType("decimal(10,2)");

        builder.Property(c => c.MaxDiscountAmount)
            .HasColumnName("max_discount_amount")
            .HasColumnType("decimal(10,2)");

        builder.Property(c => c.ValidFromUtc)
            .HasColumnName("valid_from_utc")
            .IsRequired();

        builder.Property(c => c.ValidUntilUtc)
            .HasColumnName("valid_until_utc")
            .IsRequired();

        builder.Property(c => c.UsageLimit)
            .HasColumnName("usage_limit");

        builder.Property(c => c.TimesUsed)
            .HasColumnName("times_used")
            .HasDefaultValue(0);

        builder.Property(c => c.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true);

        builder.Property(c => c.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        builder.Property(c => c.UpdatedAtUtc)
            .HasColumnName("updated_at_utc");

        builder.HasIndex(c => c.Code)
            .IsUnique();
    }
}
