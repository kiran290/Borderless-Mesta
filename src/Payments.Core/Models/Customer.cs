using Payments.Core.Enums;

namespace Payments.Core.Models;

/// <summary>
/// Represents a customer in the system.
/// </summary>
public sealed class Customer
{
    /// <summary>
    /// Unique identifier for the customer.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// External reference ID for the customer.
    /// </summary>
    public string? ExternalId { get; init; }

    /// <summary>
    /// Type of customer (individual or business).
    /// </summary>
    public required CustomerType Type { get; init; }

    /// <summary>
    /// Role of the customer in transactions.
    /// </summary>
    public required CustomerRole Role { get; init; }

    /// <summary>
    /// Current status of the customer account.
    /// </summary>
    public required CustomerStatus Status { get; init; }

    /// <summary>
    /// Individual customer details.
    /// </summary>
    public IndividualDetails? Individual { get; init; }

    /// <summary>
    /// Business customer details.
    /// </summary>
    public BusinessDetails? Business { get; init; }

    /// <summary>
    /// Contact information.
    /// </summary>
    public required ContactInfo Contact { get; init; }

    /// <summary>
    /// Customer's address.
    /// </summary>
    public Address? Address { get; init; }

    /// <summary>
    /// Bank accounts associated with the customer.
    /// </summary>
    public IReadOnlyList<BankAccount> BankAccounts { get; init; } = Array.Empty<BankAccount>();

    /// <summary>
    /// KYC verification details.
    /// </summary>
    public VerificationInfo? Verification { get; init; }

    /// <summary>
    /// Provider-specific customer IDs.
    /// </summary>
    public Dictionary<PayoutProvider, string> ProviderIds { get; init; } = new();

    /// <summary>
    /// Timestamp when the customer was created.
    /// </summary>
    public required DateTimeOffset CreatedAt { get; init; }

    /// <summary>
    /// Timestamp when the customer was last updated.
    /// </summary>
    public required DateTimeOffset UpdatedAt { get; init; }

    /// <summary>
    /// Additional metadata for the customer.
    /// </summary>
    public Dictionary<string, string>? Metadata { get; init; }

    /// <summary>
    /// Gets the display name for the customer.
    /// </summary>
    public string DisplayName => Type == CustomerType.Individual
        ? $"{Individual?.FirstName} {Individual?.LastName}".Trim()
        : Business?.LegalName ?? Business?.TradingName ?? string.Empty;
}

/// <summary>
/// Individual customer details.
/// </summary>
public sealed class IndividualDetails
{
    /// <summary>
    /// First name.
    /// </summary>
    public required string FirstName { get; init; }

    /// <summary>
    /// Middle name.
    /// </summary>
    public string? MiddleName { get; init; }

    /// <summary>
    /// Last name.
    /// </summary>
    public required string LastName { get; init; }

    /// <summary>
    /// Date of birth.
    /// </summary>
    public DateOnly? DateOfBirth { get; init; }

    /// <summary>
    /// Country of birth (ISO 3166-1 alpha-2).
    /// </summary>
    public string? CountryOfBirth { get; init; }

    /// <summary>
    /// Country of nationality (ISO 3166-1 alpha-2).
    /// </summary>
    public string? Nationality { get; init; }

    /// <summary>
    /// Gender.
    /// </summary>
    public string? Gender { get; init; }

    /// <summary>
    /// Occupation.
    /// </summary>
    public string? Occupation { get; init; }

    /// <summary>
    /// Tax identification number.
    /// </summary>
    public string? TaxId { get; init; }

    /// <summary>
    /// Social security number or equivalent.
    /// </summary>
    public string? SocialSecurityNumber { get; init; }
}

/// <summary>
/// Business customer details.
/// </summary>
public sealed class BusinessDetails
{
    /// <summary>
    /// Legal registered name of the business.
    /// </summary>
    public required string LegalName { get; init; }

    /// <summary>
    /// Trading name / DBA (Doing Business As).
    /// </summary>
    public string? TradingName { get; init; }

    /// <summary>
    /// Business registration number.
    /// </summary>
    public string? RegistrationNumber { get; init; }

    /// <summary>
    /// Tax identification number.
    /// </summary>
    public string? TaxId { get; init; }

    /// <summary>
    /// VAT number (for European businesses).
    /// </summary>
    public string? VatNumber { get; init; }

    /// <summary>
    /// Country of incorporation (ISO 3166-1 alpha-2).
    /// </summary>
    public required string CountryOfIncorporation { get; init; }

    /// <summary>
    /// Date of incorporation.
    /// </summary>
    public DateOnly? DateOfIncorporation { get; init; }

    /// <summary>
    /// Type of business entity (e.g., LLC, Corporation, Partnership).
    /// </summary>
    public string? EntityType { get; init; }

    /// <summary>
    /// Industry or business sector.
    /// </summary>
    public string? Industry { get; init; }

    /// <summary>
    /// Business website.
    /// </summary>
    public string? Website { get; init; }

    /// <summary>
    /// Brief description of the business.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Annual revenue range.
    /// </summary>
    public string? AnnualRevenue { get; init; }

    /// <summary>
    /// Number of employees.
    /// </summary>
    public string? EmployeeCount { get; init; }

    /// <summary>
    /// Registered business address.
    /// </summary>
    public Address? RegisteredAddress { get; init; }

    /// <summary>
    /// Operating business address (if different from registered).
    /// </summary>
    public Address? OperatingAddress { get; init; }

    /// <summary>
    /// Ultimate beneficial owners of the business.
    /// </summary>
    public IReadOnlyList<BeneficialOwner> BeneficialOwners { get; init; } = Array.Empty<BeneficialOwner>();

    /// <summary>
    /// Directors of the business.
    /// </summary>
    public IReadOnlyList<Director> Directors { get; init; } = Array.Empty<Director>();
}

/// <summary>
/// Contact information.
/// </summary>
public sealed class ContactInfo
{
    /// <summary>
    /// Email address.
    /// </summary>
    public required string Email { get; init; }

    /// <summary>
    /// Phone number with country code.
    /// </summary>
    public string? Phone { get; init; }

    /// <summary>
    /// Mobile number with country code.
    /// </summary>
    public string? Mobile { get; init; }

    /// <summary>
    /// Indicates if email is verified.
    /// </summary>
    public bool EmailVerified { get; init; }

    /// <summary>
    /// Indicates if phone is verified.
    /// </summary>
    public bool PhoneVerified { get; init; }
}

/// <summary>
/// Beneficial owner of a business.
/// </summary>
public sealed class BeneficialOwner
{
    /// <summary>
    /// First name.
    /// </summary>
    public required string FirstName { get; init; }

    /// <summary>
    /// Last name.
    /// </summary>
    public required string LastName { get; init; }

    /// <summary>
    /// Date of birth.
    /// </summary>
    public DateOnly? DateOfBirth { get; init; }

    /// <summary>
    /// Nationality (ISO 3166-1 alpha-2).
    /// </summary>
    public string? Nationality { get; init; }

    /// <summary>
    /// Percentage of ownership.
    /// </summary>
    public required decimal OwnershipPercentage { get; init; }

    /// <summary>
    /// Address.
    /// </summary>
    public Address? Address { get; init; }

    /// <summary>
    /// Identity document type.
    /// </summary>
    public string? DocumentType { get; init; }

    /// <summary>
    /// Identity document number.
    /// </summary>
    public string? DocumentNumber { get; init; }
}

/// <summary>
/// Director of a business.
/// </summary>
public sealed class Director
{
    /// <summary>
    /// First name.
    /// </summary>
    public required string FirstName { get; init; }

    /// <summary>
    /// Last name.
    /// </summary>
    public required string LastName { get; init; }

    /// <summary>
    /// Date of birth.
    /// </summary>
    public DateOnly? DateOfBirth { get; init; }

    /// <summary>
    /// Nationality (ISO 3166-1 alpha-2).
    /// </summary>
    public string? Nationality { get; init; }

    /// <summary>
    /// Role/title of the director.
    /// </summary>
    public string? Title { get; init; }

    /// <summary>
    /// Date appointed.
    /// </summary>
    public DateOnly? AppointedDate { get; init; }

    /// <summary>
    /// Address.
    /// </summary>
    public Address? Address { get; init; }
}
