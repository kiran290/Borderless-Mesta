using Microsoft.AspNetCore.Mvc;
using Payments.Api.Dtos;
using Payments.Core.Enums;
using Payments.Core.Exceptions;
using Payments.Core.Interfaces;
using Payments.Core.Models;
using Payments.Core.Models.Requests;

namespace Payments.Api.Controllers;

/// <summary>
/// Controller for stablecoin to fiat payout operations.
/// </summary>
[ApiController]
[Route("api/v1/payouts")]
[Produces("application/json")]
public sealed class PayoutsController : ControllerBase
{
    private readonly IPayoutService _payoutService;
    private readonly ILogger<PayoutsController> _logger;

    public PayoutsController(
        IPayoutService payoutService,
        ILogger<PayoutsController> logger)
    {
        _payoutService = payoutService;
        _logger = logger;
    }

    /// <summary>
    /// Creates a new stablecoin to fiat payout.
    /// </summary>
    /// <param name="request">Payout request details.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Created payout details including deposit wallet.</returns>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<PayoutResponseDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreatePayout(
        [FromBody] PayoutRequestDto request,
        CancellationToken cancellationToken)
    {
        var requestId = HttpContext.TraceIdentifier;

        _logger.LogInformation(
            "Creating payout: {SourceAmount} {SourceCurrency} -> {TargetCurrency} [RequestId: {RequestId}]",
            request.SourceAmount ?? request.TargetAmount,
            request.SourceCurrency,
            request.TargetCurrency,
            requestId);

        var payoutRequest = MapToCreatePayoutRequest(request);
        var result = await _payoutService.CreatePayoutAsync(payoutRequest, cancellationToken);

        if (!result.Success || result.Payout == null)
        {
            _logger.LogWarning(
                "Payout creation failed: {ErrorCode} - {ErrorMessage} [RequestId: {RequestId}]",
                result.ErrorCode,
                result.ErrorMessage,
                requestId);

            return BadRequest(ApiResponse<object>.Fail(
                result.ErrorCode ?? "PAYOUT_FAILED",
                result.ErrorMessage ?? "Failed to create payout",
                requestId));
        }

        var response = PayoutResponseDto.FromModel(result.Payout);

        _logger.LogInformation(
            "Payout created: {PayoutId} [RequestId: {RequestId}]",
            result.Payout.Id,
            requestId);

        return CreatedAtAction(
            nameof(GetPayout),
            new { id = result.Payout.Id },
            ApiResponse<PayoutResponseDto>.Ok(response, requestId));
    }

    /// <summary>
    /// Gets a payout by ID.
    /// </summary>
    /// <param name="id">Payout ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Payout details.</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<PayoutResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPayout(string id, CancellationToken cancellationToken)
    {
        var requestId = HttpContext.TraceIdentifier;

        _logger.LogInformation("Getting payout: {PayoutId} [RequestId: {RequestId}]", id, requestId);

        var statusResult = await _payoutService.GetPayoutStatusAsync(id, cancellationToken);

        if (!statusResult.Success || statusResult.StatusUpdate == null)
        {
            return NotFound(ApiResponse<object>.Fail(
                "PAYOUT_NOT_FOUND",
                $"Payout '{id}' was not found",
                requestId));
        }

        // Get full payout history to retrieve the payout
        var payouts = await _payoutService.GetPayoutHistoryAsync(cancellationToken: cancellationToken);
        var payout = payouts.FirstOrDefault(p => p.Id == id);

        if (payout == null)
        {
            return NotFound(ApiResponse<object>.Fail(
                "PAYOUT_NOT_FOUND",
                $"Payout '{id}' was not found",
                requestId));
        }

        var response = PayoutResponseDto.FromModel(payout);
        return Ok(ApiResponse<PayoutResponseDto>.Ok(response, requestId));
    }

    /// <summary>
    /// Gets the current status of a payout.
    /// </summary>
    /// <param name="id">Payout ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Current payout status.</returns>
    [HttpGet("{id}/status")]
    [ProducesResponseType(typeof(ApiResponse<PayoutStatusResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPayoutStatus(string id, CancellationToken cancellationToken)
    {
        var requestId = HttpContext.TraceIdentifier;

        _logger.LogInformation("Getting payout status: {PayoutId} [RequestId: {RequestId}]", id, requestId);

        var result = await _payoutService.GetPayoutStatusAsync(id, cancellationToken);

        if (!result.Success || result.StatusUpdate == null)
        {
            return NotFound(ApiResponse<object>.Fail(
                result.ErrorCode ?? "PAYOUT_NOT_FOUND",
                result.ErrorMessage ?? $"Payout '{id}' was not found",
                requestId));
        }

        var response = PayoutStatusResponseDto.FromModel(result.StatusUpdate);
        return Ok(ApiResponse<PayoutStatusResponseDto>.Ok(response, requestId));
    }

    /// <summary>
    /// Gets the deposit wallet for a payout.
    /// </summary>
    /// <param name="id">Payout ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Deposit wallet details.</returns>
    [HttpGet("{id}/wallet")]
    [ProducesResponseType(typeof(ApiResponse<DepositWalletDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDepositWallet(string id, CancellationToken cancellationToken)
    {
        var requestId = HttpContext.TraceIdentifier;

        _logger.LogInformation("Getting deposit wallet: {PayoutId} [RequestId: {RequestId}]", id, requestId);

        var wallet = await _payoutService.GetDepositWalletAsync(id, cancellationToken);
        var response = DepositWalletDto.FromModel(wallet);

        return Ok(ApiResponse<DepositWalletDto>.Ok(response, requestId));
    }

    /// <summary>
    /// Cancels a payout.
    /// </summary>
    /// <param name="id">Payout ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Cancellation result.</returns>
    [HttpPost("{id}/cancel")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CancelPayout(string id, CancellationToken cancellationToken)
    {
        var requestId = HttpContext.TraceIdentifier;

        _logger.LogInformation("Cancelling payout: {PayoutId} [RequestId: {RequestId}]", id, requestId);

        var cancelled = await _payoutService.CancelPayoutAsync(id, cancellationToken);

        if (!cancelled)
        {
            return BadRequest(ApiResponse<object>.Fail(
                "CANCELLATION_FAILED",
                "Failed to cancel payout. It may be in a non-cancellable state.",
                requestId));
        }

        return Ok(ApiResponse<bool>.Ok(true, requestId));
    }

    /// <summary>
    /// Gets payout history with optional filters.
    /// </summary>
    /// <param name="senderId">Filter by sender ID.</param>
    /// <param name="beneficiaryId">Filter by beneficiary ID.</param>
    /// <param name="fromDate">Filter by start date.</param>
    /// <param name="toDate">Filter by end date.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of payouts matching the filters.</returns>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<PayoutResponseDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPayoutHistory(
        [FromQuery] string? senderId = null,
        [FromQuery] string? beneficiaryId = null,
        [FromQuery] DateTimeOffset? fromDate = null,
        [FromQuery] DateTimeOffset? toDate = null,
        CancellationToken cancellationToken = default)
    {
        var requestId = HttpContext.TraceIdentifier;

        _logger.LogInformation(
            "Getting payout history: SenderId={SenderId}, BeneficiaryId={BeneficiaryId} [RequestId: {RequestId}]",
            senderId,
            beneficiaryId,
            requestId);

        var payouts = await _payoutService.GetPayoutHistoryAsync(
            senderId,
            beneficiaryId,
            fromDate,
            toDate,
            cancellationToken);

        var response = payouts.Select(PayoutResponseDto.FromModel).ToList();
        return Ok(ApiResponse<IReadOnlyList<PayoutResponseDto>>.Ok(response, requestId));
    }

    private static CreatePayoutRequest MapToCreatePayoutRequest(PayoutRequestDto dto)
    {
        return new CreatePayoutRequest
        {
            ExternalId = dto.ExternalId,
            QuoteId = dto.QuoteId,
            SourceCurrency = dto.SourceCurrency,
            TargetCurrency = dto.TargetCurrency,
            SourceAmount = dto.SourceAmount,
            TargetAmount = dto.TargetAmount,
            Network = dto.Network,
            Sender = MapToSender(dto.Sender),
            Beneficiary = MapToBeneficiary(dto.Beneficiary),
            PaymentMethod = dto.PaymentMethod,
            DeveloperFee = dto.DeveloperFee,
            Metadata = dto.Metadata,
            PreferredProvider = dto.PreferredProvider
        };
    }

    private static Sender MapToSender(SenderDto dto)
    {
        return new Sender
        {
            Id = dto.Id,
            ExternalId = dto.ExternalId,
            Type = dto.Type,
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            BusinessName = dto.BusinessName,
            Email = dto.Email,
            PhoneNumber = dto.PhoneNumber,
            DateOfBirth = dto.DateOfBirth,
            Address = dto.Address != null ? MapToAddress(dto.Address) : null,
            DocumentType = dto.DocumentType,
            DocumentNumber = dto.DocumentNumber,
            Nationality = dto.Nationality
        };
    }

    private static Beneficiary MapToBeneficiary(BeneficiaryDto dto)
    {
        return new Beneficiary
        {
            Id = dto.Id,
            ExternalId = dto.ExternalId,
            Type = dto.Type,
            FirstName = dto.FirstName,
            LastName = dto.LastName,
            BusinessName = dto.BusinessName,
            Email = dto.Email,
            PhoneNumber = dto.PhoneNumber,
            DateOfBirth = dto.DateOfBirth,
            Address = dto.Address != null ? MapToAddress(dto.Address) : null,
            BankAccount = MapToBankAccount(dto.BankAccount),
            DocumentType = dto.DocumentType,
            DocumentNumber = dto.DocumentNumber,
            Nationality = dto.Nationality
        };
    }

    private static Address MapToAddress(AddressDto dto)
    {
        return new Address
        {
            Street1 = dto.Street1,
            Street2 = dto.Street2,
            City = dto.City,
            State = dto.State,
            PostalCode = dto.PostalCode,
            CountryCode = dto.CountryCode
        };
    }

    private static BankAccount MapToBankAccount(BankAccountDto dto)
    {
        return new BankAccount
        {
            BankName = dto.BankName,
            AccountNumber = dto.AccountNumber,
            AccountHolderName = dto.AccountHolderName,
            RoutingNumber = dto.RoutingNumber,
            SwiftCode = dto.SwiftCode,
            SortCode = dto.SortCode,
            Iban = dto.Iban,
            Currency = dto.Currency,
            CountryCode = dto.CountryCode,
            BranchCode = dto.BranchCode
        };
    }
}
