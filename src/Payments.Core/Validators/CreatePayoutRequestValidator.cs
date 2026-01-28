using FluentValidation;
using Payments.Core.Enums;
using Payments.Core.Models.Requests;

namespace Payments.Core.Validators;

/// <summary>
/// Validator for CreatePayoutRequest.
/// </summary>
public sealed class CreatePayoutRequestValidator : AbstractValidator<CreatePayoutRequest>
{
    public CreatePayoutRequestValidator()
    {
        RuleFor(x => x.SourceCurrency)
            .IsInEnum()
            .WithMessage("Invalid source currency.");

        RuleFor(x => x.TargetCurrency)
            .IsInEnum()
            .WithMessage("Invalid target currency.");

        RuleFor(x => x.Network)
            .IsInEnum()
            .WithMessage("Invalid blockchain network.");

        RuleFor(x => x.PaymentMethod)
            .IsInEnum()
            .WithMessage("Invalid payment method.");

        RuleFor(x => x)
            .Must(x => x.SourceAmount.HasValue || x.TargetAmount.HasValue)
            .WithMessage("Either SourceAmount or TargetAmount must be specified.");

        RuleFor(x => x.SourceAmount)
            .GreaterThan(0)
            .When(x => x.SourceAmount.HasValue)
            .WithMessage("Source amount must be greater than zero.");

        RuleFor(x => x.TargetAmount)
            .GreaterThan(0)
            .When(x => x.TargetAmount.HasValue)
            .WithMessage("Target amount must be greater than zero.");

        RuleFor(x => x.Sender)
            .NotNull()
            .WithMessage("Sender information is required.")
            .SetValidator(new SenderValidator());

        RuleFor(x => x.Beneficiary)
            .NotNull()
            .WithMessage("Beneficiary information is required.")
            .SetValidator(new BeneficiaryValidator());

        RuleFor(x => x.DeveloperFee)
            .GreaterThanOrEqualTo(0)
            .When(x => x.DeveloperFee.HasValue)
            .WithMessage("Developer fee cannot be negative.");

        RuleFor(x => x.PreferredProvider)
            .IsInEnum()
            .When(x => x.PreferredProvider.HasValue)
            .WithMessage("Invalid provider.");
    }
}

/// <summary>
/// Validator for Sender model.
/// </summary>
public sealed class SenderValidator : AbstractValidator<Models.Sender>
{
    public SenderValidator()
    {
        RuleFor(x => x.Type)
            .IsInEnum()
            .WithMessage("Invalid sender type.");

        RuleFor(x => x.Email)
            .NotEmpty()
            .WithMessage("Sender email is required.")
            .EmailAddress()
            .WithMessage("Invalid email address format.");

        When(x => x.Type == BeneficiaryType.Individual, () =>
        {
            RuleFor(x => x.FirstName)
                .NotEmpty()
                .WithMessage("First name is required for individual senders.");

            RuleFor(x => x.LastName)
                .NotEmpty()
                .WithMessage("Last name is required for individual senders.");
        });

        When(x => x.Type == BeneficiaryType.Business, () =>
        {
            RuleFor(x => x.BusinessName)
                .NotEmpty()
                .WithMessage("Business name is required for business senders.");
        });
    }
}

/// <summary>
/// Validator for Beneficiary model.
/// </summary>
public sealed class BeneficiaryValidator : AbstractValidator<Models.Beneficiary>
{
    public BeneficiaryValidator()
    {
        RuleFor(x => x.Type)
            .IsInEnum()
            .WithMessage("Invalid beneficiary type.");

        RuleFor(x => x.Email)
            .NotEmpty()
            .WithMessage("Beneficiary email is required.")
            .EmailAddress()
            .WithMessage("Invalid email address format.");

        When(x => x.Type == BeneficiaryType.Individual, () =>
        {
            RuleFor(x => x.FirstName)
                .NotEmpty()
                .WithMessage("First name is required for individual beneficiaries.");

            RuleFor(x => x.LastName)
                .NotEmpty()
                .WithMessage("Last name is required for individual beneficiaries.");
        });

        When(x => x.Type == BeneficiaryType.Business, () =>
        {
            RuleFor(x => x.BusinessName)
                .NotEmpty()
                .WithMessage("Business name is required for business beneficiaries.");
        });

        RuleFor(x => x.BankAccount)
            .NotNull()
            .WithMessage("Bank account information is required.")
            .SetValidator(new BankAccountValidator());
    }
}

/// <summary>
/// Validator for BankAccount model.
/// </summary>
public sealed class BankAccountValidator : AbstractValidator<Models.BankAccount>
{
    public BankAccountValidator()
    {
        RuleFor(x => x.BankName)
            .NotEmpty()
            .WithMessage("Bank name is required.");

        RuleFor(x => x.AccountNumber)
            .NotEmpty()
            .WithMessage("Account number is required.");

        RuleFor(x => x.AccountHolderName)
            .NotEmpty()
            .WithMessage("Account holder name is required.");

        RuleFor(x => x.Currency)
            .IsInEnum()
            .WithMessage("Invalid currency.");

        RuleFor(x => x.CountryCode)
            .NotEmpty()
            .WithMessage("Country code is required.")
            .Length(2)
            .WithMessage("Country code must be a 2-letter ISO code.")
            .Matches("^[A-Z]{2}$")
            .WithMessage("Country code must be uppercase letters only.");
    }
}
