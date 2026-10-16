namespace HomeSphere.Application.Common.Interfaces;

public record PaymentInitiationRequest(
    Guid OrderId,
    string OrderNumber,
    decimal Amount,
    string Currency,
    string? CustomerEmail,
    string? ReturnUrl,
    string? CancelUrl
);

public record PaymentInitiationResult(
    bool Success,
    string TransactionId,
    string? PaymentUrl,
    string? ClientSecret,
    string? ErrorMessage
);

public record PaymentWebhookResult(
    bool IsValidSignature,
    bool Handled,
    string? EventType,
    string? TransactionId,
    Guid? OrderId,
    string? OrderNumber,
    decimal Amount,
    bool IsSuccess,
    string? ErrorMessage
);

public interface IPaymentProvider
{
    string ProviderName { get; }
    Task<PaymentInitiationResult> InitiatePaymentAsync(PaymentInitiationRequest request, CancellationToken cancellationToken = default);
    Task<PaymentWebhookResult> ProcessWebhookAsync(string payload, string signatureHeader, CancellationToken cancellationToken = default);
}
