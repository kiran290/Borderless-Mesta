using Microsoft.AspNetCore.Mvc;
using StablecoinPayments.Api.Dtos;
using StablecoinPayments.Core.Enums;
using StablecoinPayments.Core.Models.Requests;
using StablecoinPayments.Infrastructure.Services;

namespace StablecoinPayments.Api.Controllers;

[ApiController]
[Route("api/v1")]
[Produces("application/json")]
public sealed class PaymentController : ControllerBase
{
    private readonly UnifiedPaymentService _paymentService;
    private readonly ILogger<PaymentController> _logger;

    public PaymentController(
        UnifiedPaymentService paymentService,
        ILogger<PaymentController> logger)
    {
        _paymentService = paymentService;
        _logger = logger;
    }

    #region Health & Providers

    /// <summary>
    /// Gets all available providers.
    /// </summary>
    [HttpGet("providers")]
    public IActionResult GetProviders()
    {
        var providers = _paymentService.GetAvailableProviders().Select(p => p.ToString());
        return Ok(ApiResponse<IEnumerable<string>>.Ok(providers));
    }

    /// <summary>
    /// Checks health of all providers.
    /// </summary>
    [HttpGet("providers/health")]
    public async Task<IActionResult> GetProvidersHealth(CancellationToken cancellationToken)
    {
        var health = await _paymentService.CheckAllProvidersHealthAsync(cancellationToken);
        var response = health.ToDictionary(
            kvp => kvp.Key.ToString(),
            kvp => new HealthDto
            {
                IsHealthy = kvp.Value.IsHealthy,
                Status = kvp.Value.Status,
                Message = kvp.Value.Message,
                LatencyMs = kvp.Value.Latency?.TotalMilliseconds
            });
        return Ok(ApiResponse<Dictionary<string, HealthDto>>.Ok(response));
    }

    #endregion

    #region Customer Operations

    /// <summary>
    /// Creates a new customer.
    /// </summary>
    [HttpPost("customers")]
    public async Task<IActionResult> CreateCustomer(
        [FromBody] CreateCustomerDto request,
        [FromQuery] PaymentProvider? provider,
        CancellationToken cancellationToken)
    {
        var coreRequest = new CreateCustomerRequest
        {
            ExternalId = request.ExternalId,
            Type = request.Type,
            Individual = request.Individual != null ? new IndividualInfo
            {
                FirstName = request.Individual.FirstName,
                LastName = request.Individual.LastName,
                MiddleName = request.Individual.MiddleName,
                DateOfBirth = request.Individual.DateOfBirth,
                Nationality = request.Individual.Nationality
            } : null,
            Business = request.Business != null ? new BusinessInfo
            {
                LegalName = request.Business.LegalName,
                TradingName = request.Business.TradingName,
                RegistrationNumber = request.Business.RegistrationNumber,
                TaxId = request.Business.TaxId,
                CountryOfIncorporation = request.Business.CountryOfIncorporation
            } : null,
            Contact = new ContactInfo
            {
                Email = request.Contact.Email,
                Phone = request.Contact.Phone
            },
            Address = request.Address != null ? new AddressInfo
            {
                Street1 = request.Address.Street1,
                Street2 = request.Address.Street2,
                City = request.Address.City,
                State = request.Address.State,
                PostalCode = request.Address.PostalCode,
                CountryCode = request.Address.CountryCode
            } : null,
            Metadata = request.Metadata
        };

        var result = await _paymentService.CreateCustomerAsync(coreRequest, provider, cancellationToken);

        if (!result.Success)
            return BadRequest(ApiResponse<object>.Error(result.ErrorCode!, result.ErrorMessage!));

        return CreatedAtAction(nameof(GetCustomer), new { id = result.Customer!.Id },
            ApiResponse<CustomerDto>.Ok(MapToCustomerDto(result)));
    }

    /// <summary>
    /// Gets a customer by ID.
    /// </summary>
    [HttpGet("customers/{id}")]
    public async Task<IActionResult> GetCustomer(
        [FromRoute] string id,
        [FromQuery] PaymentProvider? provider,
        CancellationToken cancellationToken)
    {
        var result = await _paymentService.GetCustomerAsync(id, provider, cancellationToken);

        if (!result.Success)
            return NotFound(ApiResponse<object>.Error(result.ErrorCode!, result.ErrorMessage!));

        return Ok(ApiResponse<CustomerDto>.Ok(MapToCustomerDto(result)));
    }

    /// <summary>
    /// Updates a customer.
    /// </summary>
    [HttpPut("customers/{id}")]
    public async Task<IActionResult> UpdateCustomer(
        [FromRoute] string id,
        [FromBody] UpdateCustomerDto request,
        [FromQuery] PaymentProvider? provider,
        CancellationToken cancellationToken)
    {
        var coreRequest = new UpdateCustomerRequest
        {
            Individual = request.Individual != null ? new IndividualInfo
            {
                FirstName = request.Individual.FirstName,
                LastName = request.Individual.LastName,
                MiddleName = request.Individual.MiddleName,
                DateOfBirth = request.Individual.DateOfBirth,
                Nationality = request.Individual.Nationality
            } : null,
            Business = request.Business != null ? new BusinessInfo
            {
                LegalName = request.Business.LegalName,
                TradingName = request.Business.TradingName,
                RegistrationNumber = request.Business.RegistrationNumber,
                TaxId = request.Business.TaxId
            } : null,
            Contact = request.Contact != null ? new ContactInfo
            {
                Email = request.Contact.Email,
                Phone = request.Contact.Phone
            } : null,
            Address = request.Address != null ? new AddressInfo
            {
                Street1 = request.Address.Street1,
                Street2 = request.Address.Street2,
                City = request.Address.City,
                State = request.Address.State,
                PostalCode = request.Address.PostalCode,
                CountryCode = request.Address.CountryCode
            } : null,
            Metadata = request.Metadata
        };

        var result = await _paymentService.UpdateCustomerAsync(id, coreRequest, provider, cancellationToken);

        if (!result.Success)
            return BadRequest(ApiResponse<object>.Error(result.ErrorCode!, result.ErrorMessage!));

        return Ok(ApiResponse<CustomerDto>.Ok(MapToCustomerDto(result)));
    }

    /// <summary>
    /// Lists customers.
    /// </summary>
    [HttpGet("customers")]
    public async Task<IActionResult> ListCustomers(
        [FromQuery] CustomerType? type,
        [FromQuery] CustomerStatus? status,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] PaymentProvider? provider = null,
        CancellationToken cancellationToken = default)
    {
        var request = new ListCustomersRequest
        {
            Type = type,
            Status = status,
            Search = search,
            Page = page,
            PageSize = pageSize
        };

        var result = await _paymentService.ListCustomersAsync(request, provider, cancellationToken);

        if (!result.Success)
            return BadRequest(ApiResponse<object>.Error(result.ErrorCode!, result.ErrorMessage!));

        var response = new CustomerListDto
        {
            Customers = result.Customers.Select(c => new CustomerDto
            {
                Id = c.Id,
                ExternalId = c.ExternalId,
                Type = c.Type.ToString(),
                Status = c.Status.ToString(),
                Provider = result.Provider.ToString()
            }).ToList(),
            TotalCount = result.TotalCount,
            Page = result.Page,
            PageSize = result.PageSize
        };

        return Ok(ApiResponse<CustomerListDto>.Ok(response));
    }

    #endregion

    #region KYC Operations

    /// <summary>
    /// Initiates KYC verification.
    /// </summary>
    [HttpPost("kyc/initiate")]
    public async Task<IActionResult> InitiateKyc(
        [FromBody] InitiateKycDto request,
        [FromQuery] PaymentProvider? provider,
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
            return BadRequest(ApiResponse<object>.Error(result.ErrorCode!, result.ErrorMessage!));

        return Ok(ApiResponse<VerificationDto>.Ok(MapToVerificationDto(result)));
    }

    /// <summary>
    /// Gets KYC status.
    /// </summary>
    [HttpGet("kyc/{customerId}")]
    public async Task<IActionResult> GetKycStatus(
        [FromRoute] string customerId,
        [FromQuery] PaymentProvider? provider,
        CancellationToken cancellationToken)
    {
        var result = await _paymentService.GetKycStatusAsync(customerId, provider, cancellationToken);

        if (!result.Success)
            return NotFound(ApiResponse<object>.Error(result.ErrorCode!, result.ErrorMessage!));

        return Ok(ApiResponse<VerificationDto>.Ok(MapToVerificationDto(result)));
    }

    #endregion

    #region KYB Operations

    /// <summary>
    /// Initiates KYB verification.
    /// </summary>
    [HttpPost("kyb/initiate")]
    public async Task<IActionResult> InitiateKyb(
        [FromBody] InitiateKybDto request,
        [FromQuery] PaymentProvider? provider,
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
            return BadRequest(ApiResponse<object>.Error(result.ErrorCode!, result.ErrorMessage!));

        return Ok(ApiResponse<VerificationDto>.Ok(MapToVerificationDto(result)));
    }

    /// <summary>
    /// Gets KYB status.
    /// </summary>
    [HttpGet("kyb/{customerId}")]
    public async Task<IActionResult> GetKybStatus(
        [FromRoute] string customerId,
        [FromQuery] PaymentProvider? provider,
        CancellationToken cancellationToken)
    {
        var result = await _paymentService.GetKybStatusAsync(customerId, provider, cancellationToken);

        if (!result.Success)
            return NotFound(ApiResponse<object>.Error(result.ErrorCode!, result.ErrorMessage!));

        return Ok(ApiResponse<VerificationDto>.Ok(MapToVerificationDto(result)));
    }

    #endregion

    #region Document Operations

    /// <summary>
    /// Uploads a document.
    /// </summary>
    [HttpPost("documents")]
    public async Task<IActionResult> UploadDocument(
        [FromBody] UploadDocumentDto request,
        [FromQuery] PaymentProvider? provider,
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
            MimeType = request.MimeType ?? "image/jpeg"
        };

        var result = await _paymentService.UploadDocumentAsync(coreRequest, provider, cancellationToken);

        if (!result.Success)
            return BadRequest(ApiResponse<object>.Error(result.ErrorCode!, result.ErrorMessage!));

        return Ok(ApiResponse<DocumentDto>.Ok(new DocumentDto
        {
            DocumentId = result.DocumentId,
            DocumentType = result.DocumentType.ToString(),
            Status = result.Status,
            Provider = result.Provider.ToString()
        }));
    }

    /// <summary>
    /// Gets documents for a customer.
    /// </summary>
    [HttpGet("documents/{customerId}")]
    public async Task<IActionResult> GetDocuments(
        [FromRoute] string customerId,
        [FromQuery] PaymentProvider? provider,
        CancellationToken cancellationToken)
    {
        var result = await _paymentService.GetDocumentsAsync(customerId, provider, cancellationToken);

        if (!result.Success)
            return NotFound(ApiResponse<object>.Error(result.ErrorCode!, result.ErrorMessage!));

        return Ok(ApiResponse<DocumentListDto>.Ok(new DocumentListDto
        {
            Documents = result.Documents.Select(d => new DocumentDto
            {
                DocumentId = d.Id,
                DocumentType = d.Type.ToString(),
                Status = d.Status,
                UploadedAt = d.UploadedAt
            }).ToList(),
            Provider = result.Provider.ToString()
        }));
    }

    /// <summary>
    /// Submits verification for review.
    /// </summary>
    [HttpPost("verification/{customerId}/submit")]
    public async Task<IActionResult> SubmitVerification(
        [FromRoute] string customerId,
        [FromQuery] PaymentProvider? provider,
        CancellationToken cancellationToken)
    {
        var result = await _paymentService.SubmitVerificationAsync(customerId, provider, cancellationToken);

        if (!result.Success)
            return BadRequest(ApiResponse<object>.Error(result.ErrorCode!, result.ErrorMessage!));

        return Ok(ApiResponse<VerificationDto>.Ok(MapToVerificationDto(result)));
    }

    #endregion

    #region Quote Operations

    /// <summary>
    /// Creates a quote.
    /// </summary>
    [HttpPost("quotes")]
    public async Task<IActionResult> CreateQuote(
        [FromBody] CreateQuoteDto request,
        [FromQuery] PaymentProvider? provider,
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
            PaymentMethod = request.PaymentMethod ?? PaymentMethod.BankTransfer
        };

        var result = await _paymentService.CreateQuoteAsync(coreRequest, provider, cancellationToken);

        if (!result.Success)
            return BadRequest(ApiResponse<object>.Error(result.ErrorCode!, result.ErrorMessage!));

        return Ok(ApiResponse<QuoteDto>.Ok(MapToQuoteDto(result.Quote!)));
    }

    /// <summary>
    /// Compares quotes from all providers.
    /// </summary>
    [HttpPost("quotes/compare")]
    public async Task<IActionResult> CompareQuotes(
        [FromBody] CreateQuoteDto request,
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
            PaymentMethod = request.PaymentMethod ?? PaymentMethod.BankTransfer
        };

        var results = await _paymentService.CreateQuotesFromAllProvidersAsync(coreRequest, cancellationToken);

        var quotes = results
            .Where(r => r.Success && r.Quote != null)
            .Select(r => MapToQuoteDto(r.Quote!))
            .OrderBy(q => q.FeeAmount)
            .ToList();

        return Ok(ApiResponse<IReadOnlyList<QuoteDto>>.Ok(quotes));
    }

    #endregion

    #region Payout Operations

    /// <summary>
    /// Creates a payout.
    /// </summary>
    [HttpPost("payouts")]
    public async Task<IActionResult> CreatePayout(
        [FromBody] CreatePayoutDto request,
        CancellationToken cancellationToken)
    {
        var coreRequest = new CreatePayoutRequest
        {
            ExternalId = request.ExternalId,
            QuoteId = request.QuoteId,
            SourceCurrency = request.SourceCurrency,
            TargetCurrency = request.TargetCurrency,
            SourceAmount = request.SourceAmount,
            TargetAmount = request.TargetAmount,
            Network = request.Network,
            PaymentMethod = request.PaymentMethod,
            Sender = new SenderInfo
            {
                Id = request.Sender.Id,
                ExternalId = request.Sender.ExternalId,
                Type = request.Sender.Type,
                FirstName = request.Sender.FirstName,
                LastName = request.Sender.LastName,
                BusinessName = request.Sender.BusinessName,
                Email = request.Sender.Email,
                Phone = request.Sender.Phone
            },
            Beneficiary = new BeneficiaryInfo
            {
                Id = request.Beneficiary.Id,
                ExternalId = request.Beneficiary.ExternalId,
                Type = request.Beneficiary.Type,
                FirstName = request.Beneficiary.FirstName,
                LastName = request.Beneficiary.LastName,
                BusinessName = request.Beneficiary.BusinessName,
                Email = request.Beneficiary.Email,
                Phone = request.Beneficiary.Phone,
                BankAccount = new BankAccountInfo
                {
                    BankName = request.Beneficiary.BankAccount.BankName,
                    AccountNumber = request.Beneficiary.BankAccount.AccountNumber,
                    AccountHolderName = request.Beneficiary.BankAccount.AccountHolderName,
                    RoutingNumber = request.Beneficiary.BankAccount.RoutingNumber,
                    SwiftCode = request.Beneficiary.BankAccount.SwiftCode,
                    SortCode = request.Beneficiary.BankAccount.SortCode,
                    Iban = request.Beneficiary.BankAccount.Iban,
                    Currency = request.Beneficiary.BankAccount.Currency,
                    CountryCode = request.Beneficiary.BankAccount.CountryCode
                }
            },
            Purpose = request.Purpose,
            Reference = request.Reference,
            Metadata = request.Metadata
        };

        var result = await _paymentService.CreatePayoutAsync(coreRequest, cancellationToken);

        if (!result.Success)
            return BadRequest(ApiResponse<object>.Error(result.ErrorCode!, result.ErrorMessage!));

        return CreatedAtAction(nameof(GetPayout), new { id = result.Payout!.Id },
            ApiResponse<PayoutDto>.Ok(MapToPayoutDto(result.Payout!)));
    }

    /// <summary>
    /// Gets a payout.
    /// </summary>
    [HttpGet("payouts/{id}")]
    public async Task<IActionResult> GetPayout(
        [FromRoute] string id,
        [FromQuery] PaymentProvider? provider,
        CancellationToken cancellationToken)
    {
        var result = await _paymentService.GetPayoutAsync(id, provider, cancellationToken);

        if (!result.Success)
            return NotFound(ApiResponse<object>.Error(result.ErrorCode!, result.ErrorMessage!));

        return Ok(ApiResponse<PayoutDto>.Ok(MapToPayoutDto(result.Payout!)));
    }

    /// <summary>
    /// Gets payout status.
    /// </summary>
    [HttpGet("payouts/{id}/status")]
    public async Task<IActionResult> GetPayoutStatus(
        [FromRoute] string id,
        [FromQuery] PaymentProvider? provider,
        CancellationToken cancellationToken)
    {
        var result = await _paymentService.GetPayoutStatusAsync(id, provider, cancellationToken);

        if (!result.Success)
            return NotFound(ApiResponse<object>.Error(result.ErrorCode!, result.ErrorMessage!));

        return Ok(ApiResponse<PayoutStatusDto>.Ok(new PayoutStatusDto
        {
            PayoutId = result.PayoutId,
            ProviderPayoutId = result.ProviderPayoutId,
            Status = result.Status.ToString(),
            ProviderStatus = result.ProviderStatus,
            BlockchainTxHash = result.BlockchainTxHash,
            BankReference = result.BankReference,
            FailureReason = result.FailureReason,
            Timestamp = result.Timestamp,
            Provider = result.Provider.ToString()
        }));
    }

    /// <summary>
    /// Cancels a payout.
    /// </summary>
    [HttpPost("payouts/{id}/cancel")]
    public async Task<IActionResult> CancelPayout(
        [FromRoute] string id,
        [FromQuery] PaymentProvider? provider,
        CancellationToken cancellationToken)
    {
        var result = await _paymentService.CancelPayoutAsync(id, provider, cancellationToken);

        if (!result.Success)
            return BadRequest(ApiResponse<object>.Error(result.ErrorCode!, result.ErrorMessage!));

        return Ok(ApiResponse<PayoutDto>.Ok(MapToPayoutDto(result.Payout!)));
    }

    #endregion

    #region Private Mappers

    private static CustomerDto MapToCustomerDto(Core.Models.Responses.CustomerResponse result)
    {
        return new CustomerDto
        {
            Id = result.Customer!.Id,
            ExternalId = result.Customer.ExternalId,
            Type = result.Customer.Type.ToString(),
            Status = result.Customer.Status.ToString(),
            VerificationStatus = result.Customer.VerificationStatus.ToString(),
            VerificationLevel = result.Customer.VerificationLevel.ToString(),
            Provider = result.Provider.ToString(),
            ProviderCustomerId = result.ProviderCustomerId,
            CreatedAt = result.Customer.CreatedAt,
            UpdatedAt = result.Customer.UpdatedAt
        };
    }

    private static VerificationDto MapToVerificationDto(Core.Models.Responses.VerificationResponse result)
    {
        return new VerificationDto
        {
            CustomerId = result.CustomerId,
            SessionId = result.SessionId,
            VerificationUrl = result.VerificationUrl,
            Status = result.Status.ToString(),
            Level = result.Level.ToString(),
            ExpiresAt = result.ExpiresAt,
            RejectionReason = result.RejectionReason,
            Provider = result.Provider.ToString()
        };
    }

    private static QuoteDto MapToQuoteDto(Core.Models.Responses.QuoteData quote)
    {
        return new QuoteDto
        {
            Id = quote.Id,
            ProviderQuoteId = quote.ProviderQuoteId,
            SourceCurrency = quote.SourceCurrency.ToString(),
            TargetCurrency = quote.TargetCurrency.ToString(),
            SourceAmount = quote.SourceAmount,
            TargetAmount = quote.TargetAmount,
            ExchangeRate = quote.ExchangeRate,
            FeeAmount = quote.FeeAmount,
            TotalAmount = quote.TotalAmount,
            Network = quote.Network.ToString(),
            Provider = quote.Provider.ToString(),
            ExpiresAt = quote.ExpiresAt,
            CreatedAt = quote.CreatedAt
        };
    }

    private static PayoutDto MapToPayoutDto(Core.Models.Responses.PayoutData payout)
    {
        return new PayoutDto
        {
            Id = payout.Id,
            ExternalId = payout.ExternalId,
            ProviderPayoutId = payout.ProviderPayoutId,
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
