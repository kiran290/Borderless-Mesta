using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StablecoinPayments.Core.Enums;
using StablecoinPayments.Core.Interfaces;
using StablecoinPayments.Core.Models.Requests;
using StablecoinPayments.Core.Models.Responses;
using StablecoinPayments.Infrastructure.Configuration;

namespace StablecoinPayments.Infrastructure.Providers.Mesta;

public sealed class MestaPaymentProvider : IPaymentProvider
{
    private readonly HttpClient _httpClient;
    private readonly MestaSettings _settings;
    private readonly ILogger<MestaPaymentProvider> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public MestaPaymentProvider(
        HttpClient httpClient,
        IOptions<PaymentSettings> settings,
        ILogger<MestaPaymentProvider> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value.Mesta;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            PropertyNameCaseInsensitive = true
        };

        _httpClient.BaseAddress = new Uri(_settings.BaseUrl);
        _httpClient.DefaultRequestHeaders.Add("X-API-Key", _settings.ApiKey);
        _httpClient.DefaultRequestHeaders.Add("X-Merchant-Id", _settings.MerchantId);
    }

    public PaymentProvider ProviderId => PaymentProvider.Mesta;
    public string ProviderName => "Mesta";

    #region Health Check

    public async Task<HealthCheckResult> CheckHealthAsync(CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;
        try
        {
            var response = await _httpClient.GetAsync("/health", cancellationToken);
            var latency = DateTime.UtcNow - startTime;

            return new HealthCheckResult
            {
                IsHealthy = response.IsSuccessStatusCode,
                Status = response.IsSuccessStatusCode ? "Healthy" : "Unhealthy",
                Message = response.IsSuccessStatusCode ? null : $"Status code: {response.StatusCode}",
                Latency = latency
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Health check failed for Mesta");
            return new HealthCheckResult
            {
                IsHealthy = false,
                Status = "Unhealthy",
                Message = ex.Message,
                Latency = DateTime.UtcNow - startTime
            };
        }
    }

    #endregion

    #region Customer Operations

    public async Task<CustomerResponse> CreateCustomerAsync(CreateCustomerRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var mestaRequest = new
            {
                external_id = request.ExternalId,
                type = request.Type.ToString().ToLower(),
                first_name = request.Individual?.FirstName,
                last_name = request.Individual?.LastName,
                middle_name = request.Individual?.MiddleName,
                date_of_birth = request.Individual?.DateOfBirth?.ToString("yyyy-MM-dd"),
                nationality = request.Individual?.Nationality,
                business_name = request.Business?.LegalName,
                trading_name = request.Business?.TradingName,
                registration_number = request.Business?.RegistrationNumber,
                tax_id = request.Business?.TaxId,
                country_of_incorporation = request.Business?.CountryOfIncorporation,
                email = request.Contact.Email,
                phone = request.Contact.Phone,
                address = request.Address != null ? new
                {
                    street1 = request.Address.Street1,
                    street2 = request.Address.Street2,
                    city = request.Address.City,
                    state = request.Address.State,
                    postal_code = request.Address.PostalCode,
                    country_code = request.Address.CountryCode
                } : null,
                metadata = request.Metadata
            };

            var response = await _httpClient.PostAsJsonAsync("/customers", mestaRequest, _jsonOptions, cancellationToken);
            var content = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Mesta CreateCustomer failed: {Content}", content);
                return new CustomerResponse
                {
                    Success = false,
                    ErrorCode = "CREATE_FAILED",
                    ErrorMessage = content,
                    Provider = ProviderId
                };
            }

            var mestaResponse = JsonSerializer.Deserialize<MestaCustomerResponse>(content, _jsonOptions);

            return new CustomerResponse
            {
                Success = true,
                Provider = ProviderId,
                ProviderCustomerId = mestaResponse?.Id,
                Customer = MapToCustomerData(mestaResponse!)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating customer in Mesta");
            return new CustomerResponse
            {
                Success = false,
                ErrorCode = "PROVIDER_ERROR",
                ErrorMessage = ex.Message,
                Provider = ProviderId
            };
        }
    }

    public async Task<CustomerResponse> GetCustomerAsync(string customerId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/customers/{customerId}", cancellationToken);
            var content = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return new CustomerResponse
                {
                    Success = false,
                    ErrorCode = response.StatusCode == System.Net.HttpStatusCode.NotFound ? "NOT_FOUND" : "GET_FAILED",
                    ErrorMessage = content,
                    Provider = ProviderId
                };
            }

            var mestaResponse = JsonSerializer.Deserialize<MestaCustomerResponse>(content, _jsonOptions);

            return new CustomerResponse
            {
                Success = true,
                Provider = ProviderId,
                ProviderCustomerId = mestaResponse?.Id,
                Customer = MapToCustomerData(mestaResponse!)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting customer {CustomerId} from Mesta", customerId);
            return new CustomerResponse
            {
                Success = false,
                ErrorCode = "PROVIDER_ERROR",
                ErrorMessage = ex.Message,
                Provider = ProviderId
            };
        }
    }

    public async Task<CustomerResponse> UpdateCustomerAsync(string customerId, UpdateCustomerRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var mestaRequest = new
            {
                first_name = request.Individual?.FirstName,
                last_name = request.Individual?.LastName,
                middle_name = request.Individual?.MiddleName,
                date_of_birth = request.Individual?.DateOfBirth?.ToString("yyyy-MM-dd"),
                nationality = request.Individual?.Nationality,
                business_name = request.Business?.LegalName,
                trading_name = request.Business?.TradingName,
                email = request.Contact?.Email,
                phone = request.Contact?.Phone,
                address = request.Address != null ? new
                {
                    street1 = request.Address.Street1,
                    street2 = request.Address.Street2,
                    city = request.Address.City,
                    state = request.Address.State,
                    postal_code = request.Address.PostalCode,
                    country_code = request.Address.CountryCode
                } : null,
                metadata = request.Metadata
            };

            var response = await _httpClient.PatchAsJsonAsync($"/customers/{customerId}", mestaRequest, _jsonOptions, cancellationToken);
            var content = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return new CustomerResponse
                {
                    Success = false,
                    ErrorCode = "UPDATE_FAILED",
                    ErrorMessage = content,
                    Provider = ProviderId
                };
            }

            var mestaResponse = JsonSerializer.Deserialize<MestaCustomerResponse>(content, _jsonOptions);

            return new CustomerResponse
            {
                Success = true,
                Provider = ProviderId,
                ProviderCustomerId = mestaResponse?.Id,
                Customer = MapToCustomerData(mestaResponse!)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating customer {CustomerId} in Mesta", customerId);
            return new CustomerResponse
            {
                Success = false,
                ErrorCode = "PROVIDER_ERROR",
                ErrorMessage = ex.Message,
                Provider = ProviderId
            };
        }
    }

    public async Task<CustomerListResponse> ListCustomersAsync(ListCustomersRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var queryParams = new List<string>
            {
                $"page={request.Page}",
                $"page_size={request.PageSize}"
            };

            if (request.Type.HasValue)
                queryParams.Add($"type={request.Type.Value.ToString().ToLower()}");
            if (request.Status.HasValue)
                queryParams.Add($"status={request.Status.Value.ToString().ToLower()}");
            if (!string.IsNullOrEmpty(request.Search))
                queryParams.Add($"search={Uri.EscapeDataString(request.Search)}");

            var url = $"/customers?{string.Join("&", queryParams)}";
            var response = await _httpClient.GetAsync(url, cancellationToken);
            var content = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return new CustomerListResponse
                {
                    Success = false,
                    ErrorCode = "LIST_FAILED",
                    ErrorMessage = content,
                    Provider = ProviderId
                };
            }

            var mestaResponse = JsonSerializer.Deserialize<MestaCustomerListResponse>(content, _jsonOptions);

            return new CustomerListResponse
            {
                Success = true,
                Provider = ProviderId,
                Customers = mestaResponse?.Data?.Select(MapToCustomerData).ToList() ?? [],
                TotalCount = mestaResponse?.Total ?? 0,
                Page = mestaResponse?.Page ?? 1,
                PageSize = mestaResponse?.PageSize ?? 20
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing customers from Mesta");
            return new CustomerListResponse
            {
                Success = false,
                ErrorCode = "PROVIDER_ERROR",
                ErrorMessage = ex.Message,
                Provider = ProviderId
            };
        }
    }

    #endregion

    #region KYC Operations

    public async Task<VerificationResponse> InitiateKycAsync(InitiateKycRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var mestaRequest = new
            {
                customer_id = request.CustomerId,
                level = request.TargetLevel.ToString().ToLower(),
                redirect_url = request.RedirectUrl,
                webhook_url = request.WebhookUrl
            };

            var response = await _httpClient.PostAsJsonAsync("/kyc/initiate", mestaRequest, _jsonOptions, cancellationToken);
            var content = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return new VerificationResponse
                {
                    Success = false,
                    ErrorCode = "KYC_INITIATE_FAILED",
                    ErrorMessage = content,
                    Provider = ProviderId
                };
            }

            var mestaResponse = JsonSerializer.Deserialize<MestaVerificationResponse>(content, _jsonOptions);

            return new VerificationResponse
            {
                Success = true,
                Provider = ProviderId,
                CustomerId = request.CustomerId,
                SessionId = mestaResponse?.SessionId,
                VerificationUrl = mestaResponse?.VerificationUrl,
                Status = MapVerificationStatus(mestaResponse?.Status),
                Level = request.TargetLevel,
                ExpiresAt = mestaResponse?.ExpiresAt
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initiating KYC for customer {CustomerId} in Mesta", request.CustomerId);
            return new VerificationResponse
            {
                Success = false,
                ErrorCode = "PROVIDER_ERROR",
                ErrorMessage = ex.Message,
                Provider = ProviderId
            };
        }
    }

    public async Task<VerificationResponse> GetKycStatusAsync(string customerId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/kyc/{customerId}/status", cancellationToken);
            var content = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return new VerificationResponse
                {
                    Success = false,
                    ErrorCode = "KYC_STATUS_FAILED",
                    ErrorMessage = content,
                    Provider = ProviderId
                };
            }

            var mestaResponse = JsonSerializer.Deserialize<MestaVerificationResponse>(content, _jsonOptions);

            return new VerificationResponse
            {
                Success = true,
                Provider = ProviderId,
                CustomerId = customerId,
                Status = MapVerificationStatus(mestaResponse?.Status),
                Level = MapVerificationLevel(mestaResponse?.Level),
                RejectionReason = mestaResponse?.RejectionReason
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting KYC status for customer {CustomerId} from Mesta", customerId);
            return new VerificationResponse
            {
                Success = false,
                ErrorCode = "PROVIDER_ERROR",
                ErrorMessage = ex.Message,
                Provider = ProviderId
            };
        }
    }

    #endregion

    #region KYB Operations

    public async Task<VerificationResponse> InitiateKybAsync(InitiateKybRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var mestaRequest = new
            {
                customer_id = request.CustomerId,
                level = request.TargetLevel.ToString().ToLower(),
                redirect_url = request.RedirectUrl,
                webhook_url = request.WebhookUrl
            };

            var response = await _httpClient.PostAsJsonAsync("/kyb/initiate", mestaRequest, _jsonOptions, cancellationToken);
            var content = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return new VerificationResponse
                {
                    Success = false,
                    ErrorCode = "KYB_INITIATE_FAILED",
                    ErrorMessage = content,
                    Provider = ProviderId
                };
            }

            var mestaResponse = JsonSerializer.Deserialize<MestaVerificationResponse>(content, _jsonOptions);

            return new VerificationResponse
            {
                Success = true,
                Provider = ProviderId,
                CustomerId = request.CustomerId,
                SessionId = mestaResponse?.SessionId,
                VerificationUrl = mestaResponse?.VerificationUrl,
                Status = MapVerificationStatus(mestaResponse?.Status),
                Level = request.TargetLevel,
                ExpiresAt = mestaResponse?.ExpiresAt
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initiating KYB for customer {CustomerId} in Mesta", request.CustomerId);
            return new VerificationResponse
            {
                Success = false,
                ErrorCode = "PROVIDER_ERROR",
                ErrorMessage = ex.Message,
                Provider = ProviderId
            };
        }
    }

    public async Task<VerificationResponse> GetKybStatusAsync(string customerId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/kyb/{customerId}/status", cancellationToken);
            var content = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return new VerificationResponse
                {
                    Success = false,
                    ErrorCode = "KYB_STATUS_FAILED",
                    ErrorMessage = content,
                    Provider = ProviderId
                };
            }

            var mestaResponse = JsonSerializer.Deserialize<MestaVerificationResponse>(content, _jsonOptions);

            return new VerificationResponse
            {
                Success = true,
                Provider = ProviderId,
                CustomerId = customerId,
                Status = MapVerificationStatus(mestaResponse?.Status),
                Level = MapVerificationLevel(mestaResponse?.Level),
                RejectionReason = mestaResponse?.RejectionReason
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting KYB status for customer {CustomerId} from Mesta", customerId);
            return new VerificationResponse
            {
                Success = false,
                ErrorCode = "PROVIDER_ERROR",
                ErrorMessage = ex.Message,
                Provider = ProviderId
            };
        }
    }

    #endregion

    #region Document Operations

    public async Task<DocumentResponse> UploadDocumentAsync(UploadDocumentRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var mestaRequest = new
            {
                customer_id = request.CustomerId,
                document_type = request.DocumentType.ToString().ToLower(),
                document_number = request.DocumentNumber,
                issuing_country = request.IssuingCountry,
                issue_date = request.IssueDate?.ToString("yyyy-MM-dd"),
                expiry_date = request.ExpiryDate?.ToString("yyyy-MM-dd"),
                front_image = request.FrontImageBase64,
                back_image = request.BackImageBase64,
                mime_type = request.MimeType
            };

            var response = await _httpClient.PostAsJsonAsync("/documents", mestaRequest, _jsonOptions, cancellationToken);
            var content = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return new DocumentResponse
                {
                    Success = false,
                    ErrorCode = "UPLOAD_FAILED",
                    ErrorMessage = content,
                    Provider = ProviderId
                };
            }

            var mestaResponse = JsonSerializer.Deserialize<MestaDocumentResponse>(content, _jsonOptions);

            return new DocumentResponse
            {
                Success = true,
                Provider = ProviderId,
                DocumentId = mestaResponse?.Id,
                DocumentType = request.DocumentType,
                Status = mestaResponse?.Status,
                UploadedAt = mestaResponse?.CreatedAt ?? DateTimeOffset.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading document for customer {CustomerId} in Mesta", request.CustomerId);
            return new DocumentResponse
            {
                Success = false,
                ErrorCode = "PROVIDER_ERROR",
                ErrorMessage = ex.Message,
                Provider = ProviderId
            };
        }
    }

    public async Task<DocumentListResponse> GetDocumentsAsync(string customerId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/customers/{customerId}/documents", cancellationToken);
            var content = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return new DocumentListResponse
                {
                    Success = false,
                    ErrorCode = "LIST_FAILED",
                    ErrorMessage = content,
                    Provider = ProviderId
                };
            }

            var mestaResponse = JsonSerializer.Deserialize<MestaDocumentListResponse>(content, _jsonOptions);

            return new DocumentListResponse
            {
                Success = true,
                Provider = ProviderId,
                Documents = mestaResponse?.Data?.Select(d => new DocumentData
                {
                    Id = d.Id ?? string.Empty,
                    Type = Enum.TryParse<DocumentType>(d.DocumentType, true, out var dt) ? dt : DocumentType.Passport,
                    Status = d.Status ?? "unknown",
                    UploadedAt = d.CreatedAt ?? DateTimeOffset.UtcNow,
                    ReviewedAt = d.ReviewedAt
                }).ToList() ?? []
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting documents for customer {CustomerId} from Mesta", customerId);
            return new DocumentListResponse
            {
                Success = false,
                ErrorCode = "PROVIDER_ERROR",
                ErrorMessage = ex.Message,
                Provider = ProviderId
            };
        }
    }

    public async Task<VerificationResponse> SubmitVerificationAsync(string customerId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.PostAsync($"/customers/{customerId}/verification/submit", null, cancellationToken);
            var content = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return new VerificationResponse
                {
                    Success = false,
                    ErrorCode = "SUBMIT_FAILED",
                    ErrorMessage = content,
                    Provider = ProviderId
                };
            }

            var mestaResponse = JsonSerializer.Deserialize<MestaVerificationResponse>(content, _jsonOptions);

            return new VerificationResponse
            {
                Success = true,
                Provider = ProviderId,
                CustomerId = customerId,
                Status = MapVerificationStatus(mestaResponse?.Status)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting verification for customer {CustomerId} in Mesta", customerId);
            return new VerificationResponse
            {
                Success = false,
                ErrorCode = "PROVIDER_ERROR",
                ErrorMessage = ex.Message,
                Provider = ProviderId
            };
        }
    }

    #endregion

    #region Quote Operations

    public async Task<QuoteResponse> CreateQuoteAsync(CreateQuoteRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var mestaRequest = new
            {
                source_currency = request.SourceCurrency.ToString(),
                target_currency = request.TargetCurrency.ToString(),
                source_amount = request.SourceAmount,
                target_amount = request.TargetAmount,
                network = MapNetwork(request.Network),
                destination_country = request.DestinationCountry,
                payment_method = request.PaymentMethod.ToString().ToLower()
            };

            var response = await _httpClient.PostAsJsonAsync("/quotes", mestaRequest, _jsonOptions, cancellationToken);
            var content = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Mesta CreateQuote failed: {Content}", content);
                return new QuoteResponse
                {
                    Success = false,
                    ErrorCode = "QUOTE_FAILED",
                    ErrorMessage = content,
                    Provider = ProviderId
                };
            }

            var mestaResponse = JsonSerializer.Deserialize<MestaQuoteResponse>(content, _jsonOptions);

            return new QuoteResponse
            {
                Success = true,
                Provider = ProviderId,
                Quote = new QuoteData
                {
                    Id = mestaResponse?.Id ?? Guid.NewGuid().ToString(),
                    ProviderQuoteId = mestaResponse?.Id,
                    SourceCurrency = request.SourceCurrency,
                    TargetCurrency = request.TargetCurrency,
                    SourceAmount = mestaResponse?.SourceAmount ?? 0,
                    TargetAmount = mestaResponse?.TargetAmount ?? 0,
                    ExchangeRate = mestaResponse?.ExchangeRate ?? 0,
                    FeeAmount = mestaResponse?.Fee ?? 0,
                    TotalAmount = mestaResponse?.TotalAmount ?? 0,
                    Network = request.Network,
                    Provider = ProviderId,
                    ExpiresAt = mestaResponse?.ExpiresAt ?? DateTimeOffset.UtcNow.AddMinutes(5),
                    CreatedAt = DateTimeOffset.UtcNow
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating quote in Mesta");
            return new QuoteResponse
            {
                Success = false,
                ErrorCode = "PROVIDER_ERROR",
                ErrorMessage = ex.Message,
                Provider = ProviderId
            };
        }
    }

    public async Task<QuoteResponse> GetQuoteAsync(string quoteId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/quotes/{quoteId}", cancellationToken);
            var content = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return new QuoteResponse
                {
                    Success = false,
                    ErrorCode = response.StatusCode == System.Net.HttpStatusCode.NotFound ? "NOT_FOUND" : "GET_FAILED",
                    ErrorMessage = content,
                    Provider = ProviderId
                };
            }

            var mestaResponse = JsonSerializer.Deserialize<MestaQuoteResponse>(content, _jsonOptions);

            return new QuoteResponse
            {
                Success = true,
                Provider = ProviderId,
                Quote = new QuoteData
                {
                    Id = mestaResponse?.Id ?? quoteId,
                    ProviderQuoteId = mestaResponse?.Id,
                    SourceCurrency = Enum.TryParse<Stablecoin>(mestaResponse?.SourceCurrency, true, out var sc) ? sc : Stablecoin.USDT,
                    TargetCurrency = Enum.TryParse<FiatCurrency>(mestaResponse?.TargetCurrency, true, out var tc) ? tc : FiatCurrency.USD,
                    SourceAmount = mestaResponse?.SourceAmount ?? 0,
                    TargetAmount = mestaResponse?.TargetAmount ?? 0,
                    ExchangeRate = mestaResponse?.ExchangeRate ?? 0,
                    FeeAmount = mestaResponse?.Fee ?? 0,
                    TotalAmount = mestaResponse?.TotalAmount ?? 0,
                    Network = BlockchainNetwork.Polygon,
                    Provider = ProviderId,
                    ExpiresAt = mestaResponse?.ExpiresAt ?? DateTimeOffset.UtcNow,
                    CreatedAt = mestaResponse?.CreatedAt ?? DateTimeOffset.UtcNow
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting quote {QuoteId} from Mesta", quoteId);
            return new QuoteResponse
            {
                Success = false,
                ErrorCode = "PROVIDER_ERROR",
                ErrorMessage = ex.Message,
                Provider = ProviderId
            };
        }
    }

    #endregion

    #region Payout Operations

    public async Task<PayoutResponse> CreatePayoutAsync(CreatePayoutRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var mestaRequest = new
            {
                external_id = request.ExternalId,
                quote_id = request.QuoteId,
                source_currency = request.SourceCurrency.ToString(),
                target_currency = request.TargetCurrency.ToString(),
                source_amount = request.SourceAmount,
                target_amount = request.TargetAmount,
                network = MapNetwork(request.Network),
                payment_method = request.PaymentMethod.ToString().ToLower(),
                sender = new
                {
                    id = request.Sender.Id,
                    external_id = request.Sender.ExternalId,
                    type = request.Sender.Type.ToString().ToLower(),
                    first_name = request.Sender.FirstName,
                    last_name = request.Sender.LastName,
                    business_name = request.Sender.BusinessName,
                    email = request.Sender.Email,
                    phone = request.Sender.Phone
                },
                beneficiary = new
                {
                    id = request.Beneficiary.Id,
                    external_id = request.Beneficiary.ExternalId,
                    type = request.Beneficiary.Type.ToString().ToLower(),
                    first_name = request.Beneficiary.FirstName,
                    last_name = request.Beneficiary.LastName,
                    business_name = request.Beneficiary.BusinessName,
                    email = request.Beneficiary.Email,
                    phone = request.Beneficiary.Phone,
                    bank_account = new
                    {
                        bank_name = request.Beneficiary.BankAccount.BankName,
                        account_number = request.Beneficiary.BankAccount.AccountNumber,
                        account_holder_name = request.Beneficiary.BankAccount.AccountHolderName,
                        routing_number = request.Beneficiary.BankAccount.RoutingNumber,
                        swift_code = request.Beneficiary.BankAccount.SwiftCode,
                        sort_code = request.Beneficiary.BankAccount.SortCode,
                        iban = request.Beneficiary.BankAccount.Iban,
                        currency = request.Beneficiary.BankAccount.Currency.ToString(),
                        country_code = request.Beneficiary.BankAccount.CountryCode
                    }
                },
                purpose = request.Purpose,
                reference = request.Reference,
                metadata = request.Metadata
            };

            var response = await _httpClient.PostAsJsonAsync("/payouts", mestaRequest, _jsonOptions, cancellationToken);
            var content = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Mesta CreatePayout failed: {Content}", content);
                return new PayoutResponse
                {
                    Success = false,
                    ErrorCode = "PAYOUT_FAILED",
                    ErrorMessage = content,
                    Provider = ProviderId
                };
            }

            var mestaResponse = JsonSerializer.Deserialize<MestaPayoutResponse>(content, _jsonOptions);

            return new PayoutResponse
            {
                Success = true,
                Provider = ProviderId,
                Payout = MapToPayoutData(mestaResponse!, request)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating payout in Mesta");
            return new PayoutResponse
            {
                Success = false,
                ErrorCode = "PROVIDER_ERROR",
                ErrorMessage = ex.Message,
                Provider = ProviderId
            };
        }
    }

    public async Task<PayoutResponse> GetPayoutAsync(string payoutId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/payouts/{payoutId}", cancellationToken);
            var content = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return new PayoutResponse
                {
                    Success = false,
                    ErrorCode = response.StatusCode == System.Net.HttpStatusCode.NotFound ? "NOT_FOUND" : "GET_FAILED",
                    ErrorMessage = content,
                    Provider = ProviderId
                };
            }

            var mestaResponse = JsonSerializer.Deserialize<MestaPayoutResponse>(content, _jsonOptions);

            return new PayoutResponse
            {
                Success = true,
                Provider = ProviderId,
                Payout = MapToPayoutData(mestaResponse!)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting payout {PayoutId} from Mesta", payoutId);
            return new PayoutResponse
            {
                Success = false,
                ErrorCode = "PROVIDER_ERROR",
                ErrorMessage = ex.Message,
                Provider = ProviderId
            };
        }
    }

    public async Task<PayoutStatusResponse> GetPayoutStatusAsync(string payoutId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/payouts/{payoutId}/status", cancellationToken);
            var content = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return new PayoutStatusResponse
                {
                    Success = false,
                    ErrorCode = "STATUS_FAILED",
                    ErrorMessage = content,
                    Provider = ProviderId
                };
            }

            var mestaResponse = JsonSerializer.Deserialize<MestaPayoutStatusResponse>(content, _jsonOptions);

            return new PayoutStatusResponse
            {
                Success = true,
                Provider = ProviderId,
                PayoutId = payoutId,
                ProviderPayoutId = mestaResponse?.Id,
                Status = MapPayoutStatus(mestaResponse?.Status),
                ProviderStatus = mestaResponse?.Status,
                BlockchainTxHash = mestaResponse?.BlockchainTxHash,
                BankReference = mestaResponse?.BankReference,
                FailureReason = mestaResponse?.FailureReason,
                Timestamp = mestaResponse?.UpdatedAt ?? DateTimeOffset.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting payout status for {PayoutId} from Mesta", payoutId);
            return new PayoutStatusResponse
            {
                Success = false,
                ErrorCode = "PROVIDER_ERROR",
                ErrorMessage = ex.Message,
                Provider = ProviderId
            };
        }
    }

    public async Task<PayoutResponse> CancelPayoutAsync(string payoutId, CancellationToken cancellationToken = default)
    {
        try
        {
            var response = await _httpClient.PostAsync($"/payouts/{payoutId}/cancel", null, cancellationToken);
            var content = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return new PayoutResponse
                {
                    Success = false,
                    ErrorCode = "CANCEL_FAILED",
                    ErrorMessage = content,
                    Provider = ProviderId
                };
            }

            var mestaResponse = JsonSerializer.Deserialize<MestaPayoutResponse>(content, _jsonOptions);

            return new PayoutResponse
            {
                Success = true,
                Provider = ProviderId,
                Payout = MapToPayoutData(mestaResponse!)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error canceling payout {PayoutId} in Mesta", payoutId);
            return new PayoutResponse
            {
                Success = false,
                ErrorCode = "PROVIDER_ERROR",
                ErrorMessage = ex.Message,
                Provider = ProviderId
            };
        }
    }

    public async Task<PayoutListResponse> ListPayoutsAsync(ListPayoutsRequest request, CancellationToken cancellationToken = default)
    {
        try
        {
            var queryParams = new List<string>
            {
                $"page={request.Page}",
                $"page_size={request.PageSize}"
            };

            if (request.Status.HasValue)
                queryParams.Add($"status={request.Status.Value.ToString().ToLower()}");
            if (!string.IsNullOrEmpty(request.CustomerId))
                queryParams.Add($"customer_id={request.CustomerId}");
            if (request.FromDate.HasValue)
                queryParams.Add($"from_date={request.FromDate.Value:yyyy-MM-dd}");
            if (request.ToDate.HasValue)
                queryParams.Add($"to_date={request.ToDate.Value:yyyy-MM-dd}");

            var url = $"/payouts?{string.Join("&", queryParams)}";
            var response = await _httpClient.GetAsync(url, cancellationToken);
            var content = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return new PayoutListResponse
                {
                    Success = false,
                    ErrorCode = "LIST_FAILED",
                    ErrorMessage = content,
                    Provider = ProviderId
                };
            }

            var mestaResponse = JsonSerializer.Deserialize<MestaPayoutListResponse>(content, _jsonOptions);

            return new PayoutListResponse
            {
                Success = true,
                Provider = ProviderId,
                Payouts = mestaResponse?.Data?.Select(p => MapToPayoutData(p)).ToList() ?? [],
                TotalCount = mestaResponse?.Total ?? 0,
                Page = mestaResponse?.Page ?? 1,
                PageSize = mestaResponse?.PageSize ?? 20
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing payouts from Mesta");
            return new PayoutListResponse
            {
                Success = false,
                ErrorCode = "PROVIDER_ERROR",
                ErrorMessage = ex.Message,
                Provider = ProviderId
            };
        }
    }

    #endregion

    #region Webhook

    public bool ValidateWebhookSignature(string payload, string signature, string secret)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        var computedSignature = Convert.ToHexString(hash).ToLower();
        return computedSignature == signature.ToLower();
    }

    public async Task<WebhookResponse> ProcessWebhookAsync(string payload, string signature, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!ValidateWebhookSignature(payload, signature, _settings.WebhookSecret))
            {
                return new WebhookResponse
                {
                    Success = false,
                    ErrorCode = "INVALID_SIGNATURE",
                    ErrorMessage = "Webhook signature validation failed",
                    Provider = ProviderId,
                    EventType = "unknown"
                };
            }

            var webhookData = JsonSerializer.Deserialize<MestaWebhookPayload>(payload, _jsonOptions);

            return new WebhookResponse
            {
                Success = true,
                Provider = ProviderId,
                EventType = webhookData?.Event ?? "unknown",
                ResourceId = webhookData?.Data?.Id,
                ResourceType = webhookData?.ResourceType,
                Data = webhookData?.Data
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing webhook from Mesta");
            return new WebhookResponse
            {
                Success = false,
                ErrorCode = "PROCESSING_ERROR",
                ErrorMessage = ex.Message,
                Provider = ProviderId,
                EventType = "unknown"
            };
        }
    }

    #endregion

    #region Private Helpers

    private static CustomerData MapToCustomerData(MestaCustomerResponse response)
    {
        return new CustomerData
        {
            Id = response.Id ?? string.Empty,
            ExternalId = response.ExternalId,
            Type = Enum.TryParse<CustomerType>(response.Type, true, out var ct) ? ct : CustomerType.Individual,
            Status = Enum.TryParse<CustomerStatus>(response.Status, true, out var cs) ? cs : CustomerStatus.Pending,
            Individual = !string.IsNullOrEmpty(response.FirstName) ? new IndividualInfo
            {
                FirstName = response.FirstName!,
                LastName = response.LastName ?? string.Empty,
                MiddleName = response.MiddleName,
                DateOfBirth = response.DateOfBirth,
                Nationality = response.Nationality
            } : null,
            Business = !string.IsNullOrEmpty(response.BusinessName) ? new BusinessInfo
            {
                LegalName = response.BusinessName!,
                TradingName = response.TradingName,
                RegistrationNumber = response.RegistrationNumber,
                TaxId = response.TaxId,
                CountryOfIncorporation = response.CountryOfIncorporation
            } : null,
            Contact = new ContactInfo
            {
                Email = response.Email ?? string.Empty,
                Phone = response.Phone
            },
            VerificationStatus = MapVerificationStatus(response.VerificationStatus),
            VerificationLevel = MapVerificationLevel(response.VerificationLevel),
            CreatedAt = response.CreatedAt ?? DateTimeOffset.UtcNow,
            UpdatedAt = response.UpdatedAt ?? DateTimeOffset.UtcNow
        };
    }

    private static PayoutData MapToPayoutData(MestaPayoutResponse response, CreatePayoutRequest? request = null)
    {
        return new PayoutData
        {
            Id = response.Id ?? Guid.NewGuid().ToString(),
            ExternalId = response.ExternalId,
            ProviderPayoutId = response.Id,
            Status = MapPayoutStatus(response.Status),
            SourceCurrency = Enum.TryParse<Stablecoin>(response.SourceCurrency, true, out var sc) ? sc : (request?.SourceCurrency ?? Stablecoin.USDT),
            TargetCurrency = Enum.TryParse<FiatCurrency>(response.TargetCurrency, true, out var tc) ? tc : (request?.TargetCurrency ?? FiatCurrency.USD),
            SourceAmount = response.SourceAmount ?? request?.SourceAmount ?? 0,
            TargetAmount = response.TargetAmount ?? request?.TargetAmount ?? 0,
            ExchangeRate = response.ExchangeRate ?? 0,
            FeeAmount = response.Fee ?? 0,
            Network = request?.Network ?? BlockchainNetwork.Polygon,
            Provider = PaymentProvider.Mesta,
            DepositWallet = response.DepositWallet != null ? new DepositWalletData
            {
                Address = response.DepositWallet.Address ?? string.Empty,
                Network = Enum.TryParse<BlockchainNetwork>(response.DepositWallet.Network, true, out var dn) ? dn : BlockchainNetwork.Polygon,
                Currency = Enum.TryParse<Stablecoin>(response.DepositWallet.Currency, true, out var dc) ? dc : Stablecoin.USDT,
                ExpectedAmount = response.DepositWallet.ExpectedAmount ?? 0,
                ExpiresAt = response.DepositWallet.ExpiresAt,
                Memo = response.DepositWallet.Memo
            } : null,
            CreatedAt = response.CreatedAt ?? DateTimeOffset.UtcNow,
            UpdatedAt = response.UpdatedAt ?? DateTimeOffset.UtcNow
        };
    }

    private static VerificationStatus MapVerificationStatus(string? status)
    {
        return status?.ToLower() switch
        {
            "pending" => VerificationStatus.Pending,
            "in_progress" or "inprogress" => VerificationStatus.InProgress,
            "documents_required" => VerificationStatus.DocumentsRequired,
            "under_review" => VerificationStatus.UnderReview,
            "approved" or "verified" => VerificationStatus.Approved,
            "rejected" or "failed" => VerificationStatus.Rejected,
            "expired" => VerificationStatus.Expired,
            _ => VerificationStatus.NotStarted
        };
    }

    private static VerificationLevel MapVerificationLevel(string? level)
    {
        return level?.ToLower() switch
        {
            "basic" => VerificationLevel.Basic,
            "standard" => VerificationLevel.Standard,
            "enhanced" => VerificationLevel.Enhanced,
            _ => VerificationLevel.None
        };
    }

    private static PayoutStatus MapPayoutStatus(string? status)
    {
        return status?.ToLower() switch
        {
            "pending" => PayoutStatus.Pending,
            "awaiting_deposit" => PayoutStatus.AwaitingDeposit,
            "deposit_received" => PayoutStatus.DepositReceived,
            "processing" => PayoutStatus.Processing,
            "completed" or "success" => PayoutStatus.Completed,
            "failed" => PayoutStatus.Failed,
            "cancelled" or "canceled" => PayoutStatus.Cancelled,
            "refunded" => PayoutStatus.Refunded,
            "expired" => PayoutStatus.Expired,
            _ => PayoutStatus.Pending
        };
    }

    private static string MapNetwork(BlockchainNetwork network)
    {
        return network switch
        {
            BlockchainNetwork.Ethereum => "ethereum",
            BlockchainNetwork.Polygon => "polygon",
            BlockchainNetwork.Tron => "tron",
            BlockchainNetwork.Solana => "solana",
            BlockchainNetwork.BinanceSmartChain => "bsc",
            BlockchainNetwork.Avalanche => "avalanche",
            BlockchainNetwork.Arbitrum => "arbitrum",
            BlockchainNetwork.Optimism => "optimism",
            BlockchainNetwork.Base => "base",
            _ => "polygon"
        };
    }

    #endregion
}

#region Mesta DTOs

internal sealed class MestaCustomerResponse
{
    public string? Id { get; set; }
    public string? ExternalId { get; set; }
    public string? Type { get; set; }
    public string? Status { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? MiddleName { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public string? Nationality { get; set; }
    public string? BusinessName { get; set; }
    public string? TradingName { get; set; }
    public string? RegistrationNumber { get; set; }
    public string? TaxId { get; set; }
    public string? CountryOfIncorporation { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? VerificationStatus { get; set; }
    public string? VerificationLevel { get; set; }
    public DateTimeOffset? CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}

internal sealed class MestaCustomerListResponse
{
    public List<MestaCustomerResponse>? Data { get; set; }
    public int Total { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}

internal sealed class MestaVerificationResponse
{
    public string? SessionId { get; set; }
    public string? VerificationUrl { get; set; }
    public string? Status { get; set; }
    public string? Level { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }
    public string? RejectionReason { get; set; }
}

internal sealed class MestaDocumentResponse
{
    public string? Id { get; set; }
    public string? DocumentType { get; set; }
    public string? Status { get; set; }
    public DateTimeOffset? CreatedAt { get; set; }
    public DateTimeOffset? ReviewedAt { get; set; }
}

internal sealed class MestaDocumentListResponse
{
    public List<MestaDocumentResponse>? Data { get; set; }
}

internal sealed class MestaQuoteResponse
{
    public string? Id { get; set; }
    public string? SourceCurrency { get; set; }
    public string? TargetCurrency { get; set; }
    public decimal? SourceAmount { get; set; }
    public decimal? TargetAmount { get; set; }
    public decimal? ExchangeRate { get; set; }
    public decimal? Fee { get; set; }
    public decimal? TotalAmount { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }
    public DateTimeOffset? CreatedAt { get; set; }
}

internal sealed class MestaPayoutResponse
{
    public string? Id { get; set; }
    public string? ExternalId { get; set; }
    public string? Status { get; set; }
    public string? SourceCurrency { get; set; }
    public string? TargetCurrency { get; set; }
    public decimal? SourceAmount { get; set; }
    public decimal? TargetAmount { get; set; }
    public decimal? ExchangeRate { get; set; }
    public decimal? Fee { get; set; }
    public MestaDepositWalletResponse? DepositWallet { get; set; }
    public DateTimeOffset? CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}

internal sealed class MestaDepositWalletResponse
{
    public string? Address { get; set; }
    public string? Network { get; set; }
    public string? Currency { get; set; }
    public decimal? ExpectedAmount { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }
    public string? Memo { get; set; }
}

internal sealed class MestaPayoutStatusResponse
{
    public string? Id { get; set; }
    public string? Status { get; set; }
    public string? BlockchainTxHash { get; set; }
    public string? BankReference { get; set; }
    public string? FailureReason { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}

internal sealed class MestaPayoutListResponse
{
    public List<MestaPayoutResponse>? Data { get; set; }
    public int Total { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}

internal sealed class MestaWebhookPayload
{
    public string? Event { get; set; }
    public string? ResourceType { get; set; }
    public MestaWebhookData? Data { get; set; }
}

internal sealed class MestaWebhookData
{
    public string? Id { get; set; }
    public string? Status { get; set; }
}

#endregion
