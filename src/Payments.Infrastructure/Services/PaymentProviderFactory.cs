using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Payments.Core.Enums;
using Payments.Core.Interfaces;
using Payments.Infrastructure.Configuration;

namespace Payments.Infrastructure.Services;

/// <summary>
/// Factory for creating and selecting payment providers at runtime.
/// </summary>
public sealed class PaymentProviderFactory
{
    private readonly IEnumerable<IPaymentProvider> _providers;
    private readonly PayoutSettings _settings;
    private readonly ILogger<PaymentProviderFactory> _logger;

    public PaymentProviderFactory(
        IEnumerable<IPaymentProvider> providers,
        IOptions<PayoutSettings> settings,
        ILogger<PaymentProviderFactory> logger)
    {
        _providers = providers;
        _settings = settings.Value;
        _logger = logger;
    }

    /// <summary>
    /// Gets all available providers.
    /// </summary>
    public IEnumerable<IPaymentProvider> GetAllProviders() => _providers;

    /// <summary>
    /// Gets the default provider based on configuration.
    /// </summary>
    public IPaymentProvider GetDefaultProvider()
    {
        var defaultProviderName = _settings.DefaultProvider;

        if (Enum.TryParse<PayoutProvider>(defaultProviderName, true, out var providerId))
        {
            var provider = _providers.FirstOrDefault(p => p.ProviderId == providerId);
            if (provider != null)
            {
                return provider;
            }
        }

        // Fall back to first available provider
        var fallback = _providers.FirstOrDefault()
            ?? throw new InvalidOperationException("No payment providers are configured");

        _logger.LogWarning(
            "Default provider {DefaultProvider} not found, falling back to {FallbackProvider}",
            defaultProviderName,
            fallback.ProviderName);

        return fallback;
    }

    /// <summary>
    /// Gets a specific provider by ID.
    /// </summary>
    public IPaymentProvider? GetProvider(PayoutProvider providerId)
    {
        return _providers.FirstOrDefault(p => p.ProviderId == providerId);
    }

    /// <summary>
    /// Gets a provider by name.
    /// </summary>
    public IPaymentProvider? GetProviderByName(string name)
    {
        return _providers.FirstOrDefault(p =>
            p.ProviderName.Equals(name, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Gets the best available provider, with optional preference.
    /// Falls back to other providers if the preferred one is unavailable.
    /// </summary>
    public async Task<IPaymentProvider> GetBestAvailableProviderAsync(
        PayoutProvider? preferred = null,
        CancellationToken cancellationToken = default)
    {
        // Try preferred provider first
        if (preferred.HasValue)
        {
            var preferredProvider = GetProvider(preferred.Value);
            if (preferredProvider != null)
            {
                var health = await preferredProvider.CheckHealthAsync(cancellationToken);
                if (health.IsHealthy)
                {
                    return preferredProvider;
                }

                _logger.LogWarning(
                    "Preferred provider {Provider} is unhealthy: {Message}",
                    preferredProvider.ProviderName,
                    health.Message);
            }
        }

        // Try default provider
        var defaultProvider = GetDefaultProvider();
        if (!preferred.HasValue || defaultProvider.ProviderId != preferred.Value)
        {
            var health = await defaultProvider.CheckHealthAsync(cancellationToken);
            if (health.IsHealthy)
            {
                return defaultProvider;
            }

            _logger.LogWarning(
                "Default provider {Provider} is unhealthy: {Message}",
                defaultProvider.ProviderName,
                health.Message);
        }

        // Try any healthy provider if failover is enabled
        if (_settings.EnableFailover)
        {
            foreach (var provider in _providers)
            {
                if (provider.ProviderId == preferred || provider.ProviderId == defaultProvider.ProviderId)
                    continue;

                var health = await provider.CheckHealthAsync(cancellationToken);
                if (health.IsHealthy)
                {
                    _logger.LogInformation(
                        "Failing over to provider {Provider}",
                        provider.ProviderName);
                    return provider;
                }
            }
        }

        // Return default provider anyway - let it handle the error
        _logger.LogError("No healthy providers available, returning default provider");
        return defaultProvider;
    }

    /// <summary>
    /// Checks health of all providers.
    /// </summary>
    public async Task<Dictionary<PayoutProvider, ProviderHealthResult>> CheckAllProvidersHealthAsync(
        CancellationToken cancellationToken = default)
    {
        var results = new Dictionary<PayoutProvider, ProviderHealthResult>();

        foreach (var provider in _providers)
        {
            try
            {
                var health = await provider.CheckHealthAsync(cancellationToken);
                results[provider.ProviderId] = health;
            }
            catch (Exception ex)
            {
                results[provider.ProviderId] = new ProviderHealthResult
                {
                    IsHealthy = false,
                    Status = "error",
                    Message = ex.Message
                };
            }
        }

        return results;
    }
}
