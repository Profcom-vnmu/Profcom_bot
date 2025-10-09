using MediatR;
using StudentUnionBot.Core.Results;
using StudentUnionBot.Application.Events.DTOs;
using StudentUnionBot.Domain.Enums;

namespace StudentUnionBot.Application.Events.Commands.CreateEvent;

/// <summary>
/// Command для створення нової події
/// </summary>
public class CreateEventCommand : IRequest<Result<EventDto>>
{
    /// <summary>
    /// Назва події
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Детальний опис події
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Короткий опис для превʼю
    /// </summary>
    public string? Summary { get; set; }

    /// <summary>
    /// Дата та час проведення події
    /// </summary>
    public DateTime EventDate { get; set; }

    /// <summary>
    /// Дата закінчення події (якщо багатоденна)
    /// </summary>
    public DateTime? EndDate { get; set; }

    /// <summary>
    /// Місце проведення
    /// </summary>
    public string Location { get; set; } = string.Empty;

    /// <summary>
    /// Категорія події
    /// </summary>
    public EventCategory Category { get; set; }

    /// <summary>
    /// Тип події
    /// </summary>
    public EventType Type { get; set; }

    /// <summary>
    /// Максимальна кількість учасників (null = без обмежень)
    /// </summary>
    public int? MaxParticipants { get; set; }

    /// <summary>
    /// Чи потрібна реєстрація для участі
    /// </summary>
    public bool RequiresRegistration { get; set; } = false;

    /// <summary>
    /// Дедлайн реєстрації
    /// </summary>
    public DateTime? RegistrationDeadline { get; set; }

    /// <summary>
    /// Вартість участі (0 = безкоштовно)
    /// </summary>
    public decimal Price { get; set; } = 0;

    /// <summary>
    /// Валюта (за замовчуванням UAH)
    /// </summary>
    public string Currency { get; set; } = "UAH";

    /// <summary>
    /// Вимоги до учасників
    /// </summary>
    public string? Requirements { get; set; }

    /// <summary>
    /// Контактна інформація організаторів
    /// </summary>
    public string? ContactInfo { get; set; }

    /// <summary>
    /// Теги події (через кому)
    /// </summary>
    public string? Tags { get; set; }

    /// <summary>
    /// ID адміністратора-організатора
    /// </summary>
    public long OrganizerId { get; set; }

    /// <summary>
    /// Мова події
    /// </summary>
    public Language Language { get; set; } = Language.Ukrainian;

    /// <summary>
    /// Додаткові файли (фото, програма заходу тощо)
    /// </summary>
    public List<string> AttachmentFileIds { get; set; } = new();

    /// <summary>
    /// Чи потрібно опублікувати подію одразу
    /// </summary>
    public bool PublishImmediately { get; set; } = true;

    /// <summary>
    /// Чи потрібно відправити повідомлення про нову подію
    /// </summary>
    public bool SendNotification { get; set; } = false;
}