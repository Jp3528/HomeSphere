using HomeSphere.Application.Common.Models;
using HomeSphere.Application.Features.Catalog;
using HomeSphere.Application.Features.Catalog.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace HomeSphere.WebApi.Controllers;

[ApiController]
[Route("api/products")]
public class ProductsController : ControllerBase
{
    private readonly IProductService _productService;

    public ProductsController(IProductService productService)
    {
        _productService = productService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedList<ProductListDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProducts(
        [FromQuery] ProductQueryParams queryParams,
        CancellationToken cancellationToken)
    {
        // Public endpoint only returns Active products
        var publicParams = queryParams with { Status = Domain.Enums.ProductStatus.Active };
        var products = await _productService.GetProductsAsync(publicParams, cancellationToken);
        return Ok(products);
    }

    [HttpGet("filters")]
    [ProducesResponseType(typeof(ProductFiltersDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetFilters(CancellationToken cancellationToken)
    {
        var filters = await _productService.GetCatalogFiltersAsync(cancellationToken);
        return Ok(filters);
    }

    [HttpGet("{slug}")]
    [ProducesResponseType(typeof(ProductDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBySlug(
        string slug,
        CancellationToken cancellationToken)
    {
        var product = await _productService.GetProductBySlugAsync(slug, onlyActive: true, cancellationToken);
        return Ok(product);
    }
}
