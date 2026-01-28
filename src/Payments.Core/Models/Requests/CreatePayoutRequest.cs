using Payments.Core.Enums;

namespace Payments.Core.Models.Requests;

/// <summary>
/// Request to create a stablecoin to fiat payout.
/// </summary>
public sealed class CreatePayoutRequest
{
    /// <summary>
    /// External reference ID for the payout.
    /// </summary>
    public string? ExternalId { get; init; }

    /// <summary>
    /// Quote ID to use for this payout. If not provided, a quote will be created automatically.
    /// </summary>
    public string? QuoteId { get; init; }

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
    /// Sender information.
    /// </summary>
    public required Sender Sender { get; init; }

    /// <summary>
    /// Beneficiary information.
    /// </summary>
    public required Beneficiary Beneficiary { get; init; }

    /// <summary>
    /// Payment method for fiat delivery.
    /// </summary>
    public required PaymentMethod PaymentMethod { get; init; }

    /// <summary>
    /// Optional developer fee amount in source currency.
    /// </summary>
    public decimal? DeveloperFee { get; init; }

    /// <summary>
    /// Optional metadata for the payout.
    /// </summary>
    public Dictionary<string, string>? Metadata { get; init; }

    /// <summary>
    /// Preferred provider for the payout. If not specified, the default provider will be used.
    /// </summary>
    public PayoutProvider? PreferredProvider { get; init; }
}
