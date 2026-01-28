using StablecoinPayments.Core.Enums;

namespace StablecoinPayments.Core.Models.Requests;

public sealed class CreateCustomerRequest
{
    public string? ExternalId { get; set; }
    public required CustomerType Type { get; set; }
    public IndividualInfo? Individual { get; set; }
    public BusinessInfo? Business { get; set; }
    public required ContactInfo Contact { get; set; }
    public AddressInfo? Address { get; set; }
    public Dictionary<string, string>? Metadata { get; set; }
}

public sealed class UpdateCustomerRequest
{
    public IndividualInfo? Individual { get; set; }
    public BusinessInfo? Business { get; set; }
    public ContactInfo? Contact { get; set; }
    public AddressInfo? Address { get; set; }
    public Dictionary<string, string>? Metadata { get; set; }
}

public sealed class ListCustomersRequest
{
    public CustomerType? Type { get; set; }
    public CustomerStatus? Status { get; set; }
    public string? Search { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public sealed class IndividualInfo
{
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public string? MiddleName { get; set; }
    public DateOnly? DateOfBirth { get; set; }
    public string? Nationality { get; set; }
}

public sealed class BusinessInfo
{
    public required string LegalName { get; set; }
    public string? TradingName { get; set; }
    public string? RegistrationNumber { get; set; }
    public string? TaxId { get; set; }
    public string? CountryOfIncorporation { get; set; }
    public string? Website { get; set; }
    public string? Industry { get; set; }
}

public sealed class ContactInfo
{
    public required string Email { get; set; }
    public string? Phone { get; set; }
}

public sealed class AddressInfo
{
    public required string Street1 { get; set; }
    public string? Street2 { get; set; }
    public required string City { get; set; }
    public string? State { get; set; }
    public required string PostalCode { get; set; }
    public required string CountryCode { get; set; }
}
