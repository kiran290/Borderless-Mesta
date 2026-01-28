using Payments.Core.Enums;

namespace Payments.Core.Models;

/// <summary>
/// Represents a payout beneficiary (recipient).
/// </summary>
public sealed class Beneficiary
{
    /// <summary>
    /// Unique identifier for the beneficiary.
    /// </summary>
    public string? Id { get; set; }

    /// <summary>
    /// External reference ID for the beneficiary.
    /// </summary>
    public string? ExternalId { get; init; }

    /// <summary>
    /// Type of beneficiary (individual or business).
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
    /// Business name (for business beneficiaries).
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
    /// Beneficiary's address.
    /// </summary>
    public Address? Address { get; init; }

    /// <summary>
    /// Bank account details for receiving funds.
    /// </summary>
    public required BankAccount BankAccount { get; init; }

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
    /// Gets the display name for the beneficiary.
    /// </summary>
    public string DisplayName => Type == BeneficiaryType.Individual
        ? $"{FirstName} {LastName}".Trim()
        : BusinessName ?? string.Empty;
}
