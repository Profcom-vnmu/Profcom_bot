using MediatR;
using StudentUnionBot.Core.Results;
using StudentUnionBot.Domain.Enums;
using StudentUnionBot.Application.Common.Attributes;

namespace StudentUnionBot.Application.Notifications.Commands.SendBroadcast;

/// <summary>
/// Command для масової розсилки повідомлень користувачам
/// </summary>
[RequirePermission(Permission.SendBroadcast)]
public class SendBroadcastCommand : IRequest<Result<BroadcastResultDto>>
{
    /// <summary>
    /// Telegram ID адміністратора, який відправляє розсилку
    /// </summary>
    public long AdminTelegramId { get; set; }

    /// <summary>
    /// Текст повідомлення для розсилки
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Тип повідомлення (Push, Email, або обидва)
    /// </summary>
    public NotificationType NotificationType { get; set; } = NotificationType.Push;

    /// <summary>
    /// Цільова аудиторія (null = всі активні користувачі)
    /// </summary>
    public BroadcastAudience? TargetAudience { get; set; }

    /// <summary>
    /// Прикріплений файл (Telegram File ID)
    /// </summary>
    public string? AttachmentFileId { get; set; }

    /// <summary>
    /// Чи відправляти розсилку негайно (false = запланувати)
    /// </summary>
    public bool SendImmediately { get; set; } = true;

    /// <summary>
    /// Запланований час відправки
    /// </summary>
    public DateTime? ScheduledTime { get; set; }
}

/// <summary>
/// Цільова аудиторія для розсилки
/// </summary>
public enum BroadcastAudience
{
    /// <summary>
    /// Всі активні користувачі
    /// </summary>
    AllUsers = 0,

    /// <summary>
    /// Тільки студенти
    /// </summary>
    StudentsOnly = 1,

    /// <summary>
    /// Тільки адміністратори
    /// </summary>
    AdminsOnly = 2,

    /// <summary>
    /// Користувачі з email
    /// </summary>
    UsersWithEmail = 3,

    /// <summary>
    /// Користувачі певної мови
    /// </summary>
    ByLanguage = 4
}

/// <summary>
/// Результат розсилки
/// </summary>
public class BroadcastResultDto
{
    /// <summary>
    /// Кількість успішно відправлених повідомлень
    /// </summary>
    public int SuccessCount { get; set; }

    /// <summary>
    /// Кількість помилок
    /// </summary>
    public int FailureCount { get; set; }

    /// <summary>
    /// Загальна кількість спроб
    /// </summary>
    public int TotalAttempts { get; set; }

    /// <summary>
    /// Час початку розсилки
    /// </summary>
    public DateTime StartedAt { get; set; }

    /// <summary>
    /// Час завершення розсилки
    /// </summary>
    public DateTime CompletedAt { get; set; }

    /// <summary>
    /// Повідомлення про результат
    /// </summary>
    public string Message { get; set; } = string.Empty;
}
