using FluentValidation;

namespace StudentUnionBot.Application.Notifications.Commands.SendEmailNotification;

/// <summary>
/// Валідатор для SendEmailNotificationCommand
/// </summary>
public class SendEmailNotificationCommandValidator : AbstractValidator<SendEmailNotificationCommand>
{
    public SendEmailNotificationCommandValidator()
    {
        RuleFor(x => x.ToEmails)
            .NotEmpty().WithMessage("Список отримувачів не може бути порожнім")
            .Must(emails => emails.Count <= 100)
            .WithMessage("Не можна відправляти більше 100 email одночасно");

        RuleForEach(x => x.ToEmails)
            .NotEmpty().WithMessage("Email адреса не може бути порожньою")
            .EmailAddress().WithMessage("Невалідна email адреса");

        RuleFor(x => x.Type)
            .IsInEnum().WithMessage("Невалідний тип email повідомлення");

        // Валідація для верифікації email
        When(x => x.Type == EmailNotificationType.EmailVerification, () =>
        {
            RuleFor(x => x.TemplateData)
                .Must(data => data.ContainsKey("VerificationCode"))
                .WithMessage("Для верифікації email потрібен код верифікації");
        });

        // Валідація для звернень
        When(x => x.Type == EmailNotificationType.NewAppeal || x.Type == EmailNotificationType.AppealReply, () =>
        {
            RuleFor(x => x.TemplateData)
                .Must(data => data.ContainsKey("AppealId"))
                .WithMessage("Для email про звернення потрібен ID звернення");
        });

        // Валідація для новин
        When(x => x.Type == EmailNotificationType.NewsNotification, () =>
        {
            RuleFor(x => x.TemplateData)
                .Must(data => data.ContainsKey("NewsTitle") && data.ContainsKey("NewsSummary"))
                .WithMessage("Для email про новину потрібні заголовок та короткий опис");
        });

        // Валідація для подій
        When(x => x.Type == EmailNotificationType.EventNotification ||
                 x.Type == EmailNotificationType.EventReminder ||
                 x.Type == EmailNotificationType.EventRegistrationConfirmation, () =>
        {
            RuleFor(x => x.TemplateData)
                .Must(data => data.ContainsKey("EventTitle") && 
                             data.ContainsKey("EventDate") && 
                             data.ContainsKey("EventLocation"))
                .WithMessage("Для email про подію потрібні назва, дата та місце проведення");
        });

        // Валідація для користувацького шаблону
        When(x => x.Type == EmailNotificationType.CustomTemplate, () =>
        {
            RuleFor(x => x.TemplateData)
                .Must(data => data.ContainsKey("TemplateName"))
                .WithMessage("Для користувацького шаблону потрібна назва шаблону");
        });

        // Валідація для користувацького HTML
        When(x => x.Type == EmailNotificationType.CustomHtml, () =>
        {
            RuleFor(x => x.CustomSubject)
                .NotEmpty().WithMessage("Для користувацького HTML потрібна тема повідомлення");

            RuleFor(x => x.CustomHtmlBody)
                .NotEmpty().WithMessage("Для користувацького HTML потрібен контент повідомлення");
        });

        // Обмеження на розмір шаблонних даних
        RuleFor(x => x.TemplateData)
            .Must(data => data.Count <= 50)
            .WithMessage("Кількість параметрів шаблону не може перевищувати 50");

        // Обмеження на розмір користувацького контенту
        RuleFor(x => x.CustomHtmlBody)
            .MaximumLength(50000).WithMessage("Розмір HTML контенту не може перевищувати 50KB")
            .When(x => !string.IsNullOrEmpty(x.CustomHtmlBody));

        RuleFor(x => x.CustomSubject)
            .MaximumLength(200).WithMessage("Тема повідомлення не може перевищувати 200 символів")
            .When(x => !string.IsNullOrEmpty(x.CustomSubject));
    }
}