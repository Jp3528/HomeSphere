using HomeSphere.Application.Features.Orders;
using HomeSphere.Application.Features.Orders.DTOs;
using HomeSphere.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HomeSphere.WebApi.Controllers;

[ApiController]
[Route("api/admin/orders")]
[Authorize(Roles = "ADMIN")]
[Produces("application/json")]
public class AdminOrdersController : ControllerBase
{
    private readonly IOrderService _orderService;

    public AdminOrdersController(IOrderService orderService)
    {
        _orderService = orderService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<OrderSummaryDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllOrders(
        [FromQuery] string? status = null,
        CancellationToken cancellationToken = default)
    {
        var orders = await _orderService.GetAllOrdersAsync(status, cancellationToken);
        return Ok(orders);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetOrderById(Guid id, CancellationToken cancellationToken)
    {
        var order = await _orderService.GetOrderByIdAsync(id, null, isAdmin: true, cancellationToken);
        return Ok(order);
    }

    [HttpPatch("{id:guid}/status")]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateOrderStatus(
        Guid id,
        [FromBody] UpdateOrderStatusRequest request,
        CancellationToken cancellationToken)
    {
        if (!Enum.TryParse<OrderStatus>(request.Status, true, out var newStatus))
        {
            return BadRequest(new { message = $"El estado '{request.Status}' no es un estado válido de pedido." });
        }

        var order = await _orderService.UpdateOrderStatusAsync(id, newStatus, "Admin", request.Reason, cancellationToken);
        return Ok(order);
    }

    [HttpPost("cleanup-expired")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> CleanupExpiredReservations(CancellationToken cancellationToken)
    {
        var count = await _orderService.CleanupExpiredReservationsAsync(cancellationToken);
        return Ok(new { message = $"Se liberaron {count} órdenes con reservas expiradas." });
    }
}
