using StudentUnionBot.Domain.Entities;

namespace StudentUnionBot.Domain.Interfaces;

/// <summary>
/// Інтерфейс репозиторію користувачів
/// </summary>
public interface IUserRepository : IRepository<BotUser>
{
    Task<BotUser?> GetByTelegramIdAsync(long telegramId, CancellationToken cancellationToken = default);
    Task<BotUser?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<List<BotUser>> GetActiveUsersAsync(CancellationToken cancellationToken = default);
    Task<List<BotUser>> GetAdminsAsync(CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(long telegramId, CancellationToken cancellationToken = default);
}
