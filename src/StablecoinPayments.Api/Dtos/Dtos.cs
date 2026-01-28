using StablecoinPayments.Core.Enums;

namespace StablecoinPayments.Api.Dtos;

#region API Response

public sealed class ApiResponse<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }

    public static ApiResponse<T> Ok(T data) => new() { Success = true, Data = data };
    public static ApiResponse<T> Error(string code, string message) => new() { Success = false, ErrorCode = code, ErrorMessage = message };
}

#endregion

#region Health

public sealed class HealthDto
{
    public bool IsHealthy { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Message { get; set; }
    public double? LatencyMs { get; set; }
}

#endregion

#region Customer DTOs

public sealed class CreateCustomerDto
{
    public string? ExternalId { get; set; }
    public required CustomerType Type { get; set; }
    public IndividualDto? Individual { get; set; }
    public BusinessDto? Business { get; set; }
    public required ContactDto Contact { get; set; }
    public AddressDto? Address { get; set; }
    public Dictionary<string, string>? Metadata { get; set; }
}

public sealed class UpdateCustomerDto
{
    public IndividualDto? Individual { get; set; }
    public BusinessDto? Business { get; set; }
    public ContactDto? Contact { get; set; }
    public AddressDto? Address { get; set; }
    public Dictionary<string, string>? Metadata { get; set; }
}

public sealed class IndividualDto
{
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public string? MiddleName { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public string? Nationality { get; set; }
}

public sealed class BusinessDto
{
    public required string LegalName { get; set; }
    public string? TradingName { get; set; }
    public string? RegistrationNumber { get; set; }
    public string? TaxId { get; set; }
    public string? CountryOfIncorporation { get; set; }
}

public sealed class ContactDto
{
    public required string Email { get; set; }
    public string? Phone { get; set; }
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

public sealed class CustomerDto
{
    public string Id { get; set; } = string.Empty;
    public string? ExternalId { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? VerificationStatus { get; set; }
    public string? VerificationLevel { get; set; }
    public string? Provider { get; set; }
    public string? ProviderCustomerId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public sealed class CustomerListDto
{
    public IReadOnlyList<CustomerDto> Customers { get; set; } = [];
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}

#endregion

#region Verification DTOs

public sealed class InitiateKycDto
{
    public required string CustomerId { get; set; }
    public VerificationLevel? TargetLevel { get; set; }
    public string? RedirectUrl { get; set; }
    public string? WebhookUrl { get; set; }
}

public sealed class InitiateKybDto
{
    public required string CustomerId { get; set; }
    public VerificationLevel? TargetLevel { get; set; }
    public string? RedirectUrl { get; set; }
    public string? WebhookUrl { get; set; }
}

public sealed class VerificationDto
{
    public string? CustomerId { get; set; }
    public string? SessionId { get; set; }
    public string? VerificationUrl { get; set; }
    public string? Status { get; set; }
    public string? Level { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }
    public string? RejectionReason { get; set; }
    public string? Provider { get; set; }
}

#endregion

#region Document DTOs

public sealed class UploadDocumentDto
{
    public required string CustomerId { get; set; }
    public required DocumentType DocumentType { get; set; }
    public string? DocumentNumber { get; set; }
    public string? IssuingCountry { get; set; }
    public DateOnly? IssueDate { get; set; }
    public DateOnly? ExpiryDate { get; set; }
    public required string FrontImageBase64 { get; set; }
    public string? BackImageBase64 { get; set; }
    public string? MimeType { get; set; }
}

public sealed class DocumentDto
{
    public string? DocumentId { get; set; }
    public string? DocumentType { get; set; }
    public string? Status { get; set; }
    public DateTimeOffset? UploadedAt { get; set; }
    public string? Provider { get; set; }
}

public sealed class DocumentListDto
{
    public IReadOnlyList<DocumentDto> Documents { get; set; } = [];
    public string? Provider { get; set; }
}

#endregion

#region Quote DTOs

public sealed class CreateQuoteDto
{
    public required Stablecoin SourceCurrency { get; set; }
    public required FiatCurrency TargetCurrency { get; set; }
    public decimal? SourceAmount { get; set; }
    public decimal? TargetAmount { get; set; }
    public required BlockchainNetwork Network { get; set; }
    public required string DestinationCountry { get; set; }
    public PaymentMethod? PaymentMethod { get; set; }
}

public sealed class QuoteDto
{
    public string Id { get; set; } = string.Empty;
    public string? ProviderQuoteId { get; set; }
    public string SourceCurrency { get; set; } = string.Empty;
    public string TargetCurrency { get; set; } = string.Empty;
    public decimal SourceAmount { get; set; }
    public decimal TargetAmount { get; set; }
    public decimal ExchangeRate { get; set; }
    public decimal FeeAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public string Network { get; set; } = string.Empty;
    public string Provider { get; set; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

#endregion

#region Payout DTOs

public sealed class CreatePayoutDto
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
    public string? Purpose { get; set; }
    public string? Reference { get; set; }
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
    public string? Phone { get; set; }
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
    public string? Phone { get; set; }
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

public sealed class PayoutDto
{
    public string Id { get; set; } = string.Empty;
    public string? ExternalId { get; set; }
    public string? ProviderPayoutId { get; set; }
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

public sealed class PayoutStatusDto
{
    public string? PayoutId { get; set; }
    public string? ProviderPayoutId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? ProviderStatus { get; set; }
    public string? BlockchainTxHash { get; set; }
    public string? BankReference { get; set; }
    public string? FailureReason { get; set; }
    public DateTimeOffset Timestamp { get; set; }
    public string Provider { get; set; } = string.Empty;
}

#endregion
