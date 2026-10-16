using HomeSphere.Application.Features.Orders;
using Microsoft.AspNetCore.Mvc;

namespace HomeSphere.WebApi.Controllers;

[ApiController]
[Route("api/webhooks")]
public class WebhooksController : ControllerBase
{
    private readonly IOrderService _orderService;
    private readonly ILogger<WebhooksController> _logger;

    public WebhooksController(IOrderService orderService, ILogger<WebhooksController> logger)
    {
        _orderService = orderService;
        _logger = logger;
    }

    [HttpPost("payments")]
    [Consumes("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> HandlePaymentWebhook(CancellationToken cancellationToken)
    {
        // Read raw string payload for cryptographic verification
        using var reader = new StreamReader(Request.Body);
        var rawPayload = await reader.ReadToEndAsync(cancellationToken);

        var signatureHeader = Request.Headers["X-Signature"].ToString();
        if (string.IsNullOrWhiteSpace(signatureHeader))
        {
            // Support alternative header names used by providers like Stripe
            signatureHeader = Request.Headers["Stripe-Signature"].ToString();
        }

        if (string.IsNullOrWhiteSpace(signatureHeader))
        {
            _logger.LogWarning("Webhook request received without cryptographic signature header");
            return BadRequest(new { message = "Falta el encabezado de firma criptográfica X-Signature." });
        }

        var handled = await _orderService.ProcessPaymentWebhookAsync(rawPayload, signatureHeader, cancellationToken);
        if (!handled)
        {
            return BadRequest(new { message = "Firma no válida o error al procesar el webhook." });
        }

        return Ok(new { status = "received", processed = true });
    }
}
