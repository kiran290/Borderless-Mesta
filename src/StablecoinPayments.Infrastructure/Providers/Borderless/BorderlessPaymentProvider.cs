using System.Net.Http.Headers;
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

namespace StablecoinPayments.Infrastructure.Providers.Borderless;

public sealed class BorderlessPaymentProvider : IPaymentProvider
{
    private readonly HttpClient _httpClient;
    private readonly BorderlessSettings _settings;
    private readonly ILogger<BorderlessPaymentProvider> _logger;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly SemaphoreSlim _tokenLock = new(1, 1);

    private string? _accessToken;
    private DateTimeOffset _tokenExpiry = DateTimeOffset.MinValue;

    public BorderlessPaymentProvider(
        HttpClient httpClient,
        IOptions<PaymentSettings> settings,
        ILogger<BorderlessPaymentProvider> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value.Borderless;
        _logger = logger;
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true
        };

        _httpClient.BaseAddress = new Uri(_settings.BaseUrl);
    }

    public PaymentProvider ProviderId => PaymentProvider.Borderless;
    public string ProviderName => "Borderless";

    #region OAuth Token Management

    private async Task EnsureAuthenticatedAsync(CancellationToken cancellationToken)
    {
        if (_accessToken != null && DateTimeOffset.UtcNow < _tokenExpiry.AddMinutes(-1))
            return;

        await _tokenLock.WaitAsync(cancellationToken);
        try
        {
            if (_accessToken != null && DateTimeOffset.UtcNow < _tokenExpiry.AddMinutes(-1))
                return;

            var tokenRequest = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "client_credentials",
                ["client_id"] = _settings.ClientId,
                ["client_secret"] = _settings.ApiSecret
            });

            var response = await _httpClient.PostAsync("/oauth/token", tokenRequest, cancellationToken);
            var content = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Borderless OAuth failed: {Content}", content);
                throw new InvalidOperationException($"OAuth failed: {content}");
            }

            var tokenResponse = JsonSerializer.Deserialize<OAuthTokenResponse>(content, _jsonOptions);
            _accessToken = tokenResponse?.AccessToken;
            _tokenExpiry = DateTimeOffset.UtcNow.AddSeconds(tokenResponse?.ExpiresIn ?? 3600);

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);
        }
        finally
        {
            _tokenLock.Release();
        }
    }

    #endregion

    #region Health Check

    public async Task<HealthCheckResult> CheckHealthAsync(CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;
        try
        {
            await EnsureAuthenticatedAsync(cancellationToken);
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
            _logger.LogError(ex, "Health check failed for Borderless");
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
            await EnsureAuthenticatedAsync(cancellationToken);

            var borderlessRequest = new
            {
                externalId = request.ExternalId,
                type = request.Type == CustomerType.Individual ? "individual" : "business",
                individual = request.Individual != null ? new
                {
                    firstName = request.Individual.FirstName,
                    lastName = request.Individual.LastName,
                    middleName = request.Individual.MiddleName,
                    dateOfBirth = request.Individual.DateOfBirth?.ToString("yyyy-MM-dd"),
                    nationality = request.Individual.Nationality
                } : null,
                business = request.Business != null ? new
                {
                    legalName = request.Business.LegalName,
                    tradingName = request.Business.TradingName,
                    registrationNumber = request.Business.RegistrationNumber,
                    taxId = request.Business.TaxId,
                    countryOfIncorporation = request.Business.CountryOfIncorporation,
                    website = request.Business.Website,
                    industry = request.Business.Industry
                } : null,
                contact = new
                {
                    email = request.Contact.Email,
                    phone = request.Contact.Phone
                },
                address = request.Address != null ? new
                {
                    line1 = request.Address.Street1,
                    line2 = request.Address.Street2,
                    city = request.Address.City,
                    state = request.Address.State,
                    postalCode = request.Address.PostalCode,
                    country = request.Address.CountryCode
                } : null,
                metadata = request.Metadata
            };

            var response = await _httpClient.PostAsJsonAsync("/customers", borderlessRequest, _jsonOptions, cancellationToken);
            var content = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Borderless CreateCustomer failed: {Content}", content);
                return new CustomerResponse
                {
                    Success = false,
                    ErrorCode = "CREATE_FAILED",
                    ErrorMessage = content,
                    Provider = ProviderId
                };
            }

            var borderlessResponse = JsonSerializer.Deserialize<BorderlessCustomerResponse>(content, _jsonOptions);

            return new CustomerResponse
            {
                Success = true,
                Provider = ProviderId,
                ProviderCustomerId = borderlessResponse?.Id,
                Customer = MapToCustomerData(borderlessResponse!)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating customer in Borderless");
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
            await EnsureAuthenticatedAsync(cancellationToken);

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

            var borderlessResponse = JsonSerializer.Deserialize<BorderlessCustomerResponse>(content, _jsonOptions);

            return new CustomerResponse
            {
                Success = true,
                Provider = ProviderId,
                ProviderCustomerId = borderlessResponse?.Id,
                Customer = MapToCustomerData(borderlessResponse!)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting customer {CustomerId} from Borderless", customerId);
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
            await EnsureAuthenticatedAsync(cancellationToken);

            var borderlessRequest = new
            {
                individual = request.Individual != null ? new
                {
                    firstName = request.Individual.FirstName,
                    lastName = request.Individual.LastName,
                    middleName = request.Individual.MiddleName,
                    dateOfBirth = request.Individual.DateOfBirth?.ToString("yyyy-MM-dd"),
                    nationality = request.Individual.Nationality
                } : null,
                business = request.Business != null ? new
                {
                    legalName = request.Business.LegalName,
                    tradingName = request.Business.TradingName,
                    registrationNumber = request.Business.RegistrationNumber,
                    taxId = request.Business.TaxId
                } : null,
                contact = request.Contact != null ? new
                {
                    email = request.Contact.Email,
                    phone = request.Contact.Phone
                } : null,
                address = request.Address != null ? new
                {
                    line1 = request.Address.Street1,
                    line2 = request.Address.Street2,
                    city = request.Address.City,
                    state = request.Address.State,
                    postalCode = request.Address.PostalCode,
                    country = request.Address.CountryCode
                } : null,
                metadata = request.Metadata
            };

            var response = await _httpClient.PatchAsJsonAsync($"/customers/{customerId}", borderlessRequest, _jsonOptions, cancellationToken);
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

            var borderlessResponse = JsonSerializer.Deserialize<BorderlessCustomerResponse>(content, _jsonOptions);

            return new CustomerResponse
            {
                Success = true,
                Provider = ProviderId,
                ProviderCustomerId = borderlessResponse?.Id,
                Customer = MapToCustomerData(borderlessResponse!)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating customer {CustomerId} in Borderless", customerId);
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
            await EnsureAuthenticatedAsync(cancellationToken);

            var queryParams = new List<string>
            {
                $"page={request.Page}",
                $"limit={request.PageSize}"
            };

            if (request.Type.HasValue)
                queryParams.Add($"type={request.Type.Value.ToString().ToLower()}");
            if (request.Status.HasValue)
                queryParams.Add($"status={request.Status.Value.ToString().ToLower()}");
            if (!string.IsNullOrEmpty(request.Search))
                queryParams.Add($"q={Uri.EscapeDataString(request.Search)}");

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

            var borderlessResponse = JsonSerializer.Deserialize<BorderlessCustomerListResponse>(content, _jsonOptions);

            return new CustomerListResponse
            {
                Success = true,
                Provider = ProviderId,
                Customers = borderlessResponse?.Data?.Select(MapToCustomerData).ToList() ?? [],
                TotalCount = borderlessResponse?.Total ?? 0,
                Page = borderlessResponse?.Page ?? 1,
                PageSize = borderlessResponse?.Limit ?? 20
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing customers from Borderless");
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
            await EnsureAuthenticatedAsync(cancellationToken);

            var borderlessRequest = new
            {
                customerId = request.CustomerId,
                level = request.TargetLevel.ToString().ToLower(),
                redirectUrl = request.RedirectUrl,
                webhookUrl = request.WebhookUrl
            };

            var response = await _httpClient.PostAsJsonAsync("/verification/kyc", borderlessRequest, _jsonOptions, cancellationToken);
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

            var borderlessResponse = JsonSerializer.Deserialize<BorderlessVerificationResponse>(content, _jsonOptions);

            return new VerificationResponse
            {
                Success = true,
                Provider = ProviderId,
                CustomerId = request.CustomerId,
                SessionId = borderlessResponse?.SessionId,
                VerificationUrl = borderlessResponse?.Url,
                Status = MapVerificationStatus(borderlessResponse?.Status),
                Level = request.TargetLevel,
                ExpiresAt = borderlessResponse?.ExpiresAt
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initiating KYC for customer {CustomerId} in Borderless", request.CustomerId);
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
            await EnsureAuthenticatedAsync(cancellationToken);

            var response = await _httpClient.GetAsync($"/verification/kyc/{customerId}", cancellationToken);
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

            var borderlessResponse = JsonSerializer.Deserialize<BorderlessVerificationResponse>(content, _jsonOptions);

            return new VerificationResponse
            {
                Success = true,
                Provider = ProviderId,
                CustomerId = customerId,
                Status = MapVerificationStatus(borderlessResponse?.Status),
                Level = MapVerificationLevel(borderlessResponse?.Level),
                RejectionReason = borderlessResponse?.FailureReason
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting KYC status for customer {CustomerId} from Borderless", customerId);
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
            await EnsureAuthenticatedAsync(cancellationToken);

            var borderlessRequest = new
            {
                customerId = request.CustomerId,
                level = request.TargetLevel.ToString().ToLower(),
                redirectUrl = request.RedirectUrl,
                webhookUrl = request.WebhookUrl
            };

            var response = await _httpClient.PostAsJsonAsync("/verification/kyb", borderlessRequest, _jsonOptions, cancellationToken);
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

            var borderlessResponse = JsonSerializer.Deserialize<BorderlessVerificationResponse>(content, _jsonOptions);

            return new VerificationResponse
            {
                Success = true,
                Provider = ProviderId,
                CustomerId = request.CustomerId,
                SessionId = borderlessResponse?.SessionId,
                VerificationUrl = borderlessResponse?.Url,
                Status = MapVerificationStatus(borderlessResponse?.Status),
                Level = request.TargetLevel,
                ExpiresAt = borderlessResponse?.ExpiresAt
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initiating KYB for customer {CustomerId} in Borderless", request.CustomerId);
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
            await EnsureAuthenticatedAsync(cancellationToken);

            var response = await _httpClient.GetAsync($"/verification/kyb/{customerId}", cancellationToken);
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

            var borderlessResponse = JsonSerializer.Deserialize<BorderlessVerificationResponse>(content, _jsonOptions);

            return new VerificationResponse
            {
                Success = true,
                Provider = ProviderId,
                CustomerId = customerId,
                Status = MapVerificationStatus(borderlessResponse?.Status),
                Level = MapVerificationLevel(borderlessResponse?.Level),
                RejectionReason = borderlessResponse?.FailureReason
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting KYB status for customer {CustomerId} from Borderless", customerId);
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
            await EnsureAuthenticatedAsync(cancellationToken);

            var borderlessRequest = new
            {
                customerId = request.CustomerId,
                type = MapDocumentType(request.DocumentType),
                number = request.DocumentNumber,
                issuingCountry = request.IssuingCountry,
                issueDate = request.IssueDate?.ToString("yyyy-MM-dd"),
                expiryDate = request.ExpiryDate?.ToString("yyyy-MM-dd"),
                frontImage = request.FrontImageBase64,
                backImage = request.BackImageBase64,
                contentType = request.MimeType
            };

            var response = await _httpClient.PostAsJsonAsync("/documents", borderlessRequest, _jsonOptions, cancellationToken);
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

            var borderlessResponse = JsonSerializer.Deserialize<BorderlessDocumentResponse>(content, _jsonOptions);

            return new DocumentResponse
            {
                Success = true,
                Provider = ProviderId,
                DocumentId = borderlessResponse?.Id,
                DocumentType = request.DocumentType,
                Status = borderlessResponse?.Status,
                UploadedAt = borderlessResponse?.CreatedAt ?? DateTimeOffset.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading document for customer {CustomerId} in Borderless", request.CustomerId);
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
            await EnsureAuthenticatedAsync(cancellationToken);

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

            var borderlessResponse = JsonSerializer.Deserialize<BorderlessDocumentListResponse>(content, _jsonOptions);

            return new DocumentListResponse
            {
                Success = true,
                Provider = ProviderId,
                Documents = borderlessResponse?.Documents?.Select(d => new DocumentData
                {
                    Id = d.Id ?? string.Empty,
                    Type = MapDocumentTypeFromBorderless(d.Type),
                    Status = d.Status ?? "unknown",
                    UploadedAt = d.CreatedAt ?? DateTimeOffset.UtcNow,
                    ReviewedAt = d.ReviewedAt
                }).ToList() ?? []
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting documents for customer {CustomerId} from Borderless", customerId);
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
            await EnsureAuthenticatedAsync(cancellationToken);

            var response = await _httpClient.PostAsync($"/verification/{customerId}/submit", null, cancellationToken);
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

            var borderlessResponse = JsonSerializer.Deserialize<BorderlessVerificationResponse>(content, _jsonOptions);

            return new VerificationResponse
            {
                Success = true,
                Provider = ProviderId,
                CustomerId = customerId,
                Status = MapVerificationStatus(borderlessResponse?.Status)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error submitting verification for customer {CustomerId} in Borderless", customerId);
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
            await EnsureAuthenticatedAsync(cancellationToken);

            var borderlessRequest = new
            {
                sourceCurrency = request.SourceCurrency.ToString(),
                destinationCurrency = request.TargetCurrency.ToString(),
                sourceAmount = request.SourceAmount,
                destinationAmount = request.TargetAmount,
                network = MapNetwork(request.Network),
                destinationCountry = request.DestinationCountry,
                paymentMethod = request.PaymentMethod.ToString().ToLower()
            };

            var response = await _httpClient.PostAsJsonAsync("/quotes", borderlessRequest, _jsonOptions, cancellationToken);
            var content = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Borderless CreateQuote failed: {Content}", content);
                return new QuoteResponse
                {
                    Success = false,
                    ErrorCode = "QUOTE_FAILED",
                    ErrorMessage = content,
                    Provider = ProviderId
                };
            }

            var borderlessResponse = JsonSerializer.Deserialize<BorderlessQuoteResponse>(content, _jsonOptions);

            return new QuoteResponse
            {
                Success = true,
                Provider = ProviderId,
                Quote = new QuoteData
                {
                    Id = borderlessResponse?.Id ?? Guid.NewGuid().ToString(),
                    ProviderQuoteId = borderlessResponse?.Id,
                    SourceCurrency = request.SourceCurrency,
                    TargetCurrency = request.TargetCurrency,
                    SourceAmount = borderlessResponse?.SourceAmount ?? 0,
                    TargetAmount = borderlessResponse?.DestinationAmount ?? 0,
                    ExchangeRate = borderlessResponse?.Rate ?? 0,
                    FeeAmount = borderlessResponse?.Fee ?? 0,
                    TotalAmount = borderlessResponse?.TotalAmount ?? 0,
                    Network = request.Network,
                    Provider = ProviderId,
                    ExpiresAt = borderlessResponse?.ExpiresAt ?? DateTimeOffset.UtcNow.AddMinutes(5),
                    CreatedAt = DateTimeOffset.UtcNow
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating quote in Borderless");
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
            await EnsureAuthenticatedAsync(cancellationToken);

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

            var borderlessResponse = JsonSerializer.Deserialize<BorderlessQuoteResponse>(content, _jsonOptions);

            return new QuoteResponse
            {
                Success = true,
                Provider = ProviderId,
                Quote = new QuoteData
                {
                    Id = borderlessResponse?.Id ?? quoteId,
                    ProviderQuoteId = borderlessResponse?.Id,
                    SourceCurrency = Enum.TryParse<Stablecoin>(borderlessResponse?.SourceCurrency, true, out var sc) ? sc : Stablecoin.USDT,
                    TargetCurrency = Enum.TryParse<FiatCurrency>(borderlessResponse?.DestinationCurrency, true, out var tc) ? tc : FiatCurrency.USD,
                    SourceAmount = borderlessResponse?.SourceAmount ?? 0,
                    TargetAmount = borderlessResponse?.DestinationAmount ?? 0,
                    ExchangeRate = borderlessResponse?.Rate ?? 0,
                    FeeAmount = borderlessResponse?.Fee ?? 0,
                    TotalAmount = borderlessResponse?.TotalAmount ?? 0,
                    Network = BlockchainNetwork.Polygon,
                    Provider = ProviderId,
                    ExpiresAt = borderlessResponse?.ExpiresAt ?? DateTimeOffset.UtcNow,
                    CreatedAt = borderlessResponse?.CreatedAt ?? DateTimeOffset.UtcNow
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting quote {QuoteId} from Borderless", quoteId);
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
            await EnsureAuthenticatedAsync(cancellationToken);

            var borderlessRequest = new
            {
                externalId = request.ExternalId,
                quoteId = request.QuoteId,
                sourceCurrency = request.SourceCurrency.ToString(),
                destinationCurrency = request.TargetCurrency.ToString(),
                sourceAmount = request.SourceAmount,
                destinationAmount = request.TargetAmount,
                network = MapNetwork(request.Network),
                paymentMethod = request.PaymentMethod.ToString().ToLower(),
                sender = new
                {
                    id = request.Sender.Id,
                    externalId = request.Sender.ExternalId,
                    type = request.Sender.Type.ToString().ToLower(),
                    firstName = request.Sender.FirstName,
                    lastName = request.Sender.LastName,
                    businessName = request.Sender.BusinessName,
                    email = request.Sender.Email,
                    phone = request.Sender.Phone
                },
                recipient = new
                {
                    id = request.Beneficiary.Id,
                    externalId = request.Beneficiary.ExternalId,
                    type = request.Beneficiary.Type.ToString().ToLower(),
                    firstName = request.Beneficiary.FirstName,
                    lastName = request.Beneficiary.LastName,
                    businessName = request.Beneficiary.BusinessName,
                    email = request.Beneficiary.Email,
                    phone = request.Beneficiary.Phone,
                    bankAccount = new
                    {
                        bankName = request.Beneficiary.BankAccount.BankName,
                        accountNumber = request.Beneficiary.BankAccount.AccountNumber,
                        accountName = request.Beneficiary.BankAccount.AccountHolderName,
                        routingNumber = request.Beneficiary.BankAccount.RoutingNumber,
                        swiftBic = request.Beneficiary.BankAccount.SwiftCode,
                        sortCode = request.Beneficiary.BankAccount.SortCode,
                        iban = request.Beneficiary.BankAccount.Iban,
                        currency = request.Beneficiary.BankAccount.Currency.ToString(),
                        country = request.Beneficiary.BankAccount.CountryCode
                    }
                },
                purpose = request.Purpose,
                reference = request.Reference,
                metadata = request.Metadata
            };

            var response = await _httpClient.PostAsJsonAsync("/transfers", borderlessRequest, _jsonOptions, cancellationToken);
            var content = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Borderless CreatePayout failed: {Content}", content);
                return new PayoutResponse
                {
                    Success = false,
                    ErrorCode = "PAYOUT_FAILED",
                    ErrorMessage = content,
                    Provider = ProviderId
                };
            }

            var borderlessResponse = JsonSerializer.Deserialize<BorderlessTransferResponse>(content, _jsonOptions);

            return new PayoutResponse
            {
                Success = true,
                Provider = ProviderId,
                Payout = MapToPayoutData(borderlessResponse!, request)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating payout in Borderless");
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
            await EnsureAuthenticatedAsync(cancellationToken);

            var response = await _httpClient.GetAsync($"/transfers/{payoutId}", cancellationToken);
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

            var borderlessResponse = JsonSerializer.Deserialize<BorderlessTransferResponse>(content, _jsonOptions);

            return new PayoutResponse
            {
                Success = true,
                Provider = ProviderId,
                Payout = MapToPayoutData(borderlessResponse!)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting payout {PayoutId} from Borderless", payoutId);
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
            await EnsureAuthenticatedAsync(cancellationToken);

            var response = await _httpClient.GetAsync($"/transfers/{payoutId}", cancellationToken);
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

            var borderlessResponse = JsonSerializer.Deserialize<BorderlessTransferResponse>(content, _jsonOptions);

            return new PayoutStatusResponse
            {
                Success = true,
                Provider = ProviderId,
                PayoutId = payoutId,
                ProviderPayoutId = borderlessResponse?.Id,
                Status = MapPayoutStatus(borderlessResponse?.Status),
                ProviderStatus = borderlessResponse?.Status,
                BlockchainTxHash = borderlessResponse?.BlockchainTxHash,
                BankReference = borderlessResponse?.BankReference,
                FailureReason = borderlessResponse?.FailureReason,
                Timestamp = borderlessResponse?.UpdatedAt ?? DateTimeOffset.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting payout status for {PayoutId} from Borderless", payoutId);
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
            await EnsureAuthenticatedAsync(cancellationToken);

            var response = await _httpClient.PostAsync($"/transfers/{payoutId}/cancel", null, cancellationToken);
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

            var borderlessResponse = JsonSerializer.Deserialize<BorderlessTransferResponse>(content, _jsonOptions);

            return new PayoutResponse
            {
                Success = true,
                Provider = ProviderId,
                Payout = MapToPayoutData(borderlessResponse!)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error canceling payout {PayoutId} in Borderless", payoutId);
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
            await EnsureAuthenticatedAsync(cancellationToken);

            var queryParams = new List<string>
            {
                $"page={request.Page}",
                $"limit={request.PageSize}"
            };

            if (request.Status.HasValue)
                queryParams.Add($"status={request.Status.Value.ToString().ToLower()}");
            if (!string.IsNullOrEmpty(request.CustomerId))
                queryParams.Add($"customerId={request.CustomerId}");
            if (request.FromDate.HasValue)
                queryParams.Add($"from={request.FromDate.Value:yyyy-MM-dd}");
            if (request.ToDate.HasValue)
                queryParams.Add($"to={request.ToDate.Value:yyyy-MM-dd}");

            var url = $"/transfers?{string.Join("&", queryParams)}";
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

            var borderlessResponse = JsonSerializer.Deserialize<BorderlessTransferListResponse>(content, _jsonOptions);

            return new PayoutListResponse
            {
                Success = true,
                Provider = ProviderId,
                Payouts = borderlessResponse?.Transfers?.Select(p => MapToPayoutData(p)).ToList() ?? [],
                TotalCount = borderlessResponse?.Total ?? 0,
                Page = borderlessResponse?.Page ?? 1,
                PageSize = borderlessResponse?.Limit ?? 20
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error listing payouts from Borderless");
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
        var computedSignature = Convert.ToBase64String(hash);
        return computedSignature == signature;
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

            var webhookData = JsonSerializer.Deserialize<BorderlessWebhookPayload>(payload, _jsonOptions);

            return new WebhookResponse
            {
                Success = true,
                Provider = ProviderId,
                EventType = webhookData?.Type ?? "unknown",
                ResourceId = webhookData?.Data?.Id,
                ResourceType = webhookData?.ResourceType,
                Data = webhookData?.Data
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing webhook from Borderless");
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

    private static CustomerData MapToCustomerData(BorderlessCustomerResponse response)
    {
        return new CustomerData
        {
            Id = response.Id ?? string.Empty,
            ExternalId = response.ExternalId,
            Type = response.Type == "business" ? CustomerType.Business : CustomerType.Individual,
            Status = MapCustomerStatus(response.Status),
            Individual = response.Individual != null ? new IndividualInfo
            {
                FirstName = response.Individual.FirstName ?? string.Empty,
                LastName = response.Individual.LastName ?? string.Empty,
                MiddleName = response.Individual.MiddleName,
                DateOfBirth = response.Individual.DateOfBirth,
                Nationality = response.Individual.Nationality
            } : null,
            Business = response.Business != null ? new BusinessInfo
            {
                LegalName = response.Business.LegalName ?? string.Empty,
                TradingName = response.Business.TradingName,
                RegistrationNumber = response.Business.RegistrationNumber,
                TaxId = response.Business.TaxId,
                CountryOfIncorporation = response.Business.CountryOfIncorporation
            } : null,
            Contact = response.Contact != null ? new ContactInfo
            {
                Email = response.Contact.Email ?? string.Empty,
                Phone = response.Contact.Phone
            } : null,
            VerificationStatus = MapVerificationStatus(response.VerificationStatus),
            VerificationLevel = MapVerificationLevel(response.VerificationLevel),
            CreatedAt = response.CreatedAt ?? DateTimeOffset.UtcNow,
            UpdatedAt = response.UpdatedAt ?? DateTimeOffset.UtcNow
        };
    }

    private static PayoutData MapToPayoutData(BorderlessTransferResponse response, CreatePayoutRequest? request = null)
    {
        return new PayoutData
        {
            Id = response.Id ?? Guid.NewGuid().ToString(),
            ExternalId = response.ExternalId,
            ProviderPayoutId = response.Id,
            Status = MapPayoutStatus(response.Status),
            SourceCurrency = Enum.TryParse<Stablecoin>(response.SourceCurrency, true, out var sc) ? sc : (request?.SourceCurrency ?? Stablecoin.USDT),
            TargetCurrency = Enum.TryParse<FiatCurrency>(response.DestinationCurrency, true, out var tc) ? tc : (request?.TargetCurrency ?? FiatCurrency.USD),
            SourceAmount = response.SourceAmount ?? request?.SourceAmount ?? 0,
            TargetAmount = response.DestinationAmount ?? request?.TargetAmount ?? 0,
            ExchangeRate = response.Rate ?? 0,
            FeeAmount = response.Fee ?? 0,
            Network = request?.Network ?? BlockchainNetwork.Polygon,
            Provider = PaymentProvider.Borderless,
            DepositWallet = response.DepositAddress != null ? new DepositWalletData
            {
                Address = response.DepositAddress.Address ?? string.Empty,
                Network = Enum.TryParse<BlockchainNetwork>(response.DepositAddress.Network, true, out var dn) ? dn : BlockchainNetwork.Polygon,
                Currency = Enum.TryParse<Stablecoin>(response.DepositAddress.Currency, true, out var dc) ? dc : Stablecoin.USDT,
                ExpectedAmount = response.DepositAddress.ExpectedAmount ?? 0,
                ExpiresAt = response.DepositAddress.ExpiresAt,
                Memo = response.DepositAddress.Memo
            } : null,
            CreatedAt = response.CreatedAt ?? DateTimeOffset.UtcNow,
            UpdatedAt = response.UpdatedAt ?? DateTimeOffset.UtcNow
        };
    }

    private static CustomerStatus MapCustomerStatus(string? status)
    {
        return status?.ToLower() switch
        {
            "active" => CustomerStatus.Active,
            "suspended" => CustomerStatus.Suspended,
            "closed" => CustomerStatus.Closed,
            _ => CustomerStatus.Pending
        };
    }

    private static VerificationStatus MapVerificationStatus(string? status)
    {
        return status?.ToLower() switch
        {
            "pending" => VerificationStatus.Pending,
            "in_progress" or "inprogress" or "processing" => VerificationStatus.InProgress,
            "documents_required" or "awaiting_documents" => VerificationStatus.DocumentsRequired,
            "under_review" or "reviewing" => VerificationStatus.UnderReview,
            "approved" or "verified" or "complete" => VerificationStatus.Approved,
            "rejected" or "failed" or "declined" => VerificationStatus.Rejected,
            "expired" => VerificationStatus.Expired,
            _ => VerificationStatus.NotStarted
        };
    }

    private static VerificationLevel MapVerificationLevel(string? level)
    {
        return level?.ToLower() switch
        {
            "basic" or "tier1" => VerificationLevel.Basic,
            "standard" or "tier2" => VerificationLevel.Standard,
            "enhanced" or "tier3" => VerificationLevel.Enhanced,
            _ => VerificationLevel.None
        };
    }

    private static PayoutStatus MapPayoutStatus(string? status)
    {
        return status?.ToLower() switch
        {
            "pending" or "created" => PayoutStatus.Pending,
            "awaiting_deposit" or "awaiting_payment" => PayoutStatus.AwaitingDeposit,
            "deposit_received" or "funded" => PayoutStatus.DepositReceived,
            "processing" or "in_progress" => PayoutStatus.Processing,
            "completed" or "success" or "settled" => PayoutStatus.Completed,
            "failed" or "error" => PayoutStatus.Failed,
            "cancelled" or "canceled" => PayoutStatus.Cancelled,
            "refunded" or "returned" => PayoutStatus.Refunded,
            "expired" => PayoutStatus.Expired,
            _ => PayoutStatus.Pending
        };
    }

    private static string MapNetwork(BlockchainNetwork network)
    {
        return network switch
        {
            BlockchainNetwork.Ethereum => "ETH",
            BlockchainNetwork.Polygon => "MATIC",
            BlockchainNetwork.Tron => "TRX",
            BlockchainNetwork.Solana => "SOL",
            BlockchainNetwork.BinanceSmartChain => "BSC",
            BlockchainNetwork.Avalanche => "AVAX",
            BlockchainNetwork.Arbitrum => "ARB",
            BlockchainNetwork.Optimism => "OP",
            BlockchainNetwork.Base => "BASE",
            _ => "MATIC"
        };
    }

    private static string MapDocumentType(DocumentType type)
    {
        return type switch
        {
            DocumentType.Passport => "passport",
            DocumentType.NationalId => "national_id",
            DocumentType.DriversLicense => "drivers_license",
            DocumentType.ProofOfAddress => "proof_of_address",
            DocumentType.BankStatement => "bank_statement",
            DocumentType.BusinessRegistration => "business_registration",
            DocumentType.ArticlesOfIncorporation => "articles_of_incorporation",
            DocumentType.TaxDocument => "tax_document",
            DocumentType.Selfie => "selfie",
            _ => "other"
        };
    }

    private static DocumentType MapDocumentTypeFromBorderless(string? type)
    {
        return type?.ToLower() switch
        {
            "passport" => DocumentType.Passport,
            "national_id" => DocumentType.NationalId,
            "drivers_license" => DocumentType.DriversLicense,
            "proof_of_address" => DocumentType.ProofOfAddress,
            "bank_statement" => DocumentType.BankStatement,
            "business_registration" => DocumentType.BusinessRegistration,
            "articles_of_incorporation" => DocumentType.ArticlesOfIncorporation,
            "tax_document" => DocumentType.TaxDocument,
            "selfie" => DocumentType.Selfie,
            _ => DocumentType.Passport
        };
    }

    #endregion
}

#region Borderless DTOs

internal sealed class OAuthTokenResponse
{
    public string? AccessToken { get; set; }
    public string? TokenType { get; set; }
    public int ExpiresIn { get; set; }
}

internal sealed class BorderlessCustomerResponse
{
    public string? Id { get; set; }
    public string? ExternalId { get; set; }
    public string? Type { get; set; }
    public string? Status { get; set; }
    public BorderlessIndividualData? Individual { get; set; }
    public BorderlessBusinessData? Business { get; set; }
    public BorderlessContactData? Contact { get; set; }
    public string? VerificationStatus { get; set; }
    public string? VerificationLevel { get; set; }
    public DateTimeOffset? CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}

internal sealed class BorderlessIndividualData
{
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? MiddleName { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public string? Nationality { get; set; }
}

internal sealed class BorderlessBusinessData
{
    public string? LegalName { get; set; }
    public string? TradingName { get; set; }
    public string? RegistrationNumber { get; set; }
    public string? TaxId { get; set; }
    public string? CountryOfIncorporation { get; set; }
}

internal sealed class BorderlessContactData
{
    public string? Email { get; set; }
    public string? Phone { get; set; }
}

internal sealed class BorderlessCustomerListResponse
{
    public List<BorderlessCustomerResponse>? Data { get; set; }
    public int Total { get; set; }
    public int Page { get; set; }
    public int Limit { get; set; }
}

internal sealed class BorderlessVerificationResponse
{
    public string? SessionId { get; set; }
    public string? Url { get; set; }
    public string? Status { get; set; }
    public string? Level { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }
    public string? FailureReason { get; set; }
}

internal sealed class BorderlessDocumentResponse
{
    public string? Id { get; set; }
    public string? Type { get; set; }
    public string? Status { get; set; }
    public DateTimeOffset? CreatedAt { get; set; }
    public DateTimeOffset? ReviewedAt { get; set; }
}

internal sealed class BorderlessDocumentListResponse
{
    public List<BorderlessDocumentResponse>? Documents { get; set; }
}

internal sealed class BorderlessQuoteResponse
{
    public string? Id { get; set; }
    public string? SourceCurrency { get; set; }
    public string? DestinationCurrency { get; set; }
    public decimal? SourceAmount { get; set; }
    public decimal? DestinationAmount { get; set; }
    public decimal? Rate { get; set; }
    public decimal? Fee { get; set; }
    public decimal? TotalAmount { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }
    public DateTimeOffset? CreatedAt { get; set; }
}

internal sealed class BorderlessTransferResponse
{
    public string? Id { get; set; }
    public string? ExternalId { get; set; }
    public string? Status { get; set; }
    public string? SourceCurrency { get; set; }
    public string? DestinationCurrency { get; set; }
    public decimal? SourceAmount { get; set; }
    public decimal? DestinationAmount { get; set; }
    public decimal? Rate { get; set; }
    public decimal? Fee { get; set; }
    public BorderlessDepositAddressResponse? DepositAddress { get; set; }
    public string? BlockchainTxHash { get; set; }
    public string? BankReference { get; set; }
    public string? FailureReason { get; set; }
    public DateTimeOffset? CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
}

internal sealed class BorderlessDepositAddressResponse
{
    public string? Address { get; set; }
    public string? Network { get; set; }
    public string? Currency { get; set; }
    public decimal? ExpectedAmount { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }
    public string? Memo { get; set; }
}

internal sealed class BorderlessTransferListResponse
{
    public List<BorderlessTransferResponse>? Transfers { get; set; }
    public int Total { get; set; }
    public int Page { get; set; }
    public int Limit { get; set; }
}

internal sealed class BorderlessWebhookPayload
{
    public string? Type { get; set; }
    public string? ResourceType { get; set; }
    public BorderlessWebhookData? Data { get; set; }
}

internal sealed class BorderlessWebhookData
{
    public string? Id { get; set; }
    public string? Status { get; set; }
}

#endregion
