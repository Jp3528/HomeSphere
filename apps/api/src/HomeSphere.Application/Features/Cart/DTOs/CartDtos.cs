using System.ComponentModel.DataAnnotations;

namespace HomeSphere.Application.Features.Cart.DTOs;

public record CartItemDto(
    Guid Id,
    Guid ProductVariantId,
    string Sku,
    string ProductName,
    string ProductSlug,
    string VariantName,
    decimal UnitPrice,
    int Quantity,
    decimal LineTotal,
    string? PrimaryImageUrl,
    string? AttributesJson,
    int AvailableStock,
    bool IsAvailable,
    string? Warning
);

public record CartDto(
    Guid Id,
    Guid? UserId,
    string? GuestToken,
    IReadOnlyList<CartItemDto> Items,
    int TotalItems,
    decimal Subtotal,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc
);

public record AddToCartRequest(
    [Required] Guid VariantId,
    [Range(1, 99)] int Quantity = 1
);

public record UpdateCartItemRequest(
    [Range(0, 99)] int Quantity
);

public record MergeCartRequest(
    [Required] string GuestToken
);
