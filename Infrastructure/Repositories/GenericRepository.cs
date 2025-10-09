using StudentUnionBot.Infrastructure.Data;

namespace StudentUnionBot.Infrastructure.Repositories;

/// <summary>
/// Generic repository для використання в UnitOfWork
/// </summary>
public class GenericRepository<T> : BaseRepository<T> where T : class
{
    public GenericRepository(BotDbContext context) : base(context)
    {
    }
}