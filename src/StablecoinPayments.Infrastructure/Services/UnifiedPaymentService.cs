using Microsoft.Extensions.Logging;
using StablecoinPayments.Core.Enums;
using StablecoinPayments.Core.Interfaces;
using StablecoinPayments.Core.Models.Requests;
using StablecoinPayments.Core.Models.Responses;

namespace StablecoinPayments.Infrastructure.Services;

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

    public IReadOnlyList<PaymentProvider> GetAvailableProviders() => _providerFactory.GetAvailableProviderIds();

    public Task<Dictionary<PaymentProvider, HealthCheckResult>> CheckAllProvidersHealthAsync(
        CancellationToken cancellationToken = default)
        => _providerFactory.CheckAllProvidersHealthAsync(cancellationToken);

    #region Customer Operations

    public async Task<CustomerResponse> CreateCustomerAsync(
        CreateCustomerRequest request,
        PaymentProvider? provider = null,
        CancellationToken cancellationToken = default)
    {
        var paymentProvider = await GetProviderAsync(provider, cancellationToken);
        _logger.LogInformation("Creating customer with provider {Provider}", paymentProvider.ProviderId);
        return await paymentProvider.CreateCustomerAsync(request, cancellationToken);
    }

    public async Task<CustomerResponse> GetCustomerAsync(
        string customerId,
        PaymentProvider? provider = null,
        CancellationToken cancellationToken = default)
    {
        var paymentProvider = await GetProviderAsync(provider, cancellationToken);
        return await paymentProvider.GetCustomerAsync(customerId, cancellationToken);
    }

    public async Task<CustomerResponse> UpdateCustomerAsync(
        string customerId,
        UpdateCustomerRequest request,
        PaymentProvider? provider = null,
        CancellationToken cancellationToken = default)
    {
        var paymentProvider = await GetProviderAsync(provider, cancellationToken);
        return await paymentProvider.UpdateCustomerAsync(customerId, request, cancellationToken);
    }

    public async Task<CustomerListResponse> ListCustomersAsync(
        ListCustomersRequest request,
        PaymentProvider? provider = null,
        CancellationToken cancellationToken = default)
    {
        var paymentProvider = await GetProviderAsync(provider, cancellationToken);
        return await paymentProvider.ListCustomersAsync(request, cancellationToken);
    }

    #endregion

    #region KYC Operations

    public async Task<VerificationResponse> InitiateKycAsync(
        InitiateKycRequest request,
        PaymentProvider? provider = null,
        CancellationToken cancellationToken = default)
    {
        var paymentProvider = await GetProviderAsync(provider, cancellationToken);
        _logger.LogInformation("Initiating KYC for customer {CustomerId} with provider {Provider}",
            request.CustomerId, paymentProvider.ProviderId);
        return await paymentProvider.InitiateKycAsync(request, cancellationToken);
    }

    public async Task<VerificationResponse> GetKycStatusAsync(
        string customerId,
        PaymentProvider? provider = null,
        CancellationToken cancellationToken = default)
    {
        var paymentProvider = await GetProviderAsync(provider, cancellationToken);
        return await paymentProvider.GetKycStatusAsync(customerId, cancellationToken);
    }

    #endregion

    #region KYB Operations

    public async Task<VerificationResponse> InitiateKybAsync(
        InitiateKybRequest request,
        PaymentProvider? provider = null,
        CancellationToken cancellationToken = default)
    {
        var paymentProvider = await GetProviderAsync(provider, cancellationToken);
        _logger.LogInformation("Initiating KYB for customer {CustomerId} with provider {Provider}",
            request.CustomerId, paymentProvider.ProviderId);
        return await paymentProvider.InitiateKybAsync(request, cancellationToken);
    }

    public async Task<VerificationResponse> GetKybStatusAsync(
        string customerId,
        PaymentProvider? provider = null,
        CancellationToken cancellationToken = default)
    {
        var paymentProvider = await GetProviderAsync(provider, cancellationToken);
        return await paymentProvider.GetKybStatusAsync(customerId, cancellationToken);
    }

    #endregion

    #region Document Operations

    public async Task<DocumentResponse> UploadDocumentAsync(
        UploadDocumentRequest request,
        PaymentProvider? provider = null,
        CancellationToken cancellationToken = default)
    {
        var paymentProvider = await GetProviderAsync(provider, cancellationToken);
        return await paymentProvider.UploadDocumentAsync(request, cancellationToken);
    }

    public async Task<DocumentListResponse> GetDocumentsAsync(
        string customerId,
        PaymentProvider? provider = null,
        CancellationToken cancellationToken = default)
    {
        var paymentProvider = await GetProviderAsync(provider, cancellationToken);
        return await paymentProvider.GetDocumentsAsync(customerId, cancellationToken);
    }

    public async Task<VerificationResponse> SubmitVerificationAsync(
        string customerId,
        PaymentProvider? provider = null,
        CancellationToken cancellationToken = default)
    {
        var paymentProvider = await GetProviderAsync(provider, cancellationToken);
        return await paymentProvider.SubmitVerificationAsync(customerId, cancellationToken);
    }

    #endregion

    #region Quote Operations

    public async Task<QuoteResponse> CreateQuoteAsync(
        CreateQuoteRequest request,
        PaymentProvider? provider = null,
        CancellationToken cancellationToken = default)
    {
        var paymentProvider = await GetProviderAsync(provider, cancellationToken);
        _logger.LogInformation("Creating quote with provider {Provider}", paymentProvider.ProviderId);
        return await paymentProvider.CreateQuoteAsync(request, cancellationToken);
    }

    public async Task<IReadOnlyList<QuoteResponse>> CreateQuotesFromAllProvidersAsync(
        CreateQuoteRequest request,
        CancellationToken cancellationToken = default)
    {
        var providers = _providerFactory.GetAllProviders();
        var tasks = providers.Select(p => p.CreateQuoteAsync(request, cancellationToken));
        var results = await Task.WhenAll(tasks);
        return results.ToList();
    }

    public async Task<QuoteResponse> GetQuoteAsync(
        string quoteId,
        PaymentProvider? provider = null,
        CancellationToken cancellationToken = default)
    {
        var paymentProvider = await GetProviderAsync(provider, cancellationToken);
        return await paymentProvider.GetQuoteAsync(quoteId, cancellationToken);
    }

    #endregion

    #region Payout Operations

    public async Task<PayoutResponse> CreatePayoutAsync(
        CreatePayoutRequest request,
        CancellationToken cancellationToken = default)
    {
        var paymentProvider = await GetProviderAsync(null, cancellationToken);
        _logger.LogInformation("Creating payout with provider {Provider}", paymentProvider.ProviderId);
        return await paymentProvider.CreatePayoutAsync(request, cancellationToken);
    }

    public async Task<PayoutResponse> GetPayoutAsync(
        string payoutId,
        PaymentProvider? provider = null,
        CancellationToken cancellationToken = default)
    {
        var paymentProvider = await GetProviderAsync(provider, cancellationToken);
        return await paymentProvider.GetPayoutAsync(payoutId, cancellationToken);
    }

    public async Task<PayoutStatusResponse> GetPayoutStatusAsync(
        string payoutId,
        PaymentProvider? provider = null,
        CancellationToken cancellationToken = default)
    {
        var paymentProvider = await GetProviderAsync(provider, cancellationToken);
        return await paymentProvider.GetPayoutStatusAsync(payoutId, cancellationToken);
    }

    public async Task<PayoutResponse> CancelPayoutAsync(
        string payoutId,
        PaymentProvider? provider = null,
        CancellationToken cancellationToken = default)
    {
        var paymentProvider = await GetProviderAsync(provider, cancellationToken);
        _logger.LogInformation("Canceling payout {PayoutId} with provider {Provider}",
            payoutId, paymentProvider.ProviderId);
        return await paymentProvider.CancelPayoutAsync(payoutId, cancellationToken);
    }

    public async Task<PayoutListResponse> ListPayoutsAsync(
        ListPayoutsRequest request,
        PaymentProvider? provider = null,
        CancellationToken cancellationToken = default)
    {
        var paymentProvider = await GetProviderAsync(provider, cancellationToken);
        return await paymentProvider.ListPayoutsAsync(request, cancellationToken);
    }

    #endregion

    private async Task<IPaymentProvider> GetProviderAsync(
        PaymentProvider? provider,
        CancellationToken cancellationToken)
    {
        return await _providerFactory.GetProviderWithFailoverAsync(provider, cancellationToken);
    }
}
