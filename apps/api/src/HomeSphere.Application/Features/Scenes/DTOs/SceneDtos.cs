using System.ComponentModel.DataAnnotations;

namespace HomeSphere.Application.Features.Scenes.DTOs;

public record SceneHotspotDto(
    Guid Id,
    Guid RoomSceneId,
    Guid ProductId,
    string ProductName,
    string ProductSlug,
    decimal ProductBasePrice,
    string? ProductPrimaryImageUrl,
    Guid? ProductVariantId,
    string? ProductVariantName,
    decimal? ProductVariantPrice,
    decimal XCoordinate,
    decimal YCoordinate,
    string? Title,
    int DisplayOrder,
    bool IsActive,
    bool IsAvailable = true,
    string? UnavailableReason = null
);

public record RoomSceneListDto(
    Guid Id,
    string Title,
    string Slug,
    string Description,
    string ImageUrl,
    string RoomType,
    int HotspotCount,
    int DisplayOrder,
    bool IsActive,
    DateTime CreatedAtUtc
);

public record RoomSceneDetailDto(
    Guid Id,
    string Title,
    string Slug,
    string Description,
    string ImageUrl,
    string RoomType,
    int DisplayOrder,
    bool IsActive,
    IReadOnlyList<SceneHotspotDto> Hotspots,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc
);

// Admin Requests
public record CreateRoomSceneRequest(
    [Required, StringLength(256)] string Title,
    [Required, StringLength(280)] string Slug,
    [Required] string Description,
    [Required, StringLength(1024)] string ImageUrl,
    string RoomType = "Living",
    int DisplayOrder = 0,
    bool IsActive = true,
    List<CreateSceneHotspotRequest>? Hotspots = null
);

public record UpdateRoomSceneRequest(
    [Required, StringLength(256)] string Title,
    [Required, StringLength(280)] string Slug,
    [Required] string Description,
    [Required, StringLength(1024)] string ImageUrl,
    string RoomType = "Living",
    int DisplayOrder = 0,
    bool IsActive = true
);

public record CreateSceneHotspotRequest(
    [Required] Guid ProductId,
    Guid? ProductVariantId,
    [Range(0.0, 100.0)] decimal XCoordinate,
    [Range(0.0, 100.0)] decimal YCoordinate,
    [StringLength(128)] string? Title,
    int DisplayOrder = 0,
    bool IsActive = true
);

public record UpdateSceneHotspotRequest(
    [Required] Guid ProductId,
    Guid? ProductVariantId,
    [Range(0.0, 100.0)] decimal XCoordinate,
    [Range(0.0, 100.0)] decimal YCoordinate,
    [StringLength(128)] string? Title,
    int DisplayOrder = 0,
    bool IsActive = true
);
