using HomeSphere.Domain.Common;

namespace HomeSphere.Domain.Entities;

public class RoomScene : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
    public string RoomType { get; set; } = "Living";
    public int DisplayOrder { get; set; } = 0;
    public bool IsActive { get; set; } = true;

    public ICollection<SceneHotspot> Hotspots { get; set; } = new List<SceneHotspot>();
}
