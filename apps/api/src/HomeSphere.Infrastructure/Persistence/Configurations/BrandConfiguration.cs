using HomeSphere.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HomeSphere.Infrastructure.Persistence.Configurations;

public class BrandConfiguration : IEntityTypeConfiguration<Brand>
{
    public void Configure(EntityTypeBuilder<Brand> builder)
    {
        builder.ToTable("brands");

        builder.HasKey(b => b.Id);

        builder.Property(b => b.Id)
            .HasColumnName("id");

        builder.Property(b => b.Name)
            .HasColumnName("name")
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(b => b.Slug)
            .HasColumnName("slug")
            .HasMaxLength(160)
            .IsRequired();

        builder.Property(b => b.Description)
            .HasColumnName("description")
            .HasMaxLength(1000);

        builder.Property(b => b.LogoUrl)
            .HasColumnName("logo_url")
            .HasMaxLength(512);

        builder.Property(b => b.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true)
            .IsRequired();

        builder.Property(b => b.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        builder.Property(b => b.UpdatedAtUtc)
            .HasColumnName("updated_at_utc");

        builder.HasIndex(b => b.Slug)
            .IsUnique()
            .HasDatabaseName("ix_brands_slug");
    }
}
