using StablecoinPayments.Core.Enums;
using StablecoinPayments.Core.Models.Requests;
using StablecoinPayments.Core.Models.Responses;

namespace StablecoinPayments.Core.Interfaces;

/// <summary>
/// Unified interface for payment providers supporting customer management,
/// KYC/KYB verification, and stablecoin payouts.
/// </summary>
public interface IPaymentProvider
{
    /// <summary>
    /// Gets the provider identifier.
    /// </summary>
    PaymentProvider ProviderId { get; }

    /// <summary>
    /// Gets the provider name.
    /// </summary>
    string ProviderName { get; }

    /// <summary>
    /// Checks if the provider is available and healthy.
    /// </summary>
    Task<HealthCheckResult> CheckHealthAsync(CancellationToken cancellationToken = default);

    #region Customer Operations

    /// <summary>
    /// Creates a new customer (individual or business).
    /// </summary>
    Task<CustomerResponse> CreateCustomerAsync(CreateCustomerRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a customer by ID.
    /// </summary>
    Task<CustomerResponse> GetCustomerAsync(string customerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing customer.
    /// </summary>
    Task<CustomerResponse> UpdateCustomerAsync(string customerId, UpdateCustomerRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists customers with optional filters.
    /// </summary>
    Task<CustomerListResponse> ListCustomersAsync(ListCustomersRequest request, CancellationToken cancellationToken = default);

    #endregion

    #region KYC Operations

    /// <summary>
    /// Initiates KYC verification for an individual customer.
    /// </summary>
    Task<VerificationResponse> InitiateKycAsync(InitiateKycRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets KYC verification status.
    /// </summary>
    Task<VerificationResponse> GetKycStatusAsync(string customerId, CancellationToken cancellationToken = default);

    #endregion

    #region KYB Operations

    /// <summary>
    /// Initiates KYB verification for a business customer.
    /// </summary>
    Task<VerificationResponse> InitiateKybAsync(InitiateKybRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets KYB verification status.
    /// </summary>
    Task<VerificationResponse> GetKybStatusAsync(string customerId, CancellationToken cancellationToken = default);

    #endregion

    #region Document Operations

    /// <summary>
    /// Uploads a verification document.
    /// </summary>
    Task<DocumentResponse> UploadDocumentAsync(UploadDocumentRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets documents for a customer.
    /// </summary>
    Task<DocumentListResponse> GetDocumentsAsync(string customerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Submits verification for review.
    /// </summary>
    Task<VerificationResponse> SubmitVerificationAsync(string customerId, CancellationToken cancellationToken = default);

    #endregion

    #region Quote Operations

    /// <summary>
    /// Creates a quote for a stablecoin to fiat payout.
    /// </summary>
    Task<QuoteResponse> CreateQuoteAsync(CreateQuoteRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a quote by ID.
    /// </summary>
    Task<QuoteResponse> GetQuoteAsync(string quoteId, CancellationToken cancellationToken = default);

    #endregion

    #region Payout Operations

    /// <summary>
    /// Creates a payout from stablecoin to fiat.
    /// </summary>
    Task<PayoutResponse> CreatePayoutAsync(CreatePayoutRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a payout by ID.
    /// </summary>
    Task<PayoutResponse> GetPayoutAsync(string payoutId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets payout status.
    /// </summary>
    Task<PayoutStatusResponse> GetPayoutStatusAsync(string payoutId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancels a payout if possible.
    /// </summary>
    Task<PayoutResponse> CancelPayoutAsync(string payoutId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists payouts with optional filters.
    /// </summary>
    Task<PayoutListResponse> ListPayoutsAsync(ListPayoutsRequest request, CancellationToken cancellationToken = default);

    #endregion

    #region Webhook

    /// <summary>
    /// Validates a webhook signature.
    /// </summary>
    bool ValidateWebhookSignature(string payload, string signature, string secret);

    /// <summary>
    /// Processes a webhook payload.
    /// </summary>
    Task<WebhookResponse> ProcessWebhookAsync(string payload, string signature, CancellationToken cancellationToken = default);

    #endregion
}
