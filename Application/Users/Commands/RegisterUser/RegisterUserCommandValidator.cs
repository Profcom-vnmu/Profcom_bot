using FluentValidation;

namespace StudentUnionBot.Application.Users.Commands.RegisterUser;

/// <summary>
/// Валідатор для RegisterUserCommand
/// </summary>
public class RegisterUserCommandValidator : AbstractValidator<RegisterUserCommand>
{
    public RegisterUserCommandValidator()
    {
        RuleFor(x => x.TelegramId)
            .GreaterThan(0)
            .WithMessage("Telegram ID має бути більше 0");

        RuleFor(x => x.FirstName)
            .MaximumLength(100)
            .WithMessage("Ім'я не може перевищувати 100 символів");

        RuleFor(x => x.LastName)
            .MaximumLength(100)
            .WithMessage("Прізвище не може перевищувати 100 символів");

        RuleFor(x => x.Username)
            .MaximumLength(100)
            .WithMessage("Username не може перевищувати 100 символів");

        RuleFor(x => x.Language)
            .NotEmpty()
            .WithMessage("Мова обов'язкова")
            .Length(2, 5)
            .WithMessage("Код мови має бути 2-5 символів");
    }
}
