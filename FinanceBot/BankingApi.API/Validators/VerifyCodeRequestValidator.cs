using BankingApi.API.Requests;
using FluentValidation;

namespace BankingApi.API.Validators;

public class VerifyCodeRequestValidator : AbstractValidator<VerifyCodeRequest>
{
    public VerifyCodeRequestValidator()
    {
        RuleFor(x => x.PhoneNumber)
            .NotEmpty().WithMessage("Номер телефона обязателен")
            .Matches(@"^\+?[1-9]\d{7,14}$").WithMessage("Неверный формат номера телефона");

        RuleFor(x => x.Code)
            .NotEmpty().WithMessage("Код обязателен")
            .Length(6).WithMessage("Код должен содержать 6 цифр")
            .Matches(@"^\d+$").WithMessage("Код должен содержать только цифры");
    }
}