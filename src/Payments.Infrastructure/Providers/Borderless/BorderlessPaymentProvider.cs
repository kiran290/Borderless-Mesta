using System.Diagnostics;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Payments.Core.Enums;
using Payments.Core.Exceptions;
using Payments.Core.Interfaces;
using Payments.Core.Models;
using Payments.Core.Models.Requests;
using Payments.Core.Models.Responses;
using Payments.Infrastructure.Configuration;

namespace Payments.Infrastructure.Providers.Borderless;

/// <summary>
/// Unified Borderless payment provider implementation.
/// Supports customer management, KYC/KYB verification, and stablecoin payouts.
/// </summary>
public sealed class BorderlessPaymentProvider : IPaymentProvider
{
    private readonly HttpClient _httpClient;
    private readonly BorderlessSettings _settings;
    private readonly ILogger<BorderlessPaymentProvider> _logger;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly SemaphoreSlim _authLock = new(1, 1);

    private string? _accessToken;
    private DateTimeOffset _tokenExpiry = DateTimeOffset.MinValue;

    public PayoutProvider ProviderId => PayoutProvider.Borderless;
    public string ProviderName => "Borderless";

    public BorderlessPaymentProvider(
        HttpClient httpClient,
        IOptions<BorderlessSettings> settings,
        ILogger<BorderlessPaymentProvider> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };

        ConfigureHttpClient();
    }

    private void ConfigureHttpClient()
    {
        if (_httpClient.BaseAddress == null)
        {
            _httpClient.BaseAddress = new Uri(_settings.BaseUrl.TrimEnd('/') + "/");
        }
        _httpClient.Timeout = TimeSpan.FromSeconds(_settings.TimeoutSeconds);
    }

    private async Task EnsureAuthenticatedAsync(CancellationToken cancellationToken)
    {
        if (!string.IsNullOrEmpty(_accessToken) && DateTimeOffset.UtcNow < _tokenExpiry)
        {
            return;
        }

        await _authLock.WaitAsync(cancellationToken);
        try
        {
            if (!string.IsNullOrEmpty(_accessToken) && DateTimeOffset.UtcNow < _tokenExpiry)
            {
                return;
            }

            _logger.LogInformation("Authenticating with Borderless API");

            var authRequest = new BorderlessAuthRequest
            {
                ClientId = _settings.ClientId,
                ApiKey = _settings.ApiKey,
                ApiSecret = _settings.ApiSecret
            };

            var response = await _httpClient.PostAsJsonAsync("auth/token", authRequest, _jsonOptions, cancellationToken);
            var content = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                throw new ProviderAuthenticationException(PayoutProvider.Borderless, $"Authentication failed: {content}");
            }

            var authResponse = JsonSerializer.Deserialize<BorderlessResponse<BorderlessAuthResponse>>(content, _jsonOptions);

            if (authResponse?.Data == null)
            {
                throw new ProviderAuthenticationException(PayoutProvider.Borderless, "Invalid authentication response");
            }

            _accessToken = authResponse.Data.AccessToken;
            _tokenExpiry = DateTimeOffset.UtcNow.AddSeconds(authResponse.Data.ExpiresIn - 60);

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);

            _logger.LogInformation("Successfully authenticated with Borderless API");
        }
        finally
        {
            _authLock.Release();
        }
    }

    #region Health Check

    public async Task<ProviderHealthResult> CheckHealthAsync(CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            await EnsureAuthenticatedAsync(cancellationToken);
            var response = await _httpClient.GetAsync("health", cancellationToken);
            stopwatch.Stop();

            return new ProviderHealthResult
            {
                IsHealthy = response.IsSuccessStatusCode,
                Status = response.IsSuccessStatusCode ? "healthy" : "unhealthy",
                Message = response.IsSuccessStatusCode ? null : $"Status code: {response.StatusCode}",
                Latency = stopwatch.Elapsed
            };
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogWarning(ex, "Borderless health check failed");
            return new ProviderHealthResult
            {
                IsHealthy = false,
                Status = "unhealthy",
                Message = ex.Message,
                Latency = stopwatch.Elapsed
            };
        }
    }

    #endregion

    #region Customer Operations

    public async Task<CustomerResult> CreateCustomerAsync(
        CreateCustomerRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating customer in Borderless: {Email}", request.Contact.Email);

        try
        {
            await EnsureAuthenticatedAsync(cancellationToken);

            var role = request.Role switch
            {
                CustomerRole.Sender => "sender",
                CustomerRole.Beneficiary => "beneficiary",
                _ => "both"
            };

            var borderlessRequest = new BorderlessCreateCustomerRequest
            {
                Type = request.Type == CustomerType.Individual ? "individual" : "business",
                Role = role,
                FirstName = request.Individual?.FirstName,
                LastName = request.Individual?.LastName,
                CompanyName = request.Business?.LegalName,
                Email = request.Contact.Email,
                Phone = request.Contact.Phone,
                DateOfBirth = request.Individual?.DateOfBirth?.ToString("yyyy-MM-dd"),
                Nationality = request.Individual?.Nationality,
                Address = request.Address != null ? new BorderlessAddress
                {
                    Line1 = request.Address.Street1,
                    Line2 = request.Address.Street2,
                    City = request.Address.City,
                    State = request.Address.State,
                    PostalCode = request.Address.PostalCode,
                    Country = request.Address.CountryCode
                } : null,
                ExternalId = request.ExternalId,
                Metadata = request.Metadata
            };

            var response = await PostAsync<BorderlessCustomerResponse>("customers", borderlessRequest, cancellationToken);

            var customer = MapToCustomer(response, request.Type, request.Role);
            _logger.LogInformation("Customer created in Borderless with ID: {CustomerId}", response.Id);

            return CustomerResult.Succeeded(customer, PayoutProvider.Borderless, response.Id);
        }
        catch (ProviderApiException ex)
        {
            _logger.LogError(ex, "Failed to create customer in Borderless");
            return CustomerResult.Failed(ex.ProviderErrorCode ?? "CREATE_CUSTOMER_ERROR", ex.ProviderErrorMessage ?? ex.Message, PayoutProvider.Borderless);
        }
    }

    public async Task<CustomerResult> GetCustomerAsync(
        string customerId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting customer from Borderless: {CustomerId}", customerId);

        try
        {
            await EnsureAuthenticatedAsync(cancellationToken);

            var response = await GetAsync<BorderlessCustomerResponse>($"customers/{customerId}", cancellationToken);
            var customer = MapToCustomer(response, null, null);

            return CustomerResult.Succeeded(customer, PayoutProvider.Borderless, response.Id);
        }
        catch (ProviderApiException ex)
        {
            _logger.LogError(ex, "Failed to get customer from Borderless: {CustomerId}", customerId);
            return CustomerResult.Failed(ex.ProviderErrorCode ?? "GET_CUSTOMER_ERROR", ex.ProviderErrorMessage ?? ex.Message, PayoutProvider.Borderless);
        }
    }

    public async Task<CustomerResult> UpdateCustomerAsync(
        string customerId,
        UpdateCustomerRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Updating customer in Borderless: {CustomerId}", customerId);

        try
        {
            await EnsureAuthenticatedAsync(cancellationToken);

            var borderlessRequest = new BorderlessUpdateCustomerRequest
            {
                FirstName = request.Individual?.FirstName,
                LastName = request.Individual?.LastName,
                CompanyName = request.Business?.LegalName,
                Email = request.Contact?.Email,
                Phone = request.Contact?.Phone,
                Address = request.Address != null ? new BorderlessAddress
                {
                    Line1 = request.Address.Street1,
                    Line2 = request.Address.Street2,
                    City = request.Address.City,
                    State = request.Address.State,
                    PostalCode = request.Address.PostalCode,
                    Country = request.Address.CountryCode
                } : null,
                Metadata = request.Metadata
            };

            var response = await PatchAsync<BorderlessCustomerResponse>($"customers/{customerId}", borderlessRequest, cancellationToken);
            var customer = MapToCustomer(response, null, null);

            _logger.LogInformation("Customer updated in Borderless: {CustomerId}", customerId);
            return CustomerResult.Succeeded(customer, PayoutProvider.Borderless, response.Id);
        }
        catch (ProviderApiException ex)
        {
            _logger.LogError(ex, "Failed to update customer in Borderless: {CustomerId}", customerId);
            return CustomerResult.Failed(ex.ProviderErrorCode ?? "UPDATE_CUSTOMER_ERROR", ex.ProviderErrorMessage ?? ex.Message, PayoutProvider.Borderless);
        }
    }

    public async Task<CustomerListResult> ListCustomersAsync(
        CustomerListRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Listing customers from Borderless");

        try
        {
            await EnsureAuthenticatedAsync(cancellationToken);

            var queryParams = new List<string>
            {
                $"page={request.Page}",
                $"limit={request.PageSize}"
            };

            if (request.Type.HasValue)
                queryParams.Add($"type={request.Type.Value.ToString().ToLowerInvariant()}");
            if (request.Status.HasValue)
                queryParams.Add($"status={request.Status.Value.ToString().ToLowerInvariant()}");
            if (!string.IsNullOrEmpty(request.Search))
                queryParams.Add($"search={Uri.EscapeDataString(request.Search)}");

            var endpoint = $"customers?{string.Join("&", queryParams)}";
            var response = await GetAsync<BorderlessCustomerListResponse>(endpoint, cancellationToken);

            var customers = response.Items.Select(c => MapToCustomer(c, null, null)).ToList();

            return CustomerListResult.Succeeded(
                customers,
                response.Total,
                request.Page,
                request.PageSize,
                PayoutProvider.Borderless);
        }
        catch (ProviderApiException ex)
        {
            _logger.LogError(ex, "Failed to list customers from Borderless");
            return CustomerListResult.Failed(ex.ProviderErrorCode ?? "LIST_CUSTOMERS_ERROR", ex.ProviderErrorMessage ?? ex.Message, PayoutProvider.Borderless);
        }
    }

    #endregion

    #region KYC Operations

    public async Task<VerificationResult> InitiateKycAsync(
        InitiateKycRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Initiating KYC in Borderless for customer: {CustomerId}", request.CustomerId);

        try
        {
            await EnsureAuthenticatedAsync(cancellationToken);

            var borderlessRequest = new BorderlessKycRequest
            {
                CustomerId = request.CustomerId,
                Level = MapVerificationLevel(request.TargetLevel),
                RedirectUrl = request.RedirectUrl,
                WebhookUrl = request.WebhookUrl
            };

            var response = await PostAsync<BorderlessKycResponse>("kyc/sessions", borderlessRequest, cancellationToken);

            _logger.LogInformation("KYC initiated in Borderless: {SessionId}", response.SessionId);

            return VerificationResult.InitiationSucceeded(
                request.CustomerId,
                response.SessionId,
                response.VerificationUrl,
                PayoutProvider.Borderless,
                response.ExpiresAt);
        }
        catch (ProviderApiException ex)
        {
            _logger.LogError(ex, "Failed to initiate KYC in Borderless: {CustomerId}", request.CustomerId);
            return VerificationResult.Failed(ex.ProviderErrorCode ?? "KYC_INITIATE_ERROR", ex.ProviderErrorMessage ?? ex.Message, PayoutProvider.Borderless);
        }
    }

    public async Task<VerificationResult> GetKycStatusAsync(
        string customerId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting KYC status from Borderless: {CustomerId}", customerId);

        try
        {
            await EnsureAuthenticatedAsync(cancellationToken);

            var response = await GetAsync<BorderlessVerificationStatusResponse>(
                $"customers/{customerId}/verification",
                cancellationToken);

            var verification = MapToVerificationInfo(response);
            return VerificationResult.StatusSucceeded(customerId, verification, PayoutProvider.Borderless);
        }
        catch (ProviderApiException ex)
        {
            _logger.LogError(ex, "Failed to get KYC status from Borderless: {CustomerId}", customerId);
            return VerificationResult.Failed(ex.ProviderErrorCode ?? "KYC_STATUS_ERROR", ex.ProviderErrorMessage ?? ex.Message, PayoutProvider.Borderless);
        }
    }

    #endregion

    #region KYB Operations

    public async Task<VerificationResult> InitiateKybAsync(
        InitiateKybRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Initiating KYB in Borderless for customer: {CustomerId}", request.CustomerId);

        try
        {
            await EnsureAuthenticatedAsync(cancellationToken);

            var borderlessRequest = new BorderlessKybRequest
            {
                CustomerId = request.CustomerId,
                Level = MapVerificationLevel(request.TargetLevel),
                RedirectUrl = request.RedirectUrl,
                WebhookUrl = request.WebhookUrl
            };

            var response = await PostAsync<BorderlessKybResponse>("kyb/sessions", borderlessRequest, cancellationToken);

            _logger.LogInformation("KYB initiated in Borderless: {SessionId}", response.SessionId);

            return VerificationResult.InitiationSucceeded(
                request.CustomerId,
                response.SessionId,
                response.VerificationUrl,
                PayoutProvider.Borderless,
                response.ExpiresAt);
        }
        catch (ProviderApiException ex)
        {
            _logger.LogError(ex, "Failed to initiate KYB in Borderless: {CustomerId}", request.CustomerId);
            return VerificationResult.Failed(ex.ProviderErrorCode ?? "KYB_INITIATE_ERROR", ex.ProviderErrorMessage ?? ex.Message, PayoutProvider.Borderless);
        }
    }

    public async Task<VerificationResult> GetKybStatusAsync(
        string customerId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting KYB status from Borderless: {CustomerId}", customerId);

        try
        {
            await EnsureAuthenticatedAsync(cancellationToken);

            var response = await GetAsync<BorderlessVerificationStatusResponse>(
                $"customers/{customerId}/verification",
                cancellationToken);

            var verification = MapToVerificationInfo(response);
            return VerificationResult.StatusSucceeded(customerId, verification, PayoutProvider.Borderless);
        }
        catch (ProviderApiException ex)
        {
            _logger.LogError(ex, "Failed to get KYB status from Borderless: {CustomerId}", customerId);
            return VerificationResult.Failed(ex.ProviderErrorCode ?? "KYB_STATUS_ERROR", ex.ProviderErrorMessage ?? ex.Message, PayoutProvider.Borderless);
        }
    }

    #endregion

    #region Document Operations

    public async Task<DocumentUploadResult> UploadDocumentAsync(
        UploadDocumentRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Uploading document in Borderless: {CustomerId}, Type: {DocumentType}", request.CustomerId, request.DocumentType);

        try
        {
            await EnsureAuthenticatedAsync(cancellationToken);

            var borderlessRequest = new BorderlessDocumentUploadRequest
            {
                DocumentType = MapDocumentType(request.DocumentType),
                DocumentNumber = request.DocumentNumber,
                IssuingCountry = request.IssuingCountry,
                IssueDate = request.IssueDate?.ToString("yyyy-MM-dd"),
                ExpiryDate = request.ExpiryDate?.ToString("yyyy-MM-dd"),
                FrontImage = request.FrontImageBase64,
                BackImage = request.BackImageBase64,
                ContentType = request.MimeType
            };

            var response = await PostAsync<BorderlessDocumentResponse>(
                $"customers/{request.CustomerId}/documents",
                borderlessRequest,
                cancellationToken);

            _logger.LogInformation("Document uploaded in Borderless: {DocumentId}", response.DocumentId);

            return DocumentUploadResult.Succeeded(
                response.DocumentId,
                request.DocumentType,
                response.Status ?? "uploaded",
                PayoutProvider.Borderless);
        }
        catch (ProviderApiException ex)
        {
            _logger.LogError(ex, "Failed to upload document in Borderless");
            return DocumentUploadResult.Failed(ex.ProviderErrorCode ?? "DOCUMENT_UPLOAD_ERROR", ex.ProviderErrorMessage ?? ex.Message, PayoutProvider.Borderless);
        }
    }

    public async Task<DocumentListResult> GetDocumentsAsync(
        string customerId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting documents from Borderless: {CustomerId}", customerId);

        try
        {
            await EnsureAuthenticatedAsync(cancellationToken);

            var response = await GetAsync<BorderlessDocumentListResponse>(
                $"customers/{customerId}/documents",
                cancellationToken);

            var documents = response.Documents.Select(d => new VerificationDocument
            {
                Id = d.DocumentId,
                Type = MapDocumentTypeFromBorderless(d.DocumentType),
                Status = MapDocumentStatus(d.Status),
                UploadedAt = d.CreatedAt
            }).ToList();

            return DocumentListResult.Succeeded(documents, PayoutProvider.Borderless);
        }
        catch (ProviderApiException ex)
        {
            _logger.LogError(ex, "Failed to get documents from Borderless: {CustomerId}", customerId);
            return DocumentListResult.Failed(ex.ProviderErrorCode ?? "GET_DOCUMENTS_ERROR", ex.ProviderErrorMessage ?? ex.Message, PayoutProvider.Borderless);
        }
    }

    public async Task<VerificationResult> SubmitVerificationAsync(
        string customerId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Submitting verification in Borderless: {CustomerId}", customerId);

        try
        {
            await EnsureAuthenticatedAsync(cancellationToken);

            var response = await PostAsync<BorderlessVerificationStatusResponse>(
                $"customers/{customerId}/verification/submit",
                new { },
                cancellationToken);

            var verification = MapToVerificationInfo(response);
            return VerificationResult.SubmissionSucceeded(customerId, verification.Status, PayoutProvider.Borderless);
        }
        catch (ProviderApiException ex)
        {
            _logger.LogError(ex, "Failed to submit verification in Borderless: {CustomerId}", customerId);
            return VerificationResult.Failed(ex.ProviderErrorCode ?? "VERIFICATION_SUBMIT_ERROR", ex.ProviderErrorMessage ?? ex.Message, PayoutProvider.Borderless);
        }
    }

    #endregion

    #region Payout Operations

    public async Task<QuoteResult> CreateQuoteAsync(
        CreateQuoteRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Getting quote from Borderless: {SourceCurrency} -> {TargetCurrency}",
            request.SourceCurrency,
            request.TargetCurrency);

        try
        {
            await EnsureAuthenticatedAsync(cancellationToken);

            var borderlessRequest = new BorderlessCreateQuoteRequest
            {
                SourceAsset = MapStablecoinToBorderless(request.SourceCurrency),
                SourceNetwork = MapNetworkToBorderless(request.Network),
                TargetCurrency = MapFiatCurrencyToBorderless(request.TargetCurrency),
                TargetCountry = request.DestinationCountry,
                SourceAmount = request.SourceAmount,
                TargetAmount = request.TargetAmount,
                PaymentRail = request.PaymentMethod.HasValue ? MapPaymentMethodToBorderless(request.PaymentMethod.Value) : null
            };

            var response = await PostAsync<BorderlessQuoteResponse>("quotes", borderlessRequest, cancellationToken);

            var quote = new PayoutQuote
            {
                Id = Guid.NewGuid().ToString(),
                ProviderQuoteId = response.QuoteId,
                SourceCurrency = request.SourceCurrency,
                TargetCurrency = request.TargetCurrency,
                SourceAmount = response.SourceAmount,
                TargetAmount = response.TargetAmount,
                ExchangeRate = response.ExchangeRate,
                FeeAmount = response.TotalFee,
                FeeBreakdown = new FeeBreakdown
                {
                    NetworkFee = response.Fees.NetworkFee,
                    ProcessingFee = response.Fees.ProcessingFee,
                    FxSpreadFee = response.Fees.FxFee,
                    BankFee = response.Fees.SettlementFee,
                    DeveloperFee = response.Fees.PartnerFee
                },
                Network = request.Network,
                CreatedAt = response.CreatedAt,
                ExpiresAt = response.ExpiresAt,
                Provider = PayoutProvider.Borderless
            };

            _logger.LogInformation(
                "Quote received from Borderless: {QuoteId}, Rate: {Rate}, Fee: {Fee}",
                response.QuoteId,
                response.ExchangeRate,
                response.TotalFee);

            return QuoteResult.Succeeded(quote);
        }
        catch (ProviderApiException ex)
        {
            _logger.LogError(ex, "Failed to get quote from Borderless");
            return QuoteResult.Failed(ex.ProviderErrorCode ?? "QUOTE_ERROR", ex.ProviderErrorMessage ?? ex.Message);
        }
    }

    public async Task<PayoutResult> CreatePayoutAsync(
        CreatePayoutRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Creating payout via Borderless: {SourceAmount} {SourceCurrency} -> {TargetCurrency}",
            request.SourceAmount ?? request.TargetAmount,
            request.SourceCurrency,
            request.TargetCurrency);

        try
        {
            await EnsureAuthenticatedAsync(cancellationToken);

            // Create sender if no ID
            var sender = request.Sender;
            if (string.IsNullOrEmpty(sender.Id))
            {
                sender = await CreateSenderInternalAsync(sender, cancellationToken);
            }

            // Create beneficiary if no ID
            var beneficiary = request.Beneficiary;
            if (string.IsNullOrEmpty(beneficiary.Id))
            {
                beneficiary = await CreateBeneficiaryInternalAsync(beneficiary, cancellationToken);
            }

            // Get quote if not provided
            string quoteId = request.QuoteId ?? "";

            if (string.IsNullOrEmpty(quoteId))
            {
                var quoteRequest = new BorderlessCreateQuoteRequest
                {
                    SourceAsset = MapStablecoinToBorderless(request.SourceCurrency),
                    SourceNetwork = MapNetworkToBorderless(request.Network),
                    TargetCurrency = MapFiatCurrencyToBorderless(request.TargetCurrency),
                    TargetCountry = beneficiary.BankAccount.CountryCode,
                    SourceAmount = request.SourceAmount,
                    TargetAmount = request.TargetAmount,
                    PaymentRail = MapPaymentMethodToBorderless(request.PaymentMethod)
                };

                var quoteResponse = await PostAsync<BorderlessQuoteResponse>("quotes", quoteRequest, cancellationToken);
                quoteId = quoteResponse.QuoteId;
            }

            // Create offramp transaction
            var offrampRequest = new BorderlessCreateOfframpRequest
            {
                QuoteId = quoteId,
                SenderId = sender.Id!,
                BeneficiaryId = beneficiary.Id!,
                Purpose = "payout",
                Reference = request.ExternalId,
                ExternalId = request.ExternalId,
                Metadata = request.Metadata
            };

            var offrampResponse = await PostAsync<BorderlessOfframpResponse>("offramps", offrampRequest, cancellationToken);

            // Map deposit address
            DepositWallet? depositWallet = null;
            if (offrampResponse.DepositAddress != null)
            {
                depositWallet = MapToDepositWallet(offrampResponse.DepositAddress, request.SourceCurrency);
            }

            var payout = new Payout
            {
                Id = Guid.NewGuid().ToString(),
                ExternalId = request.ExternalId,
                Provider = PayoutProvider.Borderless,
                ProviderOrderId = offrampResponse.TransactionId,
                Status = MapBorderlessStatus(offrampResponse.Status),
                SourceCurrency = request.SourceCurrency,
                SourceAmount = offrampResponse.SourceAmount,
                TargetCurrency = request.TargetCurrency,
                TargetAmount = offrampResponse.TargetAmount,
                ExchangeRate = offrampResponse.ExchangeRate,
                FeeAmount = offrampResponse.TotalFee,
                Network = request.Network,
                Sender = sender,
                Beneficiary = beneficiary,
                DepositWallet = depositWallet,
                QuoteId = quoteId,
                PaymentMethod = request.PaymentMethod,
                CreatedAt = offrampResponse.CreatedAt,
                UpdatedAt = offrampResponse.UpdatedAt,
                Metadata = request.Metadata
            };

            _logger.LogInformation(
                "Payout created via Borderless: {PayoutId}, TransactionId: {TransactionId}, Status: {Status}",
                payout.Id,
                offrampResponse.TransactionId,
                offrampResponse.Status);

            return PayoutResult.Succeeded(payout, PayoutProvider.Borderless);
        }
        catch (ProviderApiException ex)
        {
            _logger.LogError(ex, "Failed to create payout via Borderless");
            return PayoutResult.Failed(ex.ProviderErrorCode ?? "PAYOUT_ERROR", ex.ProviderErrorMessage ?? ex.Message, PayoutProvider.Borderless);
        }
    }

    public async Task<PayoutResult> GetPayoutAsync(
        string payoutId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting payout from Borderless: {PayoutId}", payoutId);

        try
        {
            await EnsureAuthenticatedAsync(cancellationToken);

            var response = await GetAsync<BorderlessOfframpResponse>($"offramps/{payoutId}", cancellationToken);

            var payout = new Payout
            {
                Id = response.TransactionId,
                ProviderOrderId = response.TransactionId,
                Provider = PayoutProvider.Borderless,
                Status = MapBorderlessStatus(response.Status),
                SourceAmount = response.SourceAmount,
                TargetAmount = response.TargetAmount,
                ExchangeRate = response.ExchangeRate,
                FeeAmount = response.TotalFee,
                CreatedAt = response.CreatedAt,
                UpdatedAt = response.UpdatedAt
            };

            return PayoutResult.Succeeded(payout, PayoutProvider.Borderless);
        }
        catch (ProviderApiException ex)
        {
            _logger.LogError(ex, "Failed to get payout from Borderless: {PayoutId}", payoutId);
            return PayoutResult.Failed(ex.ProviderErrorCode ?? "GET_PAYOUT_ERROR", ex.ProviderErrorMessage ?? ex.Message, PayoutProvider.Borderless);
        }
    }

    public async Task<PayoutStatusResult> GetPayoutStatusAsync(
        string payoutId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting payout status from Borderless: {PayoutId}", payoutId);

        try
        {
            await EnsureAuthenticatedAsync(cancellationToken);

            var response = await GetAsync<BorderlessOfframpResponse>($"offramps/{payoutId}", cancellationToken);

            var statusUpdate = new PayoutStatusUpdate
            {
                PayoutId = response.ExternalId ?? payoutId,
                ProviderOrderId = response.TransactionId,
                CurrentStatus = MapBorderlessStatus(response.Status),
                ProviderStatus = response.Status,
                BlockchainTxHash = response.BlockchainTxHash,
                BankReference = response.BankReference,
                FailureReason = response.FailureReason,
                Timestamp = response.UpdatedAt,
                Provider = PayoutProvider.Borderless
            };

            return PayoutStatusResult.Succeeded(statusUpdate);
        }
        catch (ProviderApiException ex)
        {
            _logger.LogError(ex, "Failed to get payout status from Borderless: {PayoutId}", payoutId);
            return PayoutStatusResult.Failed(ex.ProviderErrorCode ?? "STATUS_ERROR", ex.ProviderErrorMessage ?? ex.Message);
        }
    }

    public async Task<PayoutResult> CancelPayoutAsync(
        string payoutId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Cancelling payout via Borderless: {PayoutId}", payoutId);

        try
        {
            await EnsureAuthenticatedAsync(cancellationToken);

            var response = await _httpClient.PostAsync($"offramps/{payoutId}/cancel", null, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Payout cancelled via Borderless: {PayoutId}", payoutId);

                var payout = new Payout
                {
                    Id = payoutId,
                    ProviderOrderId = payoutId,
                    Provider = PayoutProvider.Borderless,
                    Status = PayoutStatus.Cancelled
                };

                return PayoutResult.Succeeded(payout, PayoutProvider.Borderless);
            }

            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning("Failed to cancel payout via Borderless: {PayoutId}, Response: {Response}", payoutId, errorContent);
            return PayoutResult.Failed("CANCEL_FAILED", errorContent, PayoutProvider.Borderless);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling payout via Borderless: {PayoutId}", payoutId);
            return PayoutResult.Failed("CANCEL_ERROR", ex.Message, PayoutProvider.Borderless);
        }
    }

    public async Task<PayoutListResult> ListPayoutsAsync(
        PayoutListRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Listing payouts from Borderless");

        try
        {
            await EnsureAuthenticatedAsync(cancellationToken);

            var queryParams = new List<string>
            {
                $"page={request.Page}",
                $"limit={request.PageSize}"
            };

            if (!string.IsNullOrEmpty(request.CustomerId))
                queryParams.Add($"customerId={Uri.EscapeDataString(request.CustomerId)}");
            if (request.Status.HasValue)
                queryParams.Add($"status={request.Status.Value.ToString().ToLowerInvariant()}");
            if (request.FromDate.HasValue)
                queryParams.Add($"fromDate={request.FromDate.Value:yyyy-MM-dd}");
            if (request.ToDate.HasValue)
                queryParams.Add($"toDate={request.ToDate.Value:yyyy-MM-dd}");

            var endpoint = $"offramps?{string.Join("&", queryParams)}";
            var response = await GetAsync<BorderlessOfframpListResponse>(endpoint, cancellationToken);

            var payouts = response.Items.Select(o => new Payout
            {
                Id = o.TransactionId,
                ProviderOrderId = o.TransactionId,
                Provider = PayoutProvider.Borderless,
                Status = MapBorderlessStatus(o.Status),
                SourceAmount = o.SourceAmount,
                TargetAmount = o.TargetAmount,
                ExchangeRate = o.ExchangeRate,
                FeeAmount = o.TotalFee,
                CreatedAt = o.CreatedAt,
                UpdatedAt = o.UpdatedAt
            }).ToList();

            return PayoutListResult.Succeeded(
                payouts,
                response.Total,
                request.Page,
                request.PageSize,
                PayoutProvider.Borderless);
        }
        catch (ProviderApiException ex)
        {
            _logger.LogError(ex, "Failed to list payouts from Borderless");
            return PayoutListResult.Failed(ex.ProviderErrorCode ?? "LIST_PAYOUTS_ERROR", ex.ProviderErrorMessage ?? ex.Message, PayoutProvider.Borderless);
        }
    }

    #endregion

    #region Webhook Handling

    public bool ValidateWebhookSignature(string payload, string signature, string secret)
    {
        if (string.IsNullOrEmpty(secret))
        {
            _logger.LogWarning("Webhook secret not provided for validation");
            return false;
        }

        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        var computedSignature = Convert.ToHexString(computedHash).ToLowerInvariant();

        return string.Equals(signature, computedSignature, StringComparison.OrdinalIgnoreCase);
    }

    public Task<WebhookResult> ProcessWebhookAsync(
        string payload,
        string signature,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!ValidateWebhookSignature(payload, signature, _settings.WebhookSecret))
            {
                return Task.FromResult(new WebhookResult
                {
                    Success = false,
                    EventType = "unknown",
                    Error = "Invalid webhook signature"
                });
            }

            var webhookPayload = JsonSerializer.Deserialize<BorderlessWebhookPayload>(payload, _jsonOptions);

            if (webhookPayload?.Data == null)
            {
                _logger.LogWarning("Invalid Borderless webhook payload");
                return Task.FromResult(new WebhookResult
                {
                    Success = false,
                    EventType = "unknown",
                    Error = "Invalid payload structure"
                });
            }

            var result = new WebhookResult
            {
                Success = true,
                EventType = webhookPayload.EventType,
                ResourceId = webhookPayload.Data.TransactionId,
                ResourceType = "offramp",
                Data = new PayoutStatusUpdate
                {
                    PayoutId = webhookPayload.Data.ExternalId ?? webhookPayload.Data.TransactionId,
                    ProviderOrderId = webhookPayload.Data.TransactionId,
                    CurrentStatus = MapBorderlessStatus(webhookPayload.Data.Status),
                    ProviderStatus = webhookPayload.Data.Status,
                    BlockchainTxHash = webhookPayload.Data.BlockchainTxHash,
                    BankReference = webhookPayload.Data.BankReference,
                    FailureReason = webhookPayload.Data.FailureReason,
                    Timestamp = webhookPayload.Timestamp,
                    Provider = PayoutProvider.Borderless
                }
            };

            _logger.LogInformation(
                "Processed Borderless webhook: Event={Event}, TransactionId={TransactionId}, Status={Status}",
                webhookPayload.EventType,
                webhookPayload.Data.TransactionId,
                webhookPayload.Data.Status);

            return Task.FromResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Borderless webhook");
            return Task.FromResult(new WebhookResult
            {
                Success = false,
                EventType = "unknown",
                Error = ex.Message
            });
        }
    }

    #endregion

    #region Private Helper Methods

    private async Task<Sender> CreateSenderInternalAsync(Sender sender, CancellationToken cancellationToken)
    {
        var request = new BorderlessCreateCustomerRequest
        {
            Type = sender.Type == BeneficiaryType.Individual ? "individual" : "business",
            Role = "sender",
            FirstName = sender.FirstName,
            LastName = sender.LastName,
            CompanyName = sender.BusinessName,
            Email = sender.Email,
            Phone = sender.PhoneNumber,
            DateOfBirth = sender.DateOfBirth?.ToString("yyyy-MM-dd"),
            Nationality = sender.Nationality,
            Address = sender.Address != null ? new BorderlessAddress
            {
                Line1 = sender.Address.Street1,
                Line2 = sender.Address.Street2,
                City = sender.Address.City,
                State = sender.Address.State,
                PostalCode = sender.Address.PostalCode,
                Country = sender.Address.CountryCode
            } : null,
            Identification = !string.IsNullOrEmpty(sender.DocumentNumber) ? new BorderlessIdentification
            {
                Type = sender.DocumentType ?? "passport",
                Number = sender.DocumentNumber
            } : null,
            ExternalId = sender.ExternalId
        };

        var response = await PostAsync<BorderlessCustomerResponse>("customers", request, cancellationToken);
        sender.Id = response.Id;
        return sender;
    }

    private async Task<Beneficiary> CreateBeneficiaryInternalAsync(Beneficiary beneficiary, CancellationToken cancellationToken)
    {
        var request = new BorderlessCreateCustomerRequest
        {
            Type = beneficiary.Type == BeneficiaryType.Individual ? "individual" : "business",
            Role = "beneficiary",
            FirstName = beneficiary.FirstName,
            LastName = beneficiary.LastName,
            CompanyName = beneficiary.BusinessName,
            Email = beneficiary.Email,
            Phone = beneficiary.PhoneNumber,
            DateOfBirth = beneficiary.DateOfBirth?.ToString("yyyy-MM-dd"),
            Nationality = beneficiary.Nationality,
            Address = beneficiary.Address != null ? new BorderlessAddress
            {
                Line1 = beneficiary.Address.Street1,
                Line2 = beneficiary.Address.Street2,
                City = beneficiary.Address.City,
                State = beneficiary.Address.State,
                PostalCode = beneficiary.Address.PostalCode,
                Country = beneficiary.Address.CountryCode
            } : null,
            Identification = !string.IsNullOrEmpty(beneficiary.DocumentNumber) ? new BorderlessIdentification
            {
                Type = beneficiary.DocumentType ?? "passport",
                Number = beneficiary.DocumentNumber
            } : null,
            BankAccount = new BorderlessBankAccount
            {
                BankName = beneficiary.BankAccount.BankName,
                AccountNumber = beneficiary.BankAccount.AccountNumber,
                AccountName = beneficiary.BankAccount.AccountHolderName,
                RoutingNumber = beneficiary.BankAccount.RoutingNumber,
                SwiftCode = beneficiary.BankAccount.SwiftCode,
                SortCode = beneficiary.BankAccount.SortCode,
                Iban = beneficiary.BankAccount.Iban,
                Currency = MapFiatCurrencyToBorderless(beneficiary.BankAccount.Currency),
                Country = beneficiary.BankAccount.CountryCode,
                BranchCode = beneficiary.BankAccount.BranchCode
            },
            ExternalId = beneficiary.ExternalId
        };

        var response = await PostAsync<BorderlessCustomerResponse>("customers", request, cancellationToken);
        beneficiary.Id = response.Id;
        return beneficiary;
    }

    private async Task<T> GetAsync<T>(string endpoint, CancellationToken cancellationToken)
    {
        var response = await _httpClient.GetAsync(endpoint, cancellationToken);
        return await HandleResponseAsync<T>(response, cancellationToken);
    }

    private async Task<T> PostAsync<T>(string endpoint, object request, CancellationToken cancellationToken)
    {
        var response = await _httpClient.PostAsJsonAsync(endpoint, request, _jsonOptions, cancellationToken);
        return await HandleResponseAsync<T>(response, cancellationToken);
    }

    private async Task<T> PatchAsync<T>(string endpoint, object request, CancellationToken cancellationToken)
    {
        var content = JsonContent.Create(request, options: _jsonOptions);
        var response = await _httpClient.PatchAsync(endpoint, content, cancellationToken);
        return await HandleResponseAsync<T>(response, cancellationToken);
    }

    private async Task<T> HandleResponseAsync<T>(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var content = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            BorderlessError? error = null;
            try
            {
                var errorResponse = JsonSerializer.Deserialize<BorderlessResponse<object>>(content, _jsonOptions);
                error = errorResponse?.Error;
            }
            catch { }

            throw new ProviderApiException(
                PayoutProvider.Borderless,
                $"Borderless API error: {response.StatusCode}",
                (int)response.StatusCode,
                error?.Code,
                error?.Message ?? content);
        }

        var result = JsonSerializer.Deserialize<BorderlessResponse<T>>(content, _jsonOptions);

        if (result?.Data == null)
        {
            throw new ProviderApiException(
                PayoutProvider.Borderless,
                "Invalid response from Borderless API",
                (int)response.StatusCode);
        }

        return result.Data;
    }

    private static Customer MapToCustomer(BorderlessCustomerResponse response, CustomerType? requestType, CustomerRole? requestRole)
    {
        var customerType = requestType ?? (response.Type?.ToLowerInvariant() == "business"
            ? CustomerType.Business
            : CustomerType.Individual);

        var role = requestRole ?? (response.Role?.ToLowerInvariant() switch
        {
            "sender" => CustomerRole.Sender,
            "beneficiary" => CustomerRole.Beneficiary,
            _ => CustomerRole.Both
        });

        return new Customer
        {
            Id = response.Id,
            Type = customerType,
            Role = role,
            Status = MapCustomerStatus(response.Status),
            Individual = customerType == CustomerType.Individual ? new IndividualDetails
            {
                FirstName = response.FirstName ?? string.Empty,
                LastName = response.LastName ?? string.Empty
            } : null,
            Business = customerType == CustomerType.Business ? new BusinessDetails
            {
                LegalName = response.CompanyName ?? string.Empty,
                CountryOfIncorporation = "US"
            } : null,
            Contact = new ContactInfo
            {
                Email = response.Email,
                Phone = response.Phone
            },
            CreatedAt = response.CreatedAt,
            UpdatedAt = response.UpdatedAt
        };
    }

    private static VerificationInfo MapToVerificationInfo(BorderlessVerificationStatusResponse response)
    {
        return new VerificationInfo
        {
            Status = MapVerificationStatus(response.Status),
            Level = MapVerificationLevelFromBorderless(response.Level),
            KycCompleted = response.KycStatus?.ToLowerInvariant() == "approved",
            KybCompleted = response.KybStatus?.ToLowerInvariant() == "approved",
            RejectionReason = response.RejectionReason,
            SubmittedAt = response.SubmittedAt,
            CompletedAt = response.CompletedAt,
            ExpiresAt = response.ExpiresAt
        };
    }

    private static DepositWallet MapToDepositWallet(BorderlessDepositAddress address, Stablecoin currency)
    {
        return new DepositWallet
        {
            Id = address.Id,
            Address = address.Address,
            Network = MapBorderlessNetwork(address.Network),
            Currency = currency,
            ExpectedAmount = address.ExpectedAmount,
            ExpiresAt = address.ExpiresAt,
            Memo = address.Memo ?? address.Tag
        };
    }

    private static CustomerStatus MapCustomerStatus(string? status) => status?.ToLowerInvariant() switch
    {
        "active" => CustomerStatus.Active,
        "pending" => CustomerStatus.Pending,
        "suspended" => CustomerStatus.Suspended,
        "blocked" => CustomerStatus.Blocked,
        "closed" => CustomerStatus.Closed,
        _ => CustomerStatus.Pending
    };

    private static VerificationStatus MapVerificationStatus(string? status) => status?.ToLowerInvariant() switch
    {
        "not_started" => VerificationStatus.NotStarted,
        "pending" => VerificationStatus.Pending,
        "in_review" or "under_review" => VerificationStatus.InReview,
        "additional_info_required" => VerificationStatus.AdditionalInfoRequired,
        "approved" or "verified" => VerificationStatus.Approved,
        "rejected" or "failed" => VerificationStatus.Rejected,
        "expired" => VerificationStatus.Expired,
        _ => VerificationStatus.NotStarted
    };

    private static string MapVerificationLevel(VerificationLevel level) => level switch
    {
        VerificationLevel.Basic => "basic",
        VerificationLevel.Standard => "standard",
        VerificationLevel.Enhanced => "enhanced",
        VerificationLevel.Full => "full",
        _ => "standard"
    };

    private static VerificationLevel MapVerificationLevelFromBorderless(string? level) => level?.ToLowerInvariant() switch
    {
        "basic" => VerificationLevel.Basic,
        "standard" => VerificationLevel.Standard,
        "enhanced" => VerificationLevel.Enhanced,
        "full" => VerificationLevel.Full,
        _ => VerificationLevel.None
    };

    private static string MapDocumentType(DocumentType type) => type switch
    {
        DocumentType.Passport => "passport",
        DocumentType.NationalId => "national_id",
        DocumentType.DriversLicense => "drivers_license",
        DocumentType.UtilityBill => "utility_bill",
        DocumentType.BankStatement => "bank_statement",
        DocumentType.CertificateOfIncorporation => "certificate_of_incorporation",
        DocumentType.BusinessRegistration => "business_registration",
        DocumentType.ArticlesOfAssociation => "articles_of_association",
        DocumentType.ShareholderRegister => "shareholder_register",
        DocumentType.UboDeclaration => "ubo_declaration",
        _ => "other"
    };

    private static DocumentType MapDocumentTypeFromBorderless(string? type) => type?.ToLowerInvariant() switch
    {
        "passport" => DocumentType.Passport,
        "national_id" => DocumentType.NationalId,
        "drivers_license" => DocumentType.DriversLicense,
        "utility_bill" => DocumentType.UtilityBill,
        "bank_statement" => DocumentType.BankStatement,
        "certificate_of_incorporation" => DocumentType.CertificateOfIncorporation,
        "business_registration" => DocumentType.BusinessRegistration,
        "articles_of_association" => DocumentType.ArticlesOfAssociation,
        "shareholder_register" => DocumentType.ShareholderRegister,
        "ubo_declaration" => DocumentType.UboDeclaration,
        _ => DocumentType.Other
    };

    private static VerificationStatus MapDocumentStatus(string? status) => status?.ToLowerInvariant() switch
    {
        "uploaded" or "pending" => VerificationStatus.Pending,
        "in_review" => VerificationStatus.InReview,
        "approved" or "verified" => VerificationStatus.Approved,
        "rejected" => VerificationStatus.Rejected,
        _ => VerificationStatus.Pending
    };

    private static string MapStablecoinToBorderless(Stablecoin coin) => coin switch
    {
        Stablecoin.USDC => "USDC",
        Stablecoin.USDT => "USDT",
        _ => throw new ArgumentOutOfRangeException(nameof(coin))
    };

    private static string MapFiatCurrencyToBorderless(FiatCurrency currency) => currency switch
    {
        FiatCurrency.USD => "USD",
        FiatCurrency.EUR => "EUR",
        FiatCurrency.GBP => "GBP",
        _ => throw new ArgumentOutOfRangeException(nameof(currency))
    };

    private static string MapNetworkToBorderless(BlockchainNetwork network) => network switch
    {
        BlockchainNetwork.Ethereum => "ethereum",
        BlockchainNetwork.Polygon => "polygon",
        BlockchainNetwork.Arbitrum => "arbitrum",
        BlockchainNetwork.Optimism => "optimism",
        BlockchainNetwork.Base => "base",
        BlockchainNetwork.Tron => "tron",
        BlockchainNetwork.Solana => "solana",
        _ => throw new ArgumentOutOfRangeException(nameof(network))
    };

    private static BlockchainNetwork MapBorderlessNetwork(string network) => network.ToLowerInvariant() switch
    {
        "ethereum" or "eth" => BlockchainNetwork.Ethereum,
        "polygon" or "matic" => BlockchainNetwork.Polygon,
        "arbitrum" or "arb" => BlockchainNetwork.Arbitrum,
        "optimism" or "op" => BlockchainNetwork.Optimism,
        "base" => BlockchainNetwork.Base,
        "tron" or "trx" => BlockchainNetwork.Tron,
        "solana" or "sol" => BlockchainNetwork.Solana,
        _ => throw new ArgumentOutOfRangeException(nameof(network))
    };

    private static string MapPaymentMethodToBorderless(PaymentMethod method) => method switch
    {
        PaymentMethod.BankTransfer => "local_bank",
        PaymentMethod.Sepa => "sepa",
        PaymentMethod.Ach => "ach",
        PaymentMethod.FasterPayments => "faster_payments",
        PaymentMethod.Swift => "swift",
        _ => throw new ArgumentOutOfRangeException(nameof(method))
    };

    private static PayoutStatus MapBorderlessStatus(string status) => status.ToLowerInvariant() switch
    {
        "pending" or "created" => PayoutStatus.Created,
        "awaiting_deposit" or "awaiting_funds" => PayoutStatus.AwaitingFunds,
        "deposit_received" or "funds_received" => PayoutStatus.FundsReceived,
        "processing" or "in_progress" => PayoutStatus.Processing,
        "settlement_initiated" or "sent_to_beneficiary" => PayoutStatus.SentToBeneficiary,
        "completed" or "settled" or "success" => PayoutStatus.Completed,
        "failed" or "error" => PayoutStatus.Failed,
        "cancelled" or "canceled" => PayoutStatus.Cancelled,
        "expired" => PayoutStatus.Expired,
        "under_review" or "pending_review" => PayoutStatus.PendingReview,
        "refunded" => PayoutStatus.Refunded,
        _ => PayoutStatus.Processing
    };

    #endregion
}
