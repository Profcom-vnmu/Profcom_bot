using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StudentUnionBot.Domain.Entities;
using StudentUnionBot.Domain.Interfaces;
using StudentUnionBot.Infrastructure.Data;

namespace StudentUnionBot.Infrastructure.Repositories;

public class UserRepository : BaseRepository<BotUser>, IUserRepository
{
    private readonly ILogger<UserRepository> _logger;
    
    public UserRepository(BotDbContext context, ILogger<UserRepository> logger) : base(context)
    {
        _logger = logger;
    }

    public async Task<BotUser?> GetByTelegramIdAsync(long telegramId, CancellationToken cancellationToken = default)
    {
        var user = await DbSet
            .FirstOrDefaultAsync(u => u.TelegramId == telegramId, cancellationToken);
        
        if (user != null)
        {
            _logger.LogWarning(
                "REPOSITORY LOAD: User {TelegramId} loaded with Role={Role}", 
                telegramId, 
                user.Role);
        }
        
        return user;
    }

    public async Task<BotUser?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);
    }

    public async Task<List<BotUser>> GetActiveUsersAsync(CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AsNoTracking()
            .Where(u => u.IsActive && !u.IsBanned)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<BotUser>> GetAdminsAsync(CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AsNoTracking()
            .Where(u => u.Role >= Domain.Enums.UserRole.Admin)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsAsync(long telegramId, CancellationToken cancellationToken = default)
    {
        return await DbSet
            .AnyAsync(u => u.TelegramId == telegramId, cancellationToken);
    }
}
