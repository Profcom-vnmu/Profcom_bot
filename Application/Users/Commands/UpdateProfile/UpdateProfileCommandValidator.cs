using FluentValidation;

namespace StudentUnionBot.Application.Users.Commands.UpdateProfile;

/// <summary>
/// Валідатор для команди оновлення профілю
/// </summary>
public class UpdateProfileCommandValidator : AbstractValidator<UpdateProfileCommand>
{
    public UpdateProfileCommandValidator()
    {
        RuleFor(x => x.TelegramId)
            .GreaterThan(0)
            .WithMessage("TelegramId має бути більше 0");

        When(x => !string.IsNullOrWhiteSpace(x.FullName), () =>
        {
            RuleFor(x => x.FullName)
                .MaximumLength(100)
                .WithMessage("Повне ім'я не може бути довшим за 100 символів")
                .Must(name => !string.IsNullOrWhiteSpace(name?.Trim()))
                .WithMessage("Повне ім'я не може бути порожнім");
        });

        When(x => !string.IsNullOrWhiteSpace(x.Faculty), () =>
        {
            RuleFor(x => x.Faculty)
                .MaximumLength(200)
                .WithMessage("Назва факультету не може бути довшою за 200 символів")
                .Must(faculty => !string.IsNullOrWhiteSpace(faculty?.Trim()))
                .WithMessage("Назва факультету не може бути порожньою");
        });

        When(x => x.Course.HasValue, () =>
        {
            RuleFor(x => x.Course)
                .GreaterThan(0)
                .WithMessage("Курс має бути більше 0")
                .LessThanOrEqualTo(6)
                .WithMessage("Курс не може бути більше 6");
        });

        When(x => !string.IsNullOrWhiteSpace(x.Group), () =>
        {
            RuleFor(x => x.Group)
                .MaximumLength(50)
                .WithMessage("Назва групи не може бути довшою за 50 символів")
                .Must(group => !string.IsNullOrWhiteSpace(group?.Trim()))
                .WithMessage("Назва групи не може бути порожньою");
        });
    }
}