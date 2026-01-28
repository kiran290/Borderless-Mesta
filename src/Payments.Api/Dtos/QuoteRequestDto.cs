using System.ComponentModel.DataAnnotations;
using Payments.Core.Enums;

namespace Payments.Api.Dtos;

/// <summary>
/// Request DTO for creating a payout quote.
/// </summary>
public sealed class QuoteRequestDto
{
    /// <summary>
    /// Source stablecoin currency (USDT or USDC).
    /// </summary>
    [Required]
    public required Stablecoin SourceCurrency { get; init; }

    /// <summary>
    /// Target fiat currency (USD, EUR, or GBP).
    /// </summary>
    [Required]
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
    [Required]
    public required BlockchainNetwork Network { get; init; }

    /// <summary>
    /// Payment method for fiat delivery.
    /// </summary>
    public PaymentMethod? PaymentMethod { get; init; }

    /// <summary>
    /// Destination country code (ISO 3166-1 alpha-2).
    /// </summary>
    [Required]
    [StringLength(2, MinimumLength = 2)]
    public required string DestinationCountry { get; init; }

    /// <summary>
    /// Optional developer fee amount in source currency.
    /// </summary>
    public decimal? DeveloperFee { get; init; }

    /// <summary>
    /// Specific provider to use for the quote. If not specified, the default provider will be used.
    /// </summary>
    public PayoutProvider? Provider { get; init; }
}
