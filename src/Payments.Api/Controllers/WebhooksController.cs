using Microsoft.AspNetCore.Mvc;
using Payments.Api.Dtos;
using Payments.Core.Enums;
using Payments.Core.Interfaces;

namespace Payments.Api.Controllers;

/// <summary>
/// Controller for handling webhooks from payout providers.
/// </summary>
[ApiController]
[Route("api/v1/webhooks")]
[Produces("application/json")]
public sealed class WebhooksController : ControllerBase
{
    private readonly IEnumerable<IWebhookHandler> _webhookHandlers;
    private readonly ILogger<WebhooksController> _logger;

    public WebhooksController(
        IEnumerable<IWebhookHandler> webhookHandlers,
        ILogger<WebhooksController> logger)
    {
        _webhookHandlers = webhookHandlers;
        _logger = logger;
    }

    /// <summary>
    /// Handles webhooks from Mesta provider.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Webhook processing result.</returns>
    [HttpPost("mesta")]
    [ProducesResponseType(typeof(ApiResponse<PayoutStatusResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> HandleMestaWebhook(CancellationToken cancellationToken)
    {
        return await HandleWebhook(PayoutProvider.Mesta, cancellationToken);
    }

    /// <summary>
    /// Handles webhooks from Borderless provider.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Webhook processing result.</returns>
    [HttpPost("borderless")]
    [ProducesResponseType(typeof(ApiResponse<PayoutStatusResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> HandleBorderlessWebhook(CancellationToken cancellationToken)
    {
        return await HandleWebhook(PayoutProvider.Borderless, cancellationToken);
    }

    private async Task<IActionResult> HandleWebhook(PayoutProvider provider, CancellationToken cancellationToken)
    {
        var requestId = HttpContext.TraceIdentifier;

        _logger.LogInformation(
            "Received webhook from {Provider} [RequestId: {RequestId}]",
            provider,
            requestId);

        // Find the appropriate handler
        var handler = _webhookHandlers.FirstOrDefault(h => h.Provider == provider);
        if (handler == null)
        {
            _logger.LogWarning(
                "No webhook handler found for provider {Provider} [RequestId: {RequestId}]",
                provider,
                requestId);

            return BadRequest(ApiResponse<object>.Fail(
                "HANDLER_NOT_FOUND",
                $"No webhook handler configured for provider '{provider}'",
                requestId));
        }

        // Read the raw payload
        using var reader = new StreamReader(Request.Body);
        var payload = await reader.ReadToEndAsync(cancellationToken);

        // Get signature from headers
        var signature = Request.Headers["X-Webhook-Signature"].FirstOrDefault()
            ?? Request.Headers["X-Signature"].FirstOrDefault()
            ?? Request.Headers["Webhook-Signature"].FirstOrDefault();

        // Validate signature if provided
        if (!string.IsNullOrEmpty(signature))
        {
            if (!handler.ValidateSignature(payload, signature))
            {
                _logger.LogWarning(
                    "Invalid webhook signature from {Provider} [RequestId: {RequestId}]",
                    provider,
                    requestId);

                return Unauthorized(ApiResponse<object>.Fail(
                    "INVALID_SIGNATURE",
                    "Invalid webhook signature",
                    requestId));
            }
        }
        else
        {
            _logger.LogWarning(
                "No webhook signature provided from {Provider} [RequestId: {RequestId}]",
                provider,
                requestId);
        }

        // Process the webhook
        var statusUpdate = await handler.ProcessWebhookAsync(payload, cancellationToken);

        if (statusUpdate == null)
        {
            _logger.LogWarning(
                "Failed to process webhook from {Provider} [RequestId: {RequestId}]",
                provider,
                requestId);

            return BadRequest(ApiResponse<object>.Fail(
                "PROCESSING_FAILED",
                "Failed to process webhook payload",
                requestId));
        }

        _logger.LogInformation(
            "Webhook processed: Provider={Provider}, PayoutId={PayoutId}, Status={Status} [RequestId: {RequestId}]",
            provider,
            statusUpdate.PayoutId,
            statusUpdate.CurrentStatus,
            requestId);

        var response = PayoutStatusResponseDto.FromModel(statusUpdate);
        return Ok(ApiResponse<PayoutStatusResponseDto>.Ok(response, requestId));
    }
}
