using HomeSphere.Domain.Common;

namespace HomeSphere.Domain.Entities;

public class SceneHotspot : BaseEntity
{
    public Guid RoomSceneId { get; set; }
    public RoomScene RoomScene { get; set; } = null!;

    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;

    public Guid? ProductVariantId { get; set; }
    public ProductVariant? ProductVariant { get; set; }

    /// <summary>
    /// Percentage X coordinate (0.00 to 100.00) relative to image width.
    /// </summary>
    public decimal XCoordinate { get; set; }

    /// <summary>
    /// Percentage Y coordinate (0.00 to 100.00) relative to image height.
    /// </summary>
    public decimal YCoordinate { get; set; }

    public string? Title { get; set; }
    public int DisplayOrder { get; set; } = 0;
    public bool IsActive { get; set; } = true;
}
