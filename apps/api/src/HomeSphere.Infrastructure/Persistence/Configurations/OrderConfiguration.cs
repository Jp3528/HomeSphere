using HomeSphere.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HomeSphere.Infrastructure.Persistence.Configurations;

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("orders");

        builder.HasKey(o => o.Id);

        builder.Property(o => o.Id)
            .HasColumnName("id");

        builder.Property(o => o.OrderNumber)
            .HasColumnName("order_number")
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(o => o.PublicId)
            .HasColumnName("public_id")
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(o => o.UserId)
            .HasColumnName("user_id");

        builder.Property(o => o.GuestEmail)
            .HasColumnName("guest_email")
            .HasMaxLength(255);

        builder.Property(o => o.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(o => o.Subtotal)
            .HasColumnName("subtotal")
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(o => o.DiscountTotal)
            .HasColumnName("discount_total")
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(o => o.ShippingCost)
            .HasColumnName("shipping_cost")
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(o => o.Total)
            .HasColumnName("total")
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(o => o.CouponCode)
            .HasColumnName("coupon_code")
            .HasMaxLength(64);

        builder.Property(o => o.ShippingMethod)
            .HasColumnName("shipping_method")
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(o => o.ShippingAddressSnapshotJson)
            .HasColumnName("shipping_address_snapshot_json")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(o => o.BillingAddressSnapshotJson)
            .HasColumnName("billing_address_snapshot_json")
            .HasColumnType("text");

        builder.Property(o => o.CustomerNotes)
            .HasColumnName("customer_notes")
            .HasMaxLength(1000);

        builder.Property(o => o.CancellationReason)
            .HasColumnName("cancellation_reason")
            .HasMaxLength(500);

        builder.Property(o => o.PaidAtUtc)
            .HasColumnName("paid_at_utc");

        builder.Property(o => o.ShippedAtUtc)
            .HasColumnName("shipped_at_utc");

        builder.Property(o => o.DeliveredAtUtc)
            .HasColumnName("delivered_at_utc");

        builder.Property(o => o.CancelledAtUtc)
            .HasColumnName("cancelled_at_utc");

        builder.Property(o => o.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        builder.Property(o => o.UpdatedAtUtc)
            .HasColumnName("updated_at_utc");

        builder.HasOne(o => o.User)
            .WithMany()
            .HasForeignKey(o => o.UserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(o => o.Items)
            .WithOne(i => i.Order)
            .HasForeignKey(i => i.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(o => o.Reservations)
            .WithOne(r => r.Order)
            .HasForeignKey(r => r.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(o => o.PaymentAttempts)
            .WithOne(p => p.Order)
            .HasForeignKey(p => p.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(o => o.StatusHistory)
            .WithOne(h => h.Order)
            .HasForeignKey(h => h.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(o => o.OrderNumber).IsUnique();
        builder.HasIndex(o => o.PublicId).IsUnique();
        builder.HasIndex(o => o.UserId);
        builder.HasIndex(o => o.Status);
        builder.HasIndex(o => o.CreatedAtUtc);
    }
}
