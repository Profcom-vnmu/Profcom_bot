using MediatR;
using StudentUnionBot.Application.News.DTOs;
using StudentUnionBot.Core.Results;
using StudentUnionBot.Domain.Enums;

namespace StudentUnionBot.Application.News.Queries.GetPublishedNews;

/// <summary>
/// Запит для отримання опублікованих новин
/// </summary>
public class GetPublishedNewsQuery : IRequest<Result<NewsListDto>>
{
    /// <summary>
    /// Категорія новин (null = всі)
    /// </summary>
    public NewsCategory? Category { get; set; }
    
    /// <summary>
    /// Кількість записів на сторінці
    /// </summary>
    public int PageSize { get; set; } = 10;
    
    /// <summary>
    /// Номер сторінки (починається з 1)
    /// </summary>
    public int PageNumber { get; set; } = 1;
    
    /// <summary>
    /// Тільки закріплені новини
    /// </summary>
    public bool OnlyPinned { get; set; } = false;
}
