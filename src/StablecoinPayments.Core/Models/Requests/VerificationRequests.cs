using StablecoinPayments.Core.Enums;

namespace StablecoinPayments.Core.Models.Requests;

public sealed class InitiateKycRequest
{
    public required string CustomerId { get; set; }
    public VerificationLevel TargetLevel { get; set; } = VerificationLevel.Standard;
    public string? RedirectUrl { get; set; }
    public string? WebhookUrl { get; set; }
}

public sealed class InitiateKybRequest
{
    public required string CustomerId { get; set; }
    public VerificationLevel TargetLevel { get; set; } = VerificationLevel.Standard;
    public string? RedirectUrl { get; set; }
    public string? WebhookUrl { get; set; }
}

public sealed class UploadDocumentRequest
{
    public required string CustomerId { get; set; }
    public required DocumentType DocumentType { get; set; }
    public string? DocumentNumber { get; set; }
    public string? IssuingCountry { get; set; }
    public DateOnly? IssueDate { get; set; }
    public DateOnly? ExpiryDate { get; set; }
    public required string FrontImageBase64 { get; set; }
    public string? BackImageBase64 { get; set; }
    public string MimeType { get; set; } = "image/jpeg";
}
