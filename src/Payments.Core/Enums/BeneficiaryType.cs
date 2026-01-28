namespace Payments.Core.Enums;

/// <summary>
/// Type of beneficiary receiving the payout.
/// </summary>
public enum BeneficiaryType
{
    /// <summary>
    /// Individual person receiving the payout.
    /// </summary>
    Individual = 1,

    /// <summary>
    /// Business entity receiving the payout.
    /// </summary>
    Business = 2
}
