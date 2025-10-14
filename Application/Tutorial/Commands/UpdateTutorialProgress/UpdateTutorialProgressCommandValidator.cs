using FluentValidation;
using StudentUnionBot.Domain.Enums;

namespace StudentUnionBot.Application.Tutorial.Commands.UpdateTutorialProgress;

/// <summary>
/// Валідатор для команди оновлення прогресу туторіалу
/// </summary>
public class UpdateTutorialProgressCommandValidator : AbstractValidator<UpdateTutorialProgressCommand>
{
    public UpdateTutorialProgressCommandValidator()
    {
        RuleFor(x => x.TelegramId)
            .GreaterThan(0)
            .WithMessage("Telegram ID повинен бути більше 0");
        
        RuleFor(x => x.Step)
            .IsInEnum()
            .WithMessage("Невалідний крок туторіалу");
    }
}
