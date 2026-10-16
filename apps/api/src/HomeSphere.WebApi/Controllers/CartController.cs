using HomeSphere.Application.Common.Interfaces;
using HomeSphere.Application.Features.Cart;
using HomeSphere.Application.Features.Cart.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HomeSphere.WebApi.Controllers;

[ApiController]
[Route("api/cart")]
[Produces("application/json")]
public class CartController : ControllerBase
{
    private const string CartTokenHeader = "X-Cart-Token";
    private const string CartTokenCookie = "homesphere_cart_token";

    private readonly ICartService _cartService;
    private readonly ICurrentUserService _currentUserService;

    public CartController(ICartService cartService, ICurrentUserService currentUserService)
    {
        _cartService = cartService;
        _currentUserService = currentUserService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(CartDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCart(CancellationToken cancellationToken)
    {
        var (userId, guestToken) = ResolveUserOrGuest();
        var cart = await _cartService.GetCartAsync(userId, guestToken, cancellationToken);
        EnsureGuestTokenHeaderAndCookie(userId, guestToken);
        return Ok(cart);
    }

    [HttpPost("items")]
    [ProducesResponseType(typeof(CartDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddItem(
        [FromBody] AddToCartRequest request,
        CancellationToken cancellationToken)
    {
        var (userId, guestToken) = ResolveUserOrGuest();
        var cart = await _cartService.AddItemAsync(userId, guestToken, request, cancellationToken);
        EnsureGuestTokenHeaderAndCookie(userId, guestToken);
        return Ok(cart);
    }

    [HttpPut("items/{itemId:guid}")]
    [ProducesResponseType(typeof(CartDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateItemQuantity(
        Guid itemId,
        [FromBody] UpdateCartItemRequest request,
        CancellationToken cancellationToken)
    {
        var (userId, guestToken) = ResolveUserOrGuest();
        var cart = await _cartService.UpdateItemQuantityAsync(userId, guestToken, itemId, request.Quantity, cancellationToken);
        EnsureGuestTokenHeaderAndCookie(userId, guestToken);
        return Ok(cart);
    }

    [HttpDelete("items/{itemId:guid}")]
    [ProducesResponseType(typeof(CartDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveItem(
        Guid itemId,
        CancellationToken cancellationToken)
    {
        var (userId, guestToken) = ResolveUserOrGuest();
        var cart = await _cartService.RemoveItemAsync(userId, guestToken, itemId, cancellationToken);
        EnsureGuestTokenHeaderAndCookie(userId, guestToken);
        return Ok(cart);
    }

    [HttpDelete]
    [ProducesResponseType(typeof(CartDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> ClearCart(CancellationToken cancellationToken)
    {
        var (userId, guestToken) = ResolveUserOrGuest();
        var cart = await _cartService.ClearCartAsync(userId, guestToken, cancellationToken);
        EnsureGuestTokenHeaderAndCookie(userId, guestToken);
        return Ok(cart);
    }

    [Authorize]
    [HttpPost("merge")]
    [ProducesResponseType(typeof(CartDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> MergeCart(
        [FromBody] MergeCartRequest request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (!userId.HasValue)
        {
            return Unauthorized();
        }

        var cart = await _cartService.MergeCartAsync(userId.Value, request.GuestToken, cancellationToken);
        return Ok(cart);
    }

    private (Guid? userId, string? guestToken) ResolveUserOrGuest()
    {
        var userId = _currentUserService.UserId;
        if (userId.HasValue)
        {
            return (userId, null);
        }

        // Check header first, then cookie
        string? guestToken = null;
        if (Request.Headers.TryGetValue(CartTokenHeader, out var headerValues) && !string.IsNullOrWhiteSpace(headerValues))
        {
            guestToken = headerValues.ToString().Trim();
        }
        else if (Request.Cookies.TryGetValue(CartTokenCookie, out var cookieValue) && !string.IsNullOrWhiteSpace(cookieValue))
        {
            guestToken = cookieValue.Trim();
        }

        if (string.IsNullOrWhiteSpace(guestToken))
        {
            guestToken = Guid.NewGuid().ToString("N");
        }

        return (null, guestToken);
    }

    private void EnsureGuestTokenHeaderAndCookie(Guid? userId, string? guestToken)
    {
        if (userId.HasValue || string.IsNullOrWhiteSpace(guestToken))
        {
            return;
        }

        // Set response header
        Response.Headers[CartTokenHeader] = guestToken;

        // Set response cookie if not already set or changed
        if (!Request.Cookies.TryGetValue(CartTokenCookie, out var existingCookie) || existingCookie != guestToken)
        {
            Response.Cookies.Append(CartTokenCookie, guestToken, new CookieOptions
            {
                HttpOnly = false,
                SameSite = SameSiteMode.Lax,
                Secure = Request.IsHttps,
                Expires = DateTimeOffset.UtcNow.AddDays(30),
                Path = "/"
            });
        }
    }
}
