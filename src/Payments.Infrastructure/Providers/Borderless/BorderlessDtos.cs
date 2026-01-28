using System.Text.Json.Serialization;

namespace Payments.Infrastructure.Providers.Borderless;

#region Base Response

/// <summary>
/// Base response wrapper for Borderless API responses.
/// </summary>
/// <typeparam name="T">The data type.</typeparam>
public sealed class BorderlessResponse<T>
{
    [JsonPropertyName("data")]
    public T? Data { get; init; }

    [JsonPropertyName("error")]
    public BorderlessError? Error { get; init; }

    [JsonPropertyName("success")]
    public bool Success { get; init; }

    [JsonPropertyName("requestId")]
    public string? RequestId { get; init; }
}

/// <summary>
/// Borderless API error response.
/// </summary>
public sealed class BorderlessError
{
    [JsonPropertyName("code")]
    public string? Code { get; init; }

    [JsonPropertyName("message")]
    public string? Message { get; init; }

    [JsonPropertyName("field")]
    public string? Field { get; init; }

    [JsonPropertyName("details")]
    public List<BorderlessErrorDetail>? Details { get; init; }
}

/// <summary>
/// Borderless API error detail.
/// </summary>
public sealed class BorderlessErrorDetail
{
    [JsonPropertyName("field")]
    public string? Field { get; init; }

    [JsonPropertyName("message")]
    public string? Message { get; init; }
}

#endregion

#region Authentication

/// <summary>
/// Authentication request.
/// </summary>
public sealed class BorderlessAuthRequest
{
    [JsonPropertyName("clientId")]
    public required string ClientId { get; init; }

    [JsonPropertyName("apiKey")]
    public required string ApiKey { get; init; }

    [JsonPropertyName("apiSecret")]
    public required string ApiSecret { get; init; }
}

/// <summary>
/// Authentication token response.
/// </summary>
public sealed class BorderlessAuthResponse
{
    [JsonPropertyName("accessToken")]
    public required string AccessToken { get; init; }

    [JsonPropertyName("tokenType")]
    public string TokenType { get; init; } = "Bearer";

    [JsonPropertyName("expiresIn")]
    public int ExpiresIn { get; init; }

    [JsonPropertyName("refreshToken")]
    public string? RefreshToken { get; init; }
}

#endregion

#region Customer (Sender/Beneficiary)

/// <summary>
/// Request to create a customer.
/// </summary>
public sealed class BorderlessCreateCustomerRequest
{
    [JsonPropertyName("type")]
    public required string Type { get; init; }

    [JsonPropertyName("role")]
    public required string Role { get; init; }

    [JsonPropertyName("firstName")]
    public string? FirstName { get; init; }

    [JsonPropertyName("lastName")]
    public string? LastName { get; init; }

    [JsonPropertyName("companyName")]
    public string? CompanyName { get; init; }

    [JsonPropertyName("email")]
    public required string Email { get; init; }

    [JsonPropertyName("phone")]
    public string? Phone { get; init; }

    [JsonPropertyName("dateOfBirth")]
    public string? DateOfBirth { get; init; }

    [JsonPropertyName("nationality")]
    public string? Nationality { get; init; }

    [JsonPropertyName("address")]
    public BorderlessAddress? Address { get; init; }

    [JsonPropertyName("identification")]
    public BorderlessIdentification? Identification { get; init; }

    [JsonPropertyName("bankAccount")]
    public BorderlessBankAccount? BankAccount { get; init; }

    [JsonPropertyName("externalId")]
    public string? ExternalId { get; init; }

    [JsonPropertyName("metadata")]
    public Dictionary<string, string>? Metadata { get; init; }
}

/// <summary>
/// Customer response from Borderless API.
/// </summary>
public sealed class BorderlessCustomerResponse
{
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    [JsonPropertyName("type")]
    public required string Type { get; init; }

    [JsonPropertyName("role")]
    public required string Role { get; init; }

    [JsonPropertyName("firstName")]
    public string? FirstName { get; init; }

    [JsonPropertyName("lastName")]
    public string? LastName { get; init; }

    [JsonPropertyName("companyName")]
    public string? CompanyName { get; init; }

    [JsonPropertyName("email")]
    public required string Email { get; init; }

    [JsonPropertyName("status")]
    public string? Status { get; init; }

    [JsonPropertyName("kycStatus")]
    public string? KycStatus { get; init; }

    [JsonPropertyName("createdAt")]
    public DateTimeOffset CreatedAt { get; init; }

    [JsonPropertyName("updatedAt")]
    public DateTimeOffset UpdatedAt { get; init; }
}

#endregion

#region Quote

/// <summary>
/// Request to create a quote.
/// </summary>
public sealed class BorderlessCreateQuoteRequest
{
    [JsonPropertyName("sourceAsset")]
    public required string SourceAsset { get; init; }

    [JsonPropertyName("sourceNetwork")]
    public required string SourceNetwork { get; init; }

    [JsonPropertyName("targetCurrency")]
    public required string TargetCurrency { get; init; }

    [JsonPropertyName("targetCountry")]
    public required string TargetCountry { get; init; }

    [JsonPropertyName("sourceAmount")]
    public decimal? SourceAmount { get; init; }

    [JsonPropertyName("targetAmount")]
    public decimal? TargetAmount { get; init; }

    [JsonPropertyName("paymentRail")]
    public string? PaymentRail { get; init; }

    [JsonPropertyName("settlementSpeed")]
    public string? SettlementSpeed { get; init; }
}

/// <summary>
/// Quote response from Borderless API.
/// </summary>
public sealed class BorderlessQuoteResponse
{
    [JsonPropertyName("quoteId")]
    public required string QuoteId { get; init; }

    [JsonPropertyName("sourceAsset")]
    public required string SourceAsset { get; init; }

    [JsonPropertyName("sourceNetwork")]
    public required string SourceNetwork { get; init; }

    [JsonPropertyName("targetCurrency")]
    public required string TargetCurrency { get; init; }

    [JsonPropertyName("sourceAmount")]
    public required decimal SourceAmount { get; init; }

    [JsonPropertyName("targetAmount")]
    public required decimal TargetAmount { get; init; }

    [JsonPropertyName("exchangeRate")]
    public required decimal ExchangeRate { get; init; }

    [JsonPropertyName("fees")]
    public required BorderlessFees Fees { get; init; }

    [JsonPropertyName("totalFee")]
    public required decimal TotalFee { get; init; }

    [JsonPropertyName("paymentRail")]
    public string? PaymentRail { get; init; }

    [JsonPropertyName("settlementSpeed")]
    public string? SettlementSpeed { get; init; }

    [JsonPropertyName("estimatedDelivery")]
    public string? EstimatedDelivery { get; init; }

    [JsonPropertyName("expiresAt")]
    public required DateTimeOffset ExpiresAt { get; init; }

    [JsonPropertyName("createdAt")]
    public required DateTimeOffset CreatedAt { get; init; }
}

#endregion

#region Offramp (Payout)

/// <summary>
/// Request to create an offramp transaction.
/// </summary>
public sealed class BorderlessCreateOfframpRequest
{
    [JsonPropertyName("quoteId")]
    public required string QuoteId { get; init; }

    [JsonPropertyName("senderId")]
    public required string SenderId { get; init; }

    [JsonPropertyName("beneficiaryId")]
    public required string BeneficiaryId { get; init; }

    [JsonPropertyName("purpose")]
    public string? Purpose { get; init; }

    [JsonPropertyName("reference")]
    public string? Reference { get; init; }

    [JsonPropertyName("externalId")]
    public string? ExternalId { get; init; }

    [JsonPropertyName("callbackUrl")]
    public string? CallbackUrl { get; init; }

    [JsonPropertyName("metadata")]
    public Dictionary<string, string>? Metadata { get; init; }
}

/// <summary>
/// Offramp transaction response from Borderless API.
/// </summary>
public sealed class BorderlessOfframpResponse
{
    [JsonPropertyName("transactionId")]
    public required string TransactionId { get; init; }

    [JsonPropertyName("status")]
    public required string Status { get; init; }

    [JsonPropertyName("senderId")]
    public required string SenderId { get; init; }

    [JsonPropertyName("beneficiaryId")]
    public required string BeneficiaryId { get; init; }

    [JsonPropertyName("quoteId")]
    public required string QuoteId { get; init; }

    [JsonPropertyName("sourceAsset")]
    public required string SourceAsset { get; init; }

    [JsonPropertyName("sourceNetwork")]
    public required string SourceNetwork { get; init; }

    [JsonPropertyName("targetCurrency")]
    public required string TargetCurrency { get; init; }

    [JsonPropertyName("sourceAmount")]
    public required decimal SourceAmount { get; init; }

    [JsonPropertyName("targetAmount")]
    public required decimal TargetAmount { get; init; }

    [JsonPropertyName("exchangeRate")]
    public required decimal ExchangeRate { get; init; }

    [JsonPropertyName("totalFee")]
    public required decimal TotalFee { get; init; }

    [JsonPropertyName("depositAddress")]
    public BorderlessDepositAddress? DepositAddress { get; init; }

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

#region Deposit Address

/// <summary>
/// Deposit address for receiving stablecoins.
/// </summary>
public sealed class BorderlessDepositAddress
{
    [JsonPropertyName("id")]
    public required string Id { get; init; }

    [JsonPropertyName("address")]
    public required string Address { get; init; }

    [JsonPropertyName("network")]
    public required string Network { get; init; }

    [JsonPropertyName("asset")]
    public required string Asset { get; init; }

    [JsonPropertyName("expectedAmount")]
    public required decimal ExpectedAmount { get; init; }

    [JsonPropertyName("expiresAt")]
    public DateTimeOffset? ExpiresAt { get; init; }

    [JsonPropertyName("memo")]
    public string? Memo { get; init; }

    [JsonPropertyName("tag")]
    public string? Tag { get; init; }
}

#endregion

#region Common Types

/// <summary>
/// Address information.
/// </summary>
public sealed class BorderlessAddress
{
    [JsonPropertyName("line1")]
    public required string Line1 { get; init; }

    [JsonPropertyName("line2")]
    public string? Line2 { get; init; }

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
/// Identification document information.
/// </summary>
public sealed class BorderlessIdentification
{
    [JsonPropertyName("type")]
    public required string Type { get; init; }

    [JsonPropertyName("number")]
    public required string Number { get; init; }

    [JsonPropertyName("issuingCountry")]
    public string? IssuingCountry { get; init; }

    [JsonPropertyName("expiryDate")]
    public string? ExpiryDate { get; init; }
}

/// <summary>
/// Bank account information.
/// </summary>
public sealed class BorderlessBankAccount
{
    [JsonPropertyName("bankName")]
    public required string BankName { get; init; }

    [JsonPropertyName("accountNumber")]
    public required string AccountNumber { get; init; }

    [JsonPropertyName("accountName")]
    public required string AccountName { get; init; }

    [JsonPropertyName("accountType")]
    public string? AccountType { get; init; }

    [JsonPropertyName("routingNumber")]
    public string? RoutingNumber { get; init; }

    [JsonPropertyName("swiftCode")]
    public string? SwiftCode { get; init; }

    [JsonPropertyName("sortCode")]
    public string? SortCode { get; init; }

    [JsonPropertyName("iban")]
    public string? Iban { get; init; }

    [JsonPropertyName("bic")]
    public string? Bic { get; init; }

    [JsonPropertyName("currency")]
    public required string Currency { get; init; }

    [JsonPropertyName("country")]
    public required string Country { get; init; }

    [JsonPropertyName("branchCode")]
    public string? BranchCode { get; init; }
}

/// <summary>
/// Fee breakdown from Borderless API.
/// </summary>
public sealed class BorderlessFees
{
    [JsonPropertyName("networkFee")]
    public decimal NetworkFee { get; init; }

    [JsonPropertyName("processingFee")]
    public decimal ProcessingFee { get; init; }

    [JsonPropertyName("fxFee")]
    public decimal FxFee { get; init; }

    [JsonPropertyName("settlementFee")]
    public decimal SettlementFee { get; init; }

    [JsonPropertyName("partnerFee")]
    public decimal PartnerFee { get; init; }
}

#endregion

#region Webhook

/// <summary>
/// Webhook payload from Borderless API.
/// </summary>
public sealed class BorderlessWebhookPayload
{
    [JsonPropertyName("eventType")]
    public required string EventType { get; init; }

    [JsonPropertyName("eventId")]
    public required string EventId { get; init; }

    [JsonPropertyName("data")]
    public required BorderlessOfframpResponse Data { get; init; }

    [JsonPropertyName("timestamp")]
    public required DateTimeOffset Timestamp { get; init; }

    [JsonPropertyName("signature")]
    public string? Signature { get; init; }
}

#endregion

#region Customer Extended

/// <summary>
/// Request to update a customer.
/// </summary>
internal sealed class BorderlessUpdateCustomerRequest
{
    [JsonPropertyName("firstName")]
    public string? FirstName { get; init; }

    [JsonPropertyName("lastName")]
    public string? LastName { get; init; }

    [JsonPropertyName("companyName")]
    public string? CompanyName { get; init; }

    [JsonPropertyName("email")]
    public string? Email { get; init; }

    [JsonPropertyName("phone")]
    public string? Phone { get; init; }

    [JsonPropertyName("address")]
    public BorderlessAddress? Address { get; init; }

    [JsonPropertyName("metadata")]
    public Dictionary<string, string>? Metadata { get; init; }
}

/// <summary>
/// Customer list response from Borderless API.
/// </summary>
internal sealed class BorderlessCustomerListResponse
{
    [JsonPropertyName("items")]
    public IReadOnlyList<BorderlessCustomerResponse> Items { get; init; } = [];

    [JsonPropertyName("total")]
    public int Total { get; init; }

    [JsonPropertyName("page")]
    public int Page { get; init; }

    [JsonPropertyName("limit")]
    public int Limit { get; init; }
}

/// <summary>
/// Offramp list response from Borderless API.
/// </summary>
internal sealed class BorderlessOfframpListResponse
{
    [JsonPropertyName("items")]
    public IReadOnlyList<BorderlessOfframpResponse> Items { get; init; } = [];

    [JsonPropertyName("total")]
    public int Total { get; init; }

    [JsonPropertyName("page")]
    public int Page { get; init; }

    [JsonPropertyName("limit")]
    public int Limit { get; init; }
}

#endregion

#region KYC/KYB

/// <summary>
/// KYC initiation request.
/// </summary>
internal sealed class BorderlessKycRequest
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
internal sealed class BorderlessKybRequest
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
/// KYC session response.
/// </summary>
internal sealed class BorderlessKycResponse
{
    [JsonPropertyName("sessionId")]
    public required string SessionId { get; init; }

    [JsonPropertyName("verificationUrl")]
    public required string VerificationUrl { get; init; }

    [JsonPropertyName("status")]
    public string? Status { get; init; }

    [JsonPropertyName("expiresAt")]
    public DateTimeOffset? ExpiresAt { get; init; }
}

/// <summary>
/// KYB session response.
/// </summary>
internal sealed class BorderlessKybResponse
{
    [JsonPropertyName("sessionId")]
    public required string SessionId { get; init; }

    [JsonPropertyName("verificationUrl")]
    public required string VerificationUrl { get; init; }

    [JsonPropertyName("status")]
    public string? Status { get; init; }

    [JsonPropertyName("expiresAt")]
    public DateTimeOffset? ExpiresAt { get; init; }
}

/// <summary>
/// Verification status response.
/// </summary>
internal sealed class BorderlessVerificationStatusResponse
{
    [JsonPropertyName("status")]
    public string? Status { get; init; }

    [JsonPropertyName("level")]
    public string? Level { get; init; }

    [JsonPropertyName("kycStatus")]
    public string? KycStatus { get; init; }

    [JsonPropertyName("kybStatus")]
    public string? KybStatus { get; init; }

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
internal sealed class BorderlessDocumentUploadRequest
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

    [JsonPropertyName("contentType")]
    public required string ContentType { get; init; }
}

/// <summary>
/// Document response.
/// </summary>
internal sealed class BorderlessDocumentResponse
{
    [JsonPropertyName("documentId")]
    public required string DocumentId { get; init; }

    [JsonPropertyName("documentType")]
    public string? DocumentType { get; init; }

    [JsonPropertyName("status")]
    public string? Status { get; init; }

    [JsonPropertyName("createdAt")]
    public DateTimeOffset CreatedAt { get; init; }
}

/// <summary>
/// Document list response.
/// </summary>
internal sealed class BorderlessDocumentListResponse
{
    [JsonPropertyName("documents")]
    public IReadOnlyList<BorderlessDocumentResponse> Documents { get; init; } = [];
}

#endregion
