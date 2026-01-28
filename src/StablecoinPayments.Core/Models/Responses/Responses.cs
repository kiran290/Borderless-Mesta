using StablecoinPayments.Core.Enums;
using StablecoinPayments.Core.Models.Requests;

namespace StablecoinPayments.Core.Models.Responses;

#region Base Response

public abstract class BaseResponse
{
    public bool Success { get; set; }
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }
    public PaymentProvider Provider { get; set; }
}

#endregion

#region Health Check

public sealed class HealthCheckResult
{
    public required bool IsHealthy { get; set; }
    public required string Status { get; set; }
    public string? Message { get; set; }
    public DateTime CheckedAt { get; set; } = DateTime.UtcNow;
    public TimeSpan? Latency { get; set; }
}

#endregion

#region Customer Responses

public sealed class CustomerResponse : BaseResponse
{
    public CustomerData? Customer { get; set; }
    public string? ProviderCustomerId { get; set; }
}

public sealed class CustomerListResponse : BaseResponse
{
    public IReadOnlyList<CustomerData> Customers { get; set; } = [];
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}

public sealed class CustomerData
{
    public required string Id { get; set; }
    public string? ExternalId { get; set; }
    public CustomerType Type { get; set; }
    public CustomerStatus Status { get; set; }
    public IndividualInfo? Individual { get; set; }
    public BusinessInfo? Business { get; set; }
    public ContactInfo? Contact { get; set; }
    public AddressInfo? Address { get; set; }
    public VerificationStatus VerificationStatus { get; set; }
    public VerificationLevel VerificationLevel { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

#endregion

#region Verification Responses

public sealed class VerificationResponse : BaseResponse
{
    public string? CustomerId { get; set; }
    public string? SessionId { get; set; }
    public string? VerificationUrl { get; set; }
    public VerificationStatus Status { get; set; }
    public VerificationLevel Level { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }
    public IReadOnlyList<string>? RequiredDocuments { get; set; }
    public string? RejectionReason { get; set; }
}

public sealed class DocumentResponse : BaseResponse
{
    public string? DocumentId { get; set; }
    public DocumentType DocumentType { get; set; }
    public string? Status { get; set; }
    public DateTimeOffset UploadedAt { get; set; }
}

public sealed class DocumentListResponse : BaseResponse
{
    public IReadOnlyList<DocumentData> Documents { get; set; } = [];
}

public sealed class DocumentData
{
    public required string Id { get; set; }
    public DocumentType Type { get; set; }
    public required string Status { get; set; }
    public DateTimeOffset UploadedAt { get; set; }
    public DateTimeOffset? ReviewedAt { get; set; }
}

#endregion

#region Quote Responses

public sealed class QuoteResponse : BaseResponse
{
    public QuoteData? Quote { get; set; }
}

public sealed class QuoteData
{
    public required string Id { get; set; }
    public string? ProviderQuoteId { get; set; }
    public Stablecoin SourceCurrency { get; set; }
    public FiatCurrency TargetCurrency { get; set; }
    public decimal SourceAmount { get; set; }
    public decimal TargetAmount { get; set; }
    public decimal ExchangeRate { get; set; }
    public decimal FeeAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public BlockchainNetwork Network { get; set; }
    public PaymentProvider Provider { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}

#endregion

#region Payout Responses

public sealed class PayoutResponse : BaseResponse
{
    public PayoutData? Payout { get; set; }
}

public sealed class PayoutStatusResponse : BaseResponse
{
    public string? PayoutId { get; set; }
    public string? ProviderPayoutId { get; set; }
    public PayoutStatus Status { get; set; }
    public string? ProviderStatus { get; set; }
    public string? BlockchainTxHash { get; set; }
    public string? BankReference { get; set; }
    public string? FailureReason { get; set; }
    public DateTimeOffset Timestamp { get; set; }
}

public sealed class PayoutListResponse : BaseResponse
{
    public IReadOnlyList<PayoutData> Payouts { get; set; } = [];
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}

public sealed class PayoutData
{
    public required string Id { get; set; }
    public string? ExternalId { get; set; }
    public string? ProviderPayoutId { get; set; }
    public PayoutStatus Status { get; set; }
    public Stablecoin SourceCurrency { get; set; }
    public FiatCurrency TargetCurrency { get; set; }
    public decimal SourceAmount { get; set; }
    public decimal TargetAmount { get; set; }
    public decimal ExchangeRate { get; set; }
    public decimal FeeAmount { get; set; }
    public BlockchainNetwork Network { get; set; }
    public PaymentProvider Provider { get; set; }
    public DepositWalletData? DepositWallet { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}

public sealed class DepositWalletData
{
    public required string Address { get; set; }
    public BlockchainNetwork Network { get; set; }
    public Stablecoin Currency { get; set; }
    public decimal ExpectedAmount { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }
    public string? Memo { get; set; }
}

#endregion

#region Webhook Response

public sealed class WebhookResponse : BaseResponse
{
    public required string EventType { get; set; }
    public string? ResourceId { get; set; }
    public string? ResourceType { get; set; }
    public object? Data { get; set; }
}

#endregion
