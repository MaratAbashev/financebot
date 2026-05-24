using BankingApi.API.Requests;
using BankingApi.Domain.Enums;
using FluentValidation;

namespace BankingApi.API.Validators;

public class CreateAccountRequestValidator : AbstractValidator<CreateAccountRequest>
{
    private static readonly string[] AllowedCurrencies =
        Enum.GetNames<Currency>();

    public CreateAccountRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Название счёта обязательно")
            .MaximumLength(100).WithMessage("Название не должно превышать 100 символов");

        RuleFor(x => x.Currency)
            .NotEmpty().WithMessage("Валюта обязательна")
            .Must(c => AllowedCurrencies.Contains(c))
            .WithMessage($"Допустимые валюты: {string.Join(", ", AllowedCurrencies)}");
    }
}