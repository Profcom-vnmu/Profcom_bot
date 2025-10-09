using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using StudentUnionBot.Infrastructure.Data;

namespace StudentUnionBot.Infrastructure.HealthChecks;

/// <summary>
/// Health check для перевірки підключення до бази даних
/// </summary>
public class DatabaseHealthCheck : IHealthCheck
{
    private readonly BotDbContext _context;

    public DatabaseHealthCheck(BotDbContext context)
    {
        _context = context;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Перевіряємо можливість підключення до БД
            await _context.Database.CanConnectAsync(cancellationToken);

            // Отримуємо статистику БД
            var userCount = await _context.Users.CountAsync(cancellationToken);
            var appealCount = await _context.Appeals.CountAsync(cancellationToken);
            var notificationCount = await _context.Notifications
                .Where(n => n.CreatedAt >= DateTime.UtcNow.AddDays(-7))
                .CountAsync(cancellationToken);

            var data = new Dictionary<string, object>
            {
                { "database", _context.Database.GetDbConnection().Database },
                { "users", userCount },
                { "appeals", appealCount },
                { "notifications_last_7_days", notificationCount },
                { "connection_state", _context.Database.GetDbConnection().State.ToString() }
            };

            return HealthCheckResult.Healthy(
                "Database connection is healthy",
                data);
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy(
                "Database connection failed",
                ex,
                new Dictionary<string, object>
                {
                    { "error", ex.Message }
                });
        }
    }
}
