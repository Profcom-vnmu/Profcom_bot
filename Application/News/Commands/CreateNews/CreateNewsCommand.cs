using MediatR;
using StudentUnionBot.Core.Results;
using StudentUnionBot.Application.News.DTOs;
using StudentUnionBot.Domain.Enums;
using StudentUnionBot.Application.Common.Attributes;
using StudentUnionBot.Application.Common.Models;

namespace StudentUnionBot.Application.News.Commands.CreateNews;

/// <summary>
/// Command для створення нової новини
/// </summary>
[RequirePermission(Permission.CreateNews)]
[RateLimit("CreateNews")]
public class CreateNewsCommand : IRequest<Result<NewsDto>>
{
    /// <summary>
    /// Заголовок новини
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Контент новини (HTML підтримується)
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Короткий опис для превʼю
    /// </summary>
    public string? Summary { get; set; }

    /// <summary>
    /// Категорія новини
    /// </summary>
    public NewsCategory Category { get; set; }

    /// <summary>
    /// Мова новини
    /// </summary>
    public Language Language { get; set; } = Language.Ukrainian;

    /// <summary>
    /// Теги для новини (через кому)
    /// </summary>
    public string? Tags { get; set; }

    /// <summary>
    /// ID адміністратора, який створює новину
    /// </summary>
    public long AuthorId { get; set; }

    /// <summary>
    /// Чи потрібно опублікувати новину одразу
    /// </summary>
    public bool PublishImmediately { get; set; } = false;

    /// <summary>
    /// Дата запланованої публікації (якщо не публікується одразу)
    /// </summary>
    public DateTime? ScheduledPublishDate { get; set; }

    /// <summary>
    /// Чи потрібно відправити push-повідомлення користувачам
    /// </summary>
    public bool SendPushNotification { get; set; } = false;

    /// <summary>
    /// Прикріплені файли (фото, документи, відео)
    /// </summary>
    public List<FileAttachmentDto> Attachments { get; set; } = new();
}