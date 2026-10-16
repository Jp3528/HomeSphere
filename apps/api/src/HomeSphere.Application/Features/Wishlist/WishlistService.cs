using HomeSphere.Application.Common.Interfaces;
using HomeSphere.Domain.Entities;
using HomeSphere.Domain.Enums;
using HomeSphere.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace HomeSphere.Application.Features.Wishlist;

public class WishlistService : IWishlistService
{
    private readonly IApplicationDbContext _context;

    public WishlistService(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<WishlistItemDto>> GetUserWishlistAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var items = await _context.WishlistItems
            .AsNoTracking()
            .Where(w => w.UserId == userId)
            .Include(w => w.Product)
                .ThenInclude(p => p.Images)
            .Include(w => w.Product)
                .ThenInclude(p => p.Category)
            .Include(w => w.Product)
                .ThenInclude(p => p.Variants)
            .OrderByDescending(w => w.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        return items.Select(MapToDto).ToList();
    }

    public async Task<WishlistItemDto> AddToWishlistAsync(Guid userId, Guid productId, CancellationToken cancellationToken = default)
    {
        var product = await _context.Products
            .Include(p => p.Images)
            .Include(p => p.Category)
            .Include(p => p.Variants)
            .FirstOrDefaultAsync(p => p.Id == productId, cancellationToken);

        if (product == null)
        {
            throw new NotFoundException("Product", productId);
        }

        if (product.Status == ProductStatus.Archived)
        {
            throw new BusinessRuleValidationException("No se puede agregar a la lista de deseos un producto archivado.");
        }

        var existing = await _context.WishlistItems
            .FirstOrDefaultAsync(w => w.UserId == userId && w.ProductId == productId, cancellationToken);

        if (existing != null)
        {
            existing.Product = product;
            return MapToDto(existing);
        }

        var wishlistItem = new WishlistItem
        {
            UserId = userId,
            ProductId = productId,
            CreatedAtUtc = DateTime.UtcNow,
            Product = product
        };

        _context.WishlistItems.Add(wishlistItem);
        await _context.SaveChangesAsync(cancellationToken);

        return MapToDto(wishlistItem);
    }

    public async Task<bool> RemoveFromWishlistAsync(Guid userId, Guid productId, CancellationToken cancellationToken = default)
    {
        var item = await _context.WishlistItems
            .FirstOrDefaultAsync(w => w.UserId == userId && w.ProductId == productId, cancellationToken);

        if (item == null)
        {
            return false;
        }

        _context.WishlistItems.Remove(item);
        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> IsInWishlistAsync(Guid userId, Guid productId, CancellationToken cancellationToken = default)
    {
        return await _context.WishlistItems
            .AnyAsync(w => w.UserId == userId && w.ProductId == productId, cancellationToken);
    }

    private static WishlistItemDto MapToDto(WishlistItem item)
    {
        var product = item.Product;
        var primaryImage = product.Images?.OrderBy(i => i.DisplayOrder).FirstOrDefault()?.Url;
        var hasStock = product.Variants?.Any(v => v.IsActive && v.StockQuantity > 0) ?? false;
        var isAvailable = product.Status == ProductStatus.Active && hasStock;

        return new WishlistItemDto(
            Id: item.Id,
            ProductId: product.Id,
            ProductName: product.Name,
            ProductSlug: product.Slug,
            BasePrice: product.BasePrice,
            PrimaryImageUrl: primaryImage,
            CategoryName: product.Category?.Name ?? string.Empty,
            IsAvailable: isAvailable,
            CreatedAtUtc: item.CreatedAtUtc
        );
    }
}
