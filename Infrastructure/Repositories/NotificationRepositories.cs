using Microsoft.EntityFrameworkCore;
using StudentUnionBot.Domain.Entities;
using StudentUnionBot.Domain.Enums;
using StudentUnionBot.Domain.Interfaces;
using StudentUnionBot.Infrastructure.Data;

namespace StudentUnionBot.Infrastructure.Repositories;

/// <summary>
/// Репозиторій для сповіщень
/// </summary>
public class NotificationRepository : BaseRepository<Notification>, INotificationRepository
{
    private readonly BotDbContext _context;

    public NotificationRepository(BotDbContext context) : base(context)
    {
        _context = context;
    }

    public async Task<List<Notification>> GetUserNotificationsAsync(long userId, bool includeRead = false, int page = 1, int pageSize = 20, CancellationToken cancellationToken = default)
    {
        var query = _context.Set<Notification>()
            .AsNoTracking()
            .Where(n => n.UserId == userId);

        if (!includeRead)
        {
            query = query.Where(n => n.ReadAt == null);
        }

        return await query
            .OrderByDescending(n => n.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Notification>> GetUnreadNotificationsAsync(long userId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<Notification>()
            .AsNoTracking()
            .Where(n => n.UserId == userId && n.ReadAt == null)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetUnreadCountAsync(long userId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<Notification>()
            .CountAsync(n => n.UserId == userId && n.ReadAt == null, cancellationToken);
    }

    public async Task<List<Notification>> GetPendingNotificationsAsync(int limit = 100, CancellationToken cancellationToken = default)
    {
        return await _context.Set<Notification>()
            .Where(n => n.Status == NotificationStatus.Pending &&
                       (!n.ScheduledFor.HasValue || n.ScheduledFor.Value <= DateTime.UtcNow))
            .OrderBy(n => n.CreatedAt)
            .Take(limit)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Notification>> GetFailedNotificationsForRetryAsync(int maxRetries = 3, CancellationToken cancellationToken = default)
    {
        return await _context.Set<Notification>()
            .Where(n => n.Status == NotificationStatus.Failed && n.RetryCount < maxRetries)
            .OrderBy(n => n.CreatedAt)
            .Take(100)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> MarkAllAsReadAsync(long userId, CancellationToken cancellationToken = default)
    {
        var notifications = await _context.Set<Notification>()
            .Where(n => n.UserId == userId && n.ReadAt == null)
            .ToListAsync(cancellationToken);

        foreach (var notification in notifications)
        {
            notification.MarkAsRead();
        }

        return await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<int> DeleteOldNotificationsAsync(DateTime olderThan, CancellationToken cancellationToken = default)
    {
        var notifications = await _context.Set<Notification>()
            .Where(n => n.CreatedAt < olderThan && n.Status != NotificationStatus.Pending)
            .ToListAsync(cancellationToken);

        _context.Set<Notification>().RemoveRange(notifications);
        return await _context.SaveChangesAsync(cancellationToken);
    }
}

/// <summary>
/// Репозиторій для налаштувань сповіщень
/// </summary>
public class NotificationPreferenceRepository : BaseRepository<NotificationPreference>, INotificationPreferenceRepository
{
    private readonly BotDbContext _context;

    public NotificationPreferenceRepository(BotDbContext context) : base(context)
    {
        _context = context;
    }

    public async Task<List<NotificationPreference>> GetUserPreferencesAsync(long userId, CancellationToken cancellationToken = default)
    {
        return await _context.Set<NotificationPreference>()
            .AsNoTracking()
            .Where(np => np.UserId == userId)
            .ToListAsync(cancellationToken);
    }

    public async Task<NotificationPreference?> GetPreferenceAsync(long userId, NotificationEvent notificationEvent, CancellationToken cancellationToken = default)
    {
        return await _context.Set<NotificationPreference>()
            .FirstOrDefaultAsync(np => np.UserId == userId && np.Event == notificationEvent, cancellationToken);
    }

    public async Task<bool> IsNotificationEnabledAsync(long userId, NotificationEvent notificationEvent, NotificationType type, CancellationToken cancellationToken = default)
    {
        var preference = await GetPreferenceAsync(userId, notificationEvent, cancellationToken);
        
        if (preference == null)
        {
            // Якщо налаштувань немає - використовуємо defaults
            return type switch
            {
                NotificationType.Push => true,
                NotificationType.InApp => true,
                NotificationType.Email => false,
                _ => false
            };
        }

        return preference.IsTypeEnabled(type);
    }

    public async Task CreateDefaultPreferencesAsync(long userId, CancellationToken cancellationToken = default)
    {
        var allEvents = Enum.GetValues<NotificationEvent>();
        var existingPreferences = await GetUserPreferencesAsync(userId, cancellationToken);
        var existingEvents = existingPreferences.Select(p => p.Event).ToHashSet();

        foreach (var notificationEvent in allEvents)
        {
            if (!existingEvents.Contains(notificationEvent))
            {
                var preference = NotificationPreference.CreateDefault(userId, notificationEvent);
                await _context.Set<NotificationPreference>().AddAsync(preference, cancellationToken);
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}

/// <summary>
/// Репозиторій для шаблонів сповіщень
/// </summary>
public class NotificationTemplateRepository : BaseRepository<NotificationTemplate>, INotificationTemplateRepository
{
    private readonly BotDbContext _context;

    public NotificationTemplateRepository(BotDbContext context) : base(context)
    {
        _context = context;
    }

    public async Task<NotificationTemplate?> GetTemplateAsync(NotificationEvent notificationEvent, NotificationType type, string language = "uk", CancellationToken cancellationToken = default)
    {
        return await _context.Set<NotificationTemplate>()
            .AsNoTracking()
            .Where(t => t.Event == notificationEvent && t.Type == type && t.Language == language && t.IsActive)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<List<NotificationTemplate>> GetActiveTemplatesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Set<NotificationTemplate>()
            .AsNoTracking()
            .Where(t => t.IsActive)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<NotificationTemplate>> GetTemplatesByLanguageAsync(string language, CancellationToken cancellationToken = default)
    {
        return await _context.Set<NotificationTemplate>()
            .AsNoTracking()
            .Where(t => t.Language == language && t.IsActive)
            .ToListAsync(cancellationToken);
    }
}
