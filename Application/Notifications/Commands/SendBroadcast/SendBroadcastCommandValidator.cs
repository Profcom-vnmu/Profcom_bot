using FluentValidation;

namespace StudentUnionBot.Application.Notifications.Commands.SendBroadcast;

/// <summary>
/// Валідатор для SendBroadcastCommand
/// </summary>
public class SendBroadcastCommandValidator : AbstractValidator<SendBroadcastCommand>
{
    public SendBroadcastCommandValidator()
    {
        RuleFor(x => x.AdminTelegramId)
            .GreaterThan(0)
            .WithMessage("ID адміністратора повинен бути більше 0");

        RuleFor(x => x.Message)
            .NotEmpty()
            .WithMessage("Повідомлення не може бути порожнім")
            .MaximumLength(4096)
            .WithMessage("Повідомлення не може перевищувати 4096 символів (обмеження Telegram)");

        RuleFor(x => x.NotificationType)
            .IsInEnum()
            .WithMessage("Невірний тип повідомлення");

        RuleFor(x => x.ScheduledTime)
            .GreaterThan(DateTime.UtcNow)
            .When(x => !x.SendImmediately && x.ScheduledTime.HasValue)
            .WithMessage("Запланований час має бути в майбутньому");
    }
}
