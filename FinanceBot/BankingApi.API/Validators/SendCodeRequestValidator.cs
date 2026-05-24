using BankingApi.API.Requests;
using FluentValidation;

namespace BankingApi.API.Validators;

public class SendCodeRequestValidator : AbstractValidator<SendCodeRequest>
{
    public SendCodeRequestValidator()
    {
        RuleFor(x => x.PhoneNumber)
            .NotEmpty().WithMessage("Номер телефона обязателен")
            .Matches(@"^\+?[1-9]\d{7,14}$").WithMessage("Неверный формат номера телефона");
    }
}