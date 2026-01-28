using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Payments.Core.Enums;
using Payments.Core.Exceptions;
using Payments.Core.Interfaces;
using Payments.Infrastructure.Configuration;

namespace Payments.Infrastructure.Services;

/// <summary>
/// Factory for creating and selecting payout providers at runtime.
/// </summary>
public sealed class PayoutProviderFactory : IPayoutProviderFactory
{
    private readonly IReadOnlyDictionary<PayoutProvider, IPayoutProvider> _providers;
    private readonly PayoutSettings _settings;
    private readonly ILogger<PayoutProviderFactory> _logger;

    public PayoutProviderFactory(
        IEnumerable<IPayoutProvider> providers,
        IOptions<PayoutSettings> settings,
        ILogger<PayoutProviderFactory> logger)
    {
        _providers = providers.ToDictionary(p => p.ProviderId, p => p);
        _settings = settings.Value;
        _logger = logger;

        _logger.LogInformation(
            "PayoutProviderFactory initialized with {Count} providers: {Providers}",
            _providers.Count,
            string.Join(", ", _providers.Keys));
    }

    public IPayoutProvider GetProvider(PayoutProvider provider)
    {
        if (!_providers.TryGetValue(provider, out var payoutProvider))
        {
            throw new ProviderNotFoundException(provider);
        }

        return payoutProvider;
    }

    public IPayoutProvider GetDefaultProvider()
    {
        return GetProvider(_settings.DefaultProvider);
    }

    public IEnumerable<IPayoutProvider> GetAllProviders()
    {
        return _providers.Values;
    }

    public IEnumerable<IPayoutProvider> GetSupportingProviders(
        Stablecoin sourceCurrency,
        FiatCurrency targetCurrency,
        BlockchainNetwork network,
        string destinationCountry)
    {
        return _providers.Values
            .Where(p => p.SupportsConfiguration(sourceCurrency, targetCurrency, network, destinationCountry));
    }

    public async Task<IPayoutProvider?> SelectBestProviderAsync(
        Stablecoin sourceCurrency,
        FiatCurrency targetCurrency,
        BlockchainNetwork network,
        string destinationCountry,
        PayoutProvider? preferredProvider = null,
        CancellationToken cancellationToken = default)
    {
        // If a preferred provider is specified and available, use it
        if (preferredProvider.HasValue)
        {
            if (_providers.TryGetValue(preferredProvider.Value, out var preferred))
            {
                if (preferred.SupportsConfiguration(sourceCurrency, targetCurrency, network, destinationCountry))
                {
                    if (await preferred.IsAvailableAsync(cancellationToken))
                    {
                        _logger.LogInformation(
                            "Using preferred provider: {Provider}",
                            preferred.ProviderName);
                        return preferred;
                    }

                    _logger.LogWarning(
                        "Preferred provider {Provider} is not available, falling back",
                        preferred.ProviderName);
                }
                else
                {
                    _logger.LogWarning(
                        "Preferred provider {Provider} does not support the requested configuration",
                        preferred.ProviderName);
                }
            }
        }

        // Try the default provider first
        var defaultProvider = _providers.GetValueOrDefault(_settings.DefaultProvider);
        if (defaultProvider != null)
        {
            if (defaultProvider.SupportsConfiguration(sourceCurrency, targetCurrency, network, destinationCountry))
            {
                if (await defaultProvider.IsAvailableAsync(cancellationToken))
                {
                    _logger.LogInformation(
                        "Using default provider: {Provider}",
                        defaultProvider.ProviderName);
                    return defaultProvider;
                }
            }
        }

        // If failover is enabled, try other providers
        if (_settings.EnableFailover)
        {
            var supportingProviders = GetSupportingProviders(sourceCurrency, targetCurrency, network, destinationCountry)
                .Where(p => p.ProviderId != _settings.DefaultProvider);

            foreach (var provider in supportingProviders)
            {
                if (await provider.IsAvailableAsync(cancellationToken))
                {
                    _logger.LogInformation(
                        "Using failover provider: {Provider}",
                        provider.ProviderName);
                    return provider;
                }
            }
        }

        _logger.LogWarning(
            "No available provider found for configuration: {SourceCurrency} -> {TargetCurrency} via {Network} to {Country}",
            sourceCurrency,
            targetCurrency,
            network,
            destinationCountry);

        return null;
    }
}
