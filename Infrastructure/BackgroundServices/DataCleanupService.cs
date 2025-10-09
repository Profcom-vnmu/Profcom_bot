using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StudentUnionBot.Domain.Interfaces;

namespace StudentUnionBot.Infrastructure.BackgroundServices;

/// <summary>
/// Background service для очищення старих даних
/// </summary>
public class DataCleanupService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DataCleanupService> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromHours(24); // Щодня

    public DataCleanupService(
        IServiceProvider serviceProvider,
        ILogger<DataCleanupService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Data Cleanup Service запущено");

        // Чекаємо 1 хвилину перед першим запуском
        await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CleanupOldDataAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Помилка при очищенні даних");
            }

            await Task.Delay(_interval, stoppingToken);
        }

        _logger.LogInformation("Data Cleanup Service зупинено");
    }

    private async Task CleanupOldDataAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var cleanupTasks = new List<Task<int>>();

        // Видаляємо старі прочитані сповіщення (старше 30 днів)
        var notificationCutoff = DateTime.UtcNow.AddDays(-30);
        cleanupTasks.Add(unitOfWork.Notifications.DeleteOldNotificationsAsync(
            notificationCutoff, cancellationToken));

        // Видаляємо старі файли (старше 90 днів, які не прикріплені до звернень)
        var fileCutoff = DateTime.UtcNow.AddDays(-90);
        cleanupTasks.Add(unitOfWork.FileAttachments.DeleteOldOrphanedFilesAsync(
            fileCutoff, cancellationToken));

        var results = await Task.WhenAll(cleanupTasks);
        var totalDeleted = results.Sum();

        if (totalDeleted > 0)
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Видалено {Count} старих записів", totalDeleted);
        }
        else
        {
            _logger.LogDebug("Немає старих записів для видалення");
        }
    }
}
