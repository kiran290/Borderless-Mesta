using System.Net.Http.Headers;
using System.Net.Http.Json;
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
/// Borderless customer provider implementation for customer management and KYC/KYB operations.
/// </summary>
public sealed class BorderlessCustomerProvider : ICustomerProvider
{
    private readonly HttpClient _httpClient;
    private readonly BorderlessSettings _settings;
    private readonly ILogger<BorderlessCustomerProvider> _logger;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly SemaphoreSlim _authLock = new(1, 1);

    private string? _accessToken;
    private DateTimeOffset _tokenExpiry = DateTimeOffset.MinValue;

    public PayoutProvider ProviderId => PayoutProvider.Borderless;

    public BorderlessCustomerProvider(
        HttpClient httpClient,
        IOptions<BorderlessSettings> settings,
        ILogger<BorderlessCustomerProvider> logger)
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
        _httpClient.BaseAddress = new Uri(_settings.BaseUrl.TrimEnd('/') + "/");
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
        }
        finally
        {
            _authLock.Release();
        }
    }

    public async Task<string> CreateCustomerAsync(CreateCustomerRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating customer in Borderless: {Email}", request.Contact.Email);

        await EnsureAuthenticatedAsync(cancellationToken);

        var borderlessRequest = MapToBorderlessCreateCustomer(request);
        var response = await PostAsync<BorderlessCustomerCreateResponse>("customers", borderlessRequest, cancellationToken);

        _logger.LogInformation("Customer created in Borderless with ID: {CustomerId}", response.Id);
        return response.Id;
    }

    public async Task UpdateCustomerAsync(string providerCustomerId, UpdateCustomerRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Updating customer in Borderless: {CustomerId}", providerCustomerId);

        await EnsureAuthenticatedAsync(cancellationToken);

        var borderlessRequest = MapToBorderlessUpdateCustomer(request);
        await PatchAsync<BorderlessCustomerCreateResponse>($"customers/{providerCustomerId}", borderlessRequest, cancellationToken);

        _logger.LogInformation("Customer updated in Borderless: {CustomerId}", providerCustomerId);
    }

    public async Task<Customer> GetCustomerAsync(string providerCustomerId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting customer from Borderless: {CustomerId}", providerCustomerId);

        await EnsureAuthenticatedAsync(cancellationToken);

        var response = await GetAsync<BorderlessCustomerDetailResponse>($"customers/{providerCustomerId}", cancellationToken);
        return MapToCustomer(response);
    }

    public async Task<string> AddBankAccountAsync(string providerCustomerId, BankAccount bankAccount, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Adding bank account in Borderless: {CustomerId}", providerCustomerId);

        await EnsureAuthenticatedAsync(cancellationToken);

        var borderlessRequest = new BorderlessBankAccount
        {
            BankName = bankAccount.BankName,
            AccountNumber = bankAccount.AccountNumber,
            AccountName = bankAccount.AccountHolderName,
            RoutingNumber = bankAccount.RoutingNumber,
            SwiftCode = bankAccount.SwiftCode,
            SortCode = bankAccount.SortCode,
            Iban = bankAccount.Iban,
            Currency = bankAccount.Currency.ToString(),
            Country = bankAccount.CountryCode,
            BranchCode = bankAccount.BranchCode
        };

        var response = await PostAsync<BorderlessBankAccountResponse>(
            $"customers/{providerCustomerId}/bank-accounts",
            borderlessRequest,
            cancellationToken);

        _logger.LogInformation("Bank account added in Borderless: {BankAccountId}", response.Id);
        return response.Id;
    }

    public async Task<VerificationInitiationResult> InitiateKycAsync(string providerCustomerId, InitiateKycRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Initiating KYC in Borderless for customer: {CustomerId}", providerCustomerId);

        await EnsureAuthenticatedAsync(cancellationToken);

        var borderlessRequest = new BorderlessKycRequest
        {
            CustomerId = providerCustomerId,
            VerificationLevel = MapVerificationLevel(request.TargetLevel),
            RedirectUrl = request.RedirectUrl,
            CallbackUrl = request.WebhookUrl
        };

        var response = await PostAsync<BorderlessKycResponse>("kyc/sessions", borderlessRequest, cancellationToken);

        _logger.LogInformation("KYC initiated in Borderless: {SessionId}", response.SessionId);

        return VerificationInitiationResult.Succeeded(
            response.SessionId,
            response.VerificationUrl,
            response.ExpiresAt);
    }

    public async Task<VerificationInitiationResult> InitiateKybAsync(string providerCustomerId, InitiateKybRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Initiating KYB in Borderless for customer: {CustomerId}", providerCustomerId);

        await EnsureAuthenticatedAsync(cancellationToken);

        var borderlessRequest = new BorderlessKybRequest
        {
            CustomerId = providerCustomerId,
            VerificationLevel = MapVerificationLevel(request.TargetLevel),
            RedirectUrl = request.RedirectUrl,
            CallbackUrl = request.WebhookUrl
        };

        var response = await PostAsync<BorderlessKybResponse>("kyb/sessions", borderlessRequest, cancellationToken);

        _logger.LogInformation("KYB initiated in Borderless: {SessionId}", response.SessionId);

        return VerificationInitiationResult.Succeeded(
            response.SessionId,
            response.VerificationUrl,
            response.ExpiresAt);
    }

    public async Task<VerificationStatusResult> GetVerificationStatusAsync(string providerCustomerId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting verification status from Borderless: {CustomerId}", providerCustomerId);

        await EnsureAuthenticatedAsync(cancellationToken);

        var response = await GetAsync<BorderlessVerificationStatusResponse>(
            $"customers/{providerCustomerId}/verification",
            cancellationToken);

        var verification = MapToVerificationInfo(response);
        return VerificationStatusResult.Succeeded(providerCustomerId, verification);
    }

    public async Task<DocumentUploadResult> UploadDocumentAsync(string providerCustomerId, UploadDocumentRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Uploading document in Borderless: {CustomerId}, Type: {DocumentType}", providerCustomerId, request.DocumentType);

        await EnsureAuthenticatedAsync(cancellationToken);

        var borderlessRequest = new BorderlessDocumentUploadRequest
        {
            Type = MapDocumentType(request.DocumentType),
            Number = request.DocumentNumber,
            IssuingCountry = request.IssuingCountry,
            IssueDate = request.IssueDate?.ToString("yyyy-MM-dd"),
            ExpiryDate = request.ExpiryDate?.ToString("yyyy-MM-dd"),
            FrontImage = request.FrontImageBase64,
            BackImage = request.BackImageBase64,
            ContentType = request.MimeType
        };

        var response = await PostAsync<BorderlessDocumentResponse>(
            $"customers/{providerCustomerId}/documents",
            borderlessRequest,
            cancellationToken);

        _logger.LogInformation("Document uploaded in Borderless: {DocumentId}", response.DocumentId);
        return DocumentUploadResult.Succeeded(response.DocumentId);
    }

    public async Task<VerificationStatusResult> SubmitVerificationAsync(string providerCustomerId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Submitting verification in Borderless: {CustomerId}", providerCustomerId);

        await EnsureAuthenticatedAsync(cancellationToken);

        var response = await PostAsync<BorderlessVerificationStatusResponse>(
            $"customers/{providerCustomerId}/verification/submit",
            new { },
            cancellationToken);

        var verification = MapToVerificationInfo(response);
        return VerificationStatusResult.Succeeded(providerCustomerId, verification);
    }

    #region Private Helper Methods

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

    private static BorderlessCreateCustomerRequest MapToBorderlessCreateCustomer(CreateCustomerRequest request)
    {
        return new BorderlessCreateCustomerRequest
        {
            Type = request.Type == CustomerType.Individual ? "individual" : "business",
            Role = request.Role == CustomerRole.Beneficiary ? "beneficiary" : "sender",
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
    }

    private static BorderlessUpdateCustomerRequest MapToBorderlessUpdateCustomer(UpdateCustomerRequest request)
    {
        return new BorderlessUpdateCustomerRequest
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
    }

    private static Customer MapToCustomer(BorderlessCustomerDetailResponse response)
    {
        var customerType = response.Type?.ToLowerInvariant() == "business"
            ? CustomerType.Business
            : CustomerType.Individual;

        var customerRole = response.Role?.ToLowerInvariant() switch
        {
            "sender" => CustomerRole.Sender,
            "beneficiary" => CustomerRole.Beneficiary,
            _ => CustomerRole.Both
        };

        return new Customer
        {
            Id = response.Id,
            Type = customerType,
            Role = customerRole,
            Status = MapCustomerStatus(response.Status),
            Individual = customerType == CustomerType.Individual ? new IndividualDetails
            {
                FirstName = response.FirstName ?? string.Empty,
                LastName = response.LastName ?? string.Empty
            } : null,
            Business = customerType == CustomerType.Business ? new BusinessDetails
            {
                LegalName = response.CompanyName ?? string.Empty,
                CountryOfIncorporation = response.Address?.Country ?? "US"
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
            KycCompleted = response.KycStatus == "approved",
            KybCompleted = response.KybStatus == "approved",
            RiskScore = response.RiskScore,
            RiskLevel = response.RiskLevel,
            RejectionReason = response.RejectionReason,
            SubmittedAt = response.SubmittedAt,
            CompletedAt = response.CompletedAt,
            ExpiresAt = response.ExpiresAt
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
        "in_review" or "processing" => VerificationStatus.InReview,
        "additional_info_required" or "needs_info" => VerificationStatus.AdditionalInfoRequired,
        "approved" or "verified" or "completed" => VerificationStatus.Approved,
        "rejected" or "failed" or "denied" => VerificationStatus.Rejected,
        "expired" => VerificationStatus.Expired,
        _ => VerificationStatus.NotStarted
    };

    private static string MapVerificationLevel(VerificationLevel level) => level switch
    {
        VerificationLevel.Basic => "tier1",
        VerificationLevel.Standard => "tier2",
        VerificationLevel.Enhanced => "tier3",
        VerificationLevel.Full => "tier4",
        _ => "tier2"
    };

    private static VerificationLevel MapVerificationLevelFromBorderless(string? level) => level?.ToLowerInvariant() switch
    {
        "tier1" or "basic" => VerificationLevel.Basic,
        "tier2" or "standard" => VerificationLevel.Standard,
        "tier3" or "enhanced" => VerificationLevel.Enhanced,
        "tier4" or "full" => VerificationLevel.Full,
        _ => VerificationLevel.None
    };

    private static string MapDocumentType(DocumentType type) => type switch
    {
        DocumentType.Passport => "passport",
        DocumentType.NationalId => "national_id",
        DocumentType.DriversLicense => "driving_license",
        DocumentType.UtilityBill => "utility_bill",
        DocumentType.BankStatement => "bank_statement",
        DocumentType.CertificateOfIncorporation => "incorporation_certificate",
        DocumentType.BusinessRegistration => "business_registration",
        DocumentType.ArticlesOfAssociation => "articles_of_association",
        DocumentType.ShareholderRegister => "shareholder_registry",
        DocumentType.UboDeclaration => "ubo_declaration",
        _ => "other"
    };

    #endregion
}

#region Borderless DTOs for Customer/KYC/KYB

internal sealed class BorderlessCreateCustomerRequest
{
    public string? Type { get; init; }
    public string? Role { get; init; }
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public string? CompanyName { get; init; }
    public string? Email { get; init; }
    public string? Phone { get; init; }
    public string? DateOfBirth { get; init; }
    public string? Nationality { get; init; }
    public BorderlessAddress? Address { get; init; }
    public string? ExternalId { get; init; }
    public Dictionary<string, string>? Metadata { get; init; }
}

internal sealed class BorderlessUpdateCustomerRequest
{
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public string? CompanyName { get; init; }
    public string? Email { get; init; }
    public string? Phone { get; init; }
    public BorderlessAddress? Address { get; init; }
    public Dictionary<string, string>? Metadata { get; init; }
}

internal sealed class BorderlessCustomerCreateResponse
{
    public required string Id { get; init; }
    public string? Status { get; init; }
}

internal sealed class BorderlessCustomerDetailResponse
{
    public required string Id { get; init; }
    public string? Type { get; init; }
    public string? Role { get; init; }
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public string? CompanyName { get; init; }
    public required string Email { get; init; }
    public string? Phone { get; init; }
    public string? Status { get; init; }
    public BorderlessAddress? Address { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
}

internal sealed class BorderlessBankAccountResponse
{
    public required string Id { get; init; }
    public string? Status { get; init; }
}

internal sealed class BorderlessKycRequest
{
    public required string CustomerId { get; init; }
    public string? VerificationLevel { get; init; }
    public string? RedirectUrl { get; init; }
    public string? CallbackUrl { get; init; }
}

internal sealed class BorderlessKybRequest
{
    public required string CustomerId { get; init; }
    public string? VerificationLevel { get; init; }
    public string? RedirectUrl { get; init; }
    public string? CallbackUrl { get; init; }
}

internal sealed class BorderlessKycResponse
{
    public required string SessionId { get; init; }
    public required string VerificationUrl { get; init; }
    public DateTimeOffset? ExpiresAt { get; init; }
}

internal sealed class BorderlessKybResponse
{
    public required string SessionId { get; init; }
    public required string VerificationUrl { get; init; }
    public DateTimeOffset? ExpiresAt { get; init; }
}

internal sealed class BorderlessVerificationStatusResponse
{
    public string? Status { get; init; }
    public string? Level { get; init; }
    public string? KycStatus { get; init; }
    public string? KybStatus { get; init; }
    public int? RiskScore { get; init; }
    public string? RiskLevel { get; init; }
    public string? RejectionReason { get; init; }
    public DateTimeOffset? SubmittedAt { get; init; }
    public DateTimeOffset? CompletedAt { get; init; }
    public DateTimeOffset? ExpiresAt { get; init; }
}

internal sealed class BorderlessDocumentUploadRequest
{
    public required string Type { get; init; }
    public string? Number { get; init; }
    public string? IssuingCountry { get; init; }
    public string? IssueDate { get; init; }
    public string? ExpiryDate { get; init; }
    public required string FrontImage { get; init; }
    public string? BackImage { get; init; }
    public required string ContentType { get; init; }
}

internal sealed class BorderlessDocumentResponse
{
    public required string DocumentId { get; init; }
    public string? Status { get; init; }
}

#endregion
