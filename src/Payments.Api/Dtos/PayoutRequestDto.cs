using System.ComponentModel.DataAnnotations;
using Payments.Core.Enums;

namespace Payments.Api.Dtos;

/// <summary>
/// Request DTO for creating a stablecoin to fiat payout.
/// </summary>
public sealed class PayoutRequestDto
{
    /// <summary>
    /// External reference ID for the payout.
    /// </summary>
    [StringLength(100)]
    public string? ExternalId { get; init; }

    /// <summary>
    /// Quote ID to use for this payout. If not provided, a quote will be created automatically.
    /// </summary>
    public string? QuoteId { get; init; }

    /// <summary>
    /// Source stablecoin currency (USDT or USDC).
    /// </summary>
    [Required]
    public required Stablecoin SourceCurrency { get; init; }

    /// <summary>
    /// Target fiat currency (USD, EUR, or GBP).
    /// </summary>
    [Required]
    public required FiatCurrency TargetCurrency { get; init; }

    /// <summary>
    /// Source amount in stablecoin. Either SourceAmount or TargetAmount must be specified.
    /// </summary>
    public decimal? SourceAmount { get; init; }

    /// <summary>
    /// Target amount in fiat currency. Either SourceAmount or TargetAmount must be specified.
    /// </summary>
    public decimal? TargetAmount { get; init; }

    /// <summary>
    /// Blockchain network for the stablecoin transfer.
    /// </summary>
    [Required]
    public required BlockchainNetwork Network { get; init; }

    /// <summary>
    /// Sender information.
    /// </summary>
    [Required]
    public required SenderDto Sender { get; init; }

    /// <summary>
    /// Beneficiary information.
    /// </summary>
    [Required]
    public required BeneficiaryDto Beneficiary { get; init; }

    /// <summary>
    /// Payment method for fiat delivery.
    /// </summary>
    [Required]
    public required PaymentMethod PaymentMethod { get; init; }

    /// <summary>
    /// Optional developer fee amount in source currency.
    /// </summary>
    public decimal? DeveloperFee { get; init; }

    /// <summary>
    /// Optional metadata for the payout.
    /// </summary>
    public Dictionary<string, string>? Metadata { get; init; }

    /// <summary>
    /// Preferred provider for the payout. If not specified, the best available provider will be used.
    /// </summary>
    public PayoutProvider? PreferredProvider { get; init; }
}

/// <summary>
/// DTO for sender information.
/// </summary>
public sealed class SenderDto
{
    /// <summary>
    /// Existing sender ID (if previously created).
    /// </summary>
    public string? Id { get; init; }

    /// <summary>
    /// External reference ID for the sender.
    /// </summary>
    public string? ExternalId { get; init; }

    /// <summary>
    /// Type of sender (individual or business).
    /// </summary>
    [Required]
    public required BeneficiaryType Type { get; init; }

    /// <summary>
    /// First name (required for individuals).
    /// </summary>
    public string? FirstName { get; init; }

    /// <summary>
    /// Last name (required for individuals).
    /// </summary>
    public string? LastName { get; init; }

    /// <summary>
    /// Business name (required for business senders).
    /// </summary>
    public string? BusinessName { get; init; }

    /// <summary>
    /// Email address.
    /// </summary>
    [Required]
    [EmailAddress]
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
    public AddressDto? Address { get; init; }

    /// <summary>
    /// Identity document type.
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
}

/// <summary>
/// DTO for beneficiary information.
/// </summary>
public sealed class BeneficiaryDto
{
    /// <summary>
    /// Existing beneficiary ID (if previously created).
    /// </summary>
    public string? Id { get; init; }

    /// <summary>
    /// External reference ID for the beneficiary.
    /// </summary>
    public string? ExternalId { get; init; }

    /// <summary>
    /// Type of beneficiary (individual or business).
    /// </summary>
    [Required]
    public required BeneficiaryType Type { get; init; }

    /// <summary>
    /// First name (required for individuals).
    /// </summary>
    public string? FirstName { get; init; }

    /// <summary>
    /// Last name (required for individuals).
    /// </summary>
    public string? LastName { get; init; }

    /// <summary>
    /// Business name (required for business beneficiaries).
    /// </summary>
    public string? BusinessName { get; init; }

    /// <summary>
    /// Email address.
    /// </summary>
    [Required]
    [EmailAddress]
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
    public AddressDto? Address { get; init; }

    /// <summary>
    /// Bank account details for receiving funds.
    /// </summary>
    [Required]
    public required BankAccountDto BankAccount { get; init; }

    /// <summary>
    /// Identity document type.
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
}

/// <summary>
/// DTO for address information.
/// </summary>
public sealed class AddressDto
{
    /// <summary>
    /// Street address line 1.
    /// </summary>
    [Required]
    public required string Street1 { get; init; }

    /// <summary>
    /// Street address line 2.
    /// </summary>
    public string? Street2 { get; init; }

    /// <summary>
    /// City name.
    /// </summary>
    [Required]
    public required string City { get; init; }

    /// <summary>
    /// State or province.
    /// </summary>
    public string? State { get; init; }

    /// <summary>
    /// Postal or ZIP code.
    /// </summary>
    [Required]
    public required string PostalCode { get; init; }

    /// <summary>
    /// Country code (ISO 3166-1 alpha-2).
    /// </summary>
    [Required]
    [StringLength(2, MinimumLength = 2)]
    public required string CountryCode { get; init; }
}

/// <summary>
/// DTO for bank account information.
/// </summary>
public sealed class BankAccountDto
{
    /// <summary>
    /// Name of the bank.
    /// </summary>
    [Required]
    public required string BankName { get; init; }

    /// <summary>
    /// Bank account number or IBAN.
    /// </summary>
    [Required]
    public required string AccountNumber { get; init; }

    /// <summary>
    /// Account holder's name.
    /// </summary>
    [Required]
    public required string AccountHolderName { get; init; }

    /// <summary>
    /// Bank routing number (for US banks).
    /// </summary>
    public string? RoutingNumber { get; init; }

    /// <summary>
    /// SWIFT/BIC code for international transfers.
    /// </summary>
    public string? SwiftCode { get; init; }

    /// <summary>
    /// Sort code (for UK banks).
    /// </summary>
    public string? SortCode { get; init; }

    /// <summary>
    /// IBAN for European bank accounts.
    /// </summary>
    public string? Iban { get; init; }

    /// <summary>
    /// Currency of the bank account.
    /// </summary>
    [Required]
    public required FiatCurrency Currency { get; init; }

    /// <summary>
    /// Country code where the bank is located (ISO 3166-1 alpha-2).
    /// </summary>
    [Required]
    [StringLength(2, MinimumLength = 2)]
    public required string CountryCode { get; init; }

    /// <summary>
    /// Bank branch code if applicable.
    /// </summary>
    public string? BranchCode { get; init; }
}
