using HomeSphere.Application.Features.Catalog;
using HomeSphere.Application.Features.Catalog.DTOs;
using HomeSphere.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HomeSphere.WebApi.Controllers;

[ApiController]
[Route("api/brands")]
public class BrandsController : ControllerBase
{
    private readonly IBrandService _brandService;

    public BrandsController(IBrandService brandService)
    {
        _brandService = brandService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<BrandDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetBrands(
        [FromQuery] bool onlyActive = true,
        CancellationToken cancellationToken = default)
    {
        var brands = await _brandService.GetBrandsAsync(onlyActive, cancellationToken);
        return Ok(brands);
    }

    [HttpGet("{slug}")]
    [ProducesResponseType(typeof(BrandDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBySlug(string slug, CancellationToken cancellationToken)
    {
        var brand = await _brandService.GetBrandBySlugAsync(slug, cancellationToken);
        return Ok(brand);
    }

    [HttpPost]
    [Authorize(Roles = Role.Admin)]
    [ProducesResponseType(typeof(BrandDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(
        [FromBody] CreateBrandRequest request,
        CancellationToken cancellationToken)
    {
        var created = await _brandService.CreateBrandAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetBySlug), new { slug = created.Slug }, created);
    }

    [HttpPut("{id:guid}")]
    [Authorize(Roles = Role.Admin)]
    [ProducesResponseType(typeof(BrandDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateBrandRequest request,
        CancellationToken cancellationToken)
    {
        var updated = await _brandService.UpdateBrandAsync(id, request, cancellationToken);
        return Ok(updated);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Roles = Role.Admin)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _brandService.DeleteBrandAsync(id, cancellationToken);
        return NoContent();
    }
}
