using Microsoft.AspNetCore.Mvc;
using Payments.Api.Dtos;
using Payments.Core.Interfaces;
using Payments.Core.Models.Requests;

namespace Payments.Api.Controllers;

/// <summary>
/// Controller for payout quote operations.
/// </summary>
[ApiController]
[Route("api/v1/quotes")]
[Produces("application/json")]
public sealed class QuotesController : ControllerBase
{
    private readonly IPayoutService _payoutService;
    private readonly ILogger<QuotesController> _logger;

    public QuotesController(
        IPayoutService payoutService,
        ILogger<QuotesController> logger)
    {
        _payoutService = payoutService;
        _logger = logger;
    }

    /// <summary>
    /// Creates a new payout quote.
    /// </summary>
    /// <param name="request">Quote request details.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Quote with exchange rate and fee information.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<QuoteResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateQuote(
        [FromBody] QuoteRequestDto request,
        CancellationToken cancellationToken)
    {
        var requestId = HttpContext.TraceIdentifier;

        _logger.LogInformation(
            "Creating quote: {SourceCurrency} -> {TargetCurrency}, Provider: {Provider} [RequestId: {RequestId}]",
            request.SourceCurrency,
            request.TargetCurrency,
            request.Provider?.ToString() ?? "Auto",
            requestId);

        var quoteRequest = new CreateQuoteRequest
        {
            SourceCurrency = request.SourceCurrency,
            TargetCurrency = request.TargetCurrency,
            SourceAmount = request.SourceAmount,
            TargetAmount = request.TargetAmount,
            Network = request.Network,
            PaymentMethod = request.PaymentMethod,
            DestinationCountry = request.DestinationCountry,
            DeveloperFee = request.DeveloperFee
        };

        var result = await _payoutService.GetQuoteAsync(quoteRequest, request.Provider, cancellationToken);

        if (!result.Success || result.Quote == null)
        {
            _logger.LogWarning(
                "Quote creation failed: {ErrorCode} - {ErrorMessage} [RequestId: {RequestId}]",
                result.ErrorCode,
                result.ErrorMessage,
                requestId);

            return BadRequest(ApiResponse<object>.Fail(
                result.ErrorCode ?? "QUOTE_FAILED",
                result.ErrorMessage ?? "Failed to create quote",
                requestId));
        }

        var response = QuoteResponseDto.FromModel(result.Quote);

        _logger.LogInformation(
            "Quote created: {QuoteId}, Rate: {Rate}, Fee: {Fee} [RequestId: {RequestId}]",
            result.Quote.Id,
            result.Quote.ExchangeRate,
            result.Quote.FeeAmount,
            requestId);

        return Ok(ApiResponse<QuoteResponseDto>.Ok(response, requestId));
    }

    /// <summary>
    /// Gets quotes from all available providers for comparison.
    /// </summary>
    /// <param name="request">Quote request details.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of quotes from different providers.</returns>
    [HttpPost("compare")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<QuoteResponseDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CompareQuotes(
        [FromBody] QuoteRequestDto request,
        CancellationToken cancellationToken)
    {
        var requestId = HttpContext.TraceIdentifier;

        _logger.LogInformation(
            "Comparing quotes: {SourceCurrency} -> {TargetCurrency} [RequestId: {RequestId}]",
            request.SourceCurrency,
            request.TargetCurrency,
            requestId);

        var quoteRequest = new CreateQuoteRequest
        {
            SourceCurrency = request.SourceCurrency,
            TargetCurrency = request.TargetCurrency,
            SourceAmount = request.SourceAmount,
            TargetAmount = request.TargetAmount,
            Network = request.Network,
            PaymentMethod = request.PaymentMethod,
            DestinationCountry = request.DestinationCountry,
            DeveloperFee = request.DeveloperFee
        };

        var results = await _payoutService.GetAllQuotesAsync(quoteRequest, cancellationToken);

        var successfulQuotes = results
            .Where(r => r.Success && r.Quote != null)
            .Select(r => QuoteResponseDto.FromModel(r.Quote!))
            .OrderByDescending(q => q.TargetAmount) // Best rate first
            .ToList();

        _logger.LogInformation(
            "Quote comparison: {Count} successful quotes [RequestId: {RequestId}]",
            successfulQuotes.Count,
            requestId);

        return Ok(ApiResponse<IReadOnlyList<QuoteResponseDto>>.Ok(successfulQuotes, requestId));
    }
}
