using Payments.Core.Enums;

namespace Payments.Core.Models;

/// <summary>
/// Represents bank account details for fiat payouts.
/// </summary>
public sealed class BankAccount
{
    /// <summary>
    /// Name of the bank.
    /// </summary>
    public required string BankName { get; init; }

    /// <summary>
    /// Bank account number or IBAN.
    /// </summary>
    public required string AccountNumber { get; init; }

    /// <summary>
    /// Account holder's name.
    /// </summary>
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
    public required FiatCurrency Currency { get; init; }

    /// <summary>
    /// Country code where the bank is located (ISO 3166-1 alpha-2).
    /// </summary>
    public required string CountryCode { get; init; }

    /// <summary>
    /// Bank branch code if applicable.
    /// </summary>
    public string? BranchCode { get; init; }
}
