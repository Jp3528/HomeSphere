using HomeSphere.Application.Common.Interfaces;
using HomeSphere.Application.Common.Models;
using HomeSphere.Application.Features.Catalog.DTOs;
using HomeSphere.Domain.Entities;
using HomeSphere.Domain.Enums;
using HomeSphere.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace HomeSphere.Application.Features.Catalog;

public interface IProductService
{
    Task<PagedList<ProductListDto>> GetProductsAsync(ProductQueryParams queryParams, CancellationToken cancellationToken = default);
    Task<ProductDetailDto> GetProductBySlugAsync(string slug, bool onlyActive = true, CancellationToken cancellationToken = default);
    Task<ProductDetailDto> GetProductByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ProductDetailDto> CreateProductAsync(CreateProductRequest request, CancellationToken cancellationToken = default);
    Task<ProductDetailDto> UpdateProductAsync(Guid id, UpdateProductRequest request, CancellationToken cancellationToken = default);
    Task UpdateProductStatusAsync(Guid id, ProductStatus status, CancellationToken cancellationToken = default);
    Task<ProductVariantDto> AddVariantAsync(Guid productId, CreateProductVariantRequest request, CancellationToken cancellationToken = default);
    Task<ProductVariantDto> UpdateVariantAsync(Guid productId, Guid variantId, UpdateProductVariantRequest request, CancellationToken cancellationToken = default);
    Task DeleteVariantAsync(Guid productId, Guid variantId, CancellationToken cancellationToken = default);
    Task<ProductFiltersDto> GetCatalogFiltersAsync(CancellationToken cancellationToken = default);
    Task<ProductImageDto> AddImageAsync(Guid productId, CreateProductImageRequest request, CancellationToken cancellationToken = default);
    Task DeleteImageAsync(Guid productId, Guid imageId, CancellationToken cancellationToken = default);
    Task SetPrimaryImageAsync(Guid productId, Guid imageId, CancellationToken cancellationToken = default);
}

public class ProductService : IProductService
{
    private readonly IApplicationDbContext _context;

    public ProductService(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ProductFiltersDto> GetCatalogFiltersAsync(CancellationToken cancellationToken = default)
    {
        var activeProducts = _context.Products
            .AsNoTracking()
            .Where(p => p.Status == ProductStatus.Active);

        var minPrice = await activeProducts.AnyAsync(cancellationToken)
            ? await activeProducts.MinAsync(p => p.BasePrice, cancellationToken)
            : 0m;

        var maxPrice = await activeProducts.AnyAsync(cancellationToken)
            ? await activeProducts.MaxAsync(p => p.BasePrice, cancellationToken)
            : 0m;

        var categories = await _context.Categories
            .AsNoTracking()
            .Where(c => c.IsActive)
            .OrderBy(c => c.DisplayOrder)
            .ThenBy(c => c.Name)
            .Select(c => new CategoryFilterItemDto(
                c.Slug,
                c.Name,
                c.Products.Count(p => p.Status == ProductStatus.Active)
            ))
            .ToListAsync(cancellationToken);

        var brands = await _context.Brands
            .AsNoTracking()
            .Where(b => b.IsActive)
            .OrderBy(b => b.Name)
            .Select(b => new BrandFilterItemDto(
                b.Slug,
                b.Name,
                b.Products.Count(p => p.Status == ProductStatus.Active)
            ))
            .ToListAsync(cancellationToken);

        return new ProductFiltersDto(minPrice, maxPrice, categories, brands);
    }

    public async Task<PagedList<ProductListDto>> GetProductsAsync(ProductQueryParams queryParams, CancellationToken cancellationToken = default)
    {
        var query = _context.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.Brand)
            .Include(p => p.Variants)
            .Include(p => p.Images)
            .AsQueryable();

        // 1. Status Filter: Public queries default to Active
        if (queryParams.Status.HasValue)
        {
            query = query.Where(p => p.Status == queryParams.Status.Value);
        }
        else
        {
            query = query.Where(p => p.Status == ProductStatus.Active);
        }

        // 2. Search term (Name or ShortDescription)
        if (!string.IsNullOrWhiteSpace(queryParams.Search))
        {
            var search = queryParams.Search.Trim().ToLower();
            query = query.Where(p => p.Name.ToLower().Contains(search) ||
                                     p.ShortDescription.ToLower().Contains(search));
        }

        // 3. Category Filter (including subcategories if slug is root)
        if (!string.IsNullOrWhiteSpace(queryParams.CategorySlug))
        {
            var catSlug = queryParams.CategorySlug.Trim().ToLowerInvariant();
            var targetCategory = await _context.Categories
                .AsNoTracking()
                .Include(c => c.SubCategories)
                .FirstOrDefaultAsync(c => c.Slug == catSlug, cancellationToken);

            if (targetCategory != null)
            {
                var categoryIds = new List<Guid> { targetCategory.Id };
                if (targetCategory.SubCategories.Any())
                {
                    categoryIds.AddRange(targetCategory.SubCategories.Select(sc => sc.Id));
                }

                query = query.Where(p => categoryIds.Contains(p.CategoryId));
            }
        }

        // 4. Brand Filter
        if (!string.IsNullOrWhiteSpace(queryParams.BrandSlug))
        {
            var brandSlug = queryParams.BrandSlug.Trim().ToLowerInvariant();
            query = query.Where(p => p.Brand.Slug == brandSlug);
        }

        // 5. Price Filter
        if (queryParams.MinPrice.HasValue)
        {
            query = query.Where(p => p.BasePrice >= queryParams.MinPrice.Value);
        }

        if (queryParams.MaxPrice.HasValue)
        {
            query = query.Where(p => p.BasePrice <= queryParams.MaxPrice.Value);
        }

        // 6. Sorting
        query = queryParams.Sort?.ToLowerInvariant() switch
        {
            "price_asc" => query.OrderBy(p => p.BasePrice),
            "price_desc" => query.OrderByDescending(p => p.BasePrice),
            "name_asc" => query.OrderBy(p => p.Name),
            "newest" => query.OrderByDescending(p => p.CreatedAtUtc),
            "featured" => query.OrderByDescending(p => p.IsFeatured).ThenByDescending(p => p.CreatedAtUtc),
            _ => query.OrderByDescending(p => p.IsFeatured).ThenByDescending(p => p.CreatedAtUtc)
        };

        var projected = query.Select(p => new ProductListDto(
            p.Id,
            p.Name,
            p.Slug,
            p.ShortDescription,
            p.BasePrice,
            p.Status,
            p.IsFeatured,
            p.CategoryId,
            p.Category.Name,
            p.Category.Slug,
            p.BrandId,
            p.Brand.Name,
            p.Brand.Slug,
            p.Images.Where(i => i.IsPrimary).Select(i => i.Url).FirstOrDefault() ??
                p.Images.OrderBy(i => i.DisplayOrder).Select(i => i.Url).FirstOrDefault(),
            p.Variants.Count(v => v.IsActive),
            p.Variants.Where(v => v.IsActive).Sum(v => v.StockQuantity),
            p.CreatedAtUtc
        ));

        return await PagedList<ProductListDto>.CreateAsync(projected, queryParams.Page, queryParams.PageSize, cancellationToken);
    }

    public async Task<ProductDetailDto> GetProductBySlugAsync(string slug, bool onlyActive = true, CancellationToken cancellationToken = default)
    {
        var normalizedSlug = slug.Trim().ToLowerInvariant();

        var query = _context.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.Brand)
            .Include(p => p.Variants)
            .Include(p => p.Images)
            .Where(p => p.Slug == normalizedSlug);

        if (onlyActive)
        {
            query = query.Where(p => p.Status == ProductStatus.Active);
        }

        var product = await query.FirstOrDefaultAsync(cancellationToken);

        if (product == null)
        {
            throw new NotFoundException("Product", slug);
        }

        return MapToDetailDto(product);
    }

    public async Task<ProductDetailDto> GetProductByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var product = await _context.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.Brand)
            .Include(p => p.Variants)
            .Include(p => p.Images)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

        if (product == null)
        {
            throw new NotFoundException("Product", id);
        }

        return MapToDetailDto(product);
    }

    public async Task<ProductDetailDto> CreateProductAsync(CreateProductRequest request, CancellationToken cancellationToken = default)
    {
        var normalizedSlug = request.Slug.Trim().ToLowerInvariant();

        if (await _context.Products.AnyAsync(p => p.Slug == normalizedSlug, cancellationToken))
        {
            throw new ConflictException($"Product with slug '{normalizedSlug}' already exists.");
        }

        var category = await _context.Categories.FirstOrDefaultAsync(c => c.Id == request.CategoryId, cancellationToken);
        if (category == null)
        {
            throw new NotFoundException("Category", request.CategoryId);
        }

        var brand = await _context.Brands.FirstOrDefaultAsync(b => b.Id == request.BrandId, cancellationToken);
        if (brand == null)
        {
            throw new NotFoundException("Brand", request.BrandId);
        }

        var product = new Product
        {
            Name = request.Name.Trim(),
            Slug = normalizedSlug,
            ShortDescription = request.ShortDescription.Trim(),
            Description = request.Description.Trim(),
            BasePrice = request.BasePrice,
            Status = request.Status,
            IsFeatured = request.IsFeatured,
            CategoryId = request.CategoryId,
            BrandId = request.BrandId,
            CreatedAtUtc = DateTime.UtcNow
        };

        if (request.Variants != null && request.Variants.Any())
        {
            foreach (var v in request.Variants)
            {
                var normalizedSku = v.Sku.Trim().ToUpperInvariant();
                if (await _context.ProductVariants.AnyAsync(pv => pv.Sku == normalizedSku, cancellationToken) ||
                    product.Variants.Any(pv => pv.Sku == normalizedSku))
                {
                    throw new ConflictException($"Variant with SKU '{normalizedSku}' already exists.");
                }

                product.Variants.Add(new ProductVariant
                {
                    Sku = normalizedSku,
                    Name = v.Name.Trim(),
                    Price = v.Price,
                    StockQuantity = v.StockQuantity,
                    Barcode = v.Barcode?.Trim(),
                    AttributesJson = v.AttributesJson,
                    IsActive = v.IsActive,
                    CreatedAtUtc = DateTime.UtcNow
                });
            }
        }

        if (request.Images != null && request.Images.Any())
        {
            foreach (var img in request.Images)
            {
                product.Images.Add(new ProductImage
                {
                    Url = img.Url.Trim(),
                    AltText = img.AltText?.Trim(),
                    DisplayOrder = img.DisplayOrder,
                    IsPrimary = img.IsPrimary,
                    CreatedAtUtc = DateTime.UtcNow
                });
            }
        }

        _context.Products.Add(product);
        await _context.SaveChangesAsync(cancellationToken);

        return await GetProductByIdAsync(product.Id, cancellationToken);
    }

    public async Task<ProductDetailDto> UpdateProductAsync(Guid id, UpdateProductRequest request, CancellationToken cancellationToken = default)
    {
        var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (product == null)
        {
            throw new NotFoundException("Product", id);
        }

        var normalizedSlug = request.Slug.Trim().ToLowerInvariant();
        if (product.Slug != normalizedSlug && await _context.Products.AnyAsync(p => p.Slug == normalizedSlug && p.Id != id, cancellationToken))
        {
            throw new ConflictException($"Product with slug '{normalizedSlug}' already exists.");
        }

        if (!await _context.Categories.AnyAsync(c => c.Id == request.CategoryId, cancellationToken))
        {
            throw new NotFoundException("Category", request.CategoryId);
        }

        if (!await _context.Brands.AnyAsync(b => b.Id == request.BrandId, cancellationToken))
        {
            throw new NotFoundException("Brand", request.BrandId);
        }

        product.Name = request.Name.Trim();
        product.Slug = normalizedSlug;
        product.ShortDescription = request.ShortDescription.Trim();
        product.Description = request.Description.Trim();
        product.BasePrice = request.BasePrice;
        product.Status = request.Status;
        product.IsFeatured = request.IsFeatured;
        product.CategoryId = request.CategoryId;
        product.BrandId = request.BrandId;
        product.UpdatedAtUtc = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return await GetProductByIdAsync(id, cancellationToken);
    }

    public async Task UpdateProductStatusAsync(Guid id, ProductStatus status, CancellationToken cancellationToken = default)
    {
        var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (product == null)
        {
            throw new NotFoundException("Product", id);
        }

        product.Status = status;
        product.UpdatedAtUtc = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<ProductVariantDto> AddVariantAsync(Guid productId, CreateProductVariantRequest request, CancellationToken cancellationToken = default)
    {
        var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == productId, cancellationToken);
        if (product == null)
        {
            throw new NotFoundException("Product", productId);
        }

        var normalizedSku = request.Sku.Trim().ToUpperInvariant();
        if (await _context.ProductVariants.AnyAsync(v => v.Sku == normalizedSku, cancellationToken))
        {
            throw new ConflictException($"Variant with SKU '{normalizedSku}' already exists.");
        }

        var variant = new ProductVariant
        {
            ProductId = productId,
            Sku = normalizedSku,
            Name = request.Name.Trim(),
            Price = request.Price,
            StockQuantity = request.StockQuantity,
            Barcode = request.Barcode?.Trim(),
            AttributesJson = request.AttributesJson,
            IsActive = request.IsActive,
            CreatedAtUtc = DateTime.UtcNow
        };

        _context.ProductVariants.Add(variant);
        await _context.SaveChangesAsync(cancellationToken);

        return new ProductVariantDto(
            variant.Id,
            variant.ProductId,
            variant.Sku,
            variant.Name,
            variant.Price,
            variant.StockQuantity,
            variant.Barcode,
            variant.IsActive,
            variant.AttributesJson
        );
    }

    public async Task<ProductVariantDto> UpdateVariantAsync(Guid productId, Guid variantId, UpdateProductVariantRequest request, CancellationToken cancellationToken = default)
    {
        var variant = await _context.ProductVariants
            .FirstOrDefaultAsync(v => v.Id == variantId && v.ProductId == productId, cancellationToken);

        if (variant == null)
        {
            throw new NotFoundException("ProductVariant", variantId);
        }

        var normalizedSku = request.Sku.Trim().ToUpperInvariant();
        if (variant.Sku != normalizedSku && await _context.ProductVariants.AnyAsync(v => v.Sku == normalizedSku && v.Id != variantId, cancellationToken))
        {
            throw new ConflictException($"Variant with SKU '{normalizedSku}' already exists.");
        }

        variant.Sku = normalizedSku;
        variant.Name = request.Name.Trim();
        variant.Price = request.Price;
        variant.StockQuantity = request.StockQuantity;
        variant.Barcode = request.Barcode?.Trim();
        variant.AttributesJson = request.AttributesJson;
        variant.IsActive = request.IsActive;
        variant.UpdatedAtUtc = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return new ProductVariantDto(
            variant.Id,
            variant.ProductId,
            variant.Sku,
            variant.Name,
            variant.Price,
            variant.StockQuantity,
            variant.Barcode,
            variant.IsActive,
            variant.AttributesJson
        );
    }

    public async Task DeleteVariantAsync(Guid productId, Guid variantId, CancellationToken cancellationToken = default)
    {
        var variant = await _context.ProductVariants
            .FirstOrDefaultAsync(v => v.Id == variantId && v.ProductId == productId, cancellationToken);

        if (variant == null)
        {
            throw new NotFoundException("ProductVariant", variantId);
        }

        _context.ProductVariants.Remove(variant);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<ProductImageDto> AddImageAsync(Guid productId, CreateProductImageRequest request, CancellationToken cancellationToken = default)
    {
        var product = await _context.Products
            .Include(p => p.Images)
            .FirstOrDefaultAsync(p => p.Id == productId, cancellationToken);

        if (product == null)
        {
            throw new NotFoundException("Product", productId);
        }

        if (request.IsPrimary)
        {
            foreach (var img in product.Images)
            {
                img.IsPrimary = false;
            }
        }

        var image = new ProductImage
        {
            ProductId = productId,
            Url = request.Url.Trim(),
            AltText = request.AltText?.Trim(),
            DisplayOrder = request.DisplayOrder,
            IsPrimary = request.IsPrimary || !product.Images.Any(),
            CreatedAtUtc = DateTime.UtcNow
        };

        _context.ProductImages.Add(image);
        await _context.SaveChangesAsync(cancellationToken);

        return new ProductImageDto(image.Id, image.ProductId, image.Url, image.AltText, image.DisplayOrder, image.IsPrimary);
    }

    public async Task DeleteImageAsync(Guid productId, Guid imageId, CancellationToken cancellationToken = default)
    {
        var image = await _context.ProductImages
            .FirstOrDefaultAsync(i => i.Id == imageId && i.ProductId == productId, cancellationToken);

        if (image == null)
        {
            throw new NotFoundException("ProductImage", imageId);
        }

        _context.ProductImages.Remove(image);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task SetPrimaryImageAsync(Guid productId, Guid imageId, CancellationToken cancellationToken = default)
    {
        var images = await _context.ProductImages
            .Where(i => i.ProductId == productId)
            .ToListAsync(cancellationToken);

        var targetImage = images.FirstOrDefault(i => i.Id == imageId);
        if (targetImage == null)
        {
            throw new NotFoundException("ProductImage", imageId);
        }

        foreach (var img in images)
        {
            img.IsPrimary = (img.Id == imageId);
        }

        await _context.SaveChangesAsync(cancellationToken);
    }

    private static ProductDetailDto MapToDetailDto(Product product)
    {
        return new ProductDetailDto(
            product.Id,
            product.Name,
            product.Slug,
            product.ShortDescription,
            product.Description,
            product.BasePrice,
            product.Status,
            product.IsFeatured,
            product.CategoryId,
            product.Category?.Name ?? string.Empty,
            product.Category?.Slug ?? string.Empty,
            product.BrandId,
            product.Brand?.Name ?? string.Empty,
            product.Brand?.Slug ?? string.Empty,
            product.Variants.Select(v => new ProductVariantDto(
                v.Id,
                v.ProductId,
                v.Sku,
                v.Name,
                v.Price,
                v.StockQuantity,
                v.Barcode,
                v.IsActive,
                v.AttributesJson
            )).OrderBy(v => v.Price).ToList(),
            product.Images.Select(i => new ProductImageDto(
                i.Id,
                i.ProductId,
                i.Url,
                i.AltText,
                i.DisplayOrder,
                i.IsPrimary
            )).OrderByDescending(i => i.IsPrimary).ThenBy(i => i.DisplayOrder).ToList(),
            product.CreatedAtUtc,
            product.UpdatedAtUtc
        );
    }
}
