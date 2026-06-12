using FluentValidation;

namespace Wex.Cards.Application.Cards.Commands;

public sealed class CreateCardCommandValidator : AbstractValidator<CreateCardCommand>
{
    public CreateCardCommandValidator()
    {
        RuleFor(x => x.CreditLimit)
            .NotNull().WithMessage("Credit limit is required.");

        RuleFor(x => x.CreditLimit)
            .GreaterThanOrEqualTo(0m).WithMessage("Credit limit cannot be negative.")
            .When(x => x.CreditLimit.HasValue);

        RuleFor(x => x.CreditLimit)
            .Must(v => HasAtMostTwoDecimalPlaces(v!.Value)).WithMessage("Credit limit must have at most 2 decimal places.")
            .When(x => x.CreditLimit.HasValue);
    }

    private static bool HasAtMostTwoDecimalPlaces(decimal value) =>
        value == Math.Round(value, 2, MidpointRounding.AwayFromZero);
}
