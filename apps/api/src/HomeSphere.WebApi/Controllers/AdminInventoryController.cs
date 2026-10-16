using HomeSphere.Application.Common.Interfaces;
using HomeSphere.Application.Features.Inventory;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HomeSphere.WebApi.Controllers;

[ApiController]
[Authorize(Roles = "ADMIN,WAREHOUSE,MANAGER,Admin,Warehouse,Manager")]
[Route("api/admin/inventory")]
[Produces("application/json")]
public class AdminInventoryController : ControllerBase
{
    private readonly IInventoryService _inventoryService;
    private readonly ICurrentUserService _currentUserService;

    public AdminInventoryController(
        IInventoryService inventoryService,
        ICurrentUserService currentUserService)
    {
        _inventoryService = inventoryService;
        _currentUserService = currentUserService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<InventoryItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetInventory(
        [FromQuery] string? search,
        [FromQuery] bool? lowStockOnly,
        CancellationToken cancellationToken)
    {
        var items = await _inventoryService.GetInventoryItemsAsync(search, lowStockOnly, cancellationToken);
        return Ok(items);
    }

    [HttpGet("movements")]
    [ProducesResponseType(typeof(IReadOnlyList<InventoryMovementDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetMovements(
        [FromQuery] Guid? variantId,
        [FromQuery] int limit = 50,
        CancellationToken cancellationToken = default)
    {
        var movements = await _inventoryService.GetMovementsAsync(variantId, limit, cancellationToken);
        return Ok(movements);
    }

    [HttpPost("adjust")]
    [ProducesResponseType(typeof(InventoryMovementDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AdjustStock(
        [FromBody] AdjustStockRequest request,
        CancellationToken cancellationToken)
    {
        var actorEmail = _currentUserService.Email ?? "admin";
        var movement = await _inventoryService.AdjustStockAsync(request, actorEmail, cancellationToken);
        return Ok(movement);
    }
}
