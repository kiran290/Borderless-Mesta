namespace Payments.Core.Enums;

/// <summary>
/// Level of KYC/KYB verification completed.
/// </summary>
public enum VerificationLevel
{
    /// <summary>
    /// No verification completed.
    /// </summary>
    None = 0,

    /// <summary>
    /// Basic verification (email, phone).
    /// </summary>
    Basic = 1,

    /// <summary>
    /// Standard verification (ID document).
    /// </summary>
    Standard = 2,

    /// <summary>
    /// Enhanced verification (ID + proof of address).
    /// </summary>
    Enhanced = 3,

    /// <summary>
    /// Full verification (all documents + additional checks).
    /// </summary>
    Full = 4
}
