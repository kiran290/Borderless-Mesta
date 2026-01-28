using StablecoinPayments.Core.Enums;

namespace StablecoinPayments.Core.Models.Requests;

public sealed class CreateQuoteRequest
{
    public required Stablecoin SourceCurrency { get; set; }
    public required FiatCurrency TargetCurrency { get; set; }
    public decimal? SourceAmount { get; set; }
    public decimal? TargetAmount { get; set; }
    public required BlockchainNetwork Network { get; set; }
    public required string DestinationCountry { get; set; }
    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.BankTransfer;
}

public sealed class CreatePayoutRequest
{
    public string? ExternalId { get; set; }
    public string? QuoteId { get; set; }
    public required Stablecoin SourceCurrency { get; set; }
    public required FiatCurrency TargetCurrency { get; set; }
    public decimal? SourceAmount { get; set; }
    public decimal? TargetAmount { get; set; }
    public required BlockchainNetwork Network { get; set; }
    public required PaymentMethod PaymentMethod { get; set; }
    public required SenderInfo Sender { get; set; }
    public required BeneficiaryInfo Beneficiary { get; set; }
    public string? Purpose { get; set; }
    public string? Reference { get; set; }
    public Dictionary<string, string>? Metadata { get; set; }
}

public sealed class ListPayoutsRequest
{
    public PayoutStatus? Status { get; set; }
    public string? CustomerId { get; set; }
    public DateTimeOffset? FromDate { get; set; }
    public DateTimeOffset? ToDate { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public sealed class SenderInfo
{
    public string? Id { get; set; }
    public string? ExternalId { get; set; }
    public BeneficiaryType Type { get; set; } = BeneficiaryType.Individual;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? BusinessName { get; set; }
    public required string Email { get; set; }
    public string? Phone { get; set; }
    public AddressInfo? Address { get; set; }
}

public sealed class BeneficiaryInfo
{
    public string? Id { get; set; }
    public string? ExternalId { get; set; }
    public BeneficiaryType Type { get; set; } = BeneficiaryType.Individual;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? BusinessName { get; set; }
    public required string Email { get; set; }
    public string? Phone { get; set; }
    public AddressInfo? Address { get; set; }
    public required BankAccountInfo BankAccount { get; set; }
}

public sealed class BankAccountInfo
{
    public required string BankName { get; set; }
    public required string AccountNumber { get; set; }
    public required string AccountHolderName { get; set; }
    public string? RoutingNumber { get; set; }
    public string? SwiftCode { get; set; }
    public string? SortCode { get; set; }
    public string? Iban { get; set; }
    public required FiatCurrency Currency { get; set; }
    public required string CountryCode { get; set; }
}
