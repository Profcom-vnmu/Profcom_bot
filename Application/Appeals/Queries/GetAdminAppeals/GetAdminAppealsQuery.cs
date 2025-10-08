using MediatR;
using StudentUnionBot.Application.Appeals.DTOs;
using StudentUnionBot.Core.Results;
using StudentUnionBot.Domain.Enums;

namespace StudentUnionBot.Application.Appeals.Queries.GetAdminAppeals;

/// <summary>
/// Запит для отримання звернень адміністратором з фільтрацією
/// </summary>
public class GetAdminAppealsQuery : IRequest<Result<AppealListDto>>
{
    /// <summary>
    /// ID адміністратора який запитує
    /// </summary>
    public long AdminId { get; set; }

    /// <summary>
    /// Фільтр за статусом
    /// </summary>
    public AppealStatus? Status { get; set; }

    /// <summary>
    /// Фільтр за категорією
    /// </summary>
    public AppealCategory? Category { get; set; }

    /// <summary>
    /// Фільтр за пріоритетом
    /// </summary>
    public AppealPriority? Priority { get; set; }

    /// <summary>
    /// Фільтр за призначеним адміністратором
    /// </summary>
    public long? AssignedToAdminId { get; set; }

    /// <summary>
    /// Пошук за текстом (тема або повідомлення)
    /// </summary>
    public string? SearchText { get; set; }

    /// <summary>
    /// Сортування (CreatedAt, UpdatedAt, Priority)
    /// </summary>
    public string SortBy { get; set; } = "CreatedAt";

    /// <summary>
    /// Напрямок сортування (asc, desc)
    /// </summary>
    public bool Descending { get; set; } = true;

    /// <summary>
    /// Номер сторінки (починається з 1)
    /// </summary>
    public int PageNumber { get; set; } = 1;

    /// <summary>
    /// Кількість елементів на сторінці
    /// </summary>
    public int PageSize { get; set; } = 10;

    /// <summary>
    /// Тільки непризначені звернення
    /// </summary>
    public bool OnlyUnassigned { get; set; } = false;

    /// <summary>
    /// Тільки мої звернення (призначені поточному адміну)
    /// </summary>
    public bool OnlyMy { get; set; } = false;
}
