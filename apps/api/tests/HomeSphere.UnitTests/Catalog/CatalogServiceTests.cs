using HomeSphere.Application.Features.Catalog;
using HomeSphere.Application.Features.Catalog.DTOs;
using HomeSphere.Domain.Entities;
using HomeSphere.Domain.Enums;
using HomeSphere.Domain.Exceptions;
using HomeSphere.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HomeSphere.UnitTests.Catalog;

public class CatalogServiceTests
{
    private readonly HomeSphereDbContext _dbContext;
    private readonly CategoryService _categoryService;
    private readonly BrandService _brandService;
    private readonly ProductService _productService;

    public CatalogServiceTests()
    {
        var options = new DbContextOptionsBuilder<HomeSphereDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _dbContext = new HomeSphereDbContext(options);
        _categoryService = new CategoryService(_dbContext);
        _brandService = new BrandService(_dbContext);
        _productService = new ProductService(_dbContext);
    }

    [Fact]
    public async Task CreateCategory_WhenSlugAlreadyExists_ShouldThrowConflictException()
    {
        // Arrange
        var request = new CreateCategoryRequest("Sala", "sala", "Desc", null, null);
        await _categoryService.CreateCategoryAsync(request);

        // Act & Assert
        var duplicate = new CreateCategoryRequest("Sala Renombrada", "sala", "Desc", null, null);
        await Assert.ThrowsAsync<ConflictException>(() => _categoryService.CreateCategoryAsync(duplicate));
    }

    [Fact]
    public async Task GetCategoryTree_ShouldReturnHierarchicalStructure()
    {
        // Arrange
        var root = await _categoryService.CreateCategoryAsync(new CreateCategoryRequest("Sala", "sala", null, null, null, 1));
        await _categoryService.CreateCategoryAsync(new CreateCategoryRequest("Sofás", "sofas", null, null, root.Id, 1));
        await _categoryService.CreateCategoryAsync(new CreateCategoryRequest("Mesas", "mesas", null, null, root.Id, 2));

        // Act
        var tree = await _categoryService.GetCategoryTreeAsync();

        // Assert
        Assert.Single(tree);
        var rootNode = tree[0];
        Assert.Equal("Sala", rootNode.Name);
        Assert.NotNull(rootNode.SubCategories);
        Assert.Equal(2, rootNode.SubCategories.Count);
    }

    [Fact]
    public async Task CreateProduct_WhenDuplicateSlug_ShouldThrowConflictException()
    {
        // Arrange
        var cat = await _categoryService.CreateCategoryAsync(new CreateCategoryRequest("Sala", "sala", null, null, null));
        var brand = await _brandService.CreateBrandAsync(new CreateBrandRequest("Nordic", "nordic", null, null));

        var req1 = new CreateProductRequest(
            "Mesa Nórdica",
            "mesa-nordica",
            "Corta",
            "Larga",
            499.00m,
            ProductStatus.Active,
            false,
            cat.Id,
            brand.Id,
            null,
            null
        );

        await _productService.CreateProductAsync(req1);

        // Act & Assert
        var req2 = req1 with { Name = "Otra Mesa" };
        await Assert.ThrowsAsync<ConflictException>(() => _productService.CreateProductAsync(req2));
    }

    [Fact]
    public async Task CreateProduct_WhenDuplicateVariantSku_ShouldThrowConflictException()
    {
        // Arrange
        var cat = await _categoryService.CreateCategoryAsync(new CreateCategoryRequest("Sala", "sala", null, null, null));
        var brand = await _brandService.CreateBrandAsync(new CreateBrandRequest("Nordic", "nordic", null, null));

        var req = new CreateProductRequest(
            "Sofá Tokio",
            "sofa-tokio",
            "Corta",
            "Larga",
            899.00m,
            ProductStatus.Active,
            false,
            cat.Id,
            brand.Id,
            new List<CreateProductVariantRequest>
            {
                new("SKU-TOKIO-1", "Tokio Gris", 899.00m, 10, null, null),
                new("SKU-TOKIO-1", "Tokio Beige", 899.00m, 5, null, null) // Duplicate SKU
            },
            null
        );

        // Act & Assert
        await Assert.ThrowsAsync<ConflictException>(() => _productService.CreateProductAsync(req));
    }

    [Fact]
    public async Task GetProducts_SortByPriceAsc_ShouldReturnAscendingOrder()
    {
        // Arrange
        var cat = await _categoryService.CreateCategoryAsync(new CreateCategoryRequest("Sala", "sala", null, null, null));
        var brand = await _brandService.CreateBrandAsync(new CreateBrandRequest("Nordic", "nordic", null, null));

        await _productService.CreateProductAsync(new CreateProductRequest(
            "Caro", "caro", "Desc", "Desc", 1200m, ProductStatus.Active, false, cat.Id, brand.Id, null, null));
        await _productService.CreateProductAsync(new CreateProductRequest(
            "Barato", "barato", "Desc", "Desc", 150m, ProductStatus.Active, false, cat.Id, brand.Id, null, null));
        await _productService.CreateProductAsync(new CreateProductRequest(
            "Medio", "medio", "Desc", "Desc", 600m, ProductStatus.Active, false, cat.Id, brand.Id, null, null));

        // Act
        var result = await _productService.GetProductsAsync(new ProductQueryParams(Sort: "price_asc"));

        // Assert
        Assert.Equal(3, result.TotalCount);
        Assert.Equal(150m, result.Items[0].BasePrice);
        Assert.Equal(600m, result.Items[1].BasePrice);
        Assert.Equal(1200m, result.Items[2].BasePrice);
    }
}
