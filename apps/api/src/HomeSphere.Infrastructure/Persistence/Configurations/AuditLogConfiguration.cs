using HomeSphere.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HomeSphere.Infrastructure.Persistence.Configurations;

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("audit_logs");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id)
            .HasColumnName("id");

        builder.Property(a => a.Action)
            .HasColumnName("action")
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(a => a.EntityName)
            .HasColumnName("entity_name")
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(a => a.EntityId)
            .HasColumnName("entity_id")
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(a => a.ActorId)
            .HasColumnName("actor_id")
            .HasMaxLength(64);

        builder.Property(a => a.ActorEmail)
            .HasColumnName("actor_email")
            .HasMaxLength(256);

        builder.Property(a => a.ActorIpAddress)
            .HasColumnName("actor_ip_address")
            .HasMaxLength(64);

        builder.Property(a => a.CorrelationId)
            .HasColumnName("correlation_id")
            .HasMaxLength(64);

        builder.Property(a => a.DetailsJson)
            .HasColumnName("details_json")
            .HasColumnType("jsonb");

        builder.Property(a => a.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        builder.Property(a => a.UpdatedAtUtc)
            .HasColumnName("updated_at_utc");

        builder.HasIndex(a => a.Action);
        builder.HasIndex(a => a.EntityName);
        builder.HasIndex(a => a.CreatedAtUtc);
        builder.HasIndex(a => a.CorrelationId);
    }
}
