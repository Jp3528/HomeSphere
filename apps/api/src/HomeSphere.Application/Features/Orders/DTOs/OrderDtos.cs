using System.ComponentModel.DataAnnotations;
using HomeSphere.Application.Features.Addresses.DTOs;

namespace HomeSphere.Application.Features.Orders.DTOs;

public record CreateOrderRequest(
    Guid? ShippingAddressId,
    CreateAddressRequest? GuestShippingAddress,
    string ShippingMethod = "standard",
    string? CouponCode = null,
    string? CustomerNotes = null,
    [EmailAddress] string? GuestEmail = null
);

public record OrderDto(
    Guid Id,
    string OrderNumber,
    string PublicId,
    Guid? UserId,
    string? GuestEmail,
    string Status,
    decimal Subtotal,
    decimal DiscountTotal,
    decimal ShippingCost,
    decimal Total,
    string? CouponCode,
    string ShippingMethod,
    string ShippingAddressSnapshotJson,
    string? CustomerNotes,
    DateTime? PaidAtUtc,
    DateTime? ShippedAtUtc,
    DateTime? DeliveredAtUtc,
    DateTime? CancelledAtUtc,
    string? CancellationReason,
    DateTime CreatedAtUtc,
    IReadOnlyList<OrderItemDto> Items,
    IReadOnlyList<OrderStatusHistoryDto> StatusHistory
);

public record OrderItemDto(
    Guid Id,
    Guid ProductVariantId,
    string SkuSnapshot,
    string ProductNameSnapshot,
    decimal UnitPrice,
    int Quantity,
    decimal TotalPrice,
    string? AttributesJsonSnapshot,
    string? ImageUrlSnapshot
);

public record OrderStatusHistoryDto(
    string PreviousStatus,
    string NewStatus,
    string ChangedBy,
    string? Reason,
    DateTime CreatedAtUtc
);

public record OrderSummaryDto(
    Guid Id,
    string OrderNumber,
    string PublicId,
    string Status,
    decimal Total,
    int ItemsCount,
    DateTime CreatedAtUtc
);

public record UpdateOrderStatusRequest(
    [Required] string Status,
    string? Reason = null
);

public record CancelOrderRequest(
    string? Reason = "Cancelled by user"
);
