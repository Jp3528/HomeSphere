using HomeSphere.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HomeSphere.Infrastructure.Persistence.Configurations;

public class OrderStatusHistoryConfiguration : IEntityTypeConfiguration<OrderStatusHistory>
{
    public void Configure(EntityTypeBuilder<OrderStatusHistory> builder)
    {
        builder.ToTable("order_status_history");

        builder.HasKey(h => h.Id);

        builder.Property(h => h.Id)
            .HasColumnName("id");

        builder.Property(h => h.OrderId)
            .HasColumnName("order_id")
            .IsRequired();

        builder.Property(h => h.PreviousStatus)
            .HasColumnName("previous_status")
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(h => h.NewStatus)
            .HasColumnName("new_status")
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(h => h.ChangedBy)
            .HasColumnName("changed_by")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(h => h.Reason)
            .HasColumnName("reason")
            .HasMaxLength(500);

        builder.Property(h => h.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        builder.Property(h => h.UpdatedAtUtc)
            .HasColumnName("updated_at_utc");

        builder.HasIndex(h => h.OrderId);
        builder.HasIndex(h => h.CreatedAtUtc);
    }
}
