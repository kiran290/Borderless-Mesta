namespace Payments.Core.Models;

/// <summary>
/// Detailed breakdown of fees for a payout.
/// </summary>
public sealed class FeeBreakdown
{
    /// <summary>
    /// Network/gas fee for blockchain transaction.
    /// </summary>
    public decimal NetworkFee { get; init; }

    /// <summary>
    /// Provider's processing fee.
    /// </summary>
    public decimal ProcessingFee { get; init; }

    /// <summary>
    /// Foreign exchange spread fee.
    /// </summary>
    public decimal FxSpreadFee { get; init; }

    /// <summary>
    /// Bank transfer fee.
    /// </summary>
    public decimal BankFee { get; init; }

    /// <summary>
    /// Developer/platform fee (if applicable).
    /// </summary>
    public decimal DeveloperFee { get; init; }

    /// <summary>
    /// Total of all fees.
    /// </summary>
    public decimal Total => NetworkFee + ProcessingFee + FxSpreadFee + BankFee + DeveloperFee;
}
