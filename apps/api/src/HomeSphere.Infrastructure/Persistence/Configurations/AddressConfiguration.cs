using HomeSphere.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HomeSphere.Infrastructure.Persistence.Configurations;

public class AddressConfiguration : IEntityTypeConfiguration<Address>
{
    public void Configure(EntityTypeBuilder<Address> builder)
    {
        builder.ToTable("addresses");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Id)
            .HasColumnName("id");

        builder.Property(a => a.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(a => a.Label)
            .HasColumnName("label")
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(a => a.RecipientName)
            .HasColumnName("recipient_name")
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(a => a.PhoneNumber)
            .HasColumnName("phone_number")
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(a => a.Line1)
            .HasColumnName("line1")
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(a => a.Line2)
            .HasColumnName("line2")
            .HasMaxLength(256);

        builder.Property(a => a.District)
            .HasColumnName("district")
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(a => a.City)
            .HasColumnName("city")
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(a => a.Region)
            .HasColumnName("region")
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(a => a.Reference)
            .HasColumnName("reference")
            .HasMaxLength(256);

        builder.Property(a => a.IsDefault)
            .HasColumnName("is_default")
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(a => a.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        builder.Property(a => a.UpdatedAtUtc)
            .HasColumnName("updated_at_utc");

        builder.HasIndex(a => a.UserId)
            .HasDatabaseName("ix_addresses_user_id");

        builder.HasOne(a => a.User)
            .WithMany(u => u.Addresses)
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
