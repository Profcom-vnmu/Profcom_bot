using FluentValidation;

namespace StudentUnionBot.Application.Users.Commands.SendVerificationEmail;

/// <summary>
/// Валідатор для SendVerificationEmailCommand
/// </summary>
public class SendVerificationEmailCommandValidator : AbstractValidator<SendVerificationEmailCommand>
{
    public SendVerificationEmailCommandValidator()
    {
        RuleFor(x => x.TelegramId)
            .GreaterThan(0)
            .WithMessage("Telegram ID повинен бути більше 0");

        RuleFor(x => x.Email)
            .NotEmpty()
            .WithMessage("Email обов'язковий")
            .EmailAddress()
            .WithMessage("Невірний формат email")
            .MaximumLength(100)
            .WithMessage("Email не може бути довшим за 100 символів");
    }
}
