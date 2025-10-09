using FluentValidation;
using StudentUnionBot.Domain.Enums;

namespace StudentUnionBot.Application.Users.Commands.ChangeLanguage;

public class ChangeLanguageCommandValidator : AbstractValidator<ChangeLanguageCommand>
{
    public ChangeLanguageCommandValidator()
    {
        RuleFor(x => x.TelegramId)
            .GreaterThan(0)
            .WithMessage("TelegramId повинен бути більше 0");

        RuleFor(x => x.Language)
            .IsInEnum()
            .WithMessage("Невірна мова");
    }
}