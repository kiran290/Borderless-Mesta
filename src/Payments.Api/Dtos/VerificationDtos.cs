using System.ComponentModel.DataAnnotations;
using Payments.Core.Enums;
using Payments.Core.Models.Responses;

namespace Payments.Api.Dtos;

#region Verification Request DTOs

/// <summary>
/// Request DTO to initiate KYC verification.
/// </summary>
public sealed class InitiateKycRequestDto
{
    /// <summary>
    /// Customer ID to verify.
    /// </summary>
    [Required]
    public required string CustomerId { get; init; }

    /// <summary>
    /// Desired verification level.
    /// </summary>
    public VerificationLevel TargetLevel { get; init; } = VerificationLevel.Standard;

    /// <summary>
    /// Redirect URL after verification completion.
    /// </summary>
    [Url]
    public string? RedirectUrl { get; init; }

    /// <summary>
    /// Webhook URL for verification status updates.
    /// </summary>
    [Url]
    public string? WebhookUrl { get; init; }

    /// <summary>
    /// Preferred provider for verification.
    /// </summary>
    public PayoutProvider? PreferredProvider { get; init; }
}

/// <summary>
/// Request DTO to initiate KYB verification.
/// </summary>
public sealed class InitiateKybRequestDto
{
    /// <summary>
    /// Customer ID to verify.
    /// </summary>
    [Required]
    public required string CustomerId { get; init; }

    /// <summary>
    /// Desired verification level.
    /// </summary>
    public VerificationLevel TargetLevel { get; init; } = VerificationLevel.Standard;

    /// <summary>
    /// Redirect URL after verification completion.
    /// </summary>
    [Url]
    public string? RedirectUrl { get; init; }

    /// <summary>
    /// Webhook URL for verification status updates.
    /// </summary>
    [Url]
    public string? WebhookUrl { get; init; }

    /// <summary>
    /// Preferred provider for verification.
    /// </summary>
    public PayoutProvider? PreferredProvider { get; init; }
}

/// <summary>
/// Request DTO to upload a verification document.
/// </summary>
public sealed class UploadDocumentRequestDto
{
    /// <summary>
    /// Customer ID.
    /// </summary>
    [Required]
    public required string CustomerId { get; init; }

    /// <summary>
    /// Type of document being uploaded.
    /// </summary>
    [Required]
    public required DocumentType DocumentType { get; init; }

    /// <summary>
    /// Document number (if applicable).
    /// </summary>
    public string? DocumentNumber { get; init; }

    /// <summary>
    /// Issuing country (ISO 3166-1 alpha-2).
    /// </summary>
    [StringLength(2)]
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
    [Required]
    public required string FrontImageBase64 { get; init; }

    /// <summary>
    /// Back image as base64 encoded string (if applicable).
    /// </summary>
    public string? BackImageBase64 { get; init; }

    /// <summary>
    /// MIME type of the image.
    /// </summary>
    [Required]
    public required string MimeType { get; init; }
}

/// <summary>
/// Request DTO to submit verification for review.
/// </summary>
public sealed class SubmitVerificationRequestDto
{
    /// <summary>
    /// Customer ID.
    /// </summary>
    [Required]
    public required string CustomerId { get; init; }

    /// <summary>
    /// Declaration that all information is accurate.
    /// </summary>
    [Required]
    public required bool AcceptDeclaration { get; init; }
}

#endregion

#region Verification Response DTOs

/// <summary>
/// Verification initiation response DTO.
/// </summary>
public sealed class VerificationInitiationResponseDto
{
    public required string SessionId { get; init; }
    public required string VerificationUrl { get; init; }
    public required VerificationStatus Status { get; init; }
    public DateTimeOffset? ExpiresAt { get; init; }

    public static VerificationInitiationResponseDto FromResult(VerificationInitiationResult result)
    {
        return new VerificationInitiationResponseDto
        {
            SessionId = result.SessionId!,
            VerificationUrl = result.VerificationUrl!,
            Status = result.Status ?? VerificationStatus.Pending,
            ExpiresAt = result.ExpiresAt
        };
    }
}

/// <summary>
/// Verification status response DTO.
/// </summary>
public sealed class VerificationStatusResponseDto
{
    public required string CustomerId { get; init; }
    public required VerificationStatus Status { get; init; }
    public required VerificationLevel Level { get; init; }
    public bool KycCompleted { get; init; }
    public bool KybCompleted { get; init; }
    public string? RejectionReason { get; init; }
    public List<string>? RequiredInfo { get; init; }
    public int? RiskScore { get; init; }
    public string? RiskLevel { get; init; }
    public DateTimeOffset? SubmittedAt { get; init; }
    public DateTimeOffset? CompletedAt { get; init; }
    public DateTimeOffset? ExpiresAt { get; init; }

    public static VerificationStatusResponseDto FromResult(VerificationStatusResult result)
    {
        return new VerificationStatusResponseDto
        {
            CustomerId = result.CustomerId!,
            Status = result.Status ?? VerificationStatus.NotStarted,
            Level = result.Level ?? VerificationLevel.None,
            KycCompleted = result.Verification?.KycCompleted ?? false,
            KybCompleted = result.Verification?.KybCompleted ?? false,
            RejectionReason = result.Verification?.RejectionReason,
            RequiredInfo = result.Verification?.RequiredInfo?.ToList(),
            RiskScore = result.Verification?.RiskScore,
            RiskLevel = result.Verification?.RiskLevel,
            SubmittedAt = result.Verification?.SubmittedAt,
            CompletedAt = result.Verification?.CompletedAt,
            ExpiresAt = result.Verification?.ExpiresAt
        };
    }
}

/// <summary>
/// Document upload response DTO.
/// </summary>
public sealed class DocumentUploadResponseDto
{
    public required string DocumentId { get; init; }
    public required VerificationStatus Status { get; init; }

    public static DocumentUploadResponseDto FromResult(DocumentUploadResult result)
    {
        return new DocumentUploadResponseDto
        {
            DocumentId = result.DocumentId!,
            Status = result.Status ?? VerificationStatus.InReview
        };
    }
}

#endregion
