using HomeSphere.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HomeSphere.Infrastructure.Persistence.Configurations;

public class PaymentAttemptConfiguration : IEntityTypeConfiguration<PaymentAttempt>
{
    public void Configure(EntityTypeBuilder<PaymentAttempt> builder)
    {
        builder.ToTable("payment_attempts");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id)
            .HasColumnName("id");

        builder.Property(p => p.OrderId)
            .HasColumnName("order_id")
            .IsRequired();

        builder.Property(p => p.Provider)
            .HasColumnName("provider")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(p => p.TransactionId)
            .HasColumnName("transaction_id")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(p => p.Amount)
            .HasColumnName("amount")
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(p => p.Currency)
            .HasColumnName("currency")
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(p => p.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(p => p.ErrorMessage)
            .HasColumnName("error_message")
            .HasMaxLength(500);

        builder.Property(p => p.RawPayloadJson)
            .HasColumnName("raw_payload_json")
            .HasColumnType("text");

        builder.Property(p => p.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        builder.Property(p => p.UpdatedAtUtc)
            .HasColumnName("updated_at_utc");

        builder.HasIndex(p => p.OrderId);
        builder.HasIndex(p => p.TransactionId);
        builder.HasIndex(p => p.CreatedAtUtc);
    }
}
