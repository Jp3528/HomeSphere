using System.ComponentModel.DataAnnotations;

namespace HomeSphere.Application.Features.Checkout.DTOs;

public record CheckoutPreviewRequest(
    Guid? AddressId = null,
    string ShippingMethod = "standard",
    string? CouponCode = null
);

public record CheckoutItemPreviewDto(
    Guid VariantId,
    string Sku,
    string ProductName,
    string ProductSlug,
    string VariantName,
    decimal UnitPrice,
    int Quantity,
    decimal LineTotal,
    int AvailableStock,
    bool IsAvailable,
    string? WarningMessage,
    string? ErrorMessage
);

public record CouponSummaryDto(
    string Code,
    string Description,
    string DiscountType,
    decimal DiscountValue,
    decimal DiscountAmount
);

public record ShippingSummaryDto(
    string Code,
    string Name,
    decimal Cost,
    string EstimatedDelivery,
    bool IsFree
);

public record CheckoutPreviewResponse(
    Guid CartId,
    IReadOnlyList<CheckoutItemPreviewDto> Items,
    decimal Subtotal,
    decimal DiscountTotal,
    CouponSummaryDto? AppliedCoupon,
    ShippingSummaryDto Shipping,
    decimal Total,
    bool CanProceedToOrder,
    IReadOnlyList<string> ValidationErrors
);

public record ValidateCouponRequest(
    [Required] string Code,
    decimal Subtotal
);

public record ValidateCouponResponse(
    bool IsValid,
    string? Message,
    CouponSummaryDto? Coupon
);
