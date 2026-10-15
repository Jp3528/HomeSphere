using System.ComponentModel.DataAnnotations;
using HomeSphere.Domain.Enums;

namespace HomeSphere.Application.Features.Catalog.DTOs;

public record CategoryDto(
    Guid Id,
    string Name,
    string Slug,
    string? Description,
    string? ImageUrl,
    Guid? ParentId,
    int DisplayOrder,
    bool IsActive,
    IReadOnlyList<CategoryDto>? SubCategories,
    int ProductCount = 0
);

public record BrandDto(
    Guid Id,
    string Name,
    string Slug,
    string? Description,
    string? LogoUrl,
    bool IsActive,
    int ProductCount = 0
);

public record ProductVariantDto(
    Guid Id,
    Guid ProductId,
    string Sku,
    string Name,
    decimal Price,
    int StockQuantity,
    string? Barcode,
    bool IsActive,
    string? AttributesJson
);

public record ProductImageDto(
    Guid Id,
    Guid ProductId,
    string Url,
    string? AltText,
    int DisplayOrder,
    bool IsPrimary
);

public record ProductListDto(
    Guid Id,
    string Name,
    string Slug,
    string ShortDescription,
    decimal BasePrice,
    ProductStatus Status,
    bool IsFeatured,
    Guid CategoryId,
    string CategoryName,
    string CategorySlug,
    Guid BrandId,
    string BrandName,
    string BrandSlug,
    string? PrimaryImageUrl,
    int VariantCount,
    int TotalStock,
    DateTime CreatedAtUtc
);

public record ProductDetailDto(
    Guid Id,
    string Name,
    string Slug,
    string ShortDescription,
    string Description,
    decimal BasePrice,
    ProductStatus Status,
    bool IsFeatured,
    Guid CategoryId,
    string CategoryName,
    string CategorySlug,
    Guid BrandId,
    string BrandName,
    string BrandSlug,
    IReadOnlyList<ProductVariantDto> Variants,
    IReadOnlyList<ProductImageDto> Images,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc
);

public record CategoryFilterItemDto(string Slug, string Name, int Count);
public record BrandFilterItemDto(string Slug, string Name, int Count);

public record ProductFiltersDto(
    decimal MinPrice,
    decimal MaxPrice,
    IReadOnlyList<CategoryFilterItemDto> Categories,
    IReadOnlyList<BrandFilterItemDto> Brands
);

public record ProductQueryParams(
    string? Search = null,
    string? CategorySlug = null,
    string? BrandSlug = null,
    decimal? MinPrice = null,
    decimal? MaxPrice = null,
    string? Sort = null, // "price_asc", "price_desc", "name_asc", "newest", "featured"
    ProductStatus? Status = null,
    int Page = 1,
    int PageSize = 12
);

// Admin Requests
public record CreateCategoryRequest(
    [Required, StringLength(128)] string Name,
    [Required, StringLength(160)] string Slug,
    [StringLength(1000)] string? Description,
    [StringLength(512)] string? ImageUrl,
    Guid? ParentId,
    int DisplayOrder = 0,
    bool IsActive = true
);

public record UpdateCategoryRequest(
    [Required, StringLength(128)] string Name,
    [Required, StringLength(160)] string Slug,
    [StringLength(1000)] string? Description,
    [StringLength(512)] string? ImageUrl,
    Guid? ParentId,
    int DisplayOrder = 0,
    bool IsActive = true
);

public record CreateBrandRequest(
    [Required, StringLength(128)] string Name,
    [Required, StringLength(160)] string Slug,
    [StringLength(1000)] string? Description,
    [StringLength(512)] string? LogoUrl,
    bool IsActive = true
);

public record UpdateBrandRequest(
    [Required, StringLength(128)] string Name,
    [Required, StringLength(160)] string Slug,
    [StringLength(1000)] string? Description,
    [StringLength(512)] string? LogoUrl,
    bool IsActive = true
);

public record CreateProductVariantRequest(
    [Required, StringLength(64)] string Sku,
    [Required, StringLength(256)] string Name,
    [Range(0, 1000000)] decimal Price,
    [Range(0, 100000)] int StockQuantity,
    [StringLength(64)] string? Barcode,
    string? AttributesJson,
    bool IsActive = true
);

public record UpdateProductVariantRequest(
    [Required, StringLength(64)] string Sku,
    [Required, StringLength(256)] string Name,
    [Range(0, 1000000)] decimal Price,
    [Range(0, 100000)] int StockQuantity,
    [StringLength(64)] string? Barcode,
    string? AttributesJson,
    bool IsActive = true
);

public record CreateProductImageRequest(
    [Required, StringLength(1024)] string Url,
    [StringLength(256)] string? AltText,
    int DisplayOrder = 0,
    bool IsPrimary = false
);

public record CreateProductRequest(
    [Required, StringLength(256)] string Name,
    [Required, StringLength(280)] string Slug,
    [Required, StringLength(500)] string ShortDescription,
    [Required] string Description,
    [Range(0, 1000000)] decimal BasePrice,
    ProductStatus Status,
    bool IsFeatured,
    Guid CategoryId,
    Guid BrandId,
    List<CreateProductVariantRequest>? Variants,
    List<CreateProductImageRequest>? Images
);

public record UpdateProductRequest(
    [Required, StringLength(256)] string Name,
    [Required, StringLength(280)] string Slug,
    [Required, StringLength(500)] string ShortDescription,
    [Required] string Description,
    [Range(0, 1000000)] decimal BasePrice,
    ProductStatus Status,
    bool IsFeatured,
    Guid CategoryId,
    Guid BrandId
);
