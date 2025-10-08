using Microsoft.EntityFrameworkCore;
using StudentUnionBot.Domain.Entities;
using StudentUnionBot.Domain.Enums;
using StudentUnionBot.Domain.Interfaces;
using StudentUnionBot.Infrastructure.Data;

namespace StudentUnionBot.Infrastructure.Repositories;

/// <summary>
/// Репозиторій для роботи з партнерами
/// </summary>
public class PartnerRepository : BaseRepository<Partner>, IPartnerRepository
{
    public PartnerRepository(BotDbContext context) : base(context)
    {
    }

    public async Task<List<Partner>> GetActivePartnersAsync(
        PartnerType? type = null,
        bool onlyFeatured = false,
        CancellationToken cancellationToken = default)
    {
        var query = Context.Set<Partner>()
            .AsNoTracking()
            .Where(p => p.IsActive);

        if (type.HasValue)
        {
            query = query.Where(p => p.Type == type.Value);
        }

        if (onlyFeatured)
        {
            query = query.Where(p => p.IsFeatured);
        }

        return await query
            .OrderByDescending(p => p.IsFeatured)
            .ThenBy(p => p.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Partner>> GetFeaturedPartnersAsync(CancellationToken cancellationToken = default)
    {
        return await Context.Set<Partner>()
            .AsNoTracking()
            .Where(p => p.IsActive && p.IsFeatured)
            .OrderBy(p => p.Name)
            .ToListAsync(cancellationToken);
    }
}
