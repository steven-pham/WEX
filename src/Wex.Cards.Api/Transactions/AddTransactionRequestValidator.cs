using FluentValidation;

namespace Wex.Cards.Api.Transactions;

public sealed class AddTransactionRequestValidator : AbstractValidator<AddTransactionRequest>
{
    public AddTransactionRequestValidator()
    {
        RuleFor(x => x.Description)
            .Must(s => !string.IsNullOrWhiteSpace(s)).WithMessage("Description is required.")
            .MaximumLength(200).WithMessage("Description cannot exceed 200 characters.");

        RuleFor(x => x.TransactionDate)
            .NotNull().WithMessage("Transaction date is required.");

        RuleFor(x => x.Amount)
            .NotNull().WithMessage("Amount is required.");

        RuleFor(x => x.Amount)
            .GreaterThan(0m).WithMessage("Amount must be greater than zero.")
            .Must(v => HasAtMostFourDecimalPlaces(v!.Value)).WithMessage("Amount must have at most 4 decimal places.")
            .When(x => x.Amount.HasValue);
    }

    private static bool HasAtMostFourDecimalPlaces(decimal value) =>
        value == Math.Round(value, 4, MidpointRounding.AwayFromZero);
}
