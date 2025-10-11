using FluentValidation;
using StudentUnionBot.Domain.Enums;

namespace StudentUnionBot.Application.News.Commands.CreateNews;

/// <summary>
/// Валідатор для CreateNewsCommand
/// </summary>
public class CreateNewsCommandValidator : AbstractValidator<CreateNewsCommand>
{
    public CreateNewsCommandValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Заголовок новини обов'язковий")
            .MaximumLength(200).WithMessage("Заголовок не може перевищувати 200 символів")
            .Must(title => !string.IsNullOrWhiteSpace(title))
            .WithMessage("Заголовок не може містити тільки пробіли");

        RuleFor(x => x.Content)
            .NotEmpty().WithMessage("Контент новини обов'язковий")
            .MaximumLength(10000).WithMessage("Контент не може перевищувати 10000 символів")
            .Must(content => !string.IsNullOrWhiteSpace(content))
            .WithMessage("Контент не може містити тільки пробіли");

        RuleFor(x => x.Summary)
            .MaximumLength(500).WithMessage("Короткий опис не може перевищувати 500 символів")
            .When(x => !string.IsNullOrEmpty(x.Summary));

        RuleFor(x => x.Category)
            .IsInEnum().WithMessage("Невалідна категорія новини");

        RuleFor(x => x.Language)
            .IsInEnum().WithMessage("Невалідна мова новини");

        RuleFor(x => x.Tags)
            .MaximumLength(200).WithMessage("Теги не можуть перевищувати 200 символів")
            .When(x => !string.IsNullOrEmpty(x.Tags));

        RuleFor(x => x.AuthorId)
            .GreaterThan(0).WithMessage("ID автора повинен бути позитивним числом");

        // Валідація дати публікації
        RuleFor(x => x.ScheduledPublishDate)
            .GreaterThan(DateTime.UtcNow.AddMinutes(5))
            .WithMessage("Дата запланованої публікації має бути принаймні через 5 хвилин")
            .When(x => x.ScheduledPublishDate.HasValue && !x.PublishImmediately);

        // Якщо не публікується одразу, має бути встановлена дата
        RuleFor(x => x.ScheduledPublishDate)
            .NotNull()
            .WithMessage("Якщо новина не публікується одразу, треба вказати дату публікації")
            .When(x => !x.PublishImmediately);

        // Валідація вкладень
        RuleForEach(x => x.Attachments)
            .ChildRules(attachment =>
            {
                attachment.RuleFor(a => a.FileId)
                    .NotEmpty().WithMessage("ID файлу не може бути порожнім");
                attachment.RuleFor(a => a.FileType)
                    .IsInEnum().WithMessage("Невалідний тип файлу");
            })
            .When(x => x.Attachments.Any());

        RuleFor(x => x.Attachments)
            .Must(list => list.Count <= 10)
            .WithMessage("Не можна додавати більше 10 файлів до однієї новини");

        // Спеціальні правила для різних категорій
        When(x => x.Category == NewsCategory.Urgent, () =>
        {
            RuleFor(x => x.PublishImmediately)
                .Equal(true)
                .WithMessage("Термінові новини мають публікуватися одразу");

            RuleFor(x => x.SendPushNotification)
                .Equal(true)
                .WithMessage("Для термінових новин рекомендується увімкнути push-повідомлення");
        });

        When(x => x.Category == NewsCategory.Event, () =>
        {
            RuleFor(x => x.Content)
                .Must(content => content.Contains("дата") || content.Contains("час") || 
                               content.ToLower().Contains("date") || content.ToLower().Contains("time"))
                .WithMessage("Новини про події повинні містити інформацію про дату або час")
                .When(x => x.Language == Language.Ukrainian);
        });
    }
}