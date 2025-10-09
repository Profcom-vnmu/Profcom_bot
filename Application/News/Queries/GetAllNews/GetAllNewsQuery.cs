using MediatR;
using StudentUnionBot.Application.News.DTOs;
using StudentUnionBot.Core.Results;
using StudentUnionBot.Domain.Enums;

namespace StudentUnionBot.Application.News.Queries.GetAllNews;

/// <summary>
/// Запит для отримання всіх новин (для адміністраторів)
/// </summary>
public class GetAllNewsQuery : IRequest<Result<NewsListDto>>
{
    /// <summary>
    /// Категорія новин (null = всі)
    /// </summary>
    public NewsCategory? Category { get; set; }
    
    /// <summary>
    /// Статус новин (null = всі)
    /// </summary>
    public NewsStatus? Status { get; set; }
    
    /// <summary>
    /// Кількість записів на сторінці
    /// </summary>
    public int PageSize { get; set; } = 10;
    
    /// <summary>
    /// Номер сторінки (починається з 1)
    /// </summary>
    public int PageNumber { get; set; } = 1;
    
    /// <summary>
    /// Сортування за датою (true - від нових до старих)
    /// </summary>
    public bool SortByDateDesc { get; set; } = true;
}