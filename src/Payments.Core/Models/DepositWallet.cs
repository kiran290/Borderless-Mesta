using Payments.Core.Enums;

namespace Payments.Core.Models;

/// <summary>
/// Represents a deposit wallet address for receiving stablecoin funds.
/// </summary>
public sealed class DepositWallet
{
    /// <summary>
    /// Unique identifier for the wallet.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Blockchain wallet address.
    /// </summary>
    public required string Address { get; init; }

    /// <summary>
    /// Blockchain network for the wallet.
    /// </summary>
    public required BlockchainNetwork Network { get; init; }

    /// <summary>
    /// Expected stablecoin to receive.
    /// </summary>
    public required Stablecoin Currency { get; init; }

    /// <summary>
    /// Expected deposit amount.
    /// </summary>
    public required decimal ExpectedAmount { get; init; }

    /// <summary>
    /// Timestamp when the wallet address expires.
    /// </summary>
    public DateTimeOffset? ExpiresAt { get; init; }

    /// <summary>
    /// Additional memo or tag (for networks that require it).
    /// </summary>
    public string? Memo { get; init; }

    /// <summary>
    /// Indicates if the wallet is still valid for deposits.
    /// </summary>
    public bool IsValid => !ExpiresAt.HasValue || DateTimeOffset.UtcNow < ExpiresAt.Value;
}
