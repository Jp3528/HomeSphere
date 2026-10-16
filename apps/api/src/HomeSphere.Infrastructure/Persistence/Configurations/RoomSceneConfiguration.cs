using HomeSphere.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HomeSphere.Infrastructure.Persistence.Configurations;

public class RoomSceneConfiguration : IEntityTypeConfiguration<RoomScene>
{
    public void Configure(EntityTypeBuilder<RoomScene> builder)
    {
        builder.ToTable("room_scenes");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Id)
            .HasColumnName("id");

        builder.Property(s => s.Title)
            .HasColumnName("title")
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(s => s.Slug)
            .HasColumnName("slug")
            .HasMaxLength(280)
            .IsRequired();

        builder.Property(s => s.Description)
            .HasColumnName("description")
            .IsRequired();

        builder.Property(s => s.ImageUrl)
            .HasColumnName("image_url")
            .HasMaxLength(1024)
            .IsRequired();

        builder.Property(s => s.RoomType)
            .HasColumnName("room_type")
            .HasMaxLength(64)
            .HasDefaultValue("Living")
            .IsRequired();

        builder.Property(s => s.DisplayOrder)
            .HasColumnName("display_order")
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(s => s.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true)
            .IsRequired();

        builder.Property(s => s.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        builder.Property(s => s.UpdatedAtUtc)
            .HasColumnName("updated_at_utc");

        builder.HasIndex(s => s.Slug)
            .IsUnique()
            .HasDatabaseName("ix_room_scenes_slug");
    }
}
