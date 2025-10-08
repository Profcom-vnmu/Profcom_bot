using Microsoft.EntityFrameworkCore;
using StudentUnionBot.Domain.Entities;
using StudentUnionBot.Domain.Interfaces;
using StudentUnionBot.Infrastructure.Data;

namespace StudentUnionBot.Infrastructure.Repositories;

/// <summary>
/// Репозиторій для роботи з контактною інформацією
/// </summary>
public class ContactInfoRepository : BaseRepository<ContactInfo>, IContactInfoRepository
{
    public ContactInfoRepository(BotDbContext context) : base(context)
    {
    }

    public async Task<List<ContactInfo>> GetAllActiveAsync(CancellationToken cancellationToken = default)
    {
        return await Context.Set<ContactInfo>()
            .AsNoTracking()
            .Where(c => c.IsActive)
            .OrderBy(c => c.DisplayOrder)
            .ThenBy(c => c.Title)
            .ToListAsync(cancellationToken);
    }
}
