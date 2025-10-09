using FluentValidation;
using StudentUnionBot.Domain.Enums;

namespace StudentUnionBot.Application.Appeals.Commands.AssignAppeal;

/// <summary>
/// Валідатор для AssignAppealCommand
/// </summary>
public class AssignAppealCommandValidator : AbstractValidator<AssignAppealCommand>
{
    public AssignAppealCommandValidator()
    {
        RuleFor(x => x.AppealId)
            .GreaterThan(0)
            .WithMessage("ID апела має бути більше 0");

        RuleFor(x => x.AssignedByUserId)
            .GreaterThan(0)
            .WithMessage("ID користувача має бути більше 0");

        RuleFor(x => x.AdminId)
            .GreaterThan(0)
            .When(x => x.AdminId.HasValue)
            .WithMessage("ID адміністратора має бути більше 0");

        RuleFor(x => x.Reason)
            .MaximumLength(500)
            .WithMessage("Причина не може перевищувати 500 символів")
            .NotEmpty()
            .When(x => x.AdminId.HasValue && !x.ForceAssignment)
            .WithMessage("Причина обов'язкова для ручного призначення");
    }
}