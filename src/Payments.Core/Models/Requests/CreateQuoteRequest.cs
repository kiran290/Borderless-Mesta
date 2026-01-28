using Payments.Core.Enums;

namespace Payments.Core.Models.Requests;

/// <summary>
/// Request to create a payout quote.
/// </summary>
public sealed class CreateQuoteRequest
{
    /// <summary>
    /// Source stablecoin currency.
    /// </summary>
    public required Stablecoin SourceCurrency { get; init; }

    /// <summary>
    /// Target fiat currency.
    /// </summary>
    public required FiatCurrency TargetCurrency { get; init; }

    /// <summary>
    /// Source amount in stablecoin. Either SourceAmount or TargetAmount must be specified.
    /// </summary>
    public decimal? SourceAmount { get; init; }

    /// <summary>
    /// Target amount in fiat currency. Either SourceAmount or TargetAmount must be specified.
    /// </summary>
    public decimal? TargetAmount { get; init; }

    /// <summary>
    /// Blockchain network for the stablecoin transfer.
    /// </summary>
    public required BlockchainNetwork Network { get; init; }

    /// <summary>
    /// Payment method for fiat delivery.
    /// </summary>
    public PaymentMethod? PaymentMethod { get; init; }

    /// <summary>
    /// Destination country code (ISO 3166-1 alpha-2).
    /// </summary>
    public required string DestinationCountry { get; init; }

    /// <summary>
    /// Optional developer fee amount in source currency.
    /// </summary>
    public decimal? DeveloperFee { get; init; }
}
