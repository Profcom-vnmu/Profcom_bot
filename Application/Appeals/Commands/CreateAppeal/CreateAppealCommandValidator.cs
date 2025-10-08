using FluentValidation;
using StudentUnionBot.Domain.Enums;

namespace StudentUnionBot.Application.Appeals.Commands.CreateAppeal;

/// <summary>
/// Валідатор для CreateAppealCommand
/// </summary>
public class CreateAppealCommandValidator : AbstractValidator<CreateAppealCommand>
{
    public CreateAppealCommandValidator()
    {
        RuleFor(x => x.StudentId)
            .GreaterThan(0)
            .WithMessage("ID студента має бути більше 0");

        RuleFor(x => x.StudentName)
            .NotEmpty()
            .WithMessage("Ім'я студента обов'язкове")
            .MaximumLength(200)
            .WithMessage("Ім'я студента не може перевищувати 200 символів");

        RuleFor(x => x.Category)
            .IsInEnum()
            .WithMessage("Невалідна категорія звернення");

        RuleFor(x => x.Subject)
            .NotEmpty()
            .WithMessage("Тема звернення обов'язкова")
            .MinimumLength(5)
            .WithMessage("Тема звернення має містити принаймні 5 символів")
            .MaximumLength(200)
            .WithMessage("Тема звернення не може перевищувати 200 символів");

        RuleFor(x => x.Message)
            .NotEmpty()
            .WithMessage("Текст звернення обов'язковий")
            .MinimumLength(10)
            .WithMessage("Текст звернення має містити принаймні 10 символів")
            .MaximumLength(4000)
            .WithMessage("Текст звернення не може перевищувати 4000 символів");
    }
}
