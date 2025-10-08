using FluentValidation;

namespace StudentUnionBot.Application.Users.Commands.VerifyEmail;

/// <summary>
/// Валідатор для VerifyEmailCommand
/// </summary>
public class VerifyEmailCommandValidator : AbstractValidator<VerifyEmailCommand>
{
    public VerifyEmailCommandValidator()
    {
        RuleFor(x => x.TelegramId)
            .GreaterThan(0)
            .WithMessage("Telegram ID повинен бути більше 0");

        RuleFor(x => x.Code)
            .NotEmpty()
            .WithMessage("Код верифікації обов'язковий")
            .Length(6)
            .WithMessage("Код верифікації повинен містити 6 цифр")
            .Matches(@"^\d{6}$")
            .WithMessage("Код верифікації повинен містити тільки цифри");
    }
}
