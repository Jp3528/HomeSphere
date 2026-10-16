using HomeSphere.Application.Common.Interfaces;
using HomeSphere.Application.Features.Wishlist;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HomeSphere.WebApi.Controllers;

[ApiController]
[Authorize]
[Route("api/wishlist")]
[Produces("application/json")]
public class WishlistController : ControllerBase
{
    private readonly IWishlistService _wishlistService;
    private readonly ICurrentUserService _currentUserService;

    public WishlistController(IWishlistService wishlistService, ICurrentUserService currentUserService)
    {
        _wishlistService = wishlistService;
        _currentUserService = currentUserService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<WishlistItemDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMyWishlist(CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (userId == null)
        {
            return Unauthorized();
        }

        var items = await _wishlistService.GetUserWishlistAsync(userId.Value, cancellationToken);
        return Ok(items);
    }

    [HttpPost("{productId:guid}")]
    [ProducesResponseType(typeof(WishlistItemDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddToWishlist(Guid productId, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (userId == null)
        {
            return Unauthorized();
        }

        var item = await _wishlistService.AddToWishlistAsync(userId.Value, productId, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, item);
    }

    [HttpDelete("{productId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> RemoveFromWishlist(Guid productId, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (userId == null)
        {
            return Unauthorized();
        }

        await _wishlistService.RemoveFromWishlistAsync(userId.Value, productId, cancellationToken);
        return NoContent();
    }

    [HttpGet("check/{productId:guid}")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> CheckWishlistStatus(Guid productId, CancellationToken cancellationToken)
    {
        var userId = _currentUserService.UserId;
        if (userId == null)
        {
            return Unauthorized();
        }

        var isInWishlist = await _wishlistService.IsInWishlistAsync(userId.Value, productId, cancellationToken);
        return Ok(new { isInWishlist });
    }
}
