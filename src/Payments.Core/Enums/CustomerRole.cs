namespace Payments.Core.Enums;

/// <summary>
/// Role of the customer in transactions.
/// </summary>
public enum CustomerRole
{
    /// <summary>
    /// Customer acts as a sender (originator of funds).
    /// </summary>
    Sender = 1,

    /// <summary>
    /// Customer acts as a beneficiary (recipient of funds).
    /// </summary>
    Beneficiary = 2,

    /// <summary>
    /// Customer can act as both sender and beneficiary.
    /// </summary>
    Both = 3
}
