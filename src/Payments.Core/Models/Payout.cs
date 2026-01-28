using Payments.Core.Enums;

namespace Payments.Core.Models;

/// <summary>
/// Represents a stablecoin to fiat payout.
/// </summary>
public sealed class Payout
{
    /// <summary>
    /// Unique identifier for the payout.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// External reference ID for the payout.
    /// </summary>
    public string? ExternalId { get; init; }

    /// <summary>
    /// Provider that processed this payout.
    /// </summary>
    public required PayoutProvider Provider { get; init; }

    /// <summary>
    /// Provider-specific order/transaction ID.
    /// </summary>
    public required string ProviderOrderId { get; init; }

    /// <summary>
    /// Current status of the payout.
    /// </summary>
    public required PayoutStatus Status { get; init; }

    /// <summary>
    /// Source stablecoin currency.
    /// </summary>
    public required Stablecoin SourceCurrency { get; init; }

    /// <summary>
    /// Source amount in stablecoin.
    /// </summary>
    public required decimal SourceAmount { get; init; }

    /// <summary>
    /// Target fiat currency.
    /// </summary>
    public required FiatCurrency TargetCurrency { get; init; }

    /// <summary>
    /// Target amount in fiat currency.
    /// </summary>
    public required decimal TargetAmount { get; init; }

    /// <summary>
    /// Exchange rate applied.
    /// </summary>
    public required decimal ExchangeRate { get; init; }

    /// <summary>
    /// Total fee amount.
    /// </summary>
    public required decimal FeeAmount { get; init; }

    /// <summary>
    /// Blockchain network used for the transfer.
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
    /// Deposit wallet for receiving stablecoin.
    /// </summary>
    public DepositWallet? DepositWallet { get; init; }

    /// <summary>
    /// Quote ID used for this payout.
    /// </summary>
    public string? QuoteId { get; init; }

    /// <summary>
    /// Payment method for fiat delivery.
    /// </summary>
    public required PaymentMethod PaymentMethod { get; init; }

    /// <summary>
    /// Blockchain transaction hash (once funds are sent).
    /// </summary>
    public string? BlockchainTxHash { get; init; }

    /// <summary>
    /// Bank reference number (once fiat is sent).
    /// </summary>
    public string? BankReference { get; init; }

    /// <summary>
    /// Reason for failure (if status is Failed).
    /// </summary>
    public string? FailureReason { get; init; }

    /// <summary>
    /// Timestamp when the payout was created.
    /// </summary>
    public required DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    /// Timestamp when the payout was last updated.
    /// </summary>
    public required DateTimeOffset UpdatedAt { get; init; }

    /// <summary>
    /// Timestamp when the payout was completed (if applicable).
    /// </summary>
    public DateTimeOffset? CompletedAt { get; init; }

    /// <summary>
    /// Additional metadata for the payout.
    /// </summary>
    public Dictionary<string, string>? Metadata { get; init; }
}
