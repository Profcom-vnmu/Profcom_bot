using MediatR;
using StudentUnionBot.Core.Results;
using StudentUnionBot.Application.News.DTOs;
using StudentUnionBot.Application.Common.Attributes;
using StudentUnionBot.Domain.Enums;

namespace StudentUnionBot.Application.News.Commands.PublishNews;

/// <summary>
/// Command для публікації новини
/// </summary>
[RequirePermission(Permission.PublishNews)]
public class PublishNewsCommand : IRequest<Result<NewsDto>>
{
    /// <summary>
    /// ID новини для публікації
    /// </summary>
    public int NewsId { get; set; }

    /// <summary>
    /// ID адміністратора, який публікує новину
    /// </summary>
    public long PublisherId { get; set; }

    /// <summary>
    /// Чи потрібно відправити push-повідомлення користувачам
    /// </summary>
    public bool SendPushNotification { get; set; } = false;

    /// <summary>
    /// Чи потрібно закріпити новину
    /// </summary>
    public bool PinNews { get; set; } = false;

    /// <summary>
    /// Дата запланованої публікації (null = опублікувати зараз)
    /// </summary>
    public DateTime? ScheduledPublishDate { get; set; }
}