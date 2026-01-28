using Payments.Core.Enums;

namespace Payments.Core.Models.Responses;

/// <summary>
/// Result of a KYC/KYB verification initiation.
/// </summary>
public sealed class VerificationInitiationResult
{
    /// <summary>
    /// Indicates if the initiation was successful.
    /// </summary>
    public required bool Success { get; init; }

    /// <summary>
    /// Verification session ID.
    /// </summary>
    public string? SessionId { get; init; }

    /// <summary>
    /// URL to redirect the user to for verification.
    /// </summary>
    public string? VerificationUrl { get; init; }

    /// <summary>
    /// Current verification status.
    /// </summary>
    public VerificationStatus? Status { get; init; }

    /// <summary>
    /// Error code (if failed).
    /// </summary>
    public string? ErrorCode { get; init; }

    /// <summary>
    /// Error message (if failed).
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Expiry time for the verification session.
    /// </summary>
    public DateTimeOffset? ExpiresAt { get; init; }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    public static VerificationInitiationResult Succeeded(
        string sessionId,
        string verificationUrl,
        DateTimeOffset? expiresAt = null) => new()
    {
        Success = true,
        SessionId = sessionId,
        VerificationUrl = verificationUrl,
        Status = VerificationStatus.Pending,
        ExpiresAt = expiresAt
    };

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    public static VerificationInitiationResult Failed(string errorCode, string errorMessage) => new()
    {
        Success = false,
        ErrorCode = errorCode,
        ErrorMessage = errorMessage
    };
}

/// <summary>
/// Result of a verification status check.
/// </summary>
public sealed class VerificationStatusResult
{
    /// <summary>
    /// Indicates if the query was successful.
    /// </summary>
    public required bool Success { get; init; }

    /// <summary>
    /// Customer ID.
    /// </summary>
    public string? CustomerId { get; init; }

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
    /// Error code (if failed).
    /// </summary>
    public string? ErrorCode { get; init; }

    /// <summary>
    /// Error message (if failed).
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    public static VerificationStatusResult Succeeded(
        string customerId,
        VerificationInfo verification) => new()
    {
        Success = true,
        CustomerId = customerId,
        Status = verification.Status,
        Level = verification.Level,
        Verification = verification
    };

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    public static VerificationStatusResult Failed(string errorCode, string errorMessage) => new()
    {
        Success = false,
        ErrorCode = errorCode,
        ErrorMessage = errorMessage
    };
}

/// <summary>
/// Result of a document upload.
/// </summary>
public sealed class DocumentUploadResult
{
    /// <summary>
    /// Indicates if the upload was successful.
    /// </summary>
    public required bool Success { get; init; }

    /// <summary>
    /// Document ID.
    /// </summary>
    public string? DocumentId { get; init; }

    /// <summary>
    /// Document status after upload.
    /// </summary>
    public VerificationStatus? Status { get; init; }

    /// <summary>
    /// Error code (if failed).
    /// </summary>
    public string? ErrorCode { get; init; }

    /// <summary>
    /// Error message (if failed).
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    public static DocumentUploadResult Succeeded(string documentId) => new()
    {
        Success = true,
        DocumentId = documentId,
        Status = VerificationStatus.InReview
    };

    /// <summary>
    /// Creates a failed result.
    /// </summary>
    public static DocumentUploadResult Failed(string errorCode, string errorMessage) => new()
    {
        Success = false,
        ErrorCode = errorCode,
        ErrorMessage = errorMessage
    };
}
