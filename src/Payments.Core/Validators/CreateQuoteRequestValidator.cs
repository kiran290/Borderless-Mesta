using FluentValidation;
using Payments.Core.Models.Requests;

namespace Payments.Core.Validators;

/// <summary>
/// Validator for CreateQuoteRequest.
/// </summary>
public sealed class CreateQuoteRequestValidator : AbstractValidator<CreateQuoteRequest>
{
    public CreateQuoteRequestValidator()
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

        RuleFor(x => x.DestinationCountry)
            .NotEmpty()
            .WithMessage("Destination country is required.")
            .Length(2)
            .WithMessage("Destination country must be a 2-letter ISO code.")
            .Matches("^[A-Z]{2}$")
            .WithMessage("Destination country must be uppercase letters only.");

        RuleFor(x => x.PaymentMethod)
            .IsInEnum()
            .When(x => x.PaymentMethod.HasValue)
            .WithMessage("Invalid payment method.");

        RuleFor(x => x.DeveloperFee)
            .GreaterThanOrEqualTo(0)
            .When(x => x.DeveloperFee.HasValue)
            .WithMessage("Developer fee cannot be negative.");
    }
}
