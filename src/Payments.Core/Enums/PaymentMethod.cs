namespace Payments.Core.Enums;

/// <summary>
/// Payment method for fiat delivery to beneficiary.
/// </summary>
public enum PaymentMethod
{
    /// <summary>
    /// Bank wire transfer.
    /// </summary>
    BankTransfer = 1,

    /// <summary>
    /// SEPA transfer (Europe).
    /// </summary>
    Sepa = 2,

    /// <summary>
    /// ACH transfer (United States).
    /// </summary>
    Ach = 3,

    /// <summary>
    /// Faster Payments (United Kingdom).
    /// </summary>
    FasterPayments = 4,

    /// <summary>
    /// SWIFT international transfer.
    /// </summary>
    Swift = 5
}
