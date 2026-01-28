using Microsoft.Extensions.Logging;
using Payments.Core.Enums;
using Payments.Core.Interfaces;
using Payments.Core.Models.Requests;
using Payments.Core.Models.Responses;

namespace Payments.Infrastructure.Services;

/// <summary>
/// Unified payment service providing a high-level API for all payment operations.
/// Handles provider selection, failover, and operation routing.
/// </summary>
public sealed class UnifiedPaymentService
{
    private readonly PaymentProviderFactory _providerFactory;
    private readonly ILogger<UnifiedPaymentService> _logger;

    public UnifiedPaymentService(
        PaymentProviderFactory providerFactory,
        ILogger<UnifiedPaymentService> logger)
    {
        _providerFactory = providerFactory;
        _logger = logger;
    }

    #region Customer Operations

    /// <summary>
    /// Creates a new customer using the specified or default provider.
    /// </summary>
    public async Task<CustomerResult> CreateCustomerAsync(
        CreateCustomerRequest request,
        CancellationToken cancellationToken = default)
    {
        var provider = await _providerFactory.GetBestAvailableProviderAsync(
            request.PreferredProvider,
            cancellationToken);

        _logger.LogInformation(
            "Creating customer via {Provider}: {Email}",
            provider.ProviderName,
            request.Contact.Email);

        return await provider.CreateCustomerAsync(request, cancellationToken);
    }

    /// <summary>
    /// Gets a customer by ID from the specified provider.
    /// </summary>
    public async Task<CustomerResult> GetCustomerAsync(
        string customerId,
        PayoutProvider? provider = null,
        CancellationToken cancellationToken = default)
    {
        var paymentProvider = await _providerFactory.GetBestAvailableProviderAsync(provider, cancellationToken);
        return await paymentProvider.GetCustomerAsync(customerId, cancellationToken);
    }

    /// <summary>
    /// Updates a customer.
    /// </summary>
    public async Task<CustomerResult> UpdateCustomerAsync(
        string customerId,
        UpdateCustomerRequest request,
        PayoutProvider? provider = null,
        CancellationToken cancellationToken = default)
    {
        var paymentProvider = await _providerFactory.GetBestAvailableProviderAsync(provider, cancellationToken);
        return await paymentProvider.UpdateCustomerAsync(customerId, request, cancellationToken);
    }

    /// <summary>
    /// Lists customers from the specified provider.
    /// </summary>
    public async Task<CustomerListResult> ListCustomersAsync(
        CustomerListRequest request,
        PayoutProvider? provider = null,
        CancellationToken cancellationToken = default)
    {
        var paymentProvider = await _providerFactory.GetBestAvailableProviderAsync(provider, cancellationToken);
        return await paymentProvider.ListCustomersAsync(request, cancellationToken);
    }

    #endregion

    #region KYC/KYB Operations

    /// <summary>
    /// Initiates KYC verification for an individual customer.
    /// </summary>
    public async Task<VerificationResult> InitiateKycAsync(
        InitiateKycRequest request,
        PayoutProvider? provider = null,
        CancellationToken cancellationToken = default)
    {
        var paymentProvider = await _providerFactory.GetBestAvailableProviderAsync(provider, cancellationToken);
        _logger.LogInformation(
            "Initiating KYC via {Provider} for customer {CustomerId}",
            paymentProvider.ProviderName,
            request.CustomerId);

        return await paymentProvider.InitiateKycAsync(request, cancellationToken);
    }

    /// <summary>
    /// Gets KYC status for a customer.
    /// </summary>
    public async Task<VerificationResult> GetKycStatusAsync(
        string customerId,
        PayoutProvider? provider = null,
        CancellationToken cancellationToken = default)
    {
        var paymentProvider = await _providerFactory.GetBestAvailableProviderAsync(provider, cancellationToken);
        return await paymentProvider.GetKycStatusAsync(customerId, cancellationToken);
    }

    /// <summary>
    /// Initiates KYB verification for a business customer.
    /// </summary>
    public async Task<VerificationResult> InitiateKybAsync(
        InitiateKybRequest request,
        PayoutProvider? provider = null,
        CancellationToken cancellationToken = default)
    {
        var paymentProvider = await _providerFactory.GetBestAvailableProviderAsync(provider, cancellationToken);
        _logger.LogInformation(
            "Initiating KYB via {Provider} for customer {CustomerId}",
            paymentProvider.ProviderName,
            request.CustomerId);

        return await paymentProvider.InitiateKybAsync(request, cancellationToken);
    }

    /// <summary>
    /// Gets KYB status for a customer.
    /// </summary>
    public async Task<VerificationResult> GetKybStatusAsync(
        string customerId,
        PayoutProvider? provider = null,
        CancellationToken cancellationToken = default)
    {
        var paymentProvider = await _providerFactory.GetBestAvailableProviderAsync(provider, cancellationToken);
        return await paymentProvider.GetKybStatusAsync(customerId, cancellationToken);
    }

    /// <summary>
    /// Uploads a verification document.
    /// </summary>
    public async Task<DocumentUploadResult> UploadDocumentAsync(
        UploadDocumentRequest request,
        PayoutProvider? provider = null,
        CancellationToken cancellationToken = default)
    {
        var paymentProvider = await _providerFactory.GetBestAvailableProviderAsync(provider, cancellationToken);
        return await paymentProvider.UploadDocumentAsync(request, cancellationToken);
    }

    /// <summary>
    /// Gets documents for a customer.
    /// </summary>
    public async Task<DocumentListResult> GetDocumentsAsync(
        string customerId,
        PayoutProvider? provider = null,
        CancellationToken cancellationToken = default)
    {
        var paymentProvider = await _providerFactory.GetBestAvailableProviderAsync(provider, cancellationToken);
        return await paymentProvider.GetDocumentsAsync(customerId, cancellationToken);
    }

    /// <summary>
    /// Submits verification for review.
    /// </summary>
    public async Task<VerificationResult> SubmitVerificationAsync(
        string customerId,
        PayoutProvider? provider = null,
        CancellationToken cancellationToken = default)
    {
        var paymentProvider = await _providerFactory.GetBestAvailableProviderAsync(provider, cancellationToken);
        return await paymentProvider.SubmitVerificationAsync(customerId, cancellationToken);
    }

    #endregion

    #region Payout Operations

    /// <summary>
    /// Creates a quote for a stablecoin to fiat payout.
    /// </summary>
    public async Task<QuoteResult> CreateQuoteAsync(
        CreateQuoteRequest request,
        PayoutProvider? provider = null,
        CancellationToken cancellationToken = default)
    {
        var paymentProvider = await _providerFactory.GetBestAvailableProviderAsync(provider, cancellationToken);
        _logger.LogInformation(
            "Creating quote via {Provider}: {SourceCurrency} -> {TargetCurrency}",
            paymentProvider.ProviderName,
            request.SourceCurrency,
            request.TargetCurrency);

        return await paymentProvider.CreateQuoteAsync(request, cancellationToken);
    }

    /// <summary>
    /// Creates quotes from all available providers for comparison.
    /// </summary>
    public async Task<IReadOnlyList<QuoteResult>> CreateQuotesFromAllProvidersAsync(
        CreateQuoteRequest request,
        CancellationToken cancellationToken = default)
    {
        var providers = _providerFactory.GetAllProviders();
        var tasks = providers.Select(async provider =>
        {
            try
            {
                return await provider.CreateQuoteAsync(request, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to get quote from {Provider}", provider.ProviderName);
                return QuoteResult.Failed("QUOTE_ERROR", ex.Message);
            }
        });

        var results = await Task.WhenAll(tasks);
        return results.ToList();
    }

    /// <summary>
    /// Creates a payout from stablecoin to fiat.
    /// </summary>
    public async Task<PayoutResult> CreatePayoutAsync(
        CreatePayoutRequest request,
        CancellationToken cancellationToken = default)
    {
        var provider = await _providerFactory.GetBestAvailableProviderAsync(
            request.PreferredProvider,
            cancellationToken);

        _logger.LogInformation(
            "Creating payout via {Provider}: {SourceCurrency} -> {TargetCurrency}",
            provider.ProviderName,
            request.SourceCurrency,
            request.TargetCurrency);

        return await provider.CreatePayoutAsync(request, cancellationToken);
    }

    /// <summary>
    /// Gets a payout by ID.
    /// </summary>
    public async Task<PayoutResult> GetPayoutAsync(
        string payoutId,
        PayoutProvider? provider = null,
        CancellationToken cancellationToken = default)
    {
        var paymentProvider = await _providerFactory.GetBestAvailableProviderAsync(provider, cancellationToken);
        return await paymentProvider.GetPayoutAsync(payoutId, cancellationToken);
    }

    /// <summary>
    /// Gets payout status.
    /// </summary>
    public async Task<PayoutStatusResult> GetPayoutStatusAsync(
        string payoutId,
        PayoutProvider? provider = null,
        CancellationToken cancellationToken = default)
    {
        var paymentProvider = await _providerFactory.GetBestAvailableProviderAsync(provider, cancellationToken);
        return await paymentProvider.GetPayoutStatusAsync(payoutId, cancellationToken);
    }

    /// <summary>
    /// Cancels a payout.
    /// </summary>
    public async Task<PayoutResult> CancelPayoutAsync(
        string payoutId,
        PayoutProvider? provider = null,
        CancellationToken cancellationToken = default)
    {
        var paymentProvider = await _providerFactory.GetBestAvailableProviderAsync(provider, cancellationToken);
        return await paymentProvider.CancelPayoutAsync(payoutId, cancellationToken);
    }

    /// <summary>
    /// Lists payouts.
    /// </summary>
    public async Task<PayoutListResult> ListPayoutsAsync(
        PayoutListRequest request,
        PayoutProvider? provider = null,
        CancellationToken cancellationToken = default)
    {
        var paymentProvider = await _providerFactory.GetBestAvailableProviderAsync(provider, cancellationToken);
        return await paymentProvider.ListPayoutsAsync(request, cancellationToken);
    }

    #endregion

    #region Webhook Handling

    /// <summary>
    /// Processes a webhook from a specific provider.
    /// </summary>
    public async Task<WebhookResult> ProcessWebhookAsync(
        PayoutProvider providerId,
        string payload,
        string signature,
        CancellationToken cancellationToken = default)
    {
        var provider = _providerFactory.GetProvider(providerId);
        if (provider == null)
        {
            return new WebhookResult
            {
                Success = false,
                EventType = "unknown",
                Error = $"Provider {providerId} not found"
            };
        }

        return await provider.ProcessWebhookAsync(payload, signature, cancellationToken);
    }

    #endregion

    #region Provider Health

    /// <summary>
    /// Checks health of all providers.
    /// </summary>
    public Task<Dictionary<PayoutProvider, ProviderHealthResult>> CheckAllProvidersHealthAsync(
        CancellationToken cancellationToken = default)
    {
        return _providerFactory.CheckAllProvidersHealthAsync(cancellationToken);
    }

    /// <summary>
    /// Gets all available provider IDs.
    /// </summary>
    public IEnumerable<PayoutProvider> GetAvailableProviders()
    {
        return _providerFactory.GetAllProviders().Select(p => p.ProviderId);
    }

    #endregion
}
