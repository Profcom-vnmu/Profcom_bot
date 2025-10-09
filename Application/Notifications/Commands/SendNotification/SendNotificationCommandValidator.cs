using FluentValidation;

namespace StudentUnionBot.Application.Notifications.Commands.SendNotification;

/// <summary>
/// Валідатор для SendNotificationCommand
/// </summary>
public class SendNotificationCommandValidator : AbstractValidator<SendNotificationCommand>
{
    public SendNotificationCommandValidator()
    {
        RuleFor(x => x.UserId)
            .GreaterThan(0)
            .WithMessage("ID користувача має бути більше нуля");

        When(x => !x.UseTemplate, () =>
        {
            RuleFor(x => x.Title)
                .NotEmpty()
                .WithMessage("Заголовок не може бути порожнім")
                .MaximumLength(200)
                .WithMessage("Заголовок не може перевищувати 200 символів");

            RuleFor(x => x.Message)
                .NotEmpty()
                .WithMessage("Повідомлення не може бути порожнім")
                .MaximumLength(4000)
                .WithMessage("Повідомлення не може перевищувати 4000 символів");
        });

        When(x => x.UseTemplate, () =>
        {
            RuleFor(x => x.TemplateData)
                .NotNull()
                .WithMessage("Дані шаблону не можуть бути порожніми при використанні шаблону");
        });

        When(x => x.ScheduledFor.HasValue, () =>
        {
            RuleFor(x => x.ScheduledFor!.Value)
                .GreaterThan(DateTime.UtcNow)
                .WithMessage("Час відправки має бути в майбутньому");
        });
    }
}
