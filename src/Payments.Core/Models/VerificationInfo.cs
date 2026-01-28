using Payments.Core.Enums;

namespace Payments.Core.Models;

/// <summary>
/// KYC/KYB verification information for a customer.
/// </summary>
public sealed class VerificationInfo
{
    /// <summary>
    /// Current verification status.
    /// </summary>
    public required VerificationStatus Status { get; init; }

    /// <summary>
    /// Current verification level achieved.
    /// </summary>
    public required VerificationLevel Level { get; init; }

    /// <summary>
    /// Indicates if KYC is completed for individuals.
    /// </summary>
    public bool KycCompleted { get; init; }

    /// <summary>
    /// Indicates if KYB is completed for businesses.
    /// </summary>
    public bool KybCompleted { get; init; }

    /// <summary>
    /// List of verification checks performed.
    /// </summary>
    public IReadOnlyList<VerificationCheck> Checks { get; init; } = Array.Empty<VerificationCheck>();

    /// <summary>
    /// List of documents submitted.
    /// </summary>
    public IReadOnlyList<VerificationDocument> Documents { get; init; } = Array.Empty<VerificationDocument>();

    /// <summary>
    /// Rejection reason if verification was rejected.
    /// </summary>
    public string? RejectionReason { get; init; }

    /// <summary>
    /// Additional information required if status is AdditionalInfoRequired.
    /// </summary>
    public IReadOnlyList<string>? RequiredInfo { get; init; }

    /// <summary>
    /// Risk score from verification.
    /// </summary>
    public int? RiskScore { get; init; }

    /// <summary>
    /// Risk level classification.
    /// </summary>
    public string? RiskLevel { get; init; }

    /// <summary>
    /// Timestamp when verification was submitted.
    /// </summary>
    public DateTimeOffset? SubmittedAt { get; init; }

    /// <summary>
    /// Timestamp when verification was completed.
    /// </summary>
    public DateTimeOffset? CompletedAt { get; init; }

    /// <summary>
    /// Timestamp when verification expires.
    /// </summary>
    public DateTimeOffset? ExpiresAt { get; init; }

    /// <summary>
    /// Provider-specific verification IDs.
    /// </summary>
    public Dictionary<PayoutProvider, string> ProviderVerificationIds { get; init; } = new();
}

/// <summary>
/// Individual verification check result.
/// </summary>
public sealed class VerificationCheck
{
    /// <summary>
    /// Unique identifier for the check.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Type of check performed.
    /// </summary>
    public required string CheckType { get; init; }

    /// <summary>
    /// Status of the check.
    /// </summary>
    public required VerificationStatus Status { get; init; }

    /// <summary>
    /// Result of the check (pass/fail/review).
    /// </summary>
    public string? Result { get; init; }

    /// <summary>
    /// Details or notes about the check.
    /// </summary>
    public string? Details { get; init; }

    /// <summary>
    /// Timestamp when the check was performed.
    /// </summary>
    public required DateTimeOffset PerformedAt { get; init; }

    /// <summary>
    /// Provider that performed the check.
    /// </summary>
    public PayoutProvider? Provider { get; init; }
}

/// <summary>
/// Verification document submitted by customer.
/// </summary>
public sealed class VerificationDocument
{
    /// <summary>
    /// Unique identifier for the document.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Type of document.
    /// </summary>
    public required DocumentType Type { get; init; }

    /// <summary>
    /// Document status.
    /// </summary>
    public required VerificationStatus Status { get; init; }

    /// <summary>
    /// Document number (if applicable).
    /// </summary>
    public string? DocumentNumber { get; init; }

    /// <summary>
    /// Issuing country (ISO 3166-1 alpha-2).
    /// </summary>
    public string? IssuingCountry { get; init; }

    /// <summary>
    /// Issue date.
    /// </summary>
    public DateOnly? IssueDate { get; init; }

    /// <summary>
    /// Expiry date.
    /// </summary>
    public DateOnly? ExpiryDate { get; init; }

    /// <summary>
    /// Front image URL or reference.
    /// </summary>
    public string? FrontImageUrl { get; init; }

    /// <summary>
    /// Back image URL or reference.
    /// </summary>
    public string? BackImageUrl { get; init; }

    /// <summary>
    /// Rejection reason if document was rejected.
    /// </summary>
    public string? RejectionReason { get; init; }

    /// <summary>
    /// Timestamp when document was uploaded.
    /// </summary>
    public required DateTimeOffset UploadedAt { get; init; }

    /// <summary>
    /// Timestamp when document was verified.
    /// </summary>
    public DateTimeOffset? VerifiedAt { get; init; }
}
