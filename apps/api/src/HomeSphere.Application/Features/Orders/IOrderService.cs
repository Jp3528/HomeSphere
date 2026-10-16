using HomeSphere.Application.Common.Interfaces;
using HomeSphere.Application.Features.Orders.DTOs;
using HomeSphere.Domain.Enums;

namespace HomeSphere.Application.Features.Orders;

public interface IOrderService
{
    Task<OrderDto> CreateOrderAsync(
        CreateOrderRequest request,
        Guid? userId,
        string? guestCartToken,
        string? idempotencyKey,
        CancellationToken cancellationToken = default);

    Task<OrderDto> GetOrderByPublicIdAsync(
        string publicId,
        Guid? userId,
        bool isAdmin,
        CancellationToken cancellationToken = default);

    Task<OrderDto> GetOrderByIdAsync(
        Guid orderId,
        Guid? userId,
        bool isAdmin,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OrderSummaryDto>> GetUserOrdersAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OrderSummaryDto>> GetAllOrdersAsync(
        string? status = null,
        CancellationToken cancellationToken = default);

    Task<OrderDto> UpdateOrderStatusAsync(
        Guid orderId,
        OrderStatus newStatus,
        string changedBy,
        string? reason = null,
        CancellationToken cancellationToken = default);

    Task<OrderDto> CancelOrderAsync(
        Guid orderId,
        Guid? userId,
        bool isAdmin,
        string? reason = null,
        CancellationToken cancellationToken = default);

    Task<PaymentInitiationResult> InitiateOrderPaymentAsync(
        Guid orderId,
        Guid? userId,
        string returnUrl,
        string cancelUrl,
        CancellationToken cancellationToken = default);

    Task<bool> ProcessPaymentWebhookAsync(
        string payload,
        string signatureHeader,
        CancellationToken cancellationToken = default);

    Task<int> CleanupExpiredReservationsAsync(
        CancellationToken cancellationToken = default);
}
