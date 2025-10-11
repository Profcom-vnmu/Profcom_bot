using Microsoft.EntityFrameworkCore;
using StudentUnionBot.Domain.Entities;
using StudentUnionBot.Domain.Enums;

namespace StudentUnionBot.Infrastructure.Data;

/// <summary>
/// Compiled queries для оптимізації продуктивності
/// </summary>
public static class CompiledQueries
{
    /// <summary>
    /// Отримати користувача за Telegram ID
    /// </summary>
    public static readonly Func<BotDbContext, long, CancellationToken, Task<BotUser?>> GetUserByTelegramId =
        EF.CompileAsyncQuery((BotDbContext context, long telegramId, CancellationToken ct) =>
            context.Users
                .AsNoTracking()
                .FirstOrDefault(u => u.TelegramId == telegramId));

    /// <summary>
    /// Отримати активні звернення студента
    /// </summary>
    public static readonly Func<BotDbContext, long, CancellationToken, Task<Appeal?>> GetActiveAppealForStudent =
        EF.CompileAsyncQuery((BotDbContext context, long studentId, CancellationToken ct) =>
            context.Appeals
                .Include(a => a.Messages)
                .FirstOrDefault(a => a.StudentId == studentId && a.Status != AppealStatus.Closed));

    /// <summary>
    /// Перевірити чи має користувач активне звернення
    /// </summary>
    public static readonly Func<BotDbContext, long, CancellationToken, Task<bool>> HasActiveAppeal =
        EF.CompileAsyncQuery((BotDbContext context, long studentId, CancellationToken ct) =>
            context.Appeals
                .Any(a => a.StudentId == studentId && a.Status != AppealStatus.Closed));

    /// <summary>
    /// Отримати закріплені новини
    /// </summary>
    public static readonly Func<BotDbContext, CancellationToken, Task<List<News>>> GetPinnedNews =
        EF.CompileAsyncQuery((BotDbContext context, CancellationToken ct) =>
            context.News
                .AsNoTracking()
                .Where(n => n.IsPublished && n.IsPinned)
                .OrderByDescending(n => n.CreatedAt)
                .ToList());

    /// <summary>
    /// Отримати featured події
    /// </summary>
    public static readonly Func<BotDbContext, CancellationToken, Task<List<Event>>> GetFeaturedEvents =
        EF.CompileAsyncQuery((BotDbContext context, CancellationToken ct) =>
            context.Events
                .AsNoTracking()
                .Where(e => e.IsFeatured && e.StartDate > DateTime.UtcNow && e.Status == EventStatus.Planned)
                .OrderBy(e => e.StartDate)
                .ToList());

    /// <summary>
    /// Отримати кількість активних звернень
    /// </summary>
    public static readonly Func<BotDbContext, CancellationToken, Task<int>> GetActiveAppealsCount =
        EF.CompileAsyncQuery((BotDbContext context, CancellationToken ct) =>
            context.Appeals
                .Count(a => a.Status != AppealStatus.Closed));

    /// <summary>
    /// Отримати кількість активних звернень за категорією
    /// </summary>
    public static readonly Func<BotDbContext, AppealCategory, CancellationToken, Task<int>> GetActiveAppealsByCategory =
        EF.CompileAsyncQuery((BotDbContext context, AppealCategory category, CancellationToken ct) =>
            context.Appeals
                .Count(a => a.Status != AppealStatus.Closed && a.Category == category));

    /// <summary>
    /// Отримати workload адміністратора
    /// </summary>
    public static readonly Func<BotDbContext, long, CancellationToken, Task<AdminWorkload?>> GetAdminWorkload =
        EF.CompileAsyncQuery((BotDbContext context, long adminId, CancellationToken ct) =>
            context.AdminWorkloads
                .AsNoTracking()
                .FirstOrDefault(w => w.AdminId == adminId));

    /// <summary>
    /// Отримати всіх доступних адміністраторів
    /// </summary>
    public static readonly Func<BotDbContext, CancellationToken, Task<List<AdminWorkload>>> GetAvailableAdmins =
        EF.CompileAsyncQuery((BotDbContext context, CancellationToken ct) =>
            context.AdminWorkloads
                .AsNoTracking()
                .Where(w => w.IsAvailable)
                .OrderBy(w => w.ActiveAppealsCount)
                .ToList());

    /// <summary>
    /// Отримати кількість опублікованих новин
    /// </summary>
    public static readonly Func<BotDbContext, CancellationToken, Task<int>> GetPublishedNewsCount =
        EF.CompileAsyncQuery((BotDbContext context, CancellationToken ct) =>
            context.News
                .Count(n => n.IsPublished && (n.PublishAt == null || n.PublishAt <= DateTime.UtcNow)));

    /// <summary>
    /// Отримати кількість майбутніх подій
    /// </summary>
    public static readonly Func<BotDbContext, CancellationToken, Task<int>> GetUpcomingEventsCount =
        EF.CompileAsyncQuery((BotDbContext context, CancellationToken ct) =>
            context.Events
                .Count(e => e.StartDate > DateTime.UtcNow && e.Status == EventStatus.Planned));

    /// <summary>
    /// Отримати активні контакти за типом
    /// </summary>
    public static readonly Func<BotDbContext, ContactType, CancellationToken, Task<List<ContactInfo>>> GetActiveContactsByType =
        EF.CompileAsyncQuery((BotDbContext context, ContactType type, CancellationToken ct) =>
            context.Contacts
                .AsNoTracking()
                .Where(c => c.Type == type && c.IsActive)
                .OrderBy(c => c.DisplayOrder)
                .ToList());

    /// <summary>
    /// Отримати активних партнерів
    /// </summary>
    public static readonly Func<BotDbContext, CancellationToken, Task<List<Partner>>> GetActivePartners =
        EF.CompileAsyncQuery((BotDbContext context, CancellationToken ct) =>
            context.Partners
                .AsNoTracking()
                .Where(p => p.IsActive)
                .OrderBy(p => p.DisplayOrder)
                .ToList());
}
