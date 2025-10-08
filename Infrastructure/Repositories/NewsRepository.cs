using Microsoft.EntityFrameworkCore;
using StudentUnionBot.Domain.Entities;
using StudentUnionBot.Domain.Enums;
using StudentUnionBot.Domain.Interfaces;
using StudentUnionBot.Infrastructure.Data;

namespace StudentUnionBot.Infrastructure.Repositories;

/// <summary>
/// Репозиторій для роботи з новинами
/// </summary>
public class NewsRepository : BaseRepository<News>, INewsRepository
{
    public NewsRepository(BotDbContext context) : base(context)
    {
    }

    public async Task<List<News>> GetPublishedNewsAsync(
        NewsCategory? category = null,
        bool onlyPinned = false,
        int pageNumber = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var query = Context.Set<News>()
            .AsNoTracking()
            .Where(n => n.IsPublished && 
                       (n.PublishAt == null || n.PublishAt <= DateTime.UtcNow));

        if (category.HasValue)
        {
            query = query.Where(n => n.Category == category.Value);
        }

        if (onlyPinned)
        {
            query = query.Where(n => n.IsPinned);
        }

        return await query
            .OrderByDescending(n => n.IsPinned)
            .ThenByDescending(n => n.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetPublishedNewsCountAsync(
        NewsCategory? category = null,
        bool onlyPinned = false,
        CancellationToken cancellationToken = default)
    {
        var query = Context.Set<News>()
            .AsNoTracking()
            .Where(n => n.IsPublished && 
                       (n.PublishAt == null || n.PublishAt <= DateTime.UtcNow));

        if (category.HasValue)
        {
            query = query.Where(n => n.Category == category.Value);
        }

        if (onlyPinned)
        {
            query = query.Where(n => n.IsPinned);
        }

        return await query.CountAsync(cancellationToken);
    }

    public async Task<List<News>> GetPinnedNewsAsync(CancellationToken cancellationToken = default)
    {
        return await Context.Set<News>()
            .AsNoTracking()
            .Where(n => n.IsPublished && n.IsPinned)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync(cancellationToken);
    }
}
