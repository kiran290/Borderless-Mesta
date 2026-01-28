using Payments.Core.Enums;
using Payments.Core.Models;
using Payments.Core.Models.Requests;
using Payments.Core.Models.Responses;

namespace Payments.Core.Interfaces;

/// <summary>
/// Unified interface for payment providers supporting customer management,
/// KYC/KYB verification, and stablecoin payouts.
/// </summary>
public interface IPaymentProvider
{
    /// <summary>
    /// Gets the provider identifier.
    /// </summary>
    PayoutProvider ProviderId { get; }

    /// <summary>
    /// Gets the provider name.
    /// </summary>
    string ProviderName { get; }

    /// <summary>
    /// Checks if the provider is available and healthy.
    /// </summary>
    Task<ProviderHealthResult> CheckHealthAsync(CancellationToken cancellationToken = default);

    #region Customer Operations

    /// <summary>
    /// Creates a new customer (individual or business).
    /// </summary>
    Task<CustomerResult> CreateCustomerAsync(
        CreateCustomerRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a customer by ID.
    /// </summary>
    Task<CustomerResult> GetCustomerAsync(
        string customerId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing customer.
    /// </summary>
    Task<CustomerResult> UpdateCustomerAsync(
        string customerId,
        UpdateCustomerRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists customers with optional filters.
    /// </summary>
    Task<CustomerListResult> ListCustomersAsync(
        CustomerListRequest request,
        CancellationToken cancellationToken = default);

    #endregion

    #region KYC Operations (Individual Verification)

    /// <summary>
    /// Initiates KYC verification for an individual customer.
    /// </summary>
    Task<VerificationResult> InitiateKycAsync(
        InitiateKycRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets KYC verification status for a customer.
    /// </summary>
    Task<VerificationResult> GetKycStatusAsync(
        string customerId,
        CancellationToken cancellationToken = default);

    #endregion

    #region KYB Operations (Business Verification)

    /// <summary>
    /// Initiates KYB verification for a business customer.
    /// </summary>
    Task<VerificationResult> InitiateKybAsync(
        InitiateKybRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets KYB verification status for a customer.
    /// </summary>
    Task<VerificationResult> GetKybStatusAsync(
        string customerId,
        CancellationToken cancellationToken = default);

    #endregion

    #region Document Operations

    /// <summary>
    /// Uploads a verification document.
    /// </summary>
    Task<DocumentUploadResult> UploadDocumentAsync(
        UploadDocumentRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets documents for a customer.
    /// </summary>
    Task<DocumentListResult> GetDocumentsAsync(
        string customerId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Submits verification for review.
    /// </summary>
    Task<VerificationResult> SubmitVerificationAsync(
        string customerId,
        CancellationToken cancellationToken = default);

    #endregion

    #region Payout Operations

    /// <summary>
    /// Creates a quote for a stablecoin to fiat payout.
    /// </summary>
    Task<QuoteResult> CreateQuoteAsync(
        CreateQuoteRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a payout from stablecoin to fiat.
    /// </summary>
    Task<PayoutResult> CreatePayoutAsync(
        CreatePayoutRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a payout by ID.
    /// </summary>
    Task<PayoutResult> GetPayoutAsync(
        string payoutId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets payout status.
    /// </summary>
    Task<PayoutStatusResult> GetPayoutStatusAsync(
        string payoutId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancels a payout if possible.
    /// </summary>
    Task<PayoutResult> CancelPayoutAsync(
        string payoutId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists payouts with optional filters.
    /// </summary>
    Task<PayoutListResult> ListPayoutsAsync(
        PayoutListRequest request,
        CancellationToken cancellationToken = default);

    #endregion

    #region Webhook Handling

    /// <summary>
    /// Validates a webhook signature.
    /// </summary>
    bool ValidateWebhookSignature(string payload, string signature, string secret);

    /// <summary>
    /// Processes a webhook payload.
    /// </summary>
    Task<WebhookResult> ProcessWebhookAsync(
        string payload,
        string signature,
        CancellationToken cancellationToken = default);

    #endregion
}

/// <summary>
/// Result of a provider health check.
/// </summary>
public sealed record ProviderHealthResult
{
    public required bool IsHealthy { get; init; }
    public required string Status { get; init; }
    public string? Message { get; init; }
    public DateTime CheckedAt { get; init; } = DateTime.UtcNow;
    public TimeSpan? Latency { get; init; }
}

/// <summary>
/// Result of a webhook processing.
/// </summary>
public sealed record WebhookResult
{
    public required bool Success { get; init; }
    public required string EventType { get; init; }
    public string? ResourceId { get; init; }
    public string? ResourceType { get; init; }
    public object? Data { get; init; }
    public string? Error { get; init; }
}
