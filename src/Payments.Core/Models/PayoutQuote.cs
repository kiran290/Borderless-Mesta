using Payments.Core.Enums;

namespace Payments.Core.Models;

/// <summary>
/// Represents a payout quote with exchange rate and fee information.
/// </summary>
public sealed class PayoutQuote
{
    /// <summary>
    /// Unique identifier for the quote.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Source stablecoin currency.
    /// </summary>
    public required Stablecoin SourceCurrency { get; init; }

    /// <summary>
    /// Target fiat currency.
    /// </summary>
    public required FiatCurrency TargetCurrency { get; init; }

    /// <summary>
    /// Source amount in stablecoin.
    /// </summary>
    public required decimal SourceAmount { get; init; }

    /// <summary>
    /// Target amount in fiat currency.
    /// </summary>
    public required decimal TargetAmount { get; init; }

    /// <summary>
    /// Exchange rate applied.
    /// </summary>
    public required decimal ExchangeRate { get; init; }

    /// <summary>
    /// Total fee amount in source currency.
    /// </summary>
    public required decimal FeeAmount { get; init; }

    /// <summary>
    /// Fee breakdown details.
    /// </summary>
    public FeeBreakdown? FeeBreakdown { get; init; }

    /// <summary>
    /// Blockchain network for the stablecoin transfer.
    /// </summary>
    public required BlockchainNetwork Network { get; init; }

    /// <summary>
    /// Timestamp when the quote was created.
    /// </summary>
    public required DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    /// Timestamp when the quote expires.
    /// </summary>
    public required DateTimeOffset ExpiresAt { get; init; }

    /// <summary>
    /// Provider that generated this quote.
    /// </summary>
    public required PayoutProvider Provider { get; init; }

    /// <summary>
    /// Provider-specific quote reference.
    /// </summary>
    public string? ProviderQuoteId { get; init; }

    /// <summary>
    /// Indicates if the quote is still valid.
    /// </summary>
    public bool IsValid => DateTimeOffset.UtcNow < ExpiresAt;
}
