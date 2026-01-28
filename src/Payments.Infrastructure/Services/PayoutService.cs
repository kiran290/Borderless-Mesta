using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Payments.Core.Enums;
using Payments.Core.Exceptions;
using Payments.Core.Interfaces;
using Payments.Core.Models;
using Payments.Core.Models.Requests;
using Payments.Core.Models.Responses;

namespace Payments.Infrastructure.Services;

/// <summary>
/// High-level service for payout operations.
/// Orchestrates provider selection and payout execution.
/// </summary>
public sealed class PayoutService : IPayoutService
{
    private readonly IPayoutProviderFactory _providerFactory;
    private readonly ILogger<PayoutService> _logger;

    // In-memory store for demo purposes - replace with database in production
    private readonly ConcurrentDictionary<string, PayoutRecord> _payouts = new();

    public PayoutService(
        IPayoutProviderFactory providerFactory,
        ILogger<PayoutService> logger)
    {
        _providerFactory = providerFactory;
        _logger = logger;
    }

    public async Task<QuoteResult> GetQuoteAsync(
        CreateQuoteRequest request,
        PayoutProvider? provider = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Getting quote: {SourceCurrency} -> {TargetCurrency}, Provider: {Provider}",
            request.SourceCurrency,
            request.TargetCurrency,
            provider?.ToString() ?? "Auto");

        var selectedProvider = await SelectProviderAsync(
            request.SourceCurrency,
            request.TargetCurrency,
            request.Network,
            request.DestinationCountry,
            provider,
            cancellationToken);

        return await selectedProvider.GetQuoteAsync(request, cancellationToken);
    }

    public async Task<IReadOnlyList<QuoteResult>> GetAllQuotesAsync(
        CreateQuoteRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Getting quotes from all providers: {SourceCurrency} -> {TargetCurrency}",
            request.SourceCurrency,
            request.TargetCurrency);

        var supportingProviders = _providerFactory
            .GetSupportingProviders(
                request.SourceCurrency,
                request.TargetCurrency,
                request.Network,
                request.DestinationCountry)
            .ToList();

        if (supportingProviders.Count == 0)
        {
            return Array.Empty<QuoteResult>();
        }

        var tasks = supportingProviders.Select(async provider =>
        {
            try
            {
                if (await provider.IsAvailableAsync(cancellationToken))
                {
                    return await provider.GetQuoteAsync(request, cancellationToken);
                }

                return QuoteResult.Failed("PROVIDER_UNAVAILABLE", $"Provider {provider.ProviderName} is not available");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting quote from provider {Provider}", provider.ProviderName);
                return QuoteResult.Failed("QUOTE_ERROR", ex.Message);
            }
        });

        var results = await Task.WhenAll(tasks);
        return results;
    }

    public async Task<PayoutResult> CreatePayoutAsync(
        CreatePayoutRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Creating payout: {SourceAmount} {SourceCurrency} -> {TargetCurrency}, Provider: {Provider}",
            request.SourceAmount ?? request.TargetAmount,
            request.SourceCurrency,
            request.TargetCurrency,
            request.PreferredProvider?.ToString() ?? "Auto");

        var selectedProvider = await SelectProviderAsync(
            request.SourceCurrency,
            request.TargetCurrency,
            request.Network,
            request.Beneficiary.BankAccount.CountryCode,
            request.PreferredProvider,
            cancellationToken);

        var result = await selectedProvider.CreatePayoutAsync(request, cancellationToken);

        if (result.Success && result.Payout != null)
        {
            // Store payout record for tracking
            _payouts[result.Payout.Id] = new PayoutRecord
            {
                Payout = result.Payout,
                Provider = selectedProvider.ProviderId,
                ProviderOrderId = result.Payout.ProviderOrderId
            };

            _logger.LogInformation(
                "Payout created successfully: {PayoutId}, Provider: {Provider}, OrderId: {OrderId}",
                result.Payout.Id,
                selectedProvider.ProviderName,
                result.Payout.ProviderOrderId);
        }

        return result;
    }

    public async Task<PayoutStatusResult> GetPayoutStatusAsync(
        string payoutId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting payout status: {PayoutId}", payoutId);

        if (!_payouts.TryGetValue(payoutId, out var record))
        {
            throw new PayoutNotFoundException(payoutId);
        }

        var provider = _providerFactory.GetProvider(record.Provider);
        var result = await provider.GetPayoutStatusAsync(record.ProviderOrderId, cancellationToken);

        if (result.Success && result.StatusUpdate != null)
        {
            // Update stored payout status
            record.Payout = record.Payout with
            {
                Status = result.StatusUpdate.CurrentStatus,
                BlockchainTxHash = result.StatusUpdate.BlockchainTxHash ?? record.Payout.BlockchainTxHash,
                BankReference = result.StatusUpdate.BankReference ?? record.Payout.BankReference,
                FailureReason = result.StatusUpdate.FailureReason,
                UpdatedAt = result.StatusUpdate.Timestamp,
                CompletedAt = result.StatusUpdate.CurrentStatus == PayoutStatus.Completed
                    ? result.StatusUpdate.Timestamp
                    : record.Payout.CompletedAt
            };
        }

        return result;
    }

    public async Task<DepositWallet> GetDepositWalletAsync(
        string payoutId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting deposit wallet: {PayoutId}", payoutId);

        if (!_payouts.TryGetValue(payoutId, out var record))
        {
            throw new PayoutNotFoundException(payoutId);
        }

        // Return cached wallet if available
        if (record.Payout.DepositWallet != null)
        {
            return record.Payout.DepositWallet;
        }

        var provider = _providerFactory.GetProvider(record.Provider);
        var wallet = await provider.GetDepositWalletAsync(record.ProviderOrderId, cancellationToken);

        // Cache the wallet
        record.Payout = record.Payout with { DepositWallet = wallet };

        return wallet;
    }

    public async Task<bool> CancelPayoutAsync(
        string payoutId,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Cancelling payout: {PayoutId}", payoutId);

        if (!_payouts.TryGetValue(payoutId, out var record))
        {
            throw new PayoutNotFoundException(payoutId);
        }

        // Check if cancellation is allowed based on current status
        if (!CanCancel(record.Payout.Status))
        {
            throw new PayoutCancellationException(payoutId, record.Payout.Status, record.Provider);
        }

        var provider = _providerFactory.GetProvider(record.Provider);
        var cancelled = await provider.CancelPayoutAsync(record.ProviderOrderId, cancellationToken);

        if (cancelled)
        {
            record.Payout = record.Payout with
            {
                Status = PayoutStatus.Cancelled,
                UpdatedAt = DateTimeOffset.UtcNow
            };
        }

        return cancelled;
    }

    public Task<IReadOnlyList<Payout>> GetPayoutHistoryAsync(
        string? senderId = null,
        string? beneficiaryId = null,
        DateTimeOffset? fromDate = null,
        DateTimeOffset? toDate = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Getting payout history: SenderId={SenderId}, BeneficiaryId={BeneficiaryId}",
            senderId,
            beneficiaryId);

        var query = _payouts.Values.AsEnumerable();

        if (!string.IsNullOrEmpty(senderId))
        {
            query = query.Where(r => r.Payout.Sender.Id == senderId);
        }

        if (!string.IsNullOrEmpty(beneficiaryId))
        {
            query = query.Where(r => r.Payout.Beneficiary.Id == beneficiaryId);
        }

        if (fromDate.HasValue)
        {
            query = query.Where(r => r.Payout.CreatedAt >= fromDate.Value);
        }

        if (toDate.HasValue)
        {
            query = query.Where(r => r.Payout.CreatedAt <= toDate.Value);
        }

        var payouts = query
            .OrderByDescending(r => r.Payout.CreatedAt)
            .Select(r => r.Payout)
            .ToList();

        return Task.FromResult<IReadOnlyList<Payout>>(payouts);
    }

    private async Task<IPayoutProvider> SelectProviderAsync(
        Stablecoin sourceCurrency,
        FiatCurrency targetCurrency,
        BlockchainNetwork network,
        string destinationCountry,
        PayoutProvider? preferredProvider,
        CancellationToken cancellationToken)
    {
        var provider = await _providerFactory.SelectBestProviderAsync(
            sourceCurrency,
            targetCurrency,
            network,
            destinationCountry,
            preferredProvider,
            cancellationToken);

        if (provider == null)
        {
            throw new UnsupportedConfigurationException(
                sourceCurrency,
                targetCurrency,
                network,
                destinationCountry);
        }

        return provider;
    }

    private static bool CanCancel(PayoutStatus status)
    {
        return status is PayoutStatus.Created
            or PayoutStatus.AwaitingFunds
            or PayoutStatus.PendingReview;
    }

    private sealed class PayoutRecord
    {
        public required Payout Payout { get; set; }
        public required PayoutProvider Provider { get; init; }
        public required string ProviderOrderId { get; init; }
    }
}
