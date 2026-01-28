using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StablecoinPayments.Core.Enums;
using StablecoinPayments.Core.Exceptions;
using StablecoinPayments.Core.Interfaces;
using StablecoinPayments.Core.Models.Responses;
using StablecoinPayments.Infrastructure.Configuration;

namespace StablecoinPayments.Infrastructure.Services;

public sealed class PaymentProviderFactory
{
    private readonly IEnumerable<IPaymentProvider> _providers;
    private readonly PaymentSettings _settings;
    private readonly ILogger<PaymentProviderFactory> _logger;

    public PaymentProviderFactory(
        IEnumerable<IPaymentProvider> providers,
        IOptions<PaymentSettings> settings,
        ILogger<PaymentProviderFactory> logger)
    {
        _providers = providers;
        _settings = settings.Value;
        _logger = logger;
    }

    /// <summary>
    /// Gets the default provider.
    /// </summary>
    public IPaymentProvider GetDefaultProvider()
    {
        return GetProvider(_settings.DefaultProvider);
    }

    /// <summary>
    /// Gets a specific provider by ID.
    /// </summary>
    public IPaymentProvider GetProvider(PaymentProvider providerId)
    {
        var provider = _providers.FirstOrDefault(p => p.ProviderId == providerId);

        if (provider == null)
        {
            _logger.LogError("Provider {ProviderId} not found", providerId);
            throw new ProviderUnavailableException(providerId);
        }

        return provider;
    }

    /// <summary>
    /// Gets all available providers.
    /// </summary>
    public IReadOnlyList<IPaymentProvider> GetAllProviders()
    {
        return _providers.ToList();
    }

    /// <summary>
    /// Gets all available provider IDs.
    /// </summary>
    public IReadOnlyList<PaymentProvider> GetAvailableProviderIds()
    {
        return _providers.Select(p => p.ProviderId).ToList();
    }

    /// <summary>
    /// Gets the best available provider (healthy with lowest latency).
    /// </summary>
    public async Task<IPaymentProvider> GetBestAvailableProviderAsync(CancellationToken cancellationToken = default)
    {
        var healthChecks = await CheckAllProvidersHealthAsync(cancellationToken);

        var healthyProviders = healthChecks
            .Where(h => h.Value.IsHealthy)
            .OrderBy(h => h.Value.Latency)
            .ToList();

        if (healthyProviders.Count == 0)
        {
            _logger.LogError("No healthy providers available");
            throw new PaymentException("No healthy providers available", "NO_PROVIDERS_AVAILABLE");
        }

        // Prefer default provider if healthy
        var defaultHealthy = healthyProviders.FirstOrDefault(h => h.Key == _settings.DefaultProvider);
        if (defaultHealthy.Key != default)
        {
            return GetProvider(defaultHealthy.Key);
        }

        return GetProvider(healthyProviders.First().Key);
    }

    /// <summary>
    /// Gets a provider with automatic failover if the primary is unavailable.
    /// </summary>
    public async Task<IPaymentProvider> GetProviderWithFailoverAsync(
        PaymentProvider? preferredProvider = null,
        CancellationToken cancellationToken = default)
    {
        var targetProvider = preferredProvider ?? _settings.DefaultProvider;
        var provider = GetProvider(targetProvider);

        var health = await provider.CheckHealthAsync(cancellationToken);

        if (health.IsHealthy)
        {
            return provider;
        }

        if (!_settings.EnableFailover)
        {
            _logger.LogWarning("Provider {Provider} is unhealthy and failover is disabled", targetProvider);
            throw new ProviderUnavailableException(targetProvider);
        }

        _logger.LogWarning("Provider {Provider} is unhealthy, attempting failover", targetProvider);

        // Try other providers
        foreach (var fallbackProvider in _providers.Where(p => p.ProviderId != targetProvider))
        {
            var fallbackHealth = await fallbackProvider.CheckHealthAsync(cancellationToken);
            if (fallbackHealth.IsHealthy)
            {
                _logger.LogInformation("Failing over to provider {Provider}", fallbackProvider.ProviderId);
                return fallbackProvider;
            }
        }

        _logger.LogError("All providers are unhealthy");
        throw new PaymentException("All providers are unavailable", "ALL_PROVIDERS_UNAVAILABLE");
    }

    /// <summary>
    /// Checks health of all providers.
    /// </summary>
    public async Task<Dictionary<PaymentProvider, HealthCheckResult>> CheckAllProvidersHealthAsync(
        CancellationToken cancellationToken = default)
    {
        var tasks = _providers.Select(async p =>
        {
            var health = await p.CheckHealthAsync(cancellationToken);
            return (p.ProviderId, Health: health);
        });

        var results = await Task.WhenAll(tasks);

        return results.ToDictionary(r => r.ProviderId, r => r.Health);
    }
}
