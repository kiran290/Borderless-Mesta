using System.Text.Json.Serialization;

namespace Payments.Infrastructure.Providers.Mesta;

#region Base Response

/// <summary>
/// Base response wrapper for Mesta API responses.
/// </summary>
/// <typeparam name="T">The data type.</typeparam>
public sealed class MestaResponse<T>
{
    [JsonPropertyName("data")]
    public T? Data { get; init; }

    [JsonPropertyName("error")]
    public MestaError? Error { get; init; }

    [JsonPropertyName("success")]
    public bool Success { get; init; }
}

/// <summary>
/// Mesta API error response.
/// </summary>
public sealed class MestaError
{
    [JsonPropertyName("code")]
    public string? Code { get; init; }

    [JsonPropertyName("message")]
    public string? Message { get; init; }

    [JsonPropertyName("details")]
    public Dictionary<string, string[]>? Details { get; init; }
}

#endregion

#region Authentication

/// <summary>
/// Authentication token response.
/// </summary>
public sealed class MestaAuthResponse
{
    [JsonPropertyName("access_token")]
    public required string AccessToken { get; init; }

    [JsonPropertyName("token_type")]
    public string TokenType { get; init; } = "Bearer";

    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; init; }
}

#endregion

#region Sender

/// <summary>
/// Request to create a sender.
/// </summary>
public sealed class MestaCreateSenderRequest
{
    [JsonPropertyName("type")]
    public required string Type { get; init; }

    [JsonPropertyName("firstName")]
    public string? FirstName { get; init; }

    [JsonPropertyName("lastName")]
    public string? LastName { get; init; }

    [JsonPropertyName("businessName")]
    public string? BusinessName { get; init; }

    [JsonPropertyName("email")]
    public required string Email { get; init; }

    [JsonPropertyName("phone")]
    public string? Phone { get; init; }

    [JsonPropertyName("dateOfBirth")]
    public string? DateOfBirth { get; init; }

    [JsonPropertyName("identity")]
    public MestaIdentity? Identity { get; init; }

    [JsonPropertyName("address")]
    public MestaAddress? Address { get; init; }

    [JsonPropertyName("externalId")]
    public string? ExternalId { get; init; }
}

/// <summary>
/// Sender response from Mesta API.
/// </summary>
public sealed class MestaSenderResponse
{
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    [JsonPropertyName("type")]
    public required string Type { get; init; }

    [JsonPropertyName("firstName")]
    public string? FirstName { get; init; }

    [JsonPropertyName("lastName")]
    public string? LastName { get; init; }

    [JsonPropertyName("businessName")]
    public string? BusinessName { get; init; }

    [JsonPropertyName("email")]
    public required string Email { get; init; }

    [JsonPropertyName("phone")]
    public string? Phone { get; init; }

    [JsonPropertyName("status")]
    public string? Status { get; init; }

    [JsonPropertyName("createdAt")]
    public DateTimeOffset CreatedAt { get; init; }
}

#endregion

#region Beneficiary

/// <summary>
/// Request to create a beneficiary.
/// </summary>
public sealed class MestaCreateBeneficiaryRequest
{
    [JsonPropertyName("type")]
    public required string Type { get; init; }

    [JsonPropertyName("firstName")]
    public string? FirstName { get; init; }

    [JsonPropertyName("lastName")]
    public string? LastName { get; init; }

    [JsonPropertyName("businessName")]
    public string? BusinessName { get; init; }

    [JsonPropertyName("email")]
    public required string Email { get; init; }

    [JsonPropertyName("phone")]
    public string? Phone { get; init; }

    [JsonPropertyName("dateOfBirth")]
    public string? DateOfBirth { get; init; }

    [JsonPropertyName("identity")]
    public MestaIdentity? Identity { get; init; }

    [JsonPropertyName("address")]
    public MestaAddress? Address { get; init; }

    [JsonPropertyName("paymentInfo")]
    public required MestaPaymentInfo PaymentInfo { get; init; }

    [JsonPropertyName("externalId")]
    public string? ExternalId { get; init; }
}

/// <summary>
/// Beneficiary response from Mesta API.
/// </summary>
public sealed class MestaBeneficiaryResponse
{
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    [JsonPropertyName("type")]
    public required string Type { get; init; }

    [JsonPropertyName("firstName")]
    public string? FirstName { get; init; }

    [JsonPropertyName("lastName")]
    public string? LastName { get; init; }

    [JsonPropertyName("businessName")]
    public string? BusinessName { get; init; }

    [JsonPropertyName("email")]
    public required string Email { get; init; }

    [JsonPropertyName("status")]
    public string? Status { get; init; }

    [JsonPropertyName("createdAt")]
    public DateTimeOffset CreatedAt { get; init; }
}

#endregion

#region Quote

/// <summary>
/// Request to create a quote.
/// </summary>
public sealed class MestaCreateQuoteRequest
{
    [JsonPropertyName("sourceCurrency")]
    public required string SourceCurrency { get; init; }

    [JsonPropertyName("targetCurrency")]
    public required string TargetCurrency { get; init; }

    [JsonPropertyName("sourceAmount")]
    public decimal? SourceAmount { get; init; }

    [JsonPropertyName("targetAmount")]
    public decimal? TargetAmount { get; init; }

    [JsonPropertyName("chain")]
    public required string Chain { get; init; }

    [JsonPropertyName("paymentMethod")]
    public string? PaymentMethod { get; init; }

    [JsonPropertyName("destinationCountry")]
    public required string DestinationCountry { get; init; }

    [JsonPropertyName("developerFee")]
    public decimal? DeveloperFee { get; init; }
}

/// <summary>
/// Quote response from Mesta API.
/// </summary>
public sealed class MestaQuoteResponse
{
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    [JsonPropertyName("sourceCurrency")]
    public required string SourceCurrency { get; init; }

    [JsonPropertyName("targetCurrency")]
    public required string TargetCurrency { get; init; }

    [JsonPropertyName("sourceAmount")]
    public required decimal SourceAmount { get; init; }

    [JsonPropertyName("targetAmount")]
    public required decimal TargetAmount { get; init; }

    [JsonPropertyName("exchangeRate")]
    public required decimal ExchangeRate { get; init; }

    [JsonPropertyName("feeAmount")]
    public required decimal FeeAmount { get; init; }

    [JsonPropertyName("fees")]
    public MestaFeeBreakdown? Fees { get; init; }

    [JsonPropertyName("chain")]
    public required string Chain { get; init; }

    [JsonPropertyName("expiresAt")]
    public required DateTimeOffset ExpiresAt { get; init; }

    [JsonPropertyName("createdAt")]
    public required DateTimeOffset CreatedAt { get; init; }
}

#endregion

#region Order

/// <summary>
/// Request to create an order.
/// </summary>
public sealed class MestaCreateOrderRequest
{
    [JsonPropertyName("senderId")]
    public required string SenderId { get; init; }

    [JsonPropertyName("beneficiaryId")]
    public required string BeneficiaryId { get; init; }

    [JsonPropertyName("acceptedQuoteId")]
    public required string AcceptedQuoteId { get; init; }

    [JsonPropertyName("externalId")]
    public string? ExternalId { get; init; }

    [JsonPropertyName("metadata")]
    public Dictionary<string, string>? Metadata { get; init; }
}

/// <summary>
/// Order response from Mesta API.
/// </summary>
public sealed class MestaOrderResponse
{
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    [JsonPropertyName("status")]
    public required string Status { get; init; }

    [JsonPropertyName("senderId")]
    public required string SenderId { get; init; }

    [JsonPropertyName("beneficiaryId")]
    public required string BeneficiaryId { get; init; }

    [JsonPropertyName("quoteId")]
    public required string QuoteId { get; init; }

    [JsonPropertyName("sourceCurrency")]
    public required string SourceCurrency { get; init; }

    [JsonPropertyName("targetCurrency")]
    public required string TargetCurrency { get; init; }

    [JsonPropertyName("sourceAmount")]
    public required decimal SourceAmount { get; init; }

    [JsonPropertyName("targetAmount")]
    public required decimal TargetAmount { get; init; }

    [JsonPropertyName("exchangeRate")]
    public required decimal ExchangeRate { get; init; }

    [JsonPropertyName("feeAmount")]
    public required decimal FeeAmount { get; init; }

    [JsonPropertyName("chain")]
    public required string Chain { get; init; }

    [JsonPropertyName("depositWallet")]
    public MestaWalletResponse? DepositWallet { get; init; }

    [JsonPropertyName("blockchainTxHash")]
    public string? BlockchainTxHash { get; init; }

    [JsonPropertyName("bankReference")]
    public string? BankReference { get; init; }

    [JsonPropertyName("failureReason")]
    public string? FailureReason { get; init; }

    [JsonPropertyName("externalId")]
    public string? ExternalId { get; init; }

    [JsonPropertyName("createdAt")]
    public required DateTimeOffset CreatedAt { get; init; }

    [JsonPropertyName("updatedAt")]
    public required DateTimeOffset UpdatedAt { get; init; }

    [JsonPropertyName("completedAt")]
    public DateTimeOffset? CompletedAt { get; init; }
}

#endregion

#region Wallet

/// <summary>
/// Wallet response from Mesta API.
/// </summary>
public sealed class MestaWalletResponse
{
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    [JsonPropertyName("address")]
    public required string Address { get; init; }

    [JsonPropertyName("chain")]
    public required string Chain { get; init; }

    [JsonPropertyName("currency")]
    public required string Currency { get; init; }

    [JsonPropertyName("expectedAmount")]
    public required decimal ExpectedAmount { get; init; }

    [JsonPropertyName("expiresAt")]
    public DateTimeOffset? ExpiresAt { get; init; }

    [JsonPropertyName("memo")]
    public string? Memo { get; init; }
}

#endregion

#region Common Types

/// <summary>
/// Identity document information.
/// </summary>
public sealed class MestaIdentity
{
    [JsonPropertyName("documentType")]
    public string? DocumentType { get; init; }

    [JsonPropertyName("documentNumber")]
    public string? DocumentNumber { get; init; }

    [JsonPropertyName("nationality")]
    public string? Nationality { get; init; }
}

/// <summary>
/// Address information.
/// </summary>
public sealed class MestaAddress
{
    [JsonPropertyName("street1")]
    public required string Street1 { get; init; }

    [JsonPropertyName("street2")]
    public string? Street2 { get; init; }

    [JsonPropertyName("city")]
    public required string City { get; init; }

    [JsonPropertyName("state")]
    public string? State { get; init; }

    [JsonPropertyName("postalCode")]
    public required string PostalCode { get; init; }

    [JsonPropertyName("country")]
    public required string Country { get; init; }
}

/// <summary>
/// Payment information for beneficiary.
/// </summary>
public sealed class MestaPaymentInfo
{
    [JsonPropertyName("bankName")]
    public required string BankName { get; init; }

    [JsonPropertyName("accountNumber")]
    public required string AccountNumber { get; init; }

    [JsonPropertyName("accountHolderName")]
    public required string AccountHolderName { get; init; }

    [JsonPropertyName("routingNumber")]
    public string? RoutingNumber { get; init; }

    [JsonPropertyName("swiftCode")]
    public string? SwiftCode { get; init; }

    [JsonPropertyName("sortCode")]
    public string? SortCode { get; init; }

    [JsonPropertyName("iban")]
    public string? Iban { get; init; }

    [JsonPropertyName("currency")]
    public required string Currency { get; init; }

    [JsonPropertyName("country")]
    public required string Country { get; init; }

    [JsonPropertyName("branchCode")]
    public string? BranchCode { get; init; }

    [JsonPropertyName("paymentMethod")]
    public string? PaymentMethod { get; init; }
}

/// <summary>
/// Fee breakdown from Mesta API.
/// </summary>
public sealed class MestaFeeBreakdown
{
    [JsonPropertyName("networkFee")]
    public decimal NetworkFee { get; init; }

    [JsonPropertyName("processingFee")]
    public decimal ProcessingFee { get; init; }

    [JsonPropertyName("fxSpreadFee")]
    public decimal FxSpreadFee { get; init; }

    [JsonPropertyName("bankFee")]
    public decimal BankFee { get; init; }

    [JsonPropertyName("developerFee")]
    public decimal DeveloperFee { get; init; }
}

#endregion

#region Webhook

/// <summary>
/// Webhook payload from Mesta API.
/// </summary>
public sealed class MestaWebhookPayload
{
    [JsonPropertyName("event")]
    public required string Event { get; init; }

    [JsonPropertyName("data")]
    public required MestaOrderResponse Data { get; init; }

    [JsonPropertyName("timestamp")]
    public required DateTimeOffset Timestamp { get; init; }

    [JsonPropertyName("webhookId")]
    public required string WebhookId { get; init; }
}

#endregion

#region Customer

/// <summary>
/// Request to create a customer.
/// </summary>
internal sealed class MestaCreateCustomerRequest
{
    [JsonPropertyName("type")]
    public string? Type { get; init; }

    [JsonPropertyName("firstName")]
    public string? FirstName { get; init; }

    [JsonPropertyName("lastName")]
    public string? LastName { get; init; }

    [JsonPropertyName("businessName")]
    public string? BusinessName { get; init; }

    [JsonPropertyName("email")]
    public string? Email { get; init; }

    [JsonPropertyName("phone")]
    public string? Phone { get; init; }

    [JsonPropertyName("dateOfBirth")]
    public string? DateOfBirth { get; init; }

    [JsonPropertyName("address")]
    public MestaAddress? Address { get; init; }

    [JsonPropertyName("externalId")]
    public string? ExternalId { get; init; }

    [JsonPropertyName("metadata")]
    public Dictionary<string, string>? Metadata { get; init; }
}

/// <summary>
/// Request to update a customer.
/// </summary>
internal sealed class MestaUpdateCustomerRequest
{
    [JsonPropertyName("firstName")]
    public string? FirstName { get; init; }

    [JsonPropertyName("lastName")]
    public string? LastName { get; init; }

    [JsonPropertyName("businessName")]
    public string? BusinessName { get; init; }

    [JsonPropertyName("email")]
    public string? Email { get; init; }

    [JsonPropertyName("phone")]
    public string? Phone { get; init; }

    [JsonPropertyName("address")]
    public MestaAddress? Address { get; init; }

    [JsonPropertyName("metadata")]
    public Dictionary<string, string>? Metadata { get; init; }
}

/// <summary>
/// Customer response from Mesta API.
/// </summary>
internal sealed class MestaCustomerResponse
{
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    [JsonPropertyName("type")]
    public string? Type { get; init; }

    [JsonPropertyName("firstName")]
    public string? FirstName { get; init; }

    [JsonPropertyName("lastName")]
    public string? LastName { get; init; }

    [JsonPropertyName("businessName")]
    public string? BusinessName { get; init; }

    [JsonPropertyName("email")]
    public required string Email { get; init; }

    [JsonPropertyName("phone")]
    public string? Phone { get; init; }

    [JsonPropertyName("status")]
    public string? Status { get; init; }

    [JsonPropertyName("createdAt")]
    public DateTimeOffset CreatedAt { get; init; }

    [JsonPropertyName("updatedAt")]
    public DateTimeOffset UpdatedAt { get; init; }
}

/// <summary>
/// Customer list response from Mesta API.
/// </summary>
internal sealed class MestaCustomerListResponse
{
    [JsonPropertyName("items")]
    public IReadOnlyList<MestaCustomerResponse> Items { get; init; } = [];

    [JsonPropertyName("totalCount")]
    public int TotalCount { get; init; }

    [JsonPropertyName("page")]
    public int Page { get; init; }

    [JsonPropertyName("pageSize")]
    public int PageSize { get; init; }
}

/// <summary>
/// Order list response from Mesta API.
/// </summary>
internal sealed class MestaOrderListResponse
{
    [JsonPropertyName("items")]
    public IReadOnlyList<MestaOrderResponse> Items { get; init; } = [];

    [JsonPropertyName("totalCount")]
    public int TotalCount { get; init; }

    [JsonPropertyName("page")]
    public int Page { get; init; }

    [JsonPropertyName("pageSize")]
    public int PageSize { get; init; }
}

#endregion

#region KYC/KYB

/// <summary>
/// KYC initiation request.
/// </summary>
internal sealed class MestaKycRequest
{
    [JsonPropertyName("customerId")]
    public required string CustomerId { get; init; }

    [JsonPropertyName("level")]
    public string? Level { get; init; }

    [JsonPropertyName("redirectUrl")]
    public string? RedirectUrl { get; init; }

    [JsonPropertyName("webhookUrl")]
    public string? WebhookUrl { get; init; }
}

/// <summary>
/// KYB initiation request.
/// </summary>
internal sealed class MestaKybRequest
{
    [JsonPropertyName("customerId")]
    public required string CustomerId { get; init; }

    [JsonPropertyName("level")]
    public string? Level { get; init; }

    [JsonPropertyName("redirectUrl")]
    public string? RedirectUrl { get; init; }

    [JsonPropertyName("webhookUrl")]
    public string? WebhookUrl { get; init; }
}

/// <summary>
/// KYC initiation response.
/// </summary>
internal sealed class MestaKycResponse
{
    [JsonPropertyName("sessionId")]
    public required string SessionId { get; init; }

    [JsonPropertyName("verificationUrl")]
    public required string VerificationUrl { get; init; }

    [JsonPropertyName("expiresAt")]
    public DateTimeOffset? ExpiresAt { get; init; }
}

/// <summary>
/// KYB initiation response.
/// </summary>
internal sealed class MestaKybResponse
{
    [JsonPropertyName("sessionId")]
    public required string SessionId { get; init; }

    [JsonPropertyName("verificationUrl")]
    public required string VerificationUrl { get; init; }

    [JsonPropertyName("expiresAt")]
    public DateTimeOffset? ExpiresAt { get; init; }
}

/// <summary>
/// Verification status response.
/// </summary>
internal sealed class MestaVerificationStatusResponse
{
    [JsonPropertyName("status")]
    public string? Status { get; init; }

    [JsonPropertyName("level")]
    public string? Level { get; init; }

    [JsonPropertyName("kycCompleted")]
    public bool KycCompleted { get; init; }

    [JsonPropertyName("kybCompleted")]
    public bool KybCompleted { get; init; }

    [JsonPropertyName("rejectionReason")]
    public string? RejectionReason { get; init; }

    [JsonPropertyName("submittedAt")]
    public DateTimeOffset? SubmittedAt { get; init; }

    [JsonPropertyName("completedAt")]
    public DateTimeOffset? CompletedAt { get; init; }

    [JsonPropertyName("expiresAt")]
    public DateTimeOffset? ExpiresAt { get; init; }
}

/// <summary>
/// Document upload request.
/// </summary>
internal sealed class MestaDocumentUploadRequest
{
    [JsonPropertyName("documentType")]
    public required string DocumentType { get; init; }

    [JsonPropertyName("documentNumber")]
    public string? DocumentNumber { get; init; }

    [JsonPropertyName("issuingCountry")]
    public string? IssuingCountry { get; init; }

    [JsonPropertyName("issueDate")]
    public string? IssueDate { get; init; }

    [JsonPropertyName("expiryDate")]
    public string? ExpiryDate { get; init; }

    [JsonPropertyName("frontImage")]
    public required string FrontImage { get; init; }

    [JsonPropertyName("backImage")]
    public string? BackImage { get; init; }

    [JsonPropertyName("mimeType")]
    public required string MimeType { get; init; }
}

/// <summary>
/// Document response.
/// </summary>
internal sealed class MestaDocumentResponse
{
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    [JsonPropertyName("status")]
    public string? Status { get; init; }

    [JsonPropertyName("documentType")]
    public string? DocumentType { get; init; }

    [JsonPropertyName("uploadedAt")]
    public DateTimeOffset UploadedAt { get; init; }
}

/// <summary>
/// Document list response.
/// </summary>
internal sealed class MestaDocumentListResponse
{
    [JsonPropertyName("documents")]
    public IReadOnlyList<MestaDocumentResponse> Documents { get; init; } = [];
}

#endregion
