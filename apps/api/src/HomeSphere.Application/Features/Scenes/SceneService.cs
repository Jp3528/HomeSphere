using HomeSphere.Application.Common.Interfaces;
using HomeSphere.Application.Features.Scenes.DTOs;
using HomeSphere.Domain.Entities;
using HomeSphere.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace HomeSphere.Application.Features.Scenes;

public interface ISceneService
{
    Task<IReadOnlyList<RoomSceneListDto>> GetScenesAsync(bool onlyActive = true, CancellationToken cancellationToken = default);
    Task<RoomSceneDetailDto> GetSceneBySlugAsync(string slug, bool onlyActive = true, CancellationToken cancellationToken = default);
    Task<RoomSceneDetailDto> GetSceneByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<RoomSceneDetailDto> CreateSceneAsync(CreateRoomSceneRequest request, CancellationToken cancellationToken = default);
    Task<RoomSceneDetailDto> UpdateSceneAsync(Guid id, UpdateRoomSceneRequest request, CancellationToken cancellationToken = default);
    Task DeleteSceneAsync(Guid id, CancellationToken cancellationToken = default);
    Task<SceneHotspotDto> AddHotspotAsync(Guid sceneId, CreateSceneHotspotRequest request, CancellationToken cancellationToken = default);
    Task<SceneHotspotDto> UpdateHotspotAsync(Guid sceneId, Guid hotspotId, UpdateSceneHotspotRequest request, CancellationToken cancellationToken = default);
    Task DeleteHotspotAsync(Guid sceneId, Guid hotspotId, CancellationToken cancellationToken = default);
}

public class SceneService : ISceneService
{
    private readonly IApplicationDbContext _context;

    public SceneService(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<RoomSceneListDto>> GetScenesAsync(bool onlyActive = true, CancellationToken cancellationToken = default)
    {
        var query = _context.RoomScenes
            .AsNoTracking()
            .Include(s => s.Hotspots)
            .AsQueryable();

        if (onlyActive)
        {
            query = query.Where(s => s.IsActive);
        }

        return await query
            .OrderBy(s => s.DisplayOrder)
            .ThenByDescending(s => s.CreatedAtUtc)
            .Select(s => new RoomSceneListDto(
                s.Id,
                s.Title,
                s.Slug,
                s.Description,
                s.ImageUrl,
                s.RoomType,
                s.Hotspots.Count(h => !onlyActive || h.IsActive),
                s.DisplayOrder,
                s.IsActive,
                s.CreatedAtUtc
            ))
            .ToListAsync(cancellationToken);
    }

    public async Task<RoomSceneDetailDto> GetSceneBySlugAsync(string slug, bool onlyActive = true, CancellationToken cancellationToken = default)
    {
        var normalizedSlug = slug.Trim().ToLowerInvariant();

        var query = _context.RoomScenes
            .AsNoTracking()
            .Include(s => s.Hotspots)
                .ThenInclude(h => h.Product)
                    .ThenInclude(p => p.Images)
            .Include(s => s.Hotspots)
                .ThenInclude(h => h.ProductVariant)
            .Where(s => s.Slug == normalizedSlug);

        if (onlyActive)
        {
            query = query.Where(s => s.IsActive);
        }

        var scene = await query.FirstOrDefaultAsync(cancellationToken);

        if (scene == null)
        {
            throw new NotFoundException("RoomScene", slug);
        }

        return MapToDetailDto(scene, onlyActive);
    }

    public async Task<RoomSceneDetailDto> GetSceneByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var scene = await _context.RoomScenes
            .AsNoTracking()
            .Include(s => s.Hotspots)
                .ThenInclude(h => h.Product)
                    .ThenInclude(p => p.Images)
            .Include(s => s.Hotspots)
                .ThenInclude(h => h.ProductVariant)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

        if (scene == null)
        {
            throw new NotFoundException("RoomScene", id);
        }

        return MapToDetailDto(scene, false);
    }

    public async Task<RoomSceneDetailDto> CreateSceneAsync(CreateRoomSceneRequest request, CancellationToken cancellationToken = default)
    {
        var normalizedSlug = request.Slug.Trim().ToLowerInvariant();

        if (await _context.RoomScenes.AnyAsync(s => s.Slug == normalizedSlug, cancellationToken))
        {
            throw new ConflictException($"Room scene with slug '{normalizedSlug}' already exists.");
        }

        var scene = new RoomScene
        {
            Title = request.Title.Trim(),
            Slug = normalizedSlug,
            Description = request.Description.Trim(),
            ImageUrl = request.ImageUrl.Trim(),
            RoomType = string.IsNullOrWhiteSpace(request.RoomType) ? "Living" : request.RoomType.Trim(),
            DisplayOrder = request.DisplayOrder,
            IsActive = request.IsActive,
            CreatedAtUtc = DateTime.UtcNow
        };

        if (request.Hotspots != null && request.Hotspots.Any())
        {
            foreach (var h in request.Hotspots)
            {
                ValidateCoordinates(h.XCoordinate, h.YCoordinate);

                var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == h.ProductId, cancellationToken);
                if (product == null)
                {
                    throw new NotFoundException("Product", h.ProductId);
                }

                if (h.ProductVariantId.HasValue)
                {
                    var variant = await _context.ProductVariants.FirstOrDefaultAsync(v => v.Id == h.ProductVariantId.Value, cancellationToken);
                    if (variant == null || variant.ProductId != h.ProductId)
                    {
                        throw new BusinessRuleValidationException("Specified variant does not belong to the selected product.");
                    }
                }

                scene.Hotspots.Add(new SceneHotspot
                {
                    ProductId = h.ProductId,
                    ProductVariantId = h.ProductVariantId,
                    XCoordinate = h.XCoordinate,
                    YCoordinate = h.YCoordinate,
                    Title = h.Title?.Trim(),
                    DisplayOrder = h.DisplayOrder,
                    IsActive = h.IsActive,
                    CreatedAtUtc = DateTime.UtcNow
                });
            }
        }

        _context.RoomScenes.Add(scene);
        await _context.SaveChangesAsync(cancellationToken);

        return await GetSceneByIdAsync(scene.Id, cancellationToken);
    }

    public async Task<RoomSceneDetailDto> UpdateSceneAsync(Guid id, UpdateRoomSceneRequest request, CancellationToken cancellationToken = default)
    {
        var scene = await _context.RoomScenes.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
        if (scene == null)
        {
            throw new NotFoundException("RoomScene", id);
        }

        var normalizedSlug = request.Slug.Trim().ToLowerInvariant();
        if (scene.Slug != normalizedSlug && await _context.RoomScenes.AnyAsync(s => s.Slug == normalizedSlug && s.Id != id, cancellationToken))
        {
            throw new ConflictException($"Room scene with slug '{normalizedSlug}' already exists.");
        }

        scene.Title = request.Title.Trim();
        scene.Slug = normalizedSlug;
        scene.Description = request.Description.Trim();
        scene.ImageUrl = request.ImageUrl.Trim();
        scene.RoomType = string.IsNullOrWhiteSpace(request.RoomType) ? scene.RoomType : request.RoomType.Trim();
        scene.DisplayOrder = request.DisplayOrder;
        scene.IsActive = request.IsActive;
        scene.UpdatedAtUtc = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return await GetSceneByIdAsync(id, cancellationToken);
    }

    public async Task DeleteSceneAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var scene = await _context.RoomScenes.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
        if (scene == null)
        {
            throw new NotFoundException("RoomScene", id);
        }

        _context.RoomScenes.Remove(scene);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<SceneHotspotDto> AddHotspotAsync(Guid sceneId, CreateSceneHotspotRequest request, CancellationToken cancellationToken = default)
    {
        var scene = await _context.RoomScenes.FirstOrDefaultAsync(s => s.Id == sceneId, cancellationToken);
        if (scene == null)
        {
            throw new NotFoundException("RoomScene", sceneId);
        }

        ValidateCoordinates(request.XCoordinate, request.YCoordinate);

        var product = await _context.Products
            .Include(p => p.Images)
            .FirstOrDefaultAsync(p => p.Id == request.ProductId, cancellationToken);

        if (product == null)
        {
            throw new NotFoundException("Product", request.ProductId);
        }

        ProductVariant? variant = null;
        if (request.ProductVariantId.HasValue)
        {
            variant = await _context.ProductVariants.FirstOrDefaultAsync(v => v.Id == request.ProductVariantId.Value, cancellationToken);
            if (variant == null || variant.ProductId != request.ProductId)
            {
                throw new BusinessRuleValidationException("Specified variant does not belong to the selected product.");
            }
        }

        var hotspot = new SceneHotspot
        {
            RoomSceneId = sceneId,
            ProductId = request.ProductId,
            ProductVariantId = request.ProductVariantId,
            XCoordinate = request.XCoordinate,
            YCoordinate = request.YCoordinate,
            Title = request.Title?.Trim() ?? product.Name,
            DisplayOrder = request.DisplayOrder,
            IsActive = request.IsActive,
            CreatedAtUtc = DateTime.UtcNow
        };

        _context.SceneHotspots.Add(hotspot);
        await _context.SaveChangesAsync(cancellationToken);

        var isAvailable = product.Status == Domain.Enums.ProductStatus.Active &&
            (variant == null || (variant.IsActive && variant.StockQuantity > 0));
        var unavailableReason = !isAvailable
            ? (product.Status != Domain.Enums.ProductStatus.Active
                ? "Producto no disponible"
                : (variant != null && variant.StockQuantity <= 0 ? "Agotado" : "Variante no disponible"))
            : null;

        var primaryImage = product.Images.FirstOrDefault(i => i.IsPrimary)?.Url ?? product.Images.FirstOrDefault()?.Url;

        return new SceneHotspotDto(
            hotspot.Id,
            hotspot.RoomSceneId,
            hotspot.ProductId,
            product.Name,
            product.Slug,
            product.BasePrice,
            primaryImage,
            variant?.Id,
            variant?.Name,
            variant?.Price,
            hotspot.XCoordinate,
            hotspot.YCoordinate,
            hotspot.Title,
            hotspot.DisplayOrder,
            hotspot.IsActive,
            isAvailable,
            unavailableReason
        );
    }

    public async Task<SceneHotspotDto> UpdateHotspotAsync(Guid sceneId, Guid hotspotId, UpdateSceneHotspotRequest request, CancellationToken cancellationToken = default)
    {
        var hotspot = await _context.SceneHotspots
            .FirstOrDefaultAsync(h => h.Id == hotspotId && h.RoomSceneId == sceneId, cancellationToken);

        if (hotspot == null)
        {
            throw new NotFoundException("SceneHotspot", hotspotId);
        }

        ValidateCoordinates(request.XCoordinate, request.YCoordinate);

        var product = await _context.Products
            .Include(p => p.Images)
            .FirstOrDefaultAsync(p => p.Id == request.ProductId, cancellationToken);

        if (product == null)
        {
            throw new NotFoundException("Product", request.ProductId);
        }

        ProductVariant? variant = null;
        if (request.ProductVariantId.HasValue)
        {
            variant = await _context.ProductVariants.FirstOrDefaultAsync(v => v.Id == request.ProductVariantId.Value, cancellationToken);
            if (variant == null || variant.ProductId != request.ProductId)
            {
                throw new BusinessRuleValidationException("Specified variant does not belong to the selected product.");
            }
        }

        hotspot.ProductId = request.ProductId;
        hotspot.ProductVariantId = request.ProductVariantId;
        hotspot.XCoordinate = request.XCoordinate;
        hotspot.YCoordinate = request.YCoordinate;
        hotspot.Title = request.Title?.Trim();
        hotspot.DisplayOrder = request.DisplayOrder;
        hotspot.IsActive = request.IsActive;
        hotspot.UpdatedAtUtc = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        var isAvailable = product.Status == Domain.Enums.ProductStatus.Active &&
            (variant == null || (variant.IsActive && variant.StockQuantity > 0));
        var unavailableReason = !isAvailable
            ? (product.Status != Domain.Enums.ProductStatus.Active
                ? "Producto no disponible"
                : (variant != null && variant.StockQuantity <= 0 ? "Agotado" : "Variante no disponible"))
            : null;

        var primaryImage = product.Images.FirstOrDefault(i => i.IsPrimary)?.Url ?? product.Images.FirstOrDefault()?.Url;

        return new SceneHotspotDto(
            hotspot.Id,
            hotspot.RoomSceneId,
            hotspot.ProductId,
            product.Name,
            product.Slug,
            product.BasePrice,
            primaryImage,
            variant?.Id,
            variant?.Name,
            variant?.Price,
            hotspot.XCoordinate,
            hotspot.YCoordinate,
            hotspot.Title,
            hotspot.DisplayOrder,
            hotspot.IsActive,
            isAvailable,
            unavailableReason
        );
    }

    public async Task DeleteHotspotAsync(Guid sceneId, Guid hotspotId, CancellationToken cancellationToken = default)
    {
        var hotspot = await _context.SceneHotspots
            .FirstOrDefaultAsync(h => h.Id == hotspotId && h.RoomSceneId == sceneId, cancellationToken);

        if (hotspot == null)
        {
            throw new NotFoundException("SceneHotspot", hotspotId);
        }

        _context.SceneHotspots.Remove(hotspot);
        await _context.SaveChangesAsync(cancellationToken);
    }

    private static void ValidateCoordinates(decimal x, decimal y)
    {
        if (x < 0.0m || x > 100.0m || y < 0.0m || y > 100.0m)
        {
            throw new BusinessRuleValidationException("Hotspot coordinates must be between 0.00 and 100.00 percent.");
        }
    }

    private static RoomSceneDetailDto MapToDetailDto(RoomScene scene, bool onlyActiveHotspots)
    {
        var hotspotsQuery = scene.Hotspots.AsEnumerable();
        if (onlyActiveHotspots)
        {
            hotspotsQuery = hotspotsQuery.Where(h => h.IsActive);
        }

        var hotspotDtos = hotspotsQuery
            .OrderBy(h => h.DisplayOrder)
            .Select(h =>
            {
                var primaryImage = h.Product.Images.FirstOrDefault(i => i.IsPrimary)?.Url ??
                                   h.Product.Images.OrderBy(i => i.DisplayOrder).FirstOrDefault()?.Url;

                var isAvailable = h.Product.Status == Domain.Enums.ProductStatus.Active &&
                    (h.ProductVariant == null || (h.ProductVariant.IsActive && h.ProductVariant.StockQuantity > 0));
                var unavailableReason = !isAvailable
                    ? (h.Product.Status != Domain.Enums.ProductStatus.Active
                        ? "Producto no disponible"
                        : (h.ProductVariant != null && h.ProductVariant.StockQuantity <= 0 ? "Agotado" : "Variante no disponible"))
                    : null;

                return new SceneHotspotDto(
                    h.Id,
                    h.RoomSceneId,
                    h.ProductId,
                    h.Product.Name,
                    h.Product.Slug,
                    h.Product.BasePrice,
                    primaryImage,
                    h.ProductVariantId,
                    h.ProductVariant?.Name,
                    h.ProductVariant?.Price,
                    h.XCoordinate,
                    h.YCoordinate,
                    h.Title ?? h.Product.Name,
                    h.DisplayOrder,
                    h.IsActive,
                    isAvailable,
                    unavailableReason
                );
            })
            .ToList();

        return new RoomSceneDetailDto(
            scene.Id,
            scene.Title,
            scene.Slug,
            scene.Description,
            scene.ImageUrl,
            scene.RoomType,
            scene.DisplayOrder,
            scene.IsActive,
            hotspotDtos,
            scene.CreatedAtUtc,
            scene.UpdatedAtUtc
        );
    }
}

