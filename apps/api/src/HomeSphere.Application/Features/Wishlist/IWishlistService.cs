namespace HomeSphere.Application.Features.Wishlist;

public interface IWishlistService
{
    Task<IReadOnlyList<WishlistItemDto>> GetUserWishlistAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<WishlistItemDto> AddToWishlistAsync(Guid userId, Guid productId, CancellationToken cancellationToken = default);
    Task<bool> RemoveFromWishlistAsync(Guid userId, Guid productId, CancellationToken cancellationToken = default);
    Task<bool> IsInWishlistAsync(Guid userId, Guid productId, CancellationToken cancellationToken = default);
}
