using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StudentUnionBot.Domain.Interfaces;

namespace StudentUnionBot.Infrastructure.BackgroundServices;

/// <summary>
/// Background service для обробки запланованих нагадувань
/// </summary>
public class NotificationReminderService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<NotificationReminderService> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromMinutes(1); // Кожну хвилину

    public NotificationReminderService(
        IServiceProvider serviceProvider,
        ILogger<NotificationReminderService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Notification Reminder Service запущено");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessRemindersAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Помилка при обробці нагадувань");
            }

            await Task.Delay(_interval, stoppingToken);
        }

        _logger.LogInformation("Notification Reminder Service зупинено");
    }

    private async Task ProcessRemindersAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var reminderService = scope.ServiceProvider.GetRequiredService<IReminderService>();

        var result = await reminderService.ProcessScheduledRemindersAsync(cancellationToken);

        if (result.IsSuccess && result.Value > 0)
        {
            _logger.LogInformation("Оброблено {Count} запланованих нагадувань", result.Value);
        }
    }
}
