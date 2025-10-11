using FluentValidation;
using StudentUnionBot.Domain.Enums;

namespace StudentUnionBot.Application.Events.Commands.CreateEvent;

/// <summary>
/// Валідатор для CreateEventCommand
/// </summary>
public class CreateEventCommandValidator : AbstractValidator<CreateEventCommand>
{
    public CreateEventCommandValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Назва події обов'язкова")
            .MaximumLength(200).WithMessage("Назва не може перевищувати 200 символів")
            .Must(title => !string.IsNullOrWhiteSpace(title))
            .WithMessage("Назва не може містити тільки пробіли");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Опис події обов'язковий")
            .MaximumLength(5000).WithMessage("Опис не може перевищувати 5000 символів")
            .Must(description => !string.IsNullOrWhiteSpace(description))
            .WithMessage("Опис не може містити тільки пробіли");

        RuleFor(x => x.Summary)
            .MaximumLength(500).WithMessage("Короткий опис не може перевищувати 500 символів")
            .When(x => !string.IsNullOrEmpty(x.Summary));

        RuleFor(x => x.EventDate)
            .GreaterThan(DateTime.UtcNow.AddMinutes(30))
            .WithMessage("Дата події має бути принаймні через 30 хвилин");

        RuleFor(x => x.EndDate)
            .GreaterThan(x => x.EventDate)
            .WithMessage("Дата закінчення має бути після дати початку")
            .When(x => x.EndDate.HasValue);

        RuleFor(x => x.Location)
            .NotEmpty().WithMessage("Місце проведення обов'язкове")
            .MaximumLength(300).WithMessage("Місце проведення не може перевищувати 300 символів");

        RuleFor(x => x.Category)
            .IsInEnum().WithMessage("Невалідна категорія події");

        RuleFor(x => x.Type)
            .IsInEnum().WithMessage("Невалідний тип події");

        RuleFor(x => x.MaxParticipants)
            .GreaterThan(0).WithMessage("Максимальна кількість учасників має бути позитивним числом")
            .When(x => x.MaxParticipants.HasValue);

        RuleFor(x => x.RegistrationDeadline)
            .LessThanOrEqualTo(x => x.EventDate)
            .WithMessage("Дедлайн реєстрації має бути до початку події")
            .GreaterThan(DateTime.UtcNow)
            .WithMessage("Дедлайн реєстрації має бути в майбутньому")
            .When(x => x.RegistrationDeadline.HasValue);

        // Якщо потрібна реєстрація, має бути встановлений дедлайн
        RuleFor(x => x.RegistrationDeadline)
            .NotNull()
            .WithMessage("Для події з реєстрацією треба вказати дедлайн реєстрації")
            .When(x => x.RequiresRegistration);

        RuleFor(x => x.Price)
            .GreaterThanOrEqualTo(0).WithMessage("Вартість не може бути від'ємною");

        RuleFor(x => x.Currency)
            .NotEmpty().WithMessage("Валюта обов'язкова")
            .Length(3).WithMessage("Валюта має бути 3-символьним кодом")
            .When(x => x.Price > 0);

        RuleFor(x => x.Requirements)
            .MaximumLength(1000).WithMessage("Вимоги не можуть перевищувати 1000 символів")
            .When(x => !string.IsNullOrEmpty(x.Requirements));

        RuleFor(x => x.ContactInfo)
            .MaximumLength(500).WithMessage("Контактна інформація не може перевищувати 500 символів")
            .When(x => !string.IsNullOrEmpty(x.ContactInfo));

        RuleFor(x => x.Tags)
            .MaximumLength(300).WithMessage("Теги не можуть перевищувати 300 символів")
            .When(x => !string.IsNullOrEmpty(x.Tags));

        RuleFor(x => x.OrganizerId)
            .GreaterThan(0).WithMessage("ID організатора повинен бути позитивним числом");

        RuleFor(x => x.Language)
            .IsInEnum().WithMessage("Невалідна мова події");

        // Валідація файлів
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
            .Must(list => list.Count <= 5)
            .WithMessage("Не можна додавати більше 5 файлів до однієї події");

        // Спеціальні правила для різних типів подій
        When(x => x.Type == EventType.Educational, () =>
        {
            RuleFor(x => x.Requirements)
                .NotEmpty()
                .WithMessage("Для освітніх заходів рекомендується вказати вимоги до учасників");
        });

        When(x => x.Type == EventType.Sports, () =>
        {
            RuleFor(x => x.MaxParticipants)
                .NotNull()
                .WithMessage("Для спортивних заходів рекомендується обмежити кількість учасників");
        });

        When(x => x.Price > 0, () =>
        {
            RuleFor(x => x.ContactInfo)
                .NotEmpty()
                .WithMessage("Для платних заходів треба вказати контактну інформацію для оплати");
        });
    }
}