using HomeSphere.Application.Common.Interfaces;
using HomeSphere.Application.Features.Catalog.DTOs;
using HomeSphere.Domain.Entities;
using HomeSphere.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace HomeSphere.Application.Features.Catalog;

public interface IBrandService
{
    Task<IReadOnlyList<BrandDto>> GetBrandsAsync(bool onlyActive = true, CancellationToken cancellationToken = default);
    Task<BrandDto> GetBrandBySlugAsync(string slug, CancellationToken cancellationToken = default);
    Task<BrandDto> GetBrandByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<BrandDto> CreateBrandAsync(CreateBrandRequest request, CancellationToken cancellationToken = default);
    Task<BrandDto> UpdateBrandAsync(Guid id, UpdateBrandRequest request, CancellationToken cancellationToken = default);
    Task DeleteBrandAsync(Guid id, CancellationToken cancellationToken = default);
}

public class BrandService : IBrandService
{
    private readonly IApplicationDbContext _context;

    public BrandService(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<BrandDto>> GetBrandsAsync(bool onlyActive = true, CancellationToken cancellationToken = default)
    {
        var query = _context.Brands
            .AsNoTracking()
            .Include(b => b.Products)
            .AsQueryable();

        if (onlyActive)
        {
            query = query.Where(b => b.IsActive);
        }

        return await query
            .OrderBy(b => b.Name)
            .Select(b => new BrandDto(
                b.Id,
                b.Name,
                b.Slug,
                b.Description,
                b.LogoUrl,
                b.IsActive,
                b.Products.Count
            ))
            .ToListAsync(cancellationToken);
    }

    public async Task<BrandDto> GetBrandBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        var brand = await _context.Brands
            .AsNoTracking()
            .Include(b => b.Products)
            .FirstOrDefaultAsync(b => b.Slug == slug.ToLowerInvariant(), cancellationToken);

        if (brand == null)
        {
            throw new NotFoundException("Brand", slug);
        }

        return new BrandDto(
            brand.Id,
            brand.Name,
            brand.Slug,
            brand.Description,
            brand.LogoUrl,
            brand.IsActive,
            brand.Products.Count
        );
    }

    public async Task<BrandDto> GetBrandByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var brand = await _context.Brands
            .AsNoTracking()
            .Include(b => b.Products)
            .FirstOrDefaultAsync(b => b.Id == id, cancellationToken);

        if (brand == null)
        {
            throw new NotFoundException("Brand", id);
        }

        return new BrandDto(
            brand.Id,
            brand.Name,
            brand.Slug,
            brand.Description,
            brand.LogoUrl,
            brand.IsActive,
            brand.Products.Count
        );
    }

    public async Task<BrandDto> CreateBrandAsync(CreateBrandRequest request, CancellationToken cancellationToken = default)
    {
        var normalizedSlug = request.Slug.Trim().ToLowerInvariant();

        if (await _context.Brands.AnyAsync(b => b.Slug == normalizedSlug, cancellationToken))
        {
            throw new ConflictException($"Brand with slug '{normalizedSlug}' already exists.");
        }

        var brand = new Brand
        {
            Name = request.Name.Trim(),
            Slug = normalizedSlug,
            Description = request.Description?.Trim(),
            LogoUrl = request.LogoUrl?.Trim(),
            IsActive = request.IsActive,
            CreatedAtUtc = DateTime.UtcNow
        };

        _context.Brands.Add(brand);
        await _context.SaveChangesAsync(cancellationToken);

        return new BrandDto(brand.Id, brand.Name, brand.Slug, brand.Description, brand.LogoUrl, brand.IsActive, 0);
    }

    public async Task<BrandDto> UpdateBrandAsync(Guid id, UpdateBrandRequest request, CancellationToken cancellationToken = default)
    {
        var brand = await _context.Brands.FirstOrDefaultAsync(b => b.Id == id, cancellationToken);

        if (brand == null)
        {
            throw new NotFoundException("Brand", id);
        }

        var normalizedSlug = request.Slug.Trim().ToLowerInvariant();
        if (brand.Slug != normalizedSlug && await _context.Brands.AnyAsync(b => b.Slug == normalizedSlug && b.Id != id, cancellationToken))
        {
            throw new ConflictException($"Brand with slug '{normalizedSlug}' already exists.");
        }

        brand.Name = request.Name.Trim();
        brand.Slug = normalizedSlug;
        brand.Description = request.Description?.Trim();
        brand.LogoUrl = request.LogoUrl?.Trim();
        brand.IsActive = request.IsActive;
        brand.UpdatedAtUtc = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        var productCount = await _context.Products.CountAsync(p => p.BrandId == id, cancellationToken);
        return new BrandDto(brand.Id, brand.Name, brand.Slug, brand.Description, brand.LogoUrl, brand.IsActive, productCount);
    }

    public async Task DeleteBrandAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var brand = await _context.Brands
            .Include(b => b.Products)
            .FirstOrDefaultAsync(b => b.Id == id, cancellationToken);

        if (brand == null)
        {
            throw new NotFoundException("Brand", id);
        }

        if (brand.Products.Any())
        {
            throw new BusinessRuleValidationException("Cannot delete a brand that has associated products.");
        }

        _context.Brands.Remove(brand);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
