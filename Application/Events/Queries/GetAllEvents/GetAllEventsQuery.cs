using MediatR;
using StudentUnionBot.Application.Events.DTOs;
using StudentUnionBot.Core.Results;
using StudentUnionBot.Domain.Enums;

namespace StudentUnionBot.Application.Events.Queries.GetAllEvents;

/// <summary>
/// Запит для отримання всіх подій (для адміністраторів)
/// </summary>
public class GetAllEventsQuery : IRequest<Result<EventListDto>>
{
    /// <summary>
    /// Категорія подій (null = всі)
    /// </summary>
    public EventCategory? Category { get; set; }
    
    /// <summary>
    /// Тип подій (null = всі)
    /// </summary>
    public EventType? Type { get; set; }
    
    /// <summary>
    /// Статус подій (null = всі)
    /// </summary>
    public EventStatus? Status { get; set; }
    
    /// <summary>
    /// Кількість записів на сторінці
    /// </summary>
    public int PageSize { get; set; } = 10;
    
    /// <summary>
    /// Номер сторінки (починається з 1)
    /// </summary>
    public int PageNumber { get; set; } = 1;
    
    /// <summary>
    /// Сортування за датою початку (true - від найближчих до віддалених)
    /// </summary>
    public bool SortByDateAsc { get; set; } = true;
}