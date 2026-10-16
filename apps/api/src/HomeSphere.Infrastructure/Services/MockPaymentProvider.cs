using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using HomeSphere.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace HomeSphere.Infrastructure.Services;

public class MockPaymentProvider : IPaymentProvider
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<MockPaymentProvider> _logger;
    private readonly string _webhookSecret;

    public MockPaymentProvider(IConfiguration configuration, ILogger<MockPaymentProvider> _logger)
    {
        _configuration = configuration;
        this._logger = _logger;
        _webhookSecret = _configuration["Payment:WebhookSecret"] ?? "homesphere-default-webhook-mock-secret-key-32chars!";
    }

    public string ProviderName => "Mock";

    public Task<PaymentInitiationResult> InitiatePaymentAsync(
        PaymentInitiationRequest request,
        CancellationToken cancellationToken = default)
    {
        var transactionId = $"mock_txn_{Guid.NewGuid():N}";
        var mockUrl = $"/checkout/mock-payment?orderId={request.OrderId}&orderNumber={Uri.EscapeDataString(request.OrderNumber)}&amount={request.Amount:F2}&txn={transactionId}";

        _logger.LogInformation("Initiated mock payment session for Order {OrderNumber} ({OrderId}), Txn: {TxnId}",
            request.OrderNumber, request.OrderId, transactionId);

        return Task.FromResult(new PaymentInitiationResult(
            Success: true,
            TransactionId: transactionId,
            PaymentUrl: mockUrl,
            ClientSecret: $"secret_{Guid.NewGuid():N}",
            ErrorMessage: null
        ));
    }

    public Task<PaymentWebhookResult> ProcessWebhookAsync(
        string payload,
        string signatureHeader,
        CancellationToken cancellationToken = default)
    {
        // 1. Verify HMAC Signature
        if (string.IsNullOrWhiteSpace(signatureHeader))
        {
            _logger.LogWarning("Missing signature header in payment webhook");
            return Task.FromResult(new PaymentWebhookResult(
                IsValidSignature: false,
                Handled: false,
                EventType: null,
                TransactionId: null,
                OrderId: null,
                OrderNumber: null,
                Amount: 0m,
                IsSuccess: false,
                ErrorMessage: "Missing signature header"
            ));
        }

        var expectedSignature = ComputeHmacSignature(payload, _webhookSecret);
        if (!CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(signatureHeader.Trim()),
            Encoding.UTF8.GetBytes(expectedSignature)))
        {
            _logger.LogWarning("Invalid HMAC signature received in payment webhook");
            return Task.FromResult(new PaymentWebhookResult(
                IsValidSignature: false,
                Handled: false,
                EventType: null,
                TransactionId: null,
                OrderId: null,
                OrderNumber: null,
                Amount: 0m,
                IsSuccess: false,
                ErrorMessage: "Invalid HMAC signature"
            ));
        }

        // 2. Parse payload JSON
        try
        {
            using var doc = JsonDocument.Parse(payload);
            var root = doc.RootElement;

            var eventType = root.TryGetProperty("eventType", out var ev) ? ev.GetString() : "payment.succeeded";
            var transactionId = root.TryGetProperty("transactionId", out var tx) ? tx.GetString() : $"mock_txn_{Guid.NewGuid():N}";
            
            Guid? orderId = null;
            if (root.TryGetProperty("orderId", out var ord) && Guid.TryParse(ord.GetString(), out var parsedOrderId))
            {
                orderId = parsedOrderId;
            }

            var orderNumber = root.TryGetProperty("orderNumber", out var on) ? on.GetString() : null;
            var amount = root.TryGetProperty("amount", out var am) ? am.GetDecimal() : 0m;
            var status = root.TryGetProperty("status", out var st) ? st.GetString() : "succeeded";
            var isSuccess = string.Equals(status, "succeeded", StringComparison.OrdinalIgnoreCase);

            return Task.FromResult(new PaymentWebhookResult(
                IsValidSignature: true,
                Handled: true,
                EventType: eventType,
                TransactionId: transactionId,
                OrderId: orderId,
                OrderNumber: orderNumber,
                Amount: amount,
                IsSuccess: isSuccess,
                ErrorMessage: null
            ));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to parse webhook JSON payload");
            return Task.FromResult(new PaymentWebhookResult(
                IsValidSignature: true,
                Handled: false,
                EventType: null,
                TransactionId: null,
                OrderId: null,
                OrderNumber: null,
                Amount: 0m,
                IsSuccess: false,
                ErrorMessage: "Invalid payload JSON: " + ex.Message
            ));
        }
    }

    public static string ComputeHmacSignature(string payload, string secret)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
