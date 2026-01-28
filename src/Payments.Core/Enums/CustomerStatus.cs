namespace Payments.Core.Enums;

/// <summary>
/// Status of the customer account.
/// </summary>
public enum CustomerStatus
{
    /// <summary>
    /// Customer has been created but not yet verified.
    /// </summary>
    Pending = 1,

    /// <summary>
    /// Customer is active and can perform transactions.
    /// </summary>
    Active = 2,

    /// <summary>
    /// Customer verification is in progress.
    /// </summary>
    UnderReview = 3,

    /// <summary>
    /// Customer account is suspended.
    /// </summary>
    Suspended = 4,

    /// <summary>
    /// Customer account is blocked.
    /// </summary>
    Blocked = 5,

    /// <summary>
    /// Customer account is closed.
    /// </summary>
    Closed = 6
}
