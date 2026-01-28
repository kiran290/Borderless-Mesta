using Microsoft.AspNetCore.Mvc;
using Payments.Api.Dtos;
using Payments.Core.Enums;
using Payments.Core.Models;
using Payments.Core.Models.Requests;
using Payments.Infrastructure.Services;

namespace Payments.Api.Controllers;

/// <summary>
/// Unified payment controller for all payment operations.
/// Provides a single entry point for customer management, KYC/KYB verification, and payouts.
/// </summary>
[ApiController]
[Route("api/v1")]
[Produces("application/json")]
public sealed class PaymentController : ControllerBase
{
    private readonly UnifiedPaymentService _paymentService;
    private readonly ApiVerificationService _verificationService;
    private readonly ILogger<PaymentController> _logger;

    public PaymentController(
        UnifiedPaymentService paymentService,
        ApiVerificationService verificationService,
        ILogger<PaymentController> logger)
    {
        _paymentService = paymentService;
        _verificationService = verificationService;
        _logger = logger;
    }

    #region Health & Verification

    /// <summary>
    /// Checks health of all configured providers.
    /// </summary>
    [HttpGet("providers/health")]
    [ProducesResponseType(typeof(ApiResponse<Dictionary<string, ProviderHealthDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProvidersHealth(CancellationToken cancellationToken)
    {
        var health = await _paymentService.CheckAllProvidersHealthAsync(cancellationToken);

        var response = health.ToDictionary(
            kvp => kvp.Key.ToString(),
            kvp => new ProviderHealthDto
            {
                IsHealthy = kvp.Value.IsHealthy,
                Status = kvp.Value.Status,
                Message = kvp.Value.Message,
                Latency = kvp.Value.Latency?.TotalMilliseconds
            });

        return Ok(ApiResponse<Dictionary<string, ProviderHealthDto>>.Success(response));
    }

    /// <summary>
    /// Verifies all configured providers by testing API connectivity and basic operations.
    /// </summary>
    [HttpPost("providers/verify")]
    [ProducesResponseType(typeof(ApiResponse<ProviderVerificationReport>), StatusCodes.Status200OK)]
    public async Task<IActionResult> VerifyProviders(CancellationToken cancellationToken)
    {
        var report = await _verificationService.VerifyAllProvidersAsync(cancellationToken);
        return Ok(ApiResponse<ProviderVerificationReport>.Success(report));
    }

    /// <summary>
    /// Verifies a specific provider by testing API connectivity and basic operations.
    /// </summary>
    [HttpPost("providers/{provider}/verify")]
    [ProducesResponseType(typeof(ApiResponse<SingleProviderReport>), StatusCodes.Status200OK)]
    public async Task<IActionResult> VerifyProvider(
        [FromRoute] PayoutProvider provider,
        CancellationToken cancellationToken)
    {
        var report = await _verificationService.VerifyProviderAsync(provider, cancellationToken);
        return Ok(ApiResponse<SingleProviderReport>.Success(report));
    }

    /// <summary>
    /// Gets list of available providers.
    /// </summary>
    [HttpGet("providers")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<string>>), StatusCodes.Status200OK)]
    public IActionResult GetProviders()
    {
        var providers = _paymentService.GetAvailableProviders().Select(p => p.ToString());
        return Ok(ApiResponse<IEnumerable<string>>.Success(providers));
    }

    #endregion

    #region Customer Operations

    /// <summary>
    /// Creates a new customer (individual or business).
    /// </summary>
    [HttpPost("customers")]
    [ProducesResponseType(typeof(ApiResponse<CustomerResponseDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateCustomer(
        [FromBody] CreateCustomerRequestDto request,
        CancellationToken cancellationToken)
    {
        var coreRequest = MapToCreateCustomerRequest(request);
        var result = await _paymentService.CreateCustomerAsync(coreRequest, cancellationToken);

        if (!result.Success)
        {
            return BadRequest(ApiResponse<object>.Failure(result.ErrorCode!, result.ErrorMessage!));
        }

        var response = MapToCustomerResponse(result.Customer!, result.Provider, result.ProviderCustomerId);
        return CreatedAtAction(
            nameof(GetCustomer),
            new { id = result.Customer!.Id },
            ApiResponse<CustomerResponseDto>.Success(response));
    }

    /// <summary>
    /// Gets a customer by ID.
    /// </summary>
    [HttpGet("customers/{id}")]
    [ProducesResponseType(typeof(ApiResponse<CustomerResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCustomer(
        [FromRoute] string id,
        [FromQuery] PayoutProvider? provider,
        CancellationToken cancellationToken)
    {
        var result = await _paymentService.GetCustomerAsync(id, provider, cancellationToken);

        if (!result.Success)
        {
            return NotFound(ApiResponse<object>.Failure(result.ErrorCode!, result.ErrorMessage!));
        }

        var response = MapToCustomerResponse(result.Customer!, result.Provider, result.ProviderCustomerId);
        return Ok(ApiResponse<CustomerResponseDto>.Success(response));
    }

    /// <summary>
    /// Updates a customer.
    /// </summary>
    [HttpPut("customers/{id}")]
    [ProducesResponseType(typeof(ApiResponse<CustomerResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateCustomer(
        [FromRoute] string id,
        [FromBody] UpdateCustomerRequestDto request,
        [FromQuery] PayoutProvider? provider,
        CancellationToken cancellationToken)
    {
        var coreRequest = MapToUpdateCustomerRequest(request);
        var result = await _paymentService.UpdateCustomerAsync(id, coreRequest, provider, cancellationToken);

        if (!result.Success)
        {
            return BadRequest(ApiResponse<object>.Failure(result.ErrorCode!, result.ErrorMessage!));
        }

        var response = MapToCustomerResponse(result.Customer!, result.Provider, result.ProviderCustomerId);
        return Ok(ApiResponse<CustomerResponseDto>.Success(response));
    }

    /// <summary>
    /// Lists customers with optional filters.
    /// </summary>
    [HttpGet("customers")]
    [ProducesResponseType(typeof(ApiResponse<CustomerListResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListCustomers(
        [FromQuery] CustomerType? type,
        [FromQuery] CustomerStatus? status,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] PayoutProvider? provider = null,
        CancellationToken cancellationToken = default)
    {
        var request = new CustomerListRequest
        {
            Type = type,
            Status = status,
            Search = search,
            Page = page,
            PageSize = pageSize
        };

        var result = await _paymentService.ListCustomersAsync(request, provider, cancellationToken);

        if (!result.Success)
        {
            return BadRequest(ApiResponse<object>.Failure(result.ErrorCode!, result.ErrorMessage!));
        }

        var response = new CustomerListResponseDto
        {
            Customers = result.Customers.Select(c => MapToCustomerResponse(c, result.Provider, c.Id)).ToList(),
            TotalCount = result.TotalCount,
            Page = result.Page,
            PageSize = result.PageSize,
            Provider = result.Provider?.ToString()
        };

        return Ok(ApiResponse<CustomerListResponseDto>.Success(response));
    }

    #endregion

    #region KYC Operations

    /// <summary>
    /// Initiates KYC verification for an individual customer.
    /// </summary>
    [HttpPost("kyc/initiate")]
    [ProducesResponseType(typeof(ApiResponse<VerificationResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> InitiateKyc(
        [FromBody] InitiateKycRequestDto request,
        [FromQuery] PayoutProvider? provider,
        CancellationToken cancellationToken)
    {
        var coreRequest = new InitiateKycRequest
        {
            CustomerId = request.CustomerId,
            TargetLevel = request.TargetLevel ?? VerificationLevel.Standard,
            RedirectUrl = request.RedirectUrl,
            WebhookUrl = request.WebhookUrl
        };

        var result = await _paymentService.InitiateKycAsync(coreRequest, provider, cancellationToken);

        if (!result.Success)
        {
            return BadRequest(ApiResponse<object>.Failure(result.ErrorCode!, result.ErrorMessage!));
        }

        var response = MapToVerificationResponse(result);
        return Ok(ApiResponse<VerificationResponseDto>.Success(response));
    }

    /// <summary>
    /// Gets KYC status for a customer.
    /// </summary>
    [HttpGet("kyc/{customerId}")]
    [ProducesResponseType(typeof(ApiResponse<VerificationResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetKycStatus(
        [FromRoute] string customerId,
        [FromQuery] PayoutProvider? provider,
        CancellationToken cancellationToken)
    {
        var result = await _paymentService.GetKycStatusAsync(customerId, provider, cancellationToken);

        if (!result.Success)
        {
            return NotFound(ApiResponse<object>.Failure(result.ErrorCode!, result.ErrorMessage!));
        }

        var response = MapToVerificationResponse(result);
        return Ok(ApiResponse<VerificationResponseDto>.Success(response));
    }

    #endregion

    #region KYB Operations

    /// <summary>
    /// Initiates KYB verification for a business customer.
    /// </summary>
    [HttpPost("kyb/initiate")]
    [ProducesResponseType(typeof(ApiResponse<VerificationResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> InitiateKyb(
        [FromBody] InitiateKybRequestDto request,
        [FromQuery] PayoutProvider? provider,
        CancellationToken cancellationToken)
    {
        var coreRequest = new InitiateKybRequest
        {
            CustomerId = request.CustomerId,
            TargetLevel = request.TargetLevel ?? VerificationLevel.Standard,
            RedirectUrl = request.RedirectUrl,
            WebhookUrl = request.WebhookUrl
        };

        var result = await _paymentService.InitiateKybAsync(coreRequest, provider, cancellationToken);

        if (!result.Success)
        {
            return BadRequest(ApiResponse<object>.Failure(result.ErrorCode!, result.ErrorMessage!));
        }

        var response = MapToVerificationResponse(result);
        return Ok(ApiResponse<VerificationResponseDto>.Success(response));
    }

    /// <summary>
    /// Gets KYB status for a customer.
    /// </summary>
    [HttpGet("kyb/{customerId}")]
    [ProducesResponseType(typeof(ApiResponse<VerificationResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetKybStatus(
        [FromRoute] string customerId,
        [FromQuery] PayoutProvider? provider,
        CancellationToken cancellationToken)
    {
        var result = await _paymentService.GetKybStatusAsync(customerId, provider, cancellationToken);

        if (!result.Success)
        {
            return NotFound(ApiResponse<object>.Failure(result.ErrorCode!, result.ErrorMessage!));
        }

        var response = MapToVerificationResponse(result);
        return Ok(ApiResponse<VerificationResponseDto>.Success(response));
    }

    #endregion

    #region Document Operations

    /// <summary>
    /// Uploads a verification document.
    /// </summary>
    [HttpPost("documents")]
    [ProducesResponseType(typeof(ApiResponse<DocumentResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UploadDocument(
        [FromBody] UploadDocumentRequestDto request,
        [FromQuery] PayoutProvider? provider,
        CancellationToken cancellationToken)
    {
        var coreRequest = new UploadDocumentRequest
        {
            CustomerId = request.CustomerId,
            DocumentType = request.DocumentType,
            DocumentNumber = request.DocumentNumber,
            IssuingCountry = request.IssuingCountry,
            IssueDate = request.IssueDate,
            ExpiryDate = request.ExpiryDate,
            FrontImageBase64 = request.FrontImageBase64,
            BackImageBase64 = request.BackImageBase64,
            MimeType = request.MimeType
        };

        var result = await _paymentService.UploadDocumentAsync(coreRequest, provider, cancellationToken);

        if (!result.Success)
        {
            return BadRequest(ApiResponse<object>.Failure(result.ErrorCode!, result.ErrorMessage!));
        }

        var response = new DocumentResponseDto
        {
            DocumentId = result.DocumentId,
            DocumentType = result.DocumentType?.ToString(),
            Status = result.Status,
            Provider = result.Provider?.ToString()
        };

        return Ok(ApiResponse<DocumentResponseDto>.Success(response));
    }

    /// <summary>
    /// Gets documents for a customer.
    /// </summary>
    [HttpGet("documents/{customerId}")]
    [ProducesResponseType(typeof(ApiResponse<DocumentListResponseDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetDocuments(
        [FromRoute] string customerId,
        [FromQuery] PayoutProvider? provider,
        CancellationToken cancellationToken)
    {
        var result = await _paymentService.GetDocumentsAsync(customerId, provider, cancellationToken);

        if (!result.Success)
        {
            return NotFound(ApiResponse<object>.Failure(result.ErrorCode!, result.ErrorMessage!));
        }

        var response = new DocumentListResponseDto
        {
            Documents = result.Documents.Select(d => new DocumentItemDto
            {
                Id = d.Id,
                Type = d.Type.ToString(),
                Status = d.Status.ToString(),
                UploadedAt = d.UploadedAt
            }).ToList(),
            Provider = result.Provider?.ToString()
        };

        return Ok(ApiResponse<DocumentListResponseDto>.Success(response));
    }

    /// <summary>
    /// Submits verification for review.
    /// </summary>
    [HttpPost("verification/{customerId}/submit")]
    [ProducesResponseType(typeof(ApiResponse<VerificationResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SubmitVerification(
        [FromRoute] string customerId,
        [FromQuery] PayoutProvider? provider,
        CancellationToken cancellationToken)
    {
        var result = await _paymentService.SubmitVerificationAsync(customerId, provider, cancellationToken);

        if (!result.Success)
        {
            return BadRequest(ApiResponse<object>.Failure(result.ErrorCode!, result.ErrorMessage!));
        }

        var response = MapToVerificationResponse(result);
        return Ok(ApiResponse<VerificationResponseDto>.Success(response));
    }

    #endregion

    #region Payout Operations

    /// <summary>
    /// Creates a quote for a stablecoin to fiat payout.
    /// </summary>
    [HttpPost("quotes")]
    [ProducesResponseType(typeof(ApiResponse<QuoteResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateQuote(
        [FromBody] CreateQuoteRequestDto request,
        [FromQuery] PayoutProvider? provider,
        CancellationToken cancellationToken)
    {
        var coreRequest = new CreateQuoteRequest
        {
            SourceCurrency = request.SourceCurrency,
            TargetCurrency = request.TargetCurrency,
            SourceAmount = request.SourceAmount,
            TargetAmount = request.TargetAmount,
            Network = request.Network,
            DestinationCountry = request.DestinationCountry,
            PaymentMethod = request.PaymentMethod,
            DeveloperFee = request.DeveloperFee
        };

        var result = await _paymentService.CreateQuoteAsync(coreRequest, provider, cancellationToken);

        if (!result.Success)
        {
            return BadRequest(ApiResponse<object>.Failure(result.ErrorCode!, result.ErrorMessage!));
        }

        var response = MapToQuoteResponse(result.Quote!);
        return Ok(ApiResponse<QuoteResponseDto>.Success(response));
    }

    /// <summary>
    /// Creates quotes from all available providers for comparison.
    /// </summary>
    [HttpPost("quotes/compare")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<QuoteResponseDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> CompareQuotes(
        [FromBody] CreateQuoteRequestDto request,
        CancellationToken cancellationToken)
    {
        var coreRequest = new CreateQuoteRequest
        {
            SourceCurrency = request.SourceCurrency,
            TargetCurrency = request.TargetCurrency,
            SourceAmount = request.SourceAmount,
            TargetAmount = request.TargetAmount,
            Network = request.Network,
            DestinationCountry = request.DestinationCountry,
            PaymentMethod = request.PaymentMethod,
            DeveloperFee = request.DeveloperFee
        };

        var results = await _paymentService.CreateQuotesFromAllProvidersAsync(coreRequest, cancellationToken);

        var responses = results
            .Where(r => r.Success && r.Quote != null)
            .Select(r => MapToQuoteResponse(r.Quote!))
            .OrderBy(q => q.FeeAmount)
            .ToList();

        return Ok(ApiResponse<IReadOnlyList<QuoteResponseDto>>.Success(responses));
    }

    /// <summary>
    /// Creates a payout from stablecoin to fiat.
    /// </summary>
    [HttpPost("payouts")]
    [ProducesResponseType(typeof(ApiResponse<PayoutResponseDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreatePayout(
        [FromBody] CreatePayoutRequestDto request,
        CancellationToken cancellationToken)
    {
        var coreRequest = MapToCreatePayoutRequest(request);
        var result = await _paymentService.CreatePayoutAsync(coreRequest, cancellationToken);

        if (!result.Success)
        {
            return BadRequest(ApiResponse<object>.Failure(result.ErrorCode!, result.ErrorMessage!));
        }

        var response = MapToPayoutResponse(result.Payout!);
        return CreatedAtAction(
            nameof(GetPayout),
            new { id = result.Payout!.Id },
            ApiResponse<PayoutResponseDto>.Success(response));
    }

    /// <summary>
    /// Gets a payout by ID.
    /// </summary>
    [HttpGet("payouts/{id}")]
    [ProducesResponseType(typeof(ApiResponse<PayoutResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPayout(
        [FromRoute] string id,
        [FromQuery] PayoutProvider? provider,
        CancellationToken cancellationToken)
    {
        var result = await _paymentService.GetPayoutAsync(id, provider, cancellationToken);

        if (!result.Success)
        {
            return NotFound(ApiResponse<object>.Failure(result.ErrorCode!, result.ErrorMessage!));
        }

        var response = MapToPayoutResponse(result.Payout!);
        return Ok(ApiResponse<PayoutResponseDto>.Success(response));
    }

    /// <summary>
    /// Gets payout status.
    /// </summary>
    [HttpGet("payouts/{id}/status")]
    [ProducesResponseType(typeof(ApiResponse<PayoutStatusResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPayoutStatus(
        [FromRoute] string id,
        [FromQuery] PayoutProvider? provider,
        CancellationToken cancellationToken)
    {
        var result = await _paymentService.GetPayoutStatusAsync(id, provider, cancellationToken);

        if (!result.Success)
        {
            return NotFound(ApiResponse<object>.Failure(result.ErrorCode!, result.ErrorMessage!));
        }

        var response = new PayoutStatusResponseDto
        {
            PayoutId = result.StatusUpdate!.PayoutId,
            ProviderOrderId = result.StatusUpdate.ProviderOrderId,
            Status = result.StatusUpdate.CurrentStatus.ToString(),
            ProviderStatus = result.StatusUpdate.ProviderStatus,
            BlockchainTxHash = result.StatusUpdate.BlockchainTxHash,
            BankReference = result.StatusUpdate.BankReference,
            FailureReason = result.StatusUpdate.FailureReason,
            Timestamp = result.StatusUpdate.Timestamp,
            Provider = result.StatusUpdate.Provider.ToString()
        };

        return Ok(ApiResponse<PayoutStatusResponseDto>.Success(response));
    }

    /// <summary>
    /// Cancels a payout.
    /// </summary>
    [HttpPost("payouts/{id}/cancel")]
    [ProducesResponseType(typeof(ApiResponse<PayoutResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CancelPayout(
        [FromRoute] string id,
        [FromQuery] PayoutProvider? provider,
        CancellationToken cancellationToken)
    {
        var result = await _paymentService.CancelPayoutAsync(id, provider, cancellationToken);

        if (!result.Success)
        {
            return BadRequest(ApiResponse<object>.Failure(result.ErrorCode!, result.ErrorMessage!));
        }

        var response = MapToPayoutResponse(result.Payout!);
        return Ok(ApiResponse<PayoutResponseDto>.Success(response));
    }

    #endregion

    #region Private Mapping Methods

    private static CreateCustomerRequest MapToCreateCustomerRequest(CreateCustomerRequestDto dto)
    {
        return new CreateCustomerRequest
        {
            ExternalId = dto.ExternalId,
            Type = dto.Type,
            Role = dto.Role,
            Individual = dto.Individual != null ? new IndividualDetails
            {
                FirstName = dto.Individual.FirstName,
                LastName = dto.Individual.LastName,
                MiddleName = dto.Individual.MiddleName,
                DateOfBirth = dto.Individual.DateOfBirth,
                Nationality = dto.Individual.Nationality
            } : null,
            Business = dto.Business != null ? new BusinessDetails
            {
                LegalName = dto.Business.LegalName,
                TradingName = dto.Business.TradingName,
                RegistrationNumber = dto.Business.RegistrationNumber,
                TaxId = dto.Business.TaxId,
                CountryOfIncorporation = dto.Business.CountryOfIncorporation
            } : null,
            Contact = new ContactInfo
            {
                Email = dto.Contact.Email,
                Phone = dto.Contact.Phone
            },
            Address = dto.Address != null ? new Address
            {
                Street1 = dto.Address.Street1,
                Street2 = dto.Address.Street2,
                City = dto.Address.City,
                State = dto.Address.State,
                PostalCode = dto.Address.PostalCode,
                CountryCode = dto.Address.CountryCode
            } : null,
            PreferredProvider = dto.PreferredProvider,
            Metadata = dto.Metadata
        };
    }

    private static UpdateCustomerRequest MapToUpdateCustomerRequest(UpdateCustomerRequestDto dto)
    {
        return new UpdateCustomerRequest
        {
            Individual = dto.Individual != null ? new IndividualDetails
            {
                FirstName = dto.Individual.FirstName,
                LastName = dto.Individual.LastName,
                MiddleName = dto.Individual.MiddleName,
                DateOfBirth = dto.Individual.DateOfBirth,
                Nationality = dto.Individual.Nationality
            } : null,
            Business = dto.Business != null ? new BusinessDetails
            {
                LegalName = dto.Business.LegalName,
                TradingName = dto.Business.TradingName,
                RegistrationNumber = dto.Business.RegistrationNumber,
                TaxId = dto.Business.TaxId,
                CountryOfIncorporation = dto.Business.CountryOfIncorporation
            } : null,
            Contact = dto.Contact != null ? new ContactInfo
            {
                Email = dto.Contact.Email,
                Phone = dto.Contact.Phone
            } : null,
            Address = dto.Address != null ? new Address
            {
                Street1 = dto.Address.Street1,
                Street2 = dto.Address.Street2,
                City = dto.Address.City,
                State = dto.Address.State,
                PostalCode = dto.Address.PostalCode,
                CountryCode = dto.Address.CountryCode
            } : null,
            Metadata = dto.Metadata
        };
    }

    private static CustomerResponseDto MapToCustomerResponse(Customer customer, PayoutProvider? provider, string? providerCustomerId)
    {
        return new CustomerResponseDto
        {
            Id = customer.Id,
            ExternalId = customer.ExternalId,
            Type = customer.Type.ToString(),
            Role = customer.Role.ToString(),
            Status = customer.Status.ToString(),
            Individual = customer.Individual != null ? new IndividualDetailsDto
            {
                FirstName = customer.Individual.FirstName,
                LastName = customer.Individual.LastName,
                MiddleName = customer.Individual.MiddleName,
                DateOfBirth = customer.Individual.DateOfBirth,
                Nationality = customer.Individual.Nationality
            } : null,
            Business = customer.Business != null ? new BusinessDetailsDto
            {
                LegalName = customer.Business.LegalName,
                TradingName = customer.Business.TradingName,
                RegistrationNumber = customer.Business.RegistrationNumber,
                TaxId = customer.Business.TaxId,
                CountryOfIncorporation = customer.Business.CountryOfIncorporation
            } : null,
            Contact = new ContactInfoDto
            {
                Email = customer.Contact.Email,
                Phone = customer.Contact.Phone
            },
            Provider = provider?.ToString(),
            ProviderCustomerId = providerCustomerId,
            CreatedAt = customer.CreatedAt,
            UpdatedAt = customer.UpdatedAt
        };
    }

    private static VerificationResponseDto MapToVerificationResponse(Core.Models.Responses.VerificationResult result)
    {
        return new VerificationResponseDto
        {
            CustomerId = result.CustomerId,
            SessionId = result.SessionId,
            VerificationUrl = result.VerificationUrl,
            Status = result.Status?.ToString(),
            Level = result.Level?.ToString(),
            ExpiresAt = result.ExpiresAt,
            Provider = result.Provider?.ToString()
        };
    }

    private static QuoteResponseDto MapToQuoteResponse(PayoutQuote quote)
    {
        return new QuoteResponseDto
        {
            Id = quote.Id,
            ProviderQuoteId = quote.ProviderQuoteId,
            SourceCurrency = quote.SourceCurrency.ToString(),
            TargetCurrency = quote.TargetCurrency.ToString(),
            SourceAmount = quote.SourceAmount,
            TargetAmount = quote.TargetAmount,
            ExchangeRate = quote.ExchangeRate,
            FeeAmount = quote.FeeAmount,
            Network = quote.Network.ToString(),
            Provider = quote.Provider.ToString(),
            ExpiresAt = quote.ExpiresAt,
            CreatedAt = quote.CreatedAt
        };
    }

    private static CreatePayoutRequest MapToCreatePayoutRequest(CreatePayoutRequestDto dto)
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
            PaymentMethod = dto.PaymentMethod,
            Sender = new Sender
            {
                Id = dto.Sender.Id,
                ExternalId = dto.Sender.ExternalId,
                Type = dto.Sender.Type,
                FirstName = dto.Sender.FirstName,
                LastName = dto.Sender.LastName,
                BusinessName = dto.Sender.BusinessName,
                Email = dto.Sender.Email,
                PhoneNumber = dto.Sender.PhoneNumber,
                Address = dto.Sender.Address != null ? new Address
                {
                    Street1 = dto.Sender.Address.Street1,
                    Street2 = dto.Sender.Address.Street2,
                    City = dto.Sender.Address.City,
                    State = dto.Sender.Address.State,
                    PostalCode = dto.Sender.Address.PostalCode,
                    CountryCode = dto.Sender.Address.CountryCode
                } : null
            },
            Beneficiary = new Beneficiary
            {
                Id = dto.Beneficiary.Id,
                ExternalId = dto.Beneficiary.ExternalId,
                Type = dto.Beneficiary.Type,
                FirstName = dto.Beneficiary.FirstName,
                LastName = dto.Beneficiary.LastName,
                BusinessName = dto.Beneficiary.BusinessName,
                Email = dto.Beneficiary.Email,
                PhoneNumber = dto.Beneficiary.PhoneNumber,
                Address = dto.Beneficiary.Address != null ? new Address
                {
                    Street1 = dto.Beneficiary.Address.Street1,
                    Street2 = dto.Beneficiary.Address.Street2,
                    City = dto.Beneficiary.Address.City,
                    State = dto.Beneficiary.Address.State,
                    PostalCode = dto.Beneficiary.Address.PostalCode,
                    CountryCode = dto.Beneficiary.Address.CountryCode
                } : null,
                BankAccount = new BankAccount
                {
                    BankName = dto.Beneficiary.BankAccount.BankName,
                    AccountNumber = dto.Beneficiary.BankAccount.AccountNumber,
                    AccountHolderName = dto.Beneficiary.BankAccount.AccountHolderName,
                    RoutingNumber = dto.Beneficiary.BankAccount.RoutingNumber,
                    SwiftCode = dto.Beneficiary.BankAccount.SwiftCode,
                    SortCode = dto.Beneficiary.BankAccount.SortCode,
                    Iban = dto.Beneficiary.BankAccount.Iban,
                    Currency = dto.Beneficiary.BankAccount.Currency,
                    CountryCode = dto.Beneficiary.BankAccount.CountryCode
                }
            },
            PreferredProvider = dto.PreferredProvider,
            Metadata = dto.Metadata
        };
    }

    private static PayoutResponseDto MapToPayoutResponse(Payout payout)
    {
        return new PayoutResponseDto
        {
            Id = payout.Id,
            ExternalId = payout.ExternalId,
            ProviderOrderId = payout.ProviderOrderId,
            Status = payout.Status.ToString(),
            SourceCurrency = payout.SourceCurrency.ToString(),
            TargetCurrency = payout.TargetCurrency.ToString(),
            SourceAmount = payout.SourceAmount,
            TargetAmount = payout.TargetAmount,
            ExchangeRate = payout.ExchangeRate,
            FeeAmount = payout.FeeAmount,
            Network = payout.Network.ToString(),
            Provider = payout.Provider.ToString(),
            DepositWallet = payout.DepositWallet != null ? new DepositWalletDto
            {
                Address = payout.DepositWallet.Address,
                Network = payout.DepositWallet.Network.ToString(),
                Currency = payout.DepositWallet.Currency.ToString(),
                ExpectedAmount = payout.DepositWallet.ExpectedAmount,
                ExpiresAt = payout.DepositWallet.ExpiresAt,
                Memo = payout.DepositWallet.Memo
            } : null,
            CreatedAt = payout.CreatedAt,
            UpdatedAt = payout.UpdatedAt
        };
    }

    #endregion
}

#region DTOs

public sealed class ProviderHealthDto
{
    public bool IsHealthy { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Message { get; set; }
    public double? Latency { get; set; }
}

public sealed class CustomerListResponseDto
{
    public IReadOnlyList<CustomerResponseDto> Customers { get; set; } = [];
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public string? Provider { get; set; }
}

public sealed class VerificationResponseDto
{
    public string? CustomerId { get; set; }
    public string? SessionId { get; set; }
    public string? VerificationUrl { get; set; }
    public string? Status { get; set; }
    public string? Level { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }
    public string? Provider { get; set; }
}

public sealed class DocumentResponseDto
{
    public string? DocumentId { get; set; }
    public string? DocumentType { get; set; }
    public string? Status { get; set; }
    public string? Provider { get; set; }
}

public sealed class DocumentListResponseDto
{
    public IReadOnlyList<DocumentItemDto> Documents { get; set; } = [];
    public string? Provider { get; set; }
}

public sealed class DocumentItemDto
{
    public string Id { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset UploadedAt { get; set; }
}

public sealed class QuoteResponseDto
{
    public string Id { get; set; } = string.Empty;
    public string? ProviderQuoteId { get; set; }
    public string SourceCurrency { get; set; } = string.Empty;
    public string TargetCurrency { get; set; } = string.Empty;
    public decimal SourceAmount { get; set; }
    public decimal TargetAmount { get; set; }
    public decimal ExchangeRate { get; set; }
    public decimal FeeAmount { get; set; }
    public string Network { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class CreateQuoteRequestDto
{
    public required Stablecoin SourceCurrency { get; set; }
    public required FiatCurrency TargetCurrency { get; set; }
    public decimal? SourceAmount { get; set; }
    public decimal? TargetAmount { get; set; }
    public required BlockchainNetwork Network { get; set; }
    public required string DestinationCountry { get; set; }
    public PaymentMethod? PaymentMethod { get; set; }
    public decimal? DeveloperFee { get; set; }
}

public sealed class PayoutResponseDto
{
    public string Id { get; set; } = string.Empty;
    public string? ExternalId { get; set; }
    public string? ProviderOrderId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string SourceCurrency { get; set; } = string.Empty;
    public string TargetCurrency { get; set; } = string.Empty;
    public decimal SourceAmount { get; set; }
    public decimal TargetAmount { get; set; }
    public decimal ExchangeRate { get; set; }
    public decimal FeeAmount { get; set; }
    public string Network { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
    public DepositWalletDto? DepositWallet { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public sealed class DepositWalletDto
{
    public string Address { get; set; } = string.Empty;
    public string Network { get; set; } = string.Empty;
    public string Currency { get; set; } = string.Empty;
    public decimal ExpectedAmount { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }
    public string? Memo { get; set; }
}

public sealed class PayoutStatusResponseDto
{
    public string PayoutId { get; set; } = string.Empty;
    public string? ProviderOrderId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? ProviderStatus { get; set; }
    public string? BlockchainTxHash { get; set; }
    public string? BankReference { get; set; }
    public string? FailureReason { get; set; }
    public DateTimeOffset Timestamp { get; set; }
    public string Provider { get; set; } = string.Empty;
}

public sealed class InitiateKycRequestDto
{
    public required string CustomerId { get; set; }
    public VerificationLevel? TargetLevel { get; set; }
    public string? RedirectUrl { get; set; }
    public string? WebhookUrl { get; set; }
}

public sealed class InitiateKybRequestDto
{
    public required string CustomerId { get; set; }
    public VerificationLevel? TargetLevel { get; set; }
    public string? RedirectUrl { get; set; }
    public string? WebhookUrl { get; set; }
}

public sealed class UploadDocumentRequestDto
{
    public required string CustomerId { get; set; }
    public required DocumentType DocumentType { get; set; }
    public string? DocumentNumber { get; set; }
    public string? IssuingCountry { get; set; }
    public DateOnly? IssueDate { get; set; }
    public DateOnly? ExpiryDate { get; set; }
    public required string FrontImageBase64 { get; set; }
    public string? BackImageBase64 { get; set; }
    public required string MimeType { get; set; }
}

public sealed class CreatePayoutRequestDto
{
    public string? ExternalId { get; set; }
    public string? QuoteId { get; set; }
    public required Stablecoin SourceCurrency { get; set; }
    public required FiatCurrency TargetCurrency { get; set; }
    public decimal? SourceAmount { get; set; }
    public decimal? TargetAmount { get; set; }
    public required BlockchainNetwork Network { get; set; }
    public required PaymentMethod PaymentMethod { get; set; }
    public required SenderDto Sender { get; set; }
    public required BeneficiaryDto Beneficiary { get; set; }
    public PayoutProvider? PreferredProvider { get; set; }
    public Dictionary<string, string>? Metadata { get; set; }
}

public sealed class SenderDto
{
    public string? Id { get; set; }
    public string? ExternalId { get; set; }
    public BeneficiaryType Type { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? BusinessName { get; set; }
    public required string Email { get; set; }
    public string? PhoneNumber { get; set; }
    public AddressDto? Address { get; set; }
}

public sealed class BeneficiaryDto
{
    public string? Id { get; set; }
    public string? ExternalId { get; set; }
    public BeneficiaryType Type { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? BusinessName { get; set; }
    public required string Email { get; set; }
    public string? PhoneNumber { get; set; }
    public AddressDto? Address { get; set; }
    public required BankAccountDto BankAccount { get; set; }
}

public sealed class BankAccountDto
{
    public required string BankName { get; set; }
    public required string AccountNumber { get; set; }
    public required string AccountHolderName { get; set; }
    public string? RoutingNumber { get; set; }
    public string? SwiftCode { get; set; }
    public string? SortCode { get; set; }
    public string? Iban { get; set; }
    public required FiatCurrency Currency { get; set; }
    public required string CountryCode { get; set; }
}

public sealed class AddressDto
{
    public required string Street1 { get; set; }
    public string? Street2 { get; set; }
    public required string City { get; set; }
    public string? State { get; set; }
    public required string PostalCode { get; set; }
    public required string CountryCode { get; set; }
}

public sealed class UpdateCustomerRequestDto
{
    public IndividualDetailsDto? Individual { get; set; }
    public BusinessDetailsDto? Business { get; set; }
    public ContactInfoDto? Contact { get; set; }
    public AddressDto? Address { get; set; }
    public Dictionary<string, string>? Metadata { get; set; }
}

#endregion
