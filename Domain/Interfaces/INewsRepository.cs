using StudentUnionBot.Domain.Entities;
using StudentUnionBot.Domain.Enums;

namespace StudentUnionBot.Domain.Interfaces;

/// <summary>
/// Інтерфейс репозиторію новин
/// </summary>
public interface INewsRepository : IRepository<News>
{
    /// <summary>
    /// Отримати опубліковані новини
    /// </summary>
    Task<List<News>> GetPublishedNewsAsync(
        NewsCategory? category = null,
        bool onlyPinned = false,
        int pageNumber = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Отримати кількість опублікованих новин
    /// </summary>
    Task<int> GetPublishedNewsCountAsync(
        NewsCategory? category = null,
        bool onlyPinned = false,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Отримати закріплені новини
    /// </summary>
    Task<List<News>> GetPinnedNewsAsync(CancellationToken cancellationToken = default);
}
