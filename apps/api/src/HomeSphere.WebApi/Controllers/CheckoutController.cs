using HomeSphere.Application.Common.Interfaces;
using HomeSphere.Application.Features.Checkout;
using HomeSphere.Application.Features.Checkout.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace HomeSphere.WebApi.Controllers;

[ApiController]
[Route("api/checkout")]
[Produces("application/json")]
public class CheckoutController : ControllerBase
{
    private const string CartTokenHeader = "X-Cart-Token";
    private const string CartTokenCookie = "homesphere_cart_token";

    private readonly ICheckoutService _checkoutService;
    private readonly ICurrentUserService _currentUserService;

    public CheckoutController(ICheckoutService checkoutService, ICurrentUserService currentUserService)
    {
        _checkoutService = checkoutService;
        _currentUserService = currentUserService;
    }

    [HttpPost("preview")]
    [ProducesResponseType(typeof(CheckoutPreviewResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetCheckoutPreview(
        [FromBody] CheckoutPreviewRequest request,
        CancellationToken cancellationToken)
    {
        var (userId, guestToken) = ResolveUserOrGuest();
        var preview = await _checkoutService.GetCheckoutPreviewAsync(userId, guestToken, request, cancellationToken);
        return Ok(preview);
    }

    [HttpGet("coupons/validate")]
    [ProducesResponseType(typeof(ValidateCouponResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> ValidateCoupon(
        [FromQuery] string code,
        [FromQuery] decimal subtotal = 0m,
        CancellationToken cancellationToken = default)
    {
        var response = await _checkoutService.ValidateCouponAsync(code, subtotal, cancellationToken);
        return Ok(response);
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
