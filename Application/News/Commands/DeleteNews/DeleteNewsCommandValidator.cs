using FluentValidation;

namespace StudentUnionBot.Application.News.Commands.DeleteNews;

/// <summary>
/// Валідатор для DeleteNewsCommand
/// </summary>
public class DeleteNewsCommandValidator : AbstractValidator<DeleteNewsCommand>
{
    public DeleteNewsCommandValidator()
    {
        RuleFor(x => x.NewsId)
            .GreaterThan(0).WithMessage("ID новини повинен бути позитивним числом");

        RuleFor(x => x.DeleterId)
            .GreaterThan(0).WithMessage("ID того, хто видаляє, повинен бути позитивним числом");

        RuleFor(x => x.DeletionReason)
            .MaximumLength(500).WithMessage("Причина видалення не може перевищувати 500 символів")
            .When(x => !string.IsNullOrEmpty(x.DeletionReason));

        // Рекомендується вказувати причину видалення опублікованих новин
        RuleFor(x => x.DeletionReason)
            .NotEmpty()
            .WithMessage("Рекомендується вказати причину видалення")
            .When(x => !x.ArchiveInsteadOfDelete); // Якщо повністю видаляємо
    }
}