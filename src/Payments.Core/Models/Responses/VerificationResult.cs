using Payments.Core.Enums;

namespace Payments.Core.Models.Responses;

/// <summary>
/// Unified result for verification operations (KYC/KYB initiation, status, submission).
/// </summary>
public sealed class VerificationResult
{
    /// <summary>
    /// Indicates if the operation was successful.
    /// </summary>
    public required bool Success { get; init; }

    /// <summary>
    /// Customer ID.
    /// </summary>
    public string? CustomerId { get; init; }

    /// <summary>
    /// Verification session ID (for redirect-based verification).
    /// </summary>
    public string? SessionId { get; init; }

    /// <summary>
    /// URL to redirect the user to for verification (for redirect-based flows).
    /// </summary>
    public string? VerificationUrl { get; init; }

    /// <summary>
    /// Current verification status.
    /// </summary>
    public VerificationStatus? Status { get; init; }

    /// <summary>
    /// Current verification level achieved.
    /// </summary>
    public VerificationLevel? Level { get; init; }

    /// <summary>
    /// Full verification info.
    /// </summary>
    public VerificationInfo? Verification { get; init; }

    /// <summary>
    /// Expiry time for the verification session.
    /// </summary>
    public DateTimeOffset? ExpiresAt { get; init; }

    /// <summary>
    /// Provider that processed the request.
    /// </summary>
    public PayoutProvider? Provider { get; init; }

    /// <summary>
    /// Error code (if failed).
    /// </summary>
    public string? ErrorCode { get; init; }

    /// <summary>
    /// Error message (if failed).
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Creates a successful initiation result.
    /// </summary>
    public static VerificationResult InitiationSucceeded(
        string customerId,
        string sessionId,
        string? verificationUrl,
        PayoutProvider provider,
        DateTimeOffset? expiresAt = null) => new()
    {
        Success = true,
        CustomerId = customerId,
        SessionId = sessionId,
        VerificationUrl = verificationUrl,
        Status = VerificationStatus.Pending,
        ExpiresAt = expiresAt,
        Provider = provider
    };

    /// <summary>
    /// Creates a successful status result.
    /// </summary>
    public static VerificationResult StatusSucceeded(
        string customerId,
        VerificationInfo verification,
        PayoutProvider provider) => new()
    {
        Success = true,
        CustomerId = customerId,
        Status = verification.Status,
        Level = verification.Level,
        Verification = verification,
        Provider = provider
    };

    /// <summary>
    /// Creates a successful submission result.
    /// </summary>
    public static VerificationResult SubmissionSucceeded(
        string customerId,
        VerificationStatus status,
        PayoutProvider provider) => new()
    {
        Success = true,
        CustomerId = customerId,
        Status = status,
        Provider = provider
    };

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    public static VerificationResult Failed(string errorCode, string errorMessage, PayoutProvider? provider = null) => new()
    {
        Success = false,
        ErrorCode = errorCode,
        ErrorMessage = errorMessage,
        Provider = provider
    };
}
