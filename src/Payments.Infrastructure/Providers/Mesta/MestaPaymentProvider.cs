using System.Diagnostics;
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

namespace Payments.Infrastructure.Providers.Mesta;

/// <summary>
/// Unified Mesta payment provider implementation.
/// Supports customer management, KYC/KYB verification, and stablecoin payouts.
/// </summary>
public sealed class MestaPaymentProvider : IPaymentProvider
{
    private readonly HttpClient _httpClient;
    private readonly MestaSettings _settings;
    private readonly ILogger<MestaPaymentProvider> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public PayoutProvider ProviderId => PayoutProvider.Mesta;
    public string ProviderName => "Mesta";

    public MestaPaymentProvider(
        HttpClient httpClient,
        IOptions<MestaSettings> settings,
        ILogger<MestaPaymentProvider> logger)
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

        if (!_httpClient.DefaultRequestHeaders.Contains("X-API-Key"))
        {
            _httpClient.DefaultRequestHeaders.Add("X-API-Key", _settings.ApiKey);
            _httpClient.DefaultRequestHeaders.Add("X-Merchant-Id", _settings.MerchantId);
        }

        _httpClient.Timeout = TimeSpan.FromSeconds(_settings.TimeoutSeconds);
    }

    #region Health Check

    public async Task<ProviderHealthResult> CheckHealthAsync(CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var response = await _httpClient.GetAsync($"merchants/{_settings.MerchantId}", cancellationToken);
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
            _logger.LogWarning(ex, "Mesta health check failed");
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
        _logger.LogInformation("Creating customer in Mesta: {Email}", request.Contact.Email);

        try
        {
            var mestaRequest = new MestaCreateCustomerRequest
            {
                Type = request.Type == CustomerType.Individual ? "individual" : "business",
                FirstName = request.Individual?.FirstName,
                LastName = request.Individual?.LastName,
                BusinessName = request.Business?.LegalName,
                Email = request.Contact.Email,
                Phone = request.Contact.Phone,
                DateOfBirth = request.Individual?.DateOfBirth?.ToString("yyyy-MM-dd"),
                Address = request.Address != null ? new MestaAddress
                {
                    Street1 = request.Address.Street1,
                    Street2 = request.Address.Street2,
                    City = request.Address.City,
                    State = request.Address.State,
                    PostalCode = request.Address.PostalCode,
                    Country = request.Address.CountryCode
                } : null,
                ExternalId = request.ExternalId,
                Metadata = request.Metadata
            };

            var endpoint = request.Role == CustomerRole.Beneficiary ? "beneficiaries" : "senders";
            var response = await PostAsync<MestaCustomerResponse>(endpoint, mestaRequest, cancellationToken);

            var customer = MapToCustomer(response, request.Type, request.Role);
            _logger.LogInformation("Customer created in Mesta with ID: {CustomerId}", response.Id);

            return CustomerResult.Succeeded(customer, PayoutProvider.Mesta, response.Id);
        }
        catch (ProviderApiException ex)
        {
            _logger.LogError(ex, "Failed to create customer in Mesta");
            return CustomerResult.Failed(ex.ProviderErrorCode ?? "CREATE_CUSTOMER_ERROR", ex.ProviderErrorMessage ?? ex.Message, PayoutProvider.Mesta);
        }
    }

    public async Task<CustomerResult> GetCustomerAsync(
        string customerId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting customer from Mesta: {CustomerId}", customerId);

        try
        {
            var response = await GetAsync<MestaCustomerResponse>($"customers/{customerId}", cancellationToken);
            var customer = MapToCustomer(response, null, null);

            return CustomerResult.Succeeded(customer, PayoutProvider.Mesta, response.Id);
        }
        catch (ProviderApiException ex)
        {
            _logger.LogError(ex, "Failed to get customer from Mesta: {CustomerId}", customerId);
            return CustomerResult.Failed(ex.ProviderErrorCode ?? "GET_CUSTOMER_ERROR", ex.ProviderErrorMessage ?? ex.Message, PayoutProvider.Mesta);
        }
    }

    public async Task<CustomerResult> UpdateCustomerAsync(
        string customerId,
        UpdateCustomerRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Updating customer in Mesta: {CustomerId}", customerId);

        try
        {
            var mestaRequest = new MestaUpdateCustomerRequest
            {
                FirstName = request.Individual?.FirstName,
                LastName = request.Individual?.LastName,
                BusinessName = request.Business?.LegalName,
                Email = request.Contact?.Email,
                Phone = request.Contact?.Phone,
                Address = request.Address != null ? new MestaAddress
                {
                    Street1 = request.Address.Street1,
                    Street2 = request.Address.Street2,
                    City = request.Address.City,
                    State = request.Address.State,
                    PostalCode = request.Address.PostalCode,
                    Country = request.Address.CountryCode
                } : null,
                Metadata = request.Metadata
            };

            var response = await PatchAsync<MestaCustomerResponse>($"customers/{customerId}", mestaRequest, cancellationToken);
            var customer = MapToCustomer(response, null, null);

            _logger.LogInformation("Customer updated in Mesta: {CustomerId}", customerId);
            return CustomerResult.Succeeded(customer, PayoutProvider.Mesta, response.Id);
        }
        catch (ProviderApiException ex)
        {
            _logger.LogError(ex, "Failed to update customer in Mesta: {CustomerId}", customerId);
            return CustomerResult.Failed(ex.ProviderErrorCode ?? "UPDATE_CUSTOMER_ERROR", ex.ProviderErrorMessage ?? ex.Message, PayoutProvider.Mesta);
        }
    }

    public async Task<CustomerListResult> ListCustomersAsync(
        CustomerListRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Listing customers from Mesta");

        try
        {
            var queryParams = new List<string>
            {
                $"page={request.Page}",
                $"pageSize={request.PageSize}"
            };

            if (request.Type.HasValue)
                queryParams.Add($"type={request.Type.Value.ToString().ToLowerInvariant()}");
            if (request.Status.HasValue)
                queryParams.Add($"status={request.Status.Value.ToString().ToLowerInvariant()}");
            if (!string.IsNullOrEmpty(request.Search))
                queryParams.Add($"search={Uri.EscapeDataString(request.Search)}");

            var endpoint = $"customers?{string.Join("&", queryParams)}";
            var response = await GetAsync<MestaCustomerListResponse>(endpoint, cancellationToken);

            var customers = response.Items.Select(c => MapToCustomer(c, null, null)).ToList();

            return CustomerListResult.Succeeded(
                customers,
                response.TotalCount,
                request.Page,
                request.PageSize,
                PayoutProvider.Mesta);
        }
        catch (ProviderApiException ex)
        {
            _logger.LogError(ex, "Failed to list customers from Mesta");
            return CustomerListResult.Failed(ex.ProviderErrorCode ?? "LIST_CUSTOMERS_ERROR", ex.ProviderErrorMessage ?? ex.Message, PayoutProvider.Mesta);
        }
    }

    #endregion

    #region KYC Operations

    public async Task<VerificationResult> InitiateKycAsync(
        InitiateKycRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Initiating KYC in Mesta for customer: {CustomerId}", request.CustomerId);

        try
        {
            var mestaRequest = new MestaKycRequest
            {
                CustomerId = request.CustomerId,
                Level = MapVerificationLevel(request.TargetLevel),
                RedirectUrl = request.RedirectUrl,
                WebhookUrl = request.WebhookUrl
            };

            var response = await PostAsync<MestaKycResponse>("kyc/initiate", mestaRequest, cancellationToken);

            _logger.LogInformation("KYC initiated in Mesta: {SessionId}", response.SessionId);

            return VerificationResult.InitiationSucceeded(
                request.CustomerId,
                response.SessionId,
                response.VerificationUrl,
                PayoutProvider.Mesta,
                response.ExpiresAt);
        }
        catch (ProviderApiException ex)
        {
            _logger.LogError(ex, "Failed to initiate KYC in Mesta: {CustomerId}", request.CustomerId);
            return VerificationResult.Failed(ex.ProviderErrorCode ?? "KYC_INITIATE_ERROR", ex.ProviderErrorMessage ?? ex.Message, PayoutProvider.Mesta);
        }
    }

    public async Task<VerificationResult> GetKycStatusAsync(
        string customerId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting KYC status from Mesta: {CustomerId}", customerId);

        try
        {
            var response = await GetAsync<MestaVerificationStatusResponse>(
                $"customers/{customerId}/verification",
                cancellationToken);

            var verification = MapToVerificationInfo(response);
            return VerificationResult.StatusSucceeded(customerId, verification, PayoutProvider.Mesta);
        }
        catch (ProviderApiException ex)
        {
            _logger.LogError(ex, "Failed to get KYC status from Mesta: {CustomerId}", customerId);
            return VerificationResult.Failed(ex.ProviderErrorCode ?? "KYC_STATUS_ERROR", ex.ProviderErrorMessage ?? ex.Message, PayoutProvider.Mesta);
        }
    }

    #endregion

    #region KYB Operations

    public async Task<VerificationResult> InitiateKybAsync(
        InitiateKybRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Initiating KYB in Mesta for customer: {CustomerId}", request.CustomerId);

        try
        {
            var mestaRequest = new MestaKybRequest
            {
                CustomerId = request.CustomerId,
                Level = MapVerificationLevel(request.TargetLevel),
                RedirectUrl = request.RedirectUrl,
                WebhookUrl = request.WebhookUrl
            };

            var response = await PostAsync<MestaKybResponse>("kyb/initiate", mestaRequest, cancellationToken);

            _logger.LogInformation("KYB initiated in Mesta: {SessionId}", response.SessionId);

            return VerificationResult.InitiationSucceeded(
                request.CustomerId,
                response.SessionId,
                response.VerificationUrl,
                PayoutProvider.Mesta,
                response.ExpiresAt);
        }
        catch (ProviderApiException ex)
        {
            _logger.LogError(ex, "Failed to initiate KYB in Mesta: {CustomerId}", request.CustomerId);
            return VerificationResult.Failed(ex.ProviderErrorCode ?? "KYB_INITIATE_ERROR", ex.ProviderErrorMessage ?? ex.Message, PayoutProvider.Mesta);
        }
    }

    public async Task<VerificationResult> GetKybStatusAsync(
        string customerId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting KYB status from Mesta: {CustomerId}", customerId);

        try
        {
            var response = await GetAsync<MestaVerificationStatusResponse>(
                $"customers/{customerId}/verification",
                cancellationToken);

            var verification = MapToVerificationInfo(response);
            return VerificationResult.StatusSucceeded(customerId, verification, PayoutProvider.Mesta);
        }
        catch (ProviderApiException ex)
        {
            _logger.LogError(ex, "Failed to get KYB status from Mesta: {CustomerId}", customerId);
            return VerificationResult.Failed(ex.ProviderErrorCode ?? "KYB_STATUS_ERROR", ex.ProviderErrorMessage ?? ex.Message, PayoutProvider.Mesta);
        }
    }

    #endregion

    #region Document Operations

    public async Task<DocumentUploadResult> UploadDocumentAsync(
        UploadDocumentRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Uploading document in Mesta: {CustomerId}, Type: {DocumentType}", request.CustomerId, request.DocumentType);

        try
        {
            var mestaRequest = new MestaDocumentUploadRequest
            {
                DocumentType = MapDocumentType(request.DocumentType),
                DocumentNumber = request.DocumentNumber,
                IssuingCountry = request.IssuingCountry,
                IssueDate = request.IssueDate?.ToString("yyyy-MM-dd"),
                ExpiryDate = request.ExpiryDate?.ToString("yyyy-MM-dd"),
                FrontImage = request.FrontImageBase64,
                BackImage = request.BackImageBase64,
                MimeType = request.MimeType
            };

            var response = await PostAsync<MestaDocumentResponse>(
                $"customers/{request.CustomerId}/documents",
                mestaRequest,
                cancellationToken);

            _logger.LogInformation("Document uploaded in Mesta: {DocumentId}", response.Id);

            return DocumentUploadResult.Succeeded(
                response.Id,
                request.DocumentType,
                response.Status ?? "uploaded",
                PayoutProvider.Mesta);
        }
        catch (ProviderApiException ex)
        {
            _logger.LogError(ex, "Failed to upload document in Mesta");
            return DocumentUploadResult.Failed(ex.ProviderErrorCode ?? "DOCUMENT_UPLOAD_ERROR", ex.ProviderErrorMessage ?? ex.Message, PayoutProvider.Mesta);
        }
    }

    public async Task<DocumentListResult> GetDocumentsAsync(
        string customerId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting documents from Mesta: {CustomerId}", customerId);

        try
        {
            var response = await GetAsync<MestaDocumentListResponse>(
                $"customers/{customerId}/documents",
                cancellationToken);

            var documents = response.Documents.Select(d => new VerificationDocument
            {
                Id = d.Id,
                Type = MapDocumentTypeFromMesta(d.DocumentType),
                Status = MapDocumentStatus(d.Status),
                UploadedAt = d.UploadedAt
            }).ToList();

            return DocumentListResult.Succeeded(documents, PayoutProvider.Mesta);
        }
        catch (ProviderApiException ex)
        {
            _logger.LogError(ex, "Failed to get documents from Mesta: {CustomerId}", customerId);
            return DocumentListResult.Failed(ex.ProviderErrorCode ?? "GET_DOCUMENTS_ERROR", ex.ProviderErrorMessage ?? ex.Message, PayoutProvider.Mesta);
        }
    }

    public async Task<VerificationResult> SubmitVerificationAsync(
        string customerId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Submitting verification in Mesta: {CustomerId}", customerId);

        try
        {
            var response = await PostAsync<MestaVerificationStatusResponse>(
                $"customers/{customerId}/verification/submit",
                new { },
                cancellationToken);

            var verification = MapToVerificationInfo(response);
            return VerificationResult.SubmissionSucceeded(customerId, verification.Status, PayoutProvider.Mesta);
        }
        catch (ProviderApiException ex)
        {
            _logger.LogError(ex, "Failed to submit verification in Mesta: {CustomerId}", customerId);
            return VerificationResult.Failed(ex.ProviderErrorCode ?? "VERIFICATION_SUBMIT_ERROR", ex.ProviderErrorMessage ?? ex.Message, PayoutProvider.Mesta);
        }
    }

    #endregion

    #region Payout Operations

    public async Task<QuoteResult> CreateQuoteAsync(
        CreateQuoteRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Getting quote from Mesta: {SourceCurrency} -> {TargetCurrency}",
            request.SourceCurrency,
            request.TargetCurrency);

        try
        {
            var mestaRequest = new MestaCreateQuoteRequest
            {
                SourceCurrency = MapStablecoinToMesta(request.SourceCurrency),
                TargetCurrency = MapFiatCurrencyToMesta(request.TargetCurrency),
                SourceAmount = request.SourceAmount,
                TargetAmount = request.TargetAmount,
                Chain = MapNetworkToMesta(request.Network),
                PaymentMethod = request.PaymentMethod.HasValue ? MapPaymentMethodToMesta(request.PaymentMethod.Value) : null,
                DestinationCountry = request.DestinationCountry,
                DeveloperFee = request.DeveloperFee
            };

            var response = await PostAsync<MestaQuoteResponse>("quotes", mestaRequest, cancellationToken);

            var quote = new PayoutQuote
            {
                Id = Guid.NewGuid().ToString(),
                ProviderQuoteId = response.Id,
                SourceCurrency = request.SourceCurrency,
                TargetCurrency = request.TargetCurrency,
                SourceAmount = response.SourceAmount,
                TargetAmount = response.TargetAmount,
                ExchangeRate = response.ExchangeRate,
                FeeAmount = response.FeeAmount,
                FeeBreakdown = response.Fees != null ? new FeeBreakdown
                {
                    NetworkFee = response.Fees.NetworkFee,
                    ProcessingFee = response.Fees.ProcessingFee,
                    FxSpreadFee = response.Fees.FxSpreadFee,
                    BankFee = response.Fees.BankFee,
                    DeveloperFee = response.Fees.DeveloperFee
                } : null,
                Network = request.Network,
                CreatedAt = response.CreatedAt,
                ExpiresAt = response.ExpiresAt,
                Provider = PayoutProvider.Mesta
            };

            _logger.LogInformation(
                "Quote received from Mesta: {QuoteId}, Rate: {Rate}, Fee: {Fee}",
                response.Id,
                response.ExchangeRate,
                response.FeeAmount);

            return QuoteResult.Succeeded(quote);
        }
        catch (ProviderApiException ex)
        {
            _logger.LogError(ex, "Failed to get quote from Mesta");
            return QuoteResult.Failed(ex.ProviderErrorCode ?? "QUOTE_ERROR", ex.ProviderErrorMessage ?? ex.Message);
        }
    }

    public async Task<PayoutResult> CreatePayoutAsync(
        CreatePayoutRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Creating payout via Mesta: {SourceAmount} {SourceCurrency} -> {TargetCurrency}",
            request.SourceAmount ?? request.TargetAmount,
            request.SourceCurrency,
            request.TargetCurrency);

        try
        {
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
                var quoteRequest = new MestaCreateQuoteRequest
                {
                    SourceCurrency = MapStablecoinToMesta(request.SourceCurrency),
                    TargetCurrency = MapFiatCurrencyToMesta(request.TargetCurrency),
                    SourceAmount = request.SourceAmount,
                    TargetAmount = request.TargetAmount,
                    Chain = MapNetworkToMesta(request.Network),
                    PaymentMethod = MapPaymentMethodToMesta(request.PaymentMethod),
                    DestinationCountry = beneficiary.BankAccount.CountryCode,
                    DeveloperFee = request.DeveloperFee
                };

                var quoteResponse = await PostAsync<MestaQuoteResponse>("quotes", quoteRequest, cancellationToken);
                quoteId = quoteResponse.Id;
            }

            // Create order
            var orderRequest = new MestaCreateOrderRequest
            {
                SenderId = sender.Id!,
                BeneficiaryId = beneficiary.Id!,
                AcceptedQuoteId = quoteId,
                ExternalId = request.ExternalId,
                Metadata = request.Metadata
            };

            var orderResponse = await PostAsync<MestaOrderResponse>("orders", orderRequest, cancellationToken);

            // Get deposit wallet
            DepositWallet? depositWallet = null;
            if (orderResponse.DepositWallet != null)
            {
                depositWallet = MapToDepositWallet(orderResponse.DepositWallet, request.SourceCurrency);
            }

            var payout = new Payout
            {
                Id = Guid.NewGuid().ToString(),
                ExternalId = request.ExternalId,
                Provider = PayoutProvider.Mesta,
                ProviderOrderId = orderResponse.Id,
                Status = MapMestaStatus(orderResponse.Status),
                SourceCurrency = request.SourceCurrency,
                SourceAmount = orderResponse.SourceAmount,
                TargetCurrency = request.TargetCurrency,
                TargetAmount = orderResponse.TargetAmount,
                ExchangeRate = orderResponse.ExchangeRate,
                FeeAmount = orderResponse.FeeAmount,
                Network = request.Network,
                Sender = sender,
                Beneficiary = beneficiary,
                DepositWallet = depositWallet,
                QuoteId = quoteId,
                PaymentMethod = request.PaymentMethod,
                CreatedAt = orderResponse.CreatedAt,
                UpdatedAt = orderResponse.UpdatedAt,
                Metadata = request.Metadata
            };

            _logger.LogInformation(
                "Payout created via Mesta: {PayoutId}, OrderId: {OrderId}, Status: {Status}",
                payout.Id,
                orderResponse.Id,
                orderResponse.Status);

            return PayoutResult.Succeeded(payout, PayoutProvider.Mesta);
        }
        catch (ProviderApiException ex)
        {
            _logger.LogError(ex, "Failed to create payout via Mesta");
            return PayoutResult.Failed(ex.ProviderErrorCode ?? "PAYOUT_ERROR", ex.ProviderErrorMessage ?? ex.Message, PayoutProvider.Mesta);
        }
    }

    public async Task<PayoutResult> GetPayoutAsync(
        string payoutId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting payout from Mesta: {PayoutId}", payoutId);

        try
        {
            var response = await GetAsync<MestaOrderResponse>($"orders/{payoutId}", cancellationToken);

            var payout = new Payout
            {
                Id = response.Id,
                ProviderOrderId = response.Id,
                Provider = PayoutProvider.Mesta,
                Status = MapMestaStatus(response.Status),
                SourceAmount = response.SourceAmount,
                TargetAmount = response.TargetAmount,
                ExchangeRate = response.ExchangeRate,
                FeeAmount = response.FeeAmount,
                CreatedAt = response.CreatedAt,
                UpdatedAt = response.UpdatedAt
            };

            return PayoutResult.Succeeded(payout, PayoutProvider.Mesta);
        }
        catch (ProviderApiException ex)
        {
            _logger.LogError(ex, "Failed to get payout from Mesta: {PayoutId}", payoutId);
            return PayoutResult.Failed(ex.ProviderErrorCode ?? "GET_PAYOUT_ERROR", ex.ProviderErrorMessage ?? ex.Message, PayoutProvider.Mesta);
        }
    }

    public async Task<PayoutStatusResult> GetPayoutStatusAsync(
        string payoutId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting payout status from Mesta: {PayoutId}", payoutId);

        try
        {
            var response = await GetAsync<MestaOrderResponse>($"orders/{payoutId}", cancellationToken);

            var statusUpdate = new PayoutStatusUpdate
            {
                PayoutId = payoutId,
                ProviderOrderId = response.Id,
                CurrentStatus = MapMestaStatus(response.Status),
                ProviderStatus = response.Status,
                BlockchainTxHash = response.BlockchainTxHash,
                BankReference = response.BankReference,
                FailureReason = response.FailureReason,
                Timestamp = response.UpdatedAt,
                Provider = PayoutProvider.Mesta
            };

            return PayoutStatusResult.Succeeded(statusUpdate);
        }
        catch (ProviderApiException ex)
        {
            _logger.LogError(ex, "Failed to get payout status from Mesta: {PayoutId}", payoutId);
            return PayoutStatusResult.Failed(ex.ProviderErrorCode ?? "STATUS_ERROR", ex.ProviderErrorMessage ?? ex.Message);
        }
    }

    public async Task<PayoutResult> CancelPayoutAsync(
        string payoutId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Cancelling payout via Mesta: {PayoutId}", payoutId);

        try
        {
            var response = await _httpClient.PostAsync($"orders/{payoutId}/cancel", null, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Payout cancelled via Mesta: {PayoutId}", payoutId);

                var payout = new Payout
                {
                    Id = payoutId,
                    ProviderOrderId = payoutId,
                    Provider = PayoutProvider.Mesta,
                    Status = PayoutStatus.Cancelled
                };

                return PayoutResult.Succeeded(payout, PayoutProvider.Mesta);
            }

            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning("Failed to cancel payout via Mesta: {PayoutId}, Response: {Response}", payoutId, errorContent);
            return PayoutResult.Failed("CANCEL_FAILED", errorContent, PayoutProvider.Mesta);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling payout via Mesta: {PayoutId}", payoutId);
            return PayoutResult.Failed("CANCEL_ERROR", ex.Message, PayoutProvider.Mesta);
        }
    }

    public async Task<PayoutListResult> ListPayoutsAsync(
        PayoutListRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Listing payouts from Mesta");

        try
        {
            var queryParams = new List<string>
            {
                $"page={request.Page}",
                $"pageSize={request.PageSize}"
            };

            if (!string.IsNullOrEmpty(request.CustomerId))
                queryParams.Add($"customerId={Uri.EscapeDataString(request.CustomerId)}");
            if (request.Status.HasValue)
                queryParams.Add($"status={request.Status.Value.ToString().ToLowerInvariant()}");
            if (request.FromDate.HasValue)
                queryParams.Add($"fromDate={request.FromDate.Value:yyyy-MM-dd}");
            if (request.ToDate.HasValue)
                queryParams.Add($"toDate={request.ToDate.Value:yyyy-MM-dd}");

            var endpoint = $"orders?{string.Join("&", queryParams)}";
            var response = await GetAsync<MestaOrderListResponse>(endpoint, cancellationToken);

            var payouts = response.Items.Select(o => new Payout
            {
                Id = o.Id,
                ProviderOrderId = o.Id,
                Provider = PayoutProvider.Mesta,
                Status = MapMestaStatus(o.Status),
                SourceAmount = o.SourceAmount,
                TargetAmount = o.TargetAmount,
                ExchangeRate = o.ExchangeRate,
                FeeAmount = o.FeeAmount,
                CreatedAt = o.CreatedAt,
                UpdatedAt = o.UpdatedAt
            }).ToList();

            return PayoutListResult.Succeeded(
                payouts,
                response.TotalCount,
                request.Page,
                request.PageSize,
                PayoutProvider.Mesta);
        }
        catch (ProviderApiException ex)
        {
            _logger.LogError(ex, "Failed to list payouts from Mesta");
            return PayoutListResult.Failed(ex.ProviderErrorCode ?? "LIST_PAYOUTS_ERROR", ex.ProviderErrorMessage ?? ex.Message, PayoutProvider.Mesta);
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

            var webhookPayload = JsonSerializer.Deserialize<MestaWebhookPayload>(payload, _jsonOptions);

            if (webhookPayload?.Data == null)
            {
                _logger.LogWarning("Invalid Mesta webhook payload");
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
                EventType = webhookPayload.Event,
                ResourceId = webhookPayload.Data.Id,
                ResourceType = "order",
                Data = new PayoutStatusUpdate
                {
                    PayoutId = webhookPayload.Data.ExternalId ?? webhookPayload.Data.Id,
                    ProviderOrderId = webhookPayload.Data.Id,
                    CurrentStatus = MapMestaStatus(webhookPayload.Data.Status),
                    ProviderStatus = webhookPayload.Data.Status,
                    BlockchainTxHash = webhookPayload.Data.BlockchainTxHash,
                    BankReference = webhookPayload.Data.BankReference,
                    FailureReason = webhookPayload.Data.FailureReason,
                    Timestamp = webhookPayload.Timestamp,
                    Provider = PayoutProvider.Mesta
                }
            };

            _logger.LogInformation(
                "Processed Mesta webhook: Event={Event}, OrderId={OrderId}, Status={Status}",
                webhookPayload.Event,
                webhookPayload.Data.Id,
                webhookPayload.Data.Status);

            return Task.FromResult(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Mesta webhook");
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
        var request = new MestaCreateSenderRequest
        {
            Type = sender.Type == BeneficiaryType.Individual ? "individual" : "business",
            FirstName = sender.FirstName,
            LastName = sender.LastName,
            BusinessName = sender.BusinessName,
            Email = sender.Email,
            Phone = sender.PhoneNumber,
            DateOfBirth = sender.DateOfBirth?.ToString("yyyy-MM-dd"),
            Identity = !string.IsNullOrEmpty(sender.DocumentNumber) ? new MestaIdentity
            {
                DocumentType = sender.DocumentType,
                DocumentNumber = sender.DocumentNumber,
                Nationality = sender.Nationality
            } : null,
            Address = sender.Address != null ? new MestaAddress
            {
                Street1 = sender.Address.Street1,
                Street2 = sender.Address.Street2,
                City = sender.Address.City,
                State = sender.Address.State,
                PostalCode = sender.Address.PostalCode,
                Country = sender.Address.CountryCode
            } : null,
            ExternalId = sender.ExternalId
        };

        var response = await PostAsync<MestaSenderResponse>("senders", request, cancellationToken);
        sender.Id = response.Id;
        return sender;
    }

    private async Task<Beneficiary> CreateBeneficiaryInternalAsync(Beneficiary beneficiary, CancellationToken cancellationToken)
    {
        var request = new MestaCreateBeneficiaryRequest
        {
            Type = beneficiary.Type == BeneficiaryType.Individual ? "individual" : "business",
            FirstName = beneficiary.FirstName,
            LastName = beneficiary.LastName,
            BusinessName = beneficiary.BusinessName,
            Email = beneficiary.Email,
            Phone = beneficiary.PhoneNumber,
            DateOfBirth = beneficiary.DateOfBirth?.ToString("yyyy-MM-dd"),
            Identity = !string.IsNullOrEmpty(beneficiary.DocumentNumber) ? new MestaIdentity
            {
                DocumentType = beneficiary.DocumentType,
                DocumentNumber = beneficiary.DocumentNumber,
                Nationality = beneficiary.Nationality
            } : null,
            Address = beneficiary.Address != null ? new MestaAddress
            {
                Street1 = beneficiary.Address.Street1,
                Street2 = beneficiary.Address.Street2,
                City = beneficiary.Address.City,
                State = beneficiary.Address.State,
                PostalCode = beneficiary.Address.PostalCode,
                Country = beneficiary.Address.CountryCode
            } : null,
            PaymentInfo = new MestaPaymentInfo
            {
                BankName = beneficiary.BankAccount.BankName,
                AccountNumber = beneficiary.BankAccount.AccountNumber,
                AccountHolderName = beneficiary.BankAccount.AccountHolderName,
                RoutingNumber = beneficiary.BankAccount.RoutingNumber,
                SwiftCode = beneficiary.BankAccount.SwiftCode,
                SortCode = beneficiary.BankAccount.SortCode,
                Iban = beneficiary.BankAccount.Iban,
                Currency = MapFiatCurrencyToMesta(beneficiary.BankAccount.Currency),
                Country = beneficiary.BankAccount.CountryCode,
                BranchCode = beneficiary.BankAccount.BranchCode
            },
            ExternalId = beneficiary.ExternalId
        };

        var response = await PostAsync<MestaBeneficiaryResponse>("beneficiaries", request, cancellationToken);
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
            MestaError? error = null;
            try
            {
                var errorResponse = JsonSerializer.Deserialize<MestaResponse<object>>(content, _jsonOptions);
                error = errorResponse?.Error;
            }
            catch { }

            throw new ProviderApiException(
                PayoutProvider.Mesta,
                $"Mesta API error: {response.StatusCode}",
                (int)response.StatusCode,
                error?.Code,
                error?.Message ?? content);
        }

        var result = JsonSerializer.Deserialize<MestaResponse<T>>(content, _jsonOptions);

        if (result?.Data == null)
        {
            throw new ProviderApiException(
                PayoutProvider.Mesta,
                "Invalid response from Mesta API",
                (int)response.StatusCode);
        }

        return result.Data;
    }

    private static Customer MapToCustomer(MestaCustomerResponse response, CustomerType? requestType, CustomerRole? requestRole)
    {
        var customerType = requestType ?? (response.Type?.ToLowerInvariant() == "business"
            ? CustomerType.Business
            : CustomerType.Individual);

        return new Customer
        {
            Id = response.Id,
            Type = customerType,
            Role = requestRole ?? CustomerRole.Both,
            Status = MapCustomerStatus(response.Status),
            Individual = customerType == CustomerType.Individual ? new IndividualDetails
            {
                FirstName = response.FirstName ?? string.Empty,
                LastName = response.LastName ?? string.Empty
            } : null,
            Business = customerType == CustomerType.Business ? new BusinessDetails
            {
                LegalName = response.BusinessName ?? string.Empty,
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

    private static VerificationInfo MapToVerificationInfo(MestaVerificationStatusResponse response)
    {
        return new VerificationInfo
        {
            Status = MapVerificationStatus(response.Status),
            Level = MapVerificationLevelFromMesta(response.Level),
            KycCompleted = response.KycCompleted,
            KybCompleted = response.KybCompleted,
            RejectionReason = response.RejectionReason,
            SubmittedAt = response.SubmittedAt,
            CompletedAt = response.CompletedAt,
            ExpiresAt = response.ExpiresAt
        };
    }

    private static DepositWallet MapToDepositWallet(MestaWalletResponse wallet, Stablecoin currency)
    {
        return new DepositWallet
        {
            Id = wallet.Id,
            Address = wallet.Address,
            Network = MapMestaNetwork(wallet.Chain),
            Currency = currency,
            ExpectedAmount = wallet.ExpectedAmount,
            ExpiresAt = wallet.ExpiresAt,
            Memo = wallet.Memo
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

    private static VerificationLevel MapVerificationLevelFromMesta(string? level) => level?.ToLowerInvariant() switch
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

    private static DocumentType MapDocumentTypeFromMesta(string? type) => type?.ToLowerInvariant() switch
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

    private static string MapStablecoinToMesta(Stablecoin coin) => coin switch
    {
        Stablecoin.USDC => "USDC",
        Stablecoin.USDT => "USDT",
        _ => throw new ArgumentOutOfRangeException(nameof(coin))
    };

    private static string MapFiatCurrencyToMesta(FiatCurrency currency) => currency switch
    {
        FiatCurrency.USD => "USD",
        FiatCurrency.EUR => "EUR",
        FiatCurrency.GBP => "GBP",
        _ => throw new ArgumentOutOfRangeException(nameof(currency))
    };

    private static string MapNetworkToMesta(BlockchainNetwork network) => network switch
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

    private static BlockchainNetwork MapMestaNetwork(string chain) => chain.ToLowerInvariant() switch
    {
        "ethereum" or "eth" => BlockchainNetwork.Ethereum,
        "polygon" or "matic" or "pol" => BlockchainNetwork.Polygon,
        "arbitrum" or "arb" => BlockchainNetwork.Arbitrum,
        "optimism" or "op" => BlockchainNetwork.Optimism,
        "base" => BlockchainNetwork.Base,
        "tron" or "trx" => BlockchainNetwork.Tron,
        "solana" or "sol" => BlockchainNetwork.Solana,
        _ => throw new ArgumentOutOfRangeException(nameof(chain))
    };

    private static string MapPaymentMethodToMesta(PaymentMethod method) => method switch
    {
        PaymentMethod.BankTransfer => "bank_transfer",
        PaymentMethod.Sepa => "sepa",
        PaymentMethod.Ach => "ach",
        PaymentMethod.FasterPayments => "faster_payments",
        PaymentMethod.Swift => "swift",
        _ => throw new ArgumentOutOfRangeException(nameof(method))
    };

    private static PayoutStatus MapMestaStatus(string status) => status.ToLowerInvariant() switch
    {
        "created" => PayoutStatus.Created,
        "awaiting_funds" => PayoutStatus.AwaitingFunds,
        "awaiting_funds_timeout" => PayoutStatus.Expired,
        "funds_received" => PayoutStatus.FundsReceived,
        "in_progress" or "processing" => PayoutStatus.Processing,
        "sent_to_beneficiary" => PayoutStatus.SentToBeneficiary,
        "completed" or "success" => PayoutStatus.Completed,
        "failed" or "error" => PayoutStatus.Failed,
        "cancelled" or "canceled" => PayoutStatus.Cancelled,
        "need_review" or "pending_review" => PayoutStatus.PendingReview,
        "refunded" => PayoutStatus.Refunded,
        _ => PayoutStatus.Processing
    };

    #endregion
}
