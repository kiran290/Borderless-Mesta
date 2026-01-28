using Payments.Core.Enums;

namespace Payments.Core.Models;

/// <summary>
/// Represents a payout sender (originator).
/// </summary>
public sealed class Sender
{
    /// <summary>
    /// Unique identifier for the sender.
    /// </summary>
    public string? Id { get; set; }

    /// <summary>
    /// External reference ID for the sender.
    /// </summary>
    public string? ExternalId { get; init; }

    /// <summary>
    /// Type of sender (individual or business).
    /// </summary>
    public required BeneficiaryType Type { get; init; }

    /// <summary>
    /// First name (for individuals).
    /// </summary>
    public string? FirstName { get; init; }

    /// <summary>
    /// Last name (for individuals).
    /// </summary>
    public string? LastName { get; init; }

    /// <summary>
    /// Business name (for business senders).
    /// </summary>
    public string? BusinessName { get; init; }

    /// <summary>
    /// Email address.
    /// </summary>
    public required string Email { get; init; }

    /// <summary>
    /// Phone number with country code.
    /// </summary>
    public string? PhoneNumber { get; init; }

    /// <summary>
    /// Date of birth (for individuals).
    /// </summary>
    public DateOnly? DateOfBirth { get; init; }

    /// <summary>
    /// Sender's address.
    /// </summary>
    public Address? Address { get; init; }

    /// <summary>
    /// Identity document type (e.g., passport, national_id).
    /// </summary>
    public string? DocumentType { get; init; }

    /// <summary>
    /// Identity document number.
    /// </summary>
    public string? DocumentNumber { get; init; }

    /// <summary>
    /// Country of nationality (ISO 3166-1 alpha-2).
    /// </summary>
    public string? Nationality { get; init; }

    /// <summary>
    /// Gets the display name for the sender.
    /// </summary>
    public string DisplayName => Type == BeneficiaryType.Individual
        ? $"{FirstName} {LastName}".Trim()
        : BusinessName ?? string.Empty;
}
