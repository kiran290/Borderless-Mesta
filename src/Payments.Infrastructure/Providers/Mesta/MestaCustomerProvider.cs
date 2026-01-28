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

namespace Payments.Infrastructure.Providers.Mesta;

/// <summary>
/// Mesta customer provider implementation for customer management and KYC/KYB operations.
/// </summary>
public sealed class MestaCustomerProvider : ICustomerProvider
{
    private readonly HttpClient _httpClient;
    private readonly MestaSettings _settings;
    private readonly ILogger<MestaCustomerProvider> _logger;
    private readonly JsonSerializerOptions _jsonOptions;

    public PayoutProvider ProviderId => PayoutProvider.Mesta;

    public MestaCustomerProvider(
        HttpClient httpClient,
        IOptions<MestaSettings> settings,
        ILogger<MestaCustomerProvider> logger)
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
        _httpClient.DefaultRequestHeaders.Add("X-API-Key", _settings.ApiKey);
        _httpClient.DefaultRequestHeaders.Add("X-Merchant-Id", _settings.MerchantId);
        _httpClient.Timeout = TimeSpan.FromSeconds(_settings.TimeoutSeconds);
    }

    public async Task<string> CreateCustomerAsync(CreateCustomerRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating customer in Mesta: {Email}", request.Contact.Email);

        var mestaRequest = MapToMestaCreateCustomer(request);
        var endpoint = request.Role == CustomerRole.Beneficiary ? "beneficiaries" : "senders";
        var response = await PostAsync<MestaCustomerResponse>(endpoint, mestaRequest, cancellationToken);

        _logger.LogInformation("Customer created in Mesta with ID: {CustomerId}", response.Id);
        return response.Id;
    }

    public async Task UpdateCustomerAsync(string providerCustomerId, UpdateCustomerRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Updating customer in Mesta: {CustomerId}", providerCustomerId);

        var mestaRequest = MapToMestaUpdateCustomer(request);
        await PatchAsync<MestaCustomerResponse>($"customers/{providerCustomerId}", mestaRequest, cancellationToken);

        _logger.LogInformation("Customer updated in Mesta: {CustomerId}", providerCustomerId);
    }

    public async Task<Customer> GetCustomerAsync(string providerCustomerId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting customer from Mesta: {CustomerId}", providerCustomerId);

        var response = await GetAsync<MestaCustomerResponse>($"customers/{providerCustomerId}", cancellationToken);
        return MapToCustomer(response);
    }

    public async Task<string> AddBankAccountAsync(string providerCustomerId, BankAccount bankAccount, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Adding bank account in Mesta: {CustomerId}", providerCustomerId);

        var mestaRequest = new MestaPaymentInfo
        {
            BankName = bankAccount.BankName,
            AccountNumber = bankAccount.AccountNumber,
            AccountHolderName = bankAccount.AccountHolderName,
            RoutingNumber = bankAccount.RoutingNumber,
            SwiftCode = bankAccount.SwiftCode,
            SortCode = bankAccount.SortCode,
            Iban = bankAccount.Iban,
            Currency = bankAccount.Currency.ToString(),
            Country = bankAccount.CountryCode,
            BranchCode = bankAccount.BranchCode
        };

        var response = await PostAsync<MestaBankAccountResponse>(
            $"customers/{providerCustomerId}/bank-accounts",
            mestaRequest,
            cancellationToken);

        _logger.LogInformation("Bank account added in Mesta: {BankAccountId}", response.Id);
        return response.Id;
    }

    public async Task<VerificationInitiationResult> InitiateKycAsync(string providerCustomerId, InitiateKycRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Initiating KYC in Mesta for customer: {CustomerId}", providerCustomerId);

        var mestaRequest = new MestaKycRequest
        {
            CustomerId = providerCustomerId,
            Level = MapVerificationLevel(request.TargetLevel),
            RedirectUrl = request.RedirectUrl,
            WebhookUrl = request.WebhookUrl
        };

        var response = await PostAsync<MestaKycResponse>("kyc/initiate", mestaRequest, cancellationToken);

        _logger.LogInformation("KYC initiated in Mesta: {SessionId}", response.SessionId);

        return VerificationInitiationResult.Succeeded(
            response.SessionId,
            response.VerificationUrl,
            response.ExpiresAt);
    }

    public async Task<VerificationInitiationResult> InitiateKybAsync(string providerCustomerId, InitiateKybRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Initiating KYB in Mesta for customer: {CustomerId}", providerCustomerId);

        var mestaRequest = new MestaKybRequest
        {
            CustomerId = providerCustomerId,
            Level = MapVerificationLevel(request.TargetLevel),
            RedirectUrl = request.RedirectUrl,
            WebhookUrl = request.WebhookUrl
        };

        var response = await PostAsync<MestaKybResponse>("kyb/initiate", mestaRequest, cancellationToken);

        _logger.LogInformation("KYB initiated in Mesta: {SessionId}", response.SessionId);

        return VerificationInitiationResult.Succeeded(
            response.SessionId,
            response.VerificationUrl,
            response.ExpiresAt);
    }

    public async Task<VerificationStatusResult> GetVerificationStatusAsync(string providerCustomerId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting verification status from Mesta: {CustomerId}", providerCustomerId);

        var response = await GetAsync<MestaVerificationStatusResponse>(
            $"customers/{providerCustomerId}/verification",
            cancellationToken);

        var verification = MapToVerificationInfo(response);
        return VerificationStatusResult.Succeeded(providerCustomerId, verification);
    }

    public async Task<DocumentUploadResult> UploadDocumentAsync(string providerCustomerId, UploadDocumentRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Uploading document in Mesta: {CustomerId}, Type: {DocumentType}", providerCustomerId, request.DocumentType);

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
            $"customers/{providerCustomerId}/documents",
            mestaRequest,
            cancellationToken);

        _logger.LogInformation("Document uploaded in Mesta: {DocumentId}", response.Id);
        return DocumentUploadResult.Succeeded(response.Id);
    }

    public async Task<VerificationStatusResult> SubmitVerificationAsync(string providerCustomerId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Submitting verification in Mesta: {CustomerId}", providerCustomerId);

        var response = await PostAsync<MestaVerificationStatusResponse>(
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

    private static MestaCreateCustomerRequest MapToMestaCreateCustomer(CreateCustomerRequest request)
    {
        return new MestaCreateCustomerRequest
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
    }

    private static MestaUpdateCustomerRequest MapToMestaUpdateCustomer(UpdateCustomerRequest request)
    {
        return new MestaUpdateCustomerRequest
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
    }

    private static Customer MapToCustomer(MestaCustomerResponse response)
    {
        var customerType = response.Type?.ToLowerInvariant() == "business"
            ? CustomerType.Business
            : CustomerType.Individual;

        return new Customer
        {
            Id = response.Id,
            Type = customerType,
            Role = CustomerRole.Both,
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

    #endregion
}

#region Mesta DTOs for Customer/KYC/KYB

internal sealed class MestaCreateCustomerRequest
{
    public string? Type { get; init; }
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public string? BusinessName { get; init; }
    public string? Email { get; init; }
    public string? Phone { get; init; }
    public string? DateOfBirth { get; init; }
    public MestaAddress? Address { get; init; }
    public string? ExternalId { get; init; }
    public Dictionary<string, string>? Metadata { get; init; }
}

internal sealed class MestaUpdateCustomerRequest
{
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public string? BusinessName { get; init; }
    public string? Email { get; init; }
    public string? Phone { get; init; }
    public MestaAddress? Address { get; init; }
    public Dictionary<string, string>? Metadata { get; init; }
}

internal sealed class MestaCustomerResponse
{
    public required string Id { get; init; }
    public string? Type { get; init; }
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public string? BusinessName { get; init; }
    public required string Email { get; init; }
    public string? Phone { get; init; }
    public string? Status { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; init; }
}

internal sealed class MestaBankAccountResponse
{
    public required string Id { get; init; }
    public string? Status { get; init; }
}

internal sealed class MestaKycRequest
{
    public required string CustomerId { get; init; }
    public string? Level { get; init; }
    public string? RedirectUrl { get; init; }
    public string? WebhookUrl { get; init; }
}

internal sealed class MestaKybRequest
{
    public required string CustomerId { get; init; }
    public string? Level { get; init; }
    public string? RedirectUrl { get; init; }
    public string? WebhookUrl { get; init; }
}

internal sealed class MestaKycResponse
{
    public required string SessionId { get; init; }
    public required string VerificationUrl { get; init; }
    public DateTimeOffset? ExpiresAt { get; init; }
}

internal sealed class MestaKybResponse
{
    public required string SessionId { get; init; }
    public required string VerificationUrl { get; init; }
    public DateTimeOffset? ExpiresAt { get; init; }
}

internal sealed class MestaVerificationStatusResponse
{
    public string? Status { get; init; }
    public string? Level { get; init; }
    public bool KycCompleted { get; init; }
    public bool KybCompleted { get; init; }
    public string? RejectionReason { get; init; }
    public DateTimeOffset? SubmittedAt { get; init; }
    public DateTimeOffset? CompletedAt { get; init; }
    public DateTimeOffset? ExpiresAt { get; init; }
}

internal sealed class MestaDocumentUploadRequest
{
    public required string DocumentType { get; init; }
    public string? DocumentNumber { get; init; }
    public string? IssuingCountry { get; init; }
    public string? IssueDate { get; init; }
    public string? ExpiryDate { get; init; }
    public required string FrontImage { get; init; }
    public string? BackImage { get; init; }
    public required string MimeType { get; init; }
}

internal sealed class MestaDocumentResponse
{
    public required string Id { get; init; }
    public string? Status { get; init; }
}

#endregion
