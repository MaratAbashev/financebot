using BankingApi.API.Requests;
using BankingApi.Domain.Enums;
using FluentValidation;

namespace BankingApi.API.Validators;

public class CreateTransactionRequestValidator : AbstractValidator<CreateTransactionRequest>
{
    private static readonly string[] AllowedTypes =
        Enum.GetNames<TransactionType>();

    public CreateTransactionRequestValidator()
    {
        RuleFor(x => x.CategoryId)
            .NotEmpty().WithMessage("Категория обязательна");

        RuleFor(x => x.Amount)
            .GreaterThan(0).WithMessage("Сумма должна быть больше нуля");

        RuleFor(x => x.Type)
            .NotEmpty().WithMessage("Тип транзакции обязателен")
            .Must(t => AllowedTypes.Contains(t))
            .WithMessage($"Допустимые типы: {string.Join(", ", AllowedTypes)}");

        RuleFor(x => x.Description)
            .MaximumLength(500).WithMessage("Описание не должно превышать 500 символов")
            .When(x => x.Description is not null);
    }
}