using Payments.Core.Enums;

namespace Payments.Core.Models;

/// <summary>
/// Represents a status update for a payout.
/// </summary>
public sealed class PayoutStatusUpdate
{
    /// <summary>
    /// Payout identifier.
    /// </summary>
    public required string PayoutId { get; init; }

    /// <summary>
    /// Provider order identifier.
    /// </summary>
    public required string ProviderOrderId { get; init; }

    /// <summary>
    /// Previous status.
    /// </summary>
    public PayoutStatus? PreviousStatus { get; init; }

    /// <summary>
    /// Current status.
    /// </summary>
    public required PayoutStatus CurrentStatus { get; init; }

    /// <summary>
    /// Blockchain transaction hash (if funds received).
    /// </summary>
    public string? BlockchainTxHash { get; init; }

    /// <summary>
    /// Bank reference number (if fiat sent).
    /// </summary>
    public string? BankReference { get; init; }

    /// <summary>
    /// Failure reason (if failed).
    /// </summary>
    public string? FailureReason { get; init; }

    /// <summary>
    /// Timestamp of the status update.
    /// </summary>
    public required DateTimeOffset Timestamp { get; init; }

    /// <summary>
    /// Provider that reported this status.
    /// </summary>
    public required PayoutProvider Provider { get; init; }

    /// <summary>
    /// Raw provider status value.
    /// </summary>
    public string? ProviderStatus { get; init; }
}
