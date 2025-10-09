using MediatR;
using Microsoft.Extensions.Logging;
using StudentUnionBot.Core.Results;
using StudentUnionBot.Domain.Interfaces;

namespace StudentUnionBot.Application.Notifications.Commands.SendEmailNotification;

/// <summary>
/// Handler для відправки email повідомлень
/// </summary>
public class SendEmailNotificationCommandHandler : IRequestHandler<SendEmailNotificationCommand, Result<bool>>
{
    private readonly IEmailService _emailService;
    private readonly ILogger<SendEmailNotificationCommandHandler> _logger;

    public SendEmailNotificationCommandHandler(
        IEmailService emailService,
        ILogger<SendEmailNotificationCommandHandler> logger)
    {
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<Result<bool>> Handle(SendEmailNotificationCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation(
                "Sending email notification of type {Type} to {EmailCount} recipients",
                request.Type,
                request.ToEmails.Count
            );

            bool result = request.Type switch
            {
                EmailNotificationType.EmailVerification => await SendVerificationEmail(request, cancellationToken),
                EmailNotificationType.NewAppeal => await SendNewAppealEmail(request, cancellationToken),
                EmailNotificationType.AppealReply => await SendAppealReplyEmail(request, cancellationToken),
                EmailNotificationType.NewsNotification => await SendNewsNotificationEmail(request, cancellationToken),
                EmailNotificationType.EventNotification => await SendEventNotificationEmail(request, cancellationToken),
                EmailNotificationType.EventReminder => await SendEventReminderEmail(request, cancellationToken),
                EmailNotificationType.EventRegistrationConfirmation => await SendEventRegistrationConfirmationEmail(request, cancellationToken),
                EmailNotificationType.CustomTemplate => await SendCustomTemplateEmail(request, cancellationToken),
                EmailNotificationType.CustomHtml => await SendCustomHtmlEmail(request, cancellationToken),
                _ => throw new NotSupportedException($"Email notification type {request.Type} is not supported")
            };

            if (result)
            {
                _logger.LogInformation(
                    "Successfully sent email notification of type {Type}",
                    request.Type
                );
                return Result<bool>.Ok(true);
            }
            else
            {
                _logger.LogWarning(
                    "Failed to send email notification of type {Type}",
                    request.Type
                );
                return Result<bool>.Fail("Помилка при відправці email повідомлення");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error sending email notification of type {Type}",
                request.Type
            );
            return Result<bool>.Fail("Помилка при обробці email повідомлення");
        }
    }

    private async Task<bool> SendVerificationEmail(SendEmailNotificationCommand request, CancellationToken cancellationToken)
    {
        var toEmail = request.ToEmails.FirstOrDefault();
        if (string.IsNullOrEmpty(toEmail))
            return false;

        var code = request.TemplateData.GetValueOrDefault("VerificationCode")?.ToString() ?? "";
        
        return await _emailService.SendVerificationCodeAsync(toEmail, code, cancellationToken);
    }

    private async Task<bool> SendNewAppealEmail(SendEmailNotificationCommand request, CancellationToken cancellationToken)
    {
        var toEmail = request.ToEmails.FirstOrDefault();
        if (string.IsNullOrEmpty(toEmail))
            return false;

        var appealId = Convert.ToInt32(request.TemplateData.GetValueOrDefault("AppealId", 0));
        var subject = request.TemplateData.GetValueOrDefault("Subject")?.ToString() ?? "";
        
        return await _emailService.SendNewAppealNotificationAsync(toEmail, appealId, subject, cancellationToken);
    }

    private async Task<bool> SendAppealReplyEmail(SendEmailNotificationCommand request, CancellationToken cancellationToken)
    {
        var toEmail = request.ToEmails.FirstOrDefault();
        if (string.IsNullOrEmpty(toEmail))
            return false;

        var appealId = Convert.ToInt32(request.TemplateData.GetValueOrDefault("AppealId", 0));
        var replyText = request.TemplateData.GetValueOrDefault("ReplyText")?.ToString() ?? "";
        
        return await _emailService.SendAppealReplyNotificationAsync(toEmail, appealId, replyText, cancellationToken);
    }

    private async Task<bool> SendNewsNotificationEmail(SendEmailNotificationCommand request, CancellationToken cancellationToken)
    {
        var newsTitle = request.TemplateData.GetValueOrDefault("NewsTitle")?.ToString() ?? "";
        var newsSummary = request.TemplateData.GetValueOrDefault("NewsSummary")?.ToString() ?? "";
        var newsUrl = request.TemplateData.GetValueOrDefault("NewsUrl")?.ToString() ?? "";

        if (request.ToEmails.Count == 1)
        {
            return await _emailService.SendNewsNotificationAsync(request.ToEmails[0], newsTitle, newsSummary, newsUrl, cancellationToken);
        }
        else
        {
            // Масова розсилка - використовуємо спеціальний метод
            var subject = $"📰 Нова новина: {newsTitle}";
            // TODO: Тут потрібно згенерувати HTML з шаблону для масової розсилки
            return await _emailService.SendBulkEmailAsync(request.ToEmails, subject, "", cancellationToken);
        }
    }

    private async Task<bool> SendEventNotificationEmail(SendEmailNotificationCommand request, CancellationToken cancellationToken)
    {
        var eventTitle = request.TemplateData.GetValueOrDefault("EventTitle")?.ToString() ?? "";
        var eventDate = Convert.ToDateTime(request.TemplateData.GetValueOrDefault("EventDate", DateTime.Now));
        var eventLocation = request.TemplateData.GetValueOrDefault("EventLocation")?.ToString() ?? "";
        var eventUrl = request.TemplateData.GetValueOrDefault("EventUrl")?.ToString() ?? "";

        if (request.ToEmails.Count == 1)
        {
            return await _emailService.SendEventNotificationAsync(request.ToEmails[0], eventTitle, eventDate, eventLocation, eventUrl, cancellationToken);
        }
        else
        {
            // Масова розсилка
            var subject = $"🎉 Нова подія: {eventTitle}";
            // TODO: Тут потрібно згенерувати HTML з шаблону для масової розсилки
            return await _emailService.SendBulkEmailAsync(request.ToEmails, subject, "", cancellationToken);
        }
    }

    private async Task<bool> SendEventReminderEmail(SendEmailNotificationCommand request, CancellationToken cancellationToken)
    {
        var eventTitle = request.TemplateData.GetValueOrDefault("EventTitle")?.ToString() ?? "";
        var eventDate = Convert.ToDateTime(request.TemplateData.GetValueOrDefault("EventDate", DateTime.Now));
        var eventLocation = request.TemplateData.GetValueOrDefault("EventLocation")?.ToString() ?? "";

        var success = true;
        foreach (var email in request.ToEmails)
        {
            var result = await _emailService.SendEventReminderAsync(email, eventTitle, eventDate, eventLocation, cancellationToken);
            if (!result)
                success = false;
        }
        
        return success;
    }

    private async Task<bool> SendEventRegistrationConfirmationEmail(SendEmailNotificationCommand request, CancellationToken cancellationToken)
    {
        var toEmail = request.ToEmails.FirstOrDefault();
        if (string.IsNullOrEmpty(toEmail))
            return false;

        var eventTitle = request.TemplateData.GetValueOrDefault("EventTitle")?.ToString() ?? "";
        var eventDate = Convert.ToDateTime(request.TemplateData.GetValueOrDefault("EventDate", DateTime.Now));
        var eventLocation = request.TemplateData.GetValueOrDefault("EventLocation")?.ToString() ?? "";
        
        return await _emailService.SendEventRegistrationConfirmationAsync(toEmail, eventTitle, eventDate, eventLocation, cancellationToken);
    }

    private async Task<bool> SendCustomTemplateEmail(SendEmailNotificationCommand request, CancellationToken cancellationToken)
    {
        var templateName = request.TemplateData.GetValueOrDefault("TemplateName")?.ToString() ?? "";
        
        var success = true;
        foreach (var email in request.ToEmails)
        {
            var result = await _emailService.SendTemplateEmailAsync(email, templateName, request.TemplateData, cancellationToken);
            if (!result)
                success = false;
        }
        
        return success;
    }

    private async Task<bool> SendCustomHtmlEmail(SendEmailNotificationCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(request.CustomSubject) || string.IsNullOrEmpty(request.CustomHtmlBody))
            return false;
            
        return await _emailService.SendBulkEmailAsync(request.ToEmails, request.CustomSubject, request.CustomHtmlBody, cancellationToken);
    }
}