using MediatR;
using StudentUnionBot.Application.Events.DTOs;
using StudentUnionBot.Core.Results;
using StudentUnionBot.Domain.Enums;

namespace StudentUnionBot.Application.Events.Queries.GetUpcomingEvents;

/// <summary>
/// Query для отримання майбутніх подій
/// </summary>
public class GetUpcomingEventsQuery : IRequest<Result<EventListDto>>
{
    /// <summary>
    /// Фільтр за типом події
    /// </summary>
    public EventType? Type { get; set; }

    /// <summary>
    /// Тільки виділені (featured) події
    /// </summary>
    public bool OnlyFeatured { get; set; }

    /// <summary>
    /// Номер сторінки
    /// </summary>
    public int PageNumber { get; set; } = 1;

    /// <summary>
    /// Розмір сторінки
    /// </summary>
    public int PageSize { get; set; } = 10;
}
