using HomeSphere.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace HomeSphere.Infrastructure.Persistence.Configurations;

public class SceneHotspotConfiguration : IEntityTypeConfiguration<SceneHotspot>
{
    public void Configure(EntityTypeBuilder<SceneHotspot> builder)
    {
        builder.ToTable("scene_hotspots");

        builder.HasKey(h => h.Id);

        builder.Property(h => h.Id)
            .HasColumnName("id");

        builder.Property(h => h.RoomSceneId)
            .HasColumnName("room_scene_id")
            .IsRequired();

        builder.Property(h => h.ProductId)
            .HasColumnName("product_id")
            .IsRequired();

        builder.Property(h => h.ProductVariantId)
            .HasColumnName("product_variant_id");

        builder.Property(h => h.XCoordinate)
            .HasColumnName("x_coordinate")
            .HasColumnType("decimal(5,2)")
            .IsRequired();

        builder.Property(h => h.YCoordinate)
            .HasColumnName("y_coordinate")
            .HasColumnType("decimal(5,2)")
            .IsRequired();

        builder.Property(h => h.Title)
            .HasColumnName("title")
            .HasMaxLength(128);

        builder.Property(h => h.DisplayOrder)
            .HasColumnName("display_order")
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(h => h.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true)
            .IsRequired();

        builder.Property(h => h.CreatedAtUtc)
            .HasColumnName("created_at_utc")
            .IsRequired();

        builder.Property(h => h.UpdatedAtUtc)
            .HasColumnName("updated_at_utc");

        builder.HasIndex(h => h.RoomSceneId)
            .HasDatabaseName("ix_scene_hotspots_room_scene_id");

        builder.HasIndex(h => h.ProductId)
            .HasDatabaseName("ix_scene_hotspots_product_id");

        builder.HasOne(h => h.RoomScene)
            .WithMany(s => s.Hotspots)
            .HasForeignKey(h => h.RoomSceneId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(h => h.Product)
            .WithMany()
            .HasForeignKey(h => h.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(h => h.ProductVariant)
            .WithMany()
            .HasForeignKey(h => h.ProductVariantId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
