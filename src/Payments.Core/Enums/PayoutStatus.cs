namespace Payments.Core.Enums;

/// <summary>
/// Unified payout status across all providers.
/// </summary>
public enum PayoutStatus
{
    /// <summary>
    /// Payout has been created but not yet processed.
    /// </summary>
    Created = 1,

    /// <summary>
    /// Payout is awaiting funds to be deposited.
    /// </summary>
    AwaitingFunds = 2,

    /// <summary>
    /// Funds have been received and are being processed.
    /// </summary>
    FundsReceived = 3,

    /// <summary>
    /// Payout is currently being processed.
    /// </summary>
    Processing = 4,

    /// <summary>
    /// Payout has been sent to the beneficiary.
    /// </summary>
    SentToBeneficiary = 5,

    /// <summary>
    /// Payout has been completed successfully.
    /// </summary>
    Completed = 6,

    /// <summary>
    /// Payout has failed.
    /// </summary>
    Failed = 7,

    /// <summary>
    /// Payout has been cancelled.
    /// </summary>
    Cancelled = 8,

    /// <summary>
    /// Payout has expired (funds not received in time).
    /// </summary>
    Expired = 9,

    /// <summary>
    /// Payout requires manual review.
    /// </summary>
    PendingReview = 10,

    /// <summary>
    /// Payout has been refunded.
    /// </summary>
    Refunded = 11
}
