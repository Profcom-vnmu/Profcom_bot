using FluentValidation;

namespace StudentUnionBot.Application.News.Commands.UpdateNews;

/// <summary>
/// Валідатор для UpdateNewsCommand
/// </summary>
public class UpdateNewsCommandValidator : AbstractValidator<UpdateNewsCommand>
{
    public UpdateNewsCommandValidator()
    {
        RuleFor(x => x.NewsId)
            .GreaterThan(0).WithMessage("ID новини повинен бути позитивним числом");

        RuleFor(x => x.EditorId)
            .GreaterThan(0).WithMessage("ID редактора повинен бути позитивним числом");

        // Валідація заголовка, якщо він оновлюється
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Заголовок новини не може бути порожнім")
            .MaximumLength(200).WithMessage("Заголовок не може перевищувати 200 символів")
            .Must(title => !string.IsNullOrWhiteSpace(title))
            .WithMessage("Заголовок не може містити тільки пробіли")
            .When(x => x.Title != null);

        // Валідація контенту, якщо він оновлюється
        RuleFor(x => x.Content)
            .NotEmpty().WithMessage("Контент новини не може бути порожнім")
            .MaximumLength(10000).WithMessage("Контент не може перевищувати 10000 символів")
            .Must(content => !string.IsNullOrWhiteSpace(content))
            .WithMessage("Контент не може містити тільки пробіли")
            .When(x => x.Content != null);

        // Валідація короткого опису, якщо він оновлюється
        RuleFor(x => x.Summary)
            .MaximumLength(500).WithMessage("Короткий опис не може перевищувати 500 символів")
            .When(x => !string.IsNullOrEmpty(x.Summary));

        // Валідація категорії, якщо вона оновлюється
        RuleFor(x => x.Category)
            .IsInEnum().WithMessage("Невалідна категорія новини")
            .When(x => x.Category.HasValue);

        // Валідація тегів, якщо вони оновлюються
        RuleFor(x => x.Tags)
            .MaximumLength(200).WithMessage("Теги не можуть перевищувати 200 символів")
            .When(x => !string.IsNullOrEmpty(x.Tags));

        // Валідація файлів, якщо вони оновлюються
        RuleForEach(x => x.AttachmentFileIds)
            .NotEmpty().WithMessage("ID файлу не може бути порожнім")
            .When(x => x.AttachmentFileIds != null && x.AttachmentFileIds.Any());

        RuleFor(x => x.AttachmentFileIds)
            .Must(list => list!.Count <= 10)
            .WithMessage("Не можна додавати більше 10 файлів до однієї новини")
            .When(x => x.AttachmentFileIds != null);

        // Хоча б одне поле має бути для оновлення
        RuleFor(x => x)
            .Must(x => x.Title != null || x.Content != null || x.Summary != null || 
                      x.Category.HasValue || x.Tags != null || x.AttachmentFileIds != null)
            .WithMessage("Має бути вказано хоча б одне поле для оновлення");
    }
}