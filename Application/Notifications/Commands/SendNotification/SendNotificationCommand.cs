using MediatR;
using StudentUnionBot.Core.Results;
using StudentUnionBot.Domain.Enums;

namespace StudentUnionBot.Application.Notifications.Commands.SendNotification;

/// <summary>
/// Команда для відправки сповіщення
/// </summary>
public class SendNotificationCommand : IRequest<Result>
{
    public long UserId { get; set; } // TelegramId користувача
    public NotificationEvent Event { get; set; }
    public NotificationType Type { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public NotificationPriority Priority { get; set; } = NotificationPriority.Normal;
    public DateTime? ScheduledFor { get; set; }
    public int? RelatedAppealId { get; set; }
    public int? RelatedNewsId { get; set; }
    public int? RelatedEventId { get; set; }
    public Dictionary<string, string>? TemplateData { get; set; }
    public bool UseTemplate { get; set; }

    public SendNotificationCommand(
        long userId,
        NotificationEvent notificationEvent,
        NotificationType type,
        string title,
        string message,
        NotificationPriority priority = NotificationPriority.Normal)
    {
        UserId = userId;
        Event = notificationEvent;
        Type = type;
        Title = title;
        Message = message;
        Priority = priority;
    }

    public SendNotificationCommand() { }
}
