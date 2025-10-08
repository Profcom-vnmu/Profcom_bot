using Microsoft.EntityFrameworkCore;
using StudentUnionBot.Domain.Entities;
using StudentUnionBot.Domain.Enums;
using StudentUnionBot.Domain.Interfaces;
using StudentUnionBot.Infrastructure.Data;

namespace StudentUnionBot.Infrastructure.Repositories;

/// <summary>
/// Репозиторій для роботи з подіями
/// </summary>
public class EventRepository : BaseRepository<Event>, IEventRepository
{
    public EventRepository(BotDbContext context) : base(context)
    {
    }

    public async Task<List<Event>> GetUpcomingEventsAsync(
        EventType? type = null,
        bool onlyFeatured = false,
        int pageNumber = 1,
        int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var query = Context.Set<Event>()
            .AsNoTracking()
            .Where(e => e.StartDate > DateTime.UtcNow && e.Status == EventStatus.Planned);

        if (type.HasValue)
        {
            query = query.Where(e => e.Type == type.Value);
        }

        if (onlyFeatured)
        {
            query = query.Where(e => e.IsFeatured);
        }

        return await query
            .OrderBy(e => e.StartDate)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetUpcomingEventsCountAsync(
        EventType? type = null,
        bool onlyFeatured = false,
        CancellationToken cancellationToken = default)
    {
        var query = Context.Set<Event>()
            .AsNoTracking()
            .Where(e => e.StartDate > DateTime.UtcNow && e.Status == EventStatus.Planned);

        if (type.HasValue)
        {
            query = query.Where(e => e.Type == type.Value);
        }

        if (onlyFeatured)
        {
            query = query.Where(e => e.IsFeatured);
        }

        return await query.CountAsync(cancellationToken);
    }

    public async Task<List<Event>> GetFeaturedEventsAsync(CancellationToken cancellationToken = default)
    {
        return await Context.Set<Event>()
            .AsNoTracking()
            .Where(e => e.IsFeatured && e.StartDate > DateTime.UtcNow && e.Status == EventStatus.Planned)
            .OrderBy(e => e.StartDate)
            .ToListAsync(cancellationToken);
    }
}
