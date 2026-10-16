namespace HomeSphere.Application.Features.Wishlist;

public record WishlistItemDto(
    Guid Id,
    Guid ProductId,
    string ProductName,
    string ProductSlug,
    decimal BasePrice,
    string? PrimaryImageUrl,
    string CategoryName,
    bool IsAvailable,
    DateTime CreatedAtUtc
);
