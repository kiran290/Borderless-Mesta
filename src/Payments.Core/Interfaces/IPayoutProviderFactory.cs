using Payments.Core.Enums;

namespace Payments.Core.Interfaces;

/// <summary>
/// Factory interface for creating and selecting payout providers at runtime.
/// </summary>
public interface IPayoutProviderFactory
{
    /// <summary>
    /// Gets a specific provider by its identifier.
    /// </summary>
    /// <param name="provider">The provider identifier.</param>
    /// <returns>The payout provider instance.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the provider is not registered.</exception>
    IPayoutProvider GetProvider(PayoutProvider provider);

    /// <summary>
    /// Gets the default provider.
    /// </summary>
    /// <returns>The default payout provider instance.</returns>
    IPayoutProvider GetDefaultProvider();

    /// <summary>
    /// Gets all registered providers.
    /// </summary>
    /// <returns>Collection of all registered providers.</returns>
    IEnumerable<IPayoutProvider> GetAllProviders();

    /// <summary>
    /// Gets providers that support a specific configuration.
    /// </summary>
    /// <param name="sourceCurrency">Source stablecoin.</param>
    /// <param name="targetCurrency">Target fiat currency.</param>
    /// <param name="network">Blockchain network.</param>
    /// <param name="destinationCountry">Destination country code.</param>
    /// <returns>Collection of providers that support the configuration.</returns>
    IEnumerable<IPayoutProvider> GetSupportingProviders(Stablecoin sourceCurrency, FiatCurrency targetCurrency, BlockchainNetwork network, string destinationCountry);

    /// <summary>
    /// Selects the best available provider for a given configuration.
    /// </summary>
    /// <param name="sourceCurrency">Source stablecoin.</param>
    /// <param name="targetCurrency">Target fiat currency.</param>
    /// <param name="network">Blockchain network.</param>
    /// <param name="destinationCountry">Destination country code.</param>
    /// <param name="preferredProvider">Optional preferred provider.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The best available provider, or null if none available.</returns>
    Task<IPayoutProvider?> SelectBestProviderAsync(
        Stablecoin sourceCurrency,
        FiatCurrency targetCurrency,
        BlockchainNetwork network,
        string destinationCountry,
        PayoutProvider? preferredProvider = null,
        CancellationToken cancellationToken = default);
}
