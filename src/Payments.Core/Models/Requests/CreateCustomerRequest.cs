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

/// <summary>
/// Request to list customers with filters.
/// </summary>
public sealed class CustomerListRequest
{
    /// <summary>
    /// Filter by customer type.
    /// </summary>
    public CustomerType? Type { get; init; }

    /// <summary>
    /// Filter by customer status.
    /// </summary>
    public CustomerStatus? Status { get; init; }

    /// <summary>
    /// Filter by customer role.
    /// </summary>
    public CustomerRole? Role { get; init; }

    /// <summary>
    /// Filter by verification status.
    /// </summary>
    public VerificationStatus? VerificationStatus { get; init; }

    /// <summary>
    /// Search by email or name.
    /// </summary>
    public string? Search { get; init; }

    /// <summary>
    /// Page number (1-based).
    /// </summary>
    public int Page { get; init; } = 1;

    /// <summary>
    /// Page size.
    /// </summary>
    public int PageSize { get; init; } = 20;
}

/// <summary>
/// Request to list payouts with filters.
/// </summary>
public sealed class PayoutListRequest
{
    /// <summary>
    /// Filter by customer ID.
    /// </summary>
    public string? CustomerId { get; init; }

    /// <summary>
    /// Filter by payout status.
    /// </summary>
    public PayoutStatus? Status { get; init; }

    /// <summary>
    /// Filter by fiat currency.
    /// </summary>
    public FiatCurrency? Currency { get; init; }

    /// <summary>
    /// Filter by start date.
    /// </summary>
    public DateTime? FromDate { get; init; }

    /// <summary>
    /// Filter by end date.
    /// </summary>
    public DateTime? ToDate { get; init; }

    /// <summary>
    /// Page number (1-based).
    /// </summary>
    public int Page { get; init; } = 1;

    /// <summary>
    /// Page size.
    /// </summary>
    public int PageSize { get; init; } = 20;
}
