using HomeSphere.Application.Common.Interfaces;
using HomeSphere.Application.Features.Orders;
using HomeSphere.Application.Features.Orders.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HomeSphere.WebApi.Controllers;

[ApiController]
[Route("api/orders")]
[Produces("application/json")]
public class OrdersController : ControllerBase
{
    private const string CartTokenHeader = "X-Cart-Token";
    private const string CartTokenCookie = "homesphere_cart_token";
    private const string IdempotencyHeader = "Idempotency-Key";

    private readonly IOrderService _orderService;
    private readonly ICurrentUserService _currentUserService;

    public OrdersController(IOrderService orderService, ICurrentUserService currentUserService)
    {
        _orderService = orderService;
        _currentUserService = currentUserService;
    }

    [HttpPost]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateOrder(
        [FromBody] CreateOrderRequest request,
        [FromHeader(Name = IdempotencyHeader)] string? idempotencyKey,
        CancellationToken cancellationToken)
    {
        var (userId, guestToken) = ResolveUserOrGuest();
        var order = await _orderService.CreateOrderAsync(request, userId, guestToken, idempotencyKey, cancellationToken);
        return CreatedAtAction(nameof(GetOrderByPublicId), new { publicId = order.PublicId }, order);
    }

    [HttpGet("{publicId}")]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetOrderByPublicId(
        string publicId,
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        var isAdmin = _currentUserService.IsInRole("ADMIN");
        var order = await _orderService.GetOrderByPublicIdAsync(publicId, userId, isAdmin, cancellationToken);
        return Ok(order);
    }

    [HttpGet("my-orders")]
    [Authorize]
    [ProducesResponseType(typeof(IReadOnlyList<OrderSummaryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMyOrders(CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId!.Value;
        var orders = await _orderService.GetUserOrdersAsync(userId, cancellationToken);
        return Ok(orders);
    }

    [HttpPost("{id:guid}/cancel")]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CancelOrder(
        Guid id,
        [FromBody] CancelOrderRequest? request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        var isAdmin = _currentUserService.IsInRole("ADMIN");
        var order = await _orderService.CancelOrderAsync(id, userId, isAdmin, request?.Reason, cancellationToken);
        return Ok(order);
    }

    [HttpPost("{id:guid}/pay")]
    [ProducesResponseType(typeof(PaymentInitiationResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> PayOrder(
        Guid id,
        [FromQuery] string returnUrl = "/checkout/success",
        [FromQuery] string cancelUrl = "/checkout/cancel",
        CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.UserId;
        var result = await _orderService.InitiateOrderPaymentAsync(id, userId, returnUrl, cancelUrl, cancellationToken);
        return Ok(result);
    }

    private (Guid? userId, string? guestToken) ResolveUserOrGuest()
    {
        var userId = _currentUserService.UserId;
        if (userId.HasValue)
        {
            return (userId, null);
        }

        string? guestToken = null;
        if (Request.Headers.TryGetValue(CartTokenHeader, out var headerValues) && !string.IsNullOrWhiteSpace(headerValues))
        {
            guestToken = headerValues.ToString().Trim();
        }
        else if (Request.Cookies.TryGetValue(CartTokenCookie, out var cookieValue) && !string.IsNullOrWhiteSpace(cookieValue))
        {
            guestToken = cookieValue.Trim();
        }

        return (null, guestToken);
    }
}
