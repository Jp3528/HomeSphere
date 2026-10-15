using HomeSphere.Application.Common.Models;
using HomeSphere.Application.Features.Catalog;
using HomeSphere.Application.Features.Catalog.DTOs;
using HomeSphere.Domain.Entities;
using HomeSphere.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HomeSphere.WebApi.Controllers;

[ApiController]
[Route("api/admin/products")]
[Authorize(Roles = $"{Role.Admin},{Role.Manager}")]
public class AdminProductsController : ControllerBase
{
    private readonly IProductService _productService;

    public AdminProductsController(IProductService productService)
    {
        _productService = productService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedList<ProductListDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProducts(
        [FromQuery] ProductQueryParams queryParams,
        CancellationToken cancellationToken)
    {
        var products = await _productService.GetProductsAsync(queryParams, cancellationToken);
        return Ok(products);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ProductDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var product = await _productService.GetProductByIdAsync(id, cancellationToken);
        return Ok(product);
    }

    [HttpPost]
    [ProducesResponseType(typeof(ProductDetailDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(
        [FromBody] CreateProductRequest request,
        CancellationToken cancellationToken)
    {
        var created = await _productService.CreateProductAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ProductDetailDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateProductRequest request,
        CancellationToken cancellationToken)
    {
        var updated = await _productService.UpdateProductAsync(id, request, cancellationToken);
        return Ok(updated);
    }

    [HttpPatch("{id:guid}/status")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateStatus(
        Guid id,
        [FromBody] ProductStatus status,
        CancellationToken cancellationToken)
    {
        await _productService.UpdateProductStatusAsync(id, status, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/variants")]
    [ProducesResponseType(typeof(ProductVariantDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AddVariant(
        Guid id,
        [FromBody] CreateProductVariantRequest request,
        CancellationToken cancellationToken)
    {
        var variant = await _productService.AddVariantAsync(id, request, cancellationToken);
        return Created(string.Empty, variant);
    }

    [HttpPut("{id:guid}/variants/{variantId:guid}")]
    [ProducesResponseType(typeof(ProductVariantDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateVariant(
        Guid id,
        Guid variantId,
        [FromBody] UpdateProductVariantRequest request,
        CancellationToken cancellationToken)
    {
        var variant = await _productService.UpdateVariantAsync(id, variantId, request, cancellationToken);
        return Ok(variant);
    }

    [HttpDelete("{id:guid}/variants/{variantId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteVariant(
        Guid id,
        Guid variantId,
        CancellationToken cancellationToken)
    {
        await _productService.DeleteVariantAsync(id, variantId, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id:guid}/images")]
    [ProducesResponseType(typeof(ProductImageDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddImage(
        Guid id,
        [FromBody] CreateProductImageRequest request,
        CancellationToken cancellationToken)
    {
        var image = await _productService.AddImageAsync(id, request, cancellationToken);
        return Created(string.Empty, image);
    }

    [HttpDelete("{id:guid}/images/{imageId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteImage(
        Guid id,
        Guid imageId,
        CancellationToken cancellationToken)
    {
        await _productService.DeleteImageAsync(id, imageId, cancellationToken);
        return NoContent();
    }

    [HttpPatch("{id:guid}/images/{imageId:guid}/primary")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> SetPrimaryImage(
        Guid id,
        Guid imageId,
        CancellationToken cancellationToken)
    {
        await _productService.SetPrimaryImageAsync(id, imageId, cancellationToken);
        return NoContent();
    }
}
