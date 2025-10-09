using FluentValidation;

namespace StudentUnionBot.Application.News.Commands.PublishNews;

/// <summary>
/// Валідатор для PublishNewsCommand
/// </summary>
public class PublishNewsCommandValidator : AbstractValidator<PublishNewsCommand>
{
    public PublishNewsCommandValidator()
    {
        RuleFor(x => x.NewsId)
            .GreaterThan(0).WithMessage("ID новини повинен бути позитивним числом");

        RuleFor(x => x.PublisherId)
            .GreaterThan(0).WithMessage("ID публікувача повинен бути позитивним числом");

        // Валідація дати запланованої публікації
        RuleFor(x => x.ScheduledPublishDate)
            .GreaterThan(DateTime.UtcNow.AddMinutes(1))
            .WithMessage("Дата запланованої публікації має бути в майбутньому")
            .When(x => x.ScheduledPublishDate.HasValue);

        // Для термінових новин рекомендується push-повідомлення
        RuleFor(x => x.SendPushNotification)
            .Equal(true)
            .WithMessage("Рекомендується увімкнути push-повідомлення для важливих новин")
            .When(x => x.PinNews); // Якщо новина закріплюється, то вона важлива
    }
}