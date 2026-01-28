using Payments.Core.Enums;

namespace Payments.Core.Models.Requests;

/// <summary>
/// Request to create a new customer.
/// </summary>
public sealed class CreateCustomerRequest
{
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
    /// Individual customer details (required if Type is Individual).
    /// </summary>
    public IndividualDetails? Individual { get; init; }

    /// <summary>
    /// Business customer details (required if Type is Business).
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
    /// Bank accounts to associate with the customer.
    /// </summary>
    public IReadOnlyList<BankAccount>? BankAccounts { get; init; }

    /// <summary>
    /// Preferred provider for creating the customer.
    /// </summary>
    public PayoutProvider? PreferredProvider { get; init; }

    /// <summary>
    /// Additional metadata for the customer.
    /// </summary>
    public Dictionary<string, string>? Metadata { get; init; }
}

/// <summary>
/// Request to update an existing customer.
/// </summary>
public sealed class UpdateCustomerRequest
{
    /// <summary>
    /// Individual customer details updates.
    /// </summary>
    public IndividualDetails? Individual { get; init; }

    /// <summary>
    /// Business customer details updates.
    /// </summary>
    public BusinessDetails? Business { get; init; }

    /// <summary>
    /// Contact information updates.
    /// </summary>
    public ContactInfo? Contact { get; init; }

    /// <summary>
    /// Address updates.
    /// </summary>
    public Address? Address { get; init; }

    /// <summary>
    /// Metadata updates.
    /// </summary>
    public Dictionary<string, string>? Metadata { get; init; }
}

/// <summary>
/// Request to add a bank account to a customer.
/// </summary>
public sealed class AddBankAccountRequest
{
    /// <summary>
    /// Bank account details.
    /// </summary>
    public required BankAccount BankAccount { get; init; }

    /// <summary>
    /// Set as primary bank account.
    /// </summary>
    public bool SetAsPrimary { get; init; }
}
