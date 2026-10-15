using HomeSphere.Application.Common.Interfaces;
using HomeSphere.Application.Features.Catalog.DTOs;
using HomeSphere.Domain.Entities;
using HomeSphere.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace HomeSphere.Application.Features.Catalog;

public interface ICategoryService
{
    Task<IReadOnlyList<CategoryDto>> GetCategoryTreeAsync(bool onlyActive = true, CancellationToken cancellationToken = default);
    Task<CategoryDto> GetCategoryBySlugAsync(string slug, CancellationToken cancellationToken = default);
    Task<CategoryDto> GetCategoryByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<CategoryDto> CreateCategoryAsync(CreateCategoryRequest request, CancellationToken cancellationToken = default);
    Task<CategoryDto> UpdateCategoryAsync(Guid id, UpdateCategoryRequest request, CancellationToken cancellationToken = default);
    Task DeleteCategoryAsync(Guid id, CancellationToken cancellationToken = default);
}

public class CategoryService : ICategoryService
{
    private readonly IApplicationDbContext _context;

    public CategoryService(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<CategoryDto>> GetCategoryTreeAsync(bool onlyActive = true, CancellationToken cancellationToken = default)
    {
        var query = _context.Categories
            .AsNoTracking()
            .Include(c => c.Products)
            .Include(c => c.SubCategories)
                .ThenInclude(sc => sc.Products)
            .AsQueryable();

        if (onlyActive)
        {
            query = query.Where(c => c.IsActive);
        }

        var allCategories = await query
            .OrderBy(c => c.DisplayOrder)
            .ThenBy(c => c.Name)
            .ToListAsync(cancellationToken);

        // Root categories are those with ParentId == null
        var rootCategories = allCategories
            .Where(c => c.ParentId == null)
            .Select(c => MapToDtoWithSubs(c, allCategories, onlyActive))
            .ToList();

        return rootCategories;
    }

    public async Task<CategoryDto> GetCategoryBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        var category = await _context.Categories
            .AsNoTracking()
            .Include(c => c.Products)
            .Include(c => c.SubCategories)
            .FirstOrDefaultAsync(c => c.Slug == slug.ToLowerInvariant(), cancellationToken);

        if (category == null)
        {
            throw new NotFoundException("Category", slug);
        }

        var allCategories = await _context.Categories.AsNoTracking().ToListAsync(cancellationToken);
        return MapToDtoWithSubs(category, allCategories, false);
    }

    public async Task<CategoryDto> GetCategoryByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var category = await _context.Categories
            .AsNoTracking()
            .Include(c => c.Products)
            .Include(c => c.SubCategories)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        if (category == null)
        {
            throw new NotFoundException("Category", id);
        }

        var allCategories = await _context.Categories.AsNoTracking().ToListAsync(cancellationToken);
        return MapToDtoWithSubs(category, allCategories, false);
    }

    public async Task<CategoryDto> CreateCategoryAsync(CreateCategoryRequest request, CancellationToken cancellationToken = default)
    {
        var normalizedSlug = request.Slug.Trim().ToLowerInvariant();

        if (await _context.Categories.AnyAsync(c => c.Slug == normalizedSlug, cancellationToken))
        {
            throw new ConflictException($"Category with slug '{normalizedSlug}' already exists.");
        }

        if (request.ParentId.HasValue && !await _context.Categories.AnyAsync(c => c.Id == request.ParentId.Value, cancellationToken))
        {
            throw new NotFoundException("Parent Category", request.ParentId.Value);
        }

        var category = new Category
        {
            Name = request.Name.Trim(),
            Slug = normalizedSlug,
            Description = request.Description?.Trim(),
            ImageUrl = request.ImageUrl?.Trim(),
            ParentId = request.ParentId,
            DisplayOrder = request.DisplayOrder,
            IsActive = request.IsActive,
            CreatedAtUtc = DateTime.UtcNow
        };

        _context.Categories.Add(category);
        await _context.SaveChangesAsync(cancellationToken);

        return new CategoryDto(
            category.Id,
            category.Name,
            category.Slug,
            category.Description,
            category.ImageUrl,
            category.ParentId,
            category.DisplayOrder,
            category.IsActive,
            null,
            0
        );
    }

    public async Task<CategoryDto> UpdateCategoryAsync(Guid id, UpdateCategoryRequest request, CancellationToken cancellationToken = default)
    {
        var category = await _context.Categories.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        if (category == null)
        {
            throw new NotFoundException("Category", id);
        }

        var normalizedSlug = request.Slug.Trim().ToLowerInvariant();
        if (category.Slug != normalizedSlug && await _context.Categories.AnyAsync(c => c.Slug == normalizedSlug && c.Id != id, cancellationToken))
        {
            throw new ConflictException($"Category with slug '{normalizedSlug}' already exists.");
        }

        if (request.ParentId == id)
        {
            throw new BusinessRuleValidationException("A category cannot be its own parent.");
        }

        if (request.ParentId.HasValue && !await _context.Categories.AnyAsync(c => c.Id == request.ParentId.Value, cancellationToken))
        {
            throw new NotFoundException("Parent Category", request.ParentId.Value);
        }

        category.Name = request.Name.Trim();
        category.Slug = normalizedSlug;
        category.Description = request.Description?.Trim();
        category.ImageUrl = request.ImageUrl?.Trim();
        category.ParentId = request.ParentId;
        category.DisplayOrder = request.DisplayOrder;
        category.IsActive = request.IsActive;
        category.UpdatedAtUtc = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        var allCategories = await _context.Categories.AsNoTracking().ToListAsync(cancellationToken);
        return MapToDtoWithSubs(category, allCategories, false);
    }

    public async Task DeleteCategoryAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var category = await _context.Categories
            .Include(c => c.SubCategories)
            .Include(c => c.Products)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        if (category == null)
        {
            throw new NotFoundException("Category", id);
        }

        if (category.SubCategories.Any())
        {
            throw new BusinessRuleValidationException("Cannot delete a category that contains subcategories.");
        }

        if (category.Products.Any())
        {
            throw new BusinessRuleValidationException("Cannot delete a category that contains products.");
        }

        _context.Categories.Remove(category);
        await _context.SaveChangesAsync(cancellationToken);
    }

    private static CategoryDto MapToDtoWithSubs(Category category, List<Category> allCategories, bool onlyActive)
    {
        var subCats = allCategories
            .Where(c => c.ParentId == category.Id && (!onlyActive || c.IsActive))
            .OrderBy(c => c.DisplayOrder)
            .ThenBy(c => c.Name)
            .Select(c => MapToDtoWithSubs(c, allCategories, onlyActive))
            .ToList();

        var productCount = category.Products?.Count ?? 0;

        return new CategoryDto(
            category.Id,
            category.Name,
            category.Slug,
            category.Description,
            category.ImageUrl,
            category.ParentId,
            category.DisplayOrder,
            category.IsActive,
            subCats,
            productCount
        );
    }
}
