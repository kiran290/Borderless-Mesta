using Payments.Core.Enums;

namespace Payments.Core.Models.Requests;

/// <summary>
/// Request to initiate KYC verification for an individual customer.
/// </summary>
public sealed class InitiateKycRequest
{
    /// <summary>
    /// Customer ID to verify.
    /// </summary>
    public required string CustomerId { get; init; }

    /// <summary>
    /// Desired verification level.
    /// </summary>
    public VerificationLevel TargetLevel { get; init; } = VerificationLevel.Standard;

    /// <summary>
    /// Redirect URL after verification completion.
    /// </summary>
    public string? RedirectUrl { get; init; }

    /// <summary>
    /// Webhook URL for verification status updates.
    /// </summary>
    public string? WebhookUrl { get; init; }

    /// <summary>
    /// Preferred provider for verification.
    /// </summary>
    public PayoutProvider? PreferredProvider { get; init; }
}

/// <summary>
/// Request to initiate KYB verification for a business customer.
/// </summary>
public sealed class InitiateKybRequest
{
    /// <summary>
    /// Customer ID to verify.
    /// </summary>
    public required string CustomerId { get; init; }

    /// <summary>
    /// Desired verification level.
    /// </summary>
    public VerificationLevel TargetLevel { get; init; } = VerificationLevel.Standard;

    /// <summary>
    /// Redirect URL after verification completion.
    /// </summary>
    public string? RedirectUrl { get; init; }

    /// <summary>
    /// Webhook URL for verification status updates.
    /// </summary>
    public string? WebhookUrl { get; init; }

    /// <summary>
    /// Preferred provider for verification.
    /// </summary>
    public PayoutProvider? PreferredProvider { get; init; }
}

/// <summary>
/// Request to upload a verification document.
/// </summary>
public sealed class UploadDocumentRequest
{
    /// <summary>
    /// Customer ID.
    /// </summary>
    public required string CustomerId { get; init; }

    /// <summary>
    /// Type of document being uploaded.
    /// </summary>
    public required DocumentType DocumentType { get; init; }

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
    /// Front image as base64 encoded string.
    /// </summary>
    public required string FrontImageBase64 { get; init; }

    /// <summary>
    /// Back image as base64 encoded string (if applicable).
    /// </summary>
    public string? BackImageBase64 { get; init; }

    /// <summary>
    /// MIME type of the image (e.g., image/jpeg, image/png, application/pdf).
    /// </summary>
    public required string MimeType { get; init; }
}

/// <summary>
/// Request to submit verification for review.
/// </summary>
public sealed class SubmitVerificationRequest
{
    /// <summary>
    /// Customer ID.
    /// </summary>
    public required string CustomerId { get; init; }

    /// <summary>
    /// Declaration that all information is accurate.
    /// </summary>
    public required bool AcceptDeclaration { get; init; }

    /// <summary>
    /// IP address of the submitter.
    /// </summary>
    public string? IpAddress { get; init; }

    /// <summary>
    /// User agent of the submitter.
    /// </summary>
    public string? UserAgent { get; init; }
}
