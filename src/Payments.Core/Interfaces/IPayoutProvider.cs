using Payments.Core.Enums;
using Payments.Core.Models;
using Payments.Core.Models.Requests;
using Payments.Core.Models.Responses;

namespace Payments.Core.Interfaces;

/// <summary>
/// Interface for payout provider implementations.
/// Each provider (Mesta, Borderless, etc.) must implement this interface.
/// </summary>
public interface IPayoutProvider
{
    /// <summary>
    /// Gets the provider identifier.
    /// </summary>
    PayoutProvider ProviderId { get; }

    /// <summary>
    /// Gets the provider name for display purposes.
    /// </summary>
    string ProviderName { get; }

    /// <summary>
    /// Gets the supported stablecoins by this provider.
    /// </summary>
    IReadOnlyList<Stablecoin> SupportedStablecoins { get; }

    /// <summary>
    /// Gets the supported fiat currencies by this provider.
    /// </summary>
    IReadOnlyList<FiatCurrency> SupportedFiatCurrencies { get; }

    /// <summary>
    /// Gets the supported blockchain networks by this provider.
    /// </summary>
    IReadOnlyList<BlockchainNetwork> SupportedNetworks { get; }

    /// <summary>
    /// Checks if the provider is available and operational.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the provider is available.</returns>
    Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a sender entity in the provider's system.
    /// </summary>
    /// <param name="sender">Sender information.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created sender with provider-assigned ID.</returns>
    Task<Sender> CreateSenderAsync(Sender sender, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a beneficiary entity in the provider's system.
    /// </summary>
    /// <param name="beneficiary">Beneficiary information.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created beneficiary with provider-assigned ID.</returns>
    Task<Beneficiary> CreateBeneficiaryAsync(Beneficiary beneficiary, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a quote for a stablecoin to fiat payout.
    /// </summary>
    /// <param name="request">Quote request parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Quote result containing exchange rate and fee information.</returns>
    Task<QuoteResult> GetQuoteAsync(CreateQuoteRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a stablecoin to fiat payout.
    /// </summary>
    /// <param name="request">Payout request parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Payout result containing the created payout and deposit wallet information.</returns>
    Task<PayoutResult> CreatePayoutAsync(CreatePayoutRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current status of a payout.
    /// </summary>
    /// <param name="providerOrderId">The provider-specific order ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Current payout status.</returns>
    Task<PayoutStatusResult> GetPayoutStatusAsync(string providerOrderId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the deposit wallet address for a payout.
    /// </summary>
    /// <param name="providerOrderId">The provider-specific order ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Deposit wallet information.</returns>
    Task<DepositWallet> GetDepositWalletAsync(string providerOrderId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Cancels a payout if possible.
    /// </summary>
    /// <param name="providerOrderId">The provider-specific order ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if cancellation was successful.</returns>
    Task<bool> CancelPayoutAsync(string providerOrderId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates if the provider supports the requested payout configuration.
    /// </summary>
    /// <param name="sourceCurrency">Source stablecoin.</param>
    /// <param name="targetCurrency">Target fiat currency.</param>
    /// <param name="network">Blockchain network.</param>
    /// <param name="destinationCountry">Destination country code.</param>
    /// <returns>True if the configuration is supported.</returns>
    bool SupportsConfiguration(Stablecoin sourceCurrency, FiatCurrency targetCurrency, BlockchainNetwork network, string destinationCountry);
}
