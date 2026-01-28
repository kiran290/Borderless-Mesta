namespace Payments.Core.Enums;

/// <summary>
/// Status of KYC/KYB verification.
/// </summary>
public enum VerificationStatus
{
    /// <summary>
    /// Verification not started.
    /// </summary>
    NotStarted = 1,

    /// <summary>
    /// Verification is pending user action.
    /// </summary>
    Pending = 2,

    /// <summary>
    /// Documents have been submitted and are under review.
    /// </summary>
    InReview = 3,

    /// <summary>
    /// Additional information is required.
    /// </summary>
    AdditionalInfoRequired = 4,

    /// <summary>
    /// Verification has been approved.
    /// </summary>
    Approved = 5,

    /// <summary>
    /// Verification has been rejected.
    /// </summary>
    Rejected = 6,

    /// <summary>
    /// Verification has expired and needs to be renewed.
    /// </summary>
    Expired = 7
}
