using System.ComponentModel.DataAnnotations;
using Payments.Core.Enums;
using Payments.Core.Models;

namespace Payments.Api.Dtos;

#region Customer Request DTOs

/// <summary>
/// Request DTO for creating a customer.
/// </summary>
public sealed class CreateCustomerRequestDto
{
    /// <summary>
    /// External reference ID for the customer.
    /// </summary>
    [StringLength(100)]
    public string? ExternalId { get; init; }

    /// <summary>
    /// Type of customer (individual or business).
    /// </summary>
    [Required]
    public required CustomerType Type { get; init; }

    /// <summary>
    /// Role of the customer in transactions.
    /// </summary>
    [Required]
    public required CustomerRole Role { get; init; }

    /// <summary>
    /// Individual customer details (required if Type is Individual).
    /// </summary>
    public IndividualDetailsDto? Individual { get; init; }

    /// <summary>
    /// Business customer details (required if Type is Business).
    /// </summary>
    public BusinessDetailsDto? Business { get; init; }

    /// <summary>
    /// Contact information.
    /// </summary>
    [Required]
    public required ContactInfoDto Contact { get; init; }

    /// <summary>
    /// Customer's address.
    /// </summary>
    public AddressDto? Address { get; init; }

    /// <summary>
    /// Bank accounts to associate with the customer.
    /// </summary>
    public List<BankAccountDto>? BankAccounts { get; init; }

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
/// Request DTO for updating a customer.
/// </summary>
public sealed class UpdateCustomerRequestDto
{
    /// <summary>
    /// Individual customer details updates.
    /// </summary>
    public IndividualDetailsDto? Individual { get; init; }

    /// <summary>
    /// Business customer details updates.
    /// </summary>
    public BusinessDetailsDto? Business { get; init; }

    /// <summary>
    /// Contact information updates.
    /// </summary>
    public ContactInfoDto? Contact { get; init; }

    /// <summary>
    /// Address updates.
    /// </summary>
    public AddressDto? Address { get; init; }

    /// <summary>
    /// Metadata updates.
    /// </summary>
    public Dictionary<string, string>? Metadata { get; init; }
}

/// <summary>
/// Request DTO for adding a bank account.
/// </summary>
public sealed class AddBankAccountRequestDto
{
    /// <summary>
    /// Bank account details.
    /// </summary>
    [Required]
    public required BankAccountDto BankAccount { get; init; }

    /// <summary>
    /// Set as primary bank account.
    /// </summary>
    public bool SetAsPrimary { get; init; }
}

/// <summary>
/// Individual details DTO.
/// </summary>
public sealed class IndividualDetailsDto
{
    [Required]
    public required string FirstName { get; init; }

    public string? MiddleName { get; init; }

    [Required]
    public required string LastName { get; init; }

    public DateOnly? DateOfBirth { get; init; }

    [StringLength(2)]
    public string? CountryOfBirth { get; init; }

    [StringLength(2)]
    public string? Nationality { get; init; }

    public string? Gender { get; init; }

    public string? Occupation { get; init; }

    public string? TaxId { get; init; }
}

/// <summary>
/// Business details DTO.
/// </summary>
public sealed class BusinessDetailsDto
{
    [Required]
    public required string LegalName { get; init; }

    public string? TradingName { get; init; }

    public string? RegistrationNumber { get; init; }

    public string? TaxId { get; init; }

    public string? VatNumber { get; init; }

    [Required]
    [StringLength(2)]
    public required string CountryOfIncorporation { get; init; }

    public DateOnly? DateOfIncorporation { get; init; }

    public string? EntityType { get; init; }

    public string? Industry { get; init; }

    public string? Website { get; init; }

    public string? Description { get; init; }

    public AddressDto? RegisteredAddress { get; init; }

    public List<BeneficialOwnerDto>? BeneficialOwners { get; init; }

    public List<DirectorDto>? Directors { get; init; }
}

/// <summary>
/// Contact info DTO.
/// </summary>
public sealed class ContactInfoDto
{
    [Required]
    [EmailAddress]
    public required string Email { get; init; }

    public string? Phone { get; init; }

    public string? Mobile { get; init; }
}

/// <summary>
/// Beneficial owner DTO.
/// </summary>
public sealed class BeneficialOwnerDto
{
    [Required]
    public required string FirstName { get; init; }

    [Required]
    public required string LastName { get; init; }

    public DateOnly? DateOfBirth { get; init; }

    [StringLength(2)]
    public string? Nationality { get; init; }

    [Required]
    [Range(0.01, 100)]
    public required decimal OwnershipPercentage { get; init; }

    public AddressDto? Address { get; init; }

    public string? DocumentType { get; init; }

    public string? DocumentNumber { get; init; }
}

/// <summary>
/// Director DTO.
/// </summary>
public sealed class DirectorDto
{
    [Required]
    public required string FirstName { get; init; }

    [Required]
    public required string LastName { get; init; }

    public DateOnly? DateOfBirth { get; init; }

    [StringLength(2)]
    public string? Nationality { get; init; }

    public string? Title { get; init; }

    public DateOnly? AppointedDate { get; init; }

    public AddressDto? Address { get; init; }
}

#endregion

#region Customer Response DTOs

/// <summary>
/// Customer response DTO.
/// </summary>
public sealed class CustomerResponseDto
{
    public required string Id { get; init; }
    public string? ExternalId { get; init; }
    public required CustomerType Type { get; init; }
    public required CustomerRole Role { get; init; }
    public required CustomerStatus Status { get; init; }
    public string? DisplayName { get; init; }
    public IndividualDetailsDto? Individual { get; init; }
    public BusinessDetailsDto? Business { get; init; }
    public required ContactInfoDto Contact { get; init; }
    public AddressDto? Address { get; init; }
    public List<BankAccountDto>? BankAccounts { get; init; }
    public VerificationInfoDto? Verification { get; init; }
    public required DateTimeOffset CreatedAt { get; init; }
    public required DateTimeOffset UpdatedAt { get; init; }

    public static CustomerResponseDto FromModel(Customer customer)
    {
        return new CustomerResponseDto
        {
            Id = customer.Id,
            ExternalId = customer.ExternalId,
            Type = customer.Type,
            Role = customer.Role,
            Status = customer.Status,
            DisplayName = customer.DisplayName,
            Individual = customer.Individual != null ? new IndividualDetailsDto
            {
                FirstName = customer.Individual.FirstName,
                MiddleName = customer.Individual.MiddleName,
                LastName = customer.Individual.LastName,
                DateOfBirth = customer.Individual.DateOfBirth,
                CountryOfBirth = customer.Individual.CountryOfBirth,
                Nationality = customer.Individual.Nationality,
                Gender = customer.Individual.Gender,
                Occupation = customer.Individual.Occupation,
                TaxId = customer.Individual.TaxId
            } : null,
            Business = customer.Business != null ? new BusinessDetailsDto
            {
                LegalName = customer.Business.LegalName,
                TradingName = customer.Business.TradingName,
                RegistrationNumber = customer.Business.RegistrationNumber,
                TaxId = customer.Business.TaxId,
                VatNumber = customer.Business.VatNumber,
                CountryOfIncorporation = customer.Business.CountryOfIncorporation,
                DateOfIncorporation = customer.Business.DateOfIncorporation,
                EntityType = customer.Business.EntityType,
                Industry = customer.Business.Industry,
                Website = customer.Business.Website,
                Description = customer.Business.Description
            } : null,
            Contact = new ContactInfoDto
            {
                Email = customer.Contact.Email,
                Phone = customer.Contact.Phone,
                Mobile = customer.Contact.Mobile
            },
            Address = customer.Address != null ? new AddressDto
            {
                Street1 = customer.Address.Street1,
                Street2 = customer.Address.Street2,
                City = customer.Address.City,
                State = customer.Address.State,
                PostalCode = customer.Address.PostalCode,
                CountryCode = customer.Address.CountryCode
            } : null,
            BankAccounts = customer.BankAccounts.Select(ba => new BankAccountDto
            {
                BankName = ba.BankName,
                AccountNumber = ba.AccountNumber,
                AccountHolderName = ba.AccountHolderName,
                RoutingNumber = ba.RoutingNumber,
                SwiftCode = ba.SwiftCode,
                SortCode = ba.SortCode,
                Iban = ba.Iban,
                Currency = ba.Currency,
                CountryCode = ba.CountryCode,
                BranchCode = ba.BranchCode
            }).ToList(),
            Verification = customer.Verification != null ? VerificationInfoDto.FromModel(customer.Verification) : null,
            CreatedAt = customer.CreatedAt,
            UpdatedAt = customer.UpdatedAt
        };
    }
}

/// <summary>
/// Verification info response DTO.
/// </summary>
public sealed class VerificationInfoDto
{
    public required VerificationStatus Status { get; init; }
    public required VerificationLevel Level { get; init; }
    public bool KycCompleted { get; init; }
    public bool KybCompleted { get; init; }
    public string? RejectionReason { get; init; }
    public int? RiskScore { get; init; }
    public string? RiskLevel { get; init; }
    public DateTimeOffset? SubmittedAt { get; init; }
    public DateTimeOffset? CompletedAt { get; init; }
    public DateTimeOffset? ExpiresAt { get; init; }
    public List<VerificationCheckDto>? Checks { get; init; }
    public List<VerificationDocumentDto>? Documents { get; init; }

    public static VerificationInfoDto FromModel(VerificationInfo info)
    {
        return new VerificationInfoDto
        {
            Status = info.Status,
            Level = info.Level,
            KycCompleted = info.KycCompleted,
            KybCompleted = info.KybCompleted,
            RejectionReason = info.RejectionReason,
            RiskScore = info.RiskScore,
            RiskLevel = info.RiskLevel,
            SubmittedAt = info.SubmittedAt,
            CompletedAt = info.CompletedAt,
            ExpiresAt = info.ExpiresAt,
            Checks = info.Checks.Select(c => new VerificationCheckDto
            {
                Id = c.Id,
                CheckType = c.CheckType,
                Status = c.Status,
                Result = c.Result,
                Details = c.Details,
                PerformedAt = c.PerformedAt
            }).ToList(),
            Documents = info.Documents.Select(d => new VerificationDocumentDto
            {
                Id = d.Id,
                Type = d.Type,
                Status = d.Status,
                DocumentNumber = d.DocumentNumber,
                IssuingCountry = d.IssuingCountry,
                IssueDate = d.IssueDate,
                ExpiryDate = d.ExpiryDate,
                RejectionReason = d.RejectionReason,
                UploadedAt = d.UploadedAt,
                VerifiedAt = d.VerifiedAt
            }).ToList()
        };
    }
}

/// <summary>
/// Verification check DTO.
/// </summary>
public sealed class VerificationCheckDto
{
    public required string Id { get; init; }
    public required string CheckType { get; init; }
    public required VerificationStatus Status { get; init; }
    public string? Result { get; init; }
    public string? Details { get; init; }
    public required DateTimeOffset PerformedAt { get; init; }
}

/// <summary>
/// Verification document DTO.
/// </summary>
public sealed class VerificationDocumentDto
{
    public required string Id { get; init; }
    public required DocumentType Type { get; init; }
    public required VerificationStatus Status { get; init; }
    public string? DocumentNumber { get; init; }
    public string? IssuingCountry { get; init; }
    public DateOnly? IssueDate { get; init; }
    public DateOnly? ExpiryDate { get; init; }
    public string? RejectionReason { get; init; }
    public required DateTimeOffset UploadedAt { get; init; }
    public DateTimeOffset? VerifiedAt { get; init; }
}

#endregion
