using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using StudentUnionBot.Domain.Interfaces;

namespace StudentUnionBot.Infrastructure.BackgroundServices;

/// <summary>
/// Background service для автоматичної публікації запланованих новин та подій
/// </summary>
public class ScheduledPublicationService : BackgroundService
{
    private readonly ILogger<ScheduledPublicationService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(2); // Перевірка кожні 2 хвилини

    public ScheduledPublicationService(
        ILogger<ScheduledPublicationService> logger,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ScheduledPublicationService started. Checking every {Interval} minutes", _checkInterval.TotalMinutes);

        // Чекаємо 30 секунд після старту перед першою перевіркою
        await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PublishScheduledContentAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while processing scheduled publications");
            }

            // Чекаємо до наступної перевірки
            await Task.Delay(_checkInterval, stoppingToken);
        }

        _logger.LogInformation("ScheduledPublicationService stopped");
    }

    /// <summary>
    /// Знаходить та публікує новини/події, час публікації яких настав
    /// </summary>
    private async Task PublishScheduledContentAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var now = DateTime.UtcNow;

        try
        {
            // Знаходимо новини для публікації
            var newsToPublish = await unitOfWork.News.GetScheduledForPublicationAsync(now, cancellationToken);
            
            if (newsToPublish.Any())
            {
                _logger.LogInformation("Found {Count} scheduled news articles to publish", newsToPublish.Count());
                
                foreach (var news in newsToPublish)
                {
                    try
                    {
                        news.Publish();
                        await unitOfWork.SaveChangesAsync(cancellationToken);
                        
                        _logger.LogInformation(
                            "Successfully published scheduled news: {NewsId} - {Title}",
                            news.Id,
                            news.Title
                        );
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex,
                            "Failed to publish scheduled news: {NewsId} - {Title}",
                            news.Id,
                            news.Title
                        );
                    }
                }
            }

            // Знаходимо події для публікації
            var eventsToPublish = await unitOfWork.Events.GetScheduledForPublicationAsync(now, cancellationToken);
            
            if (eventsToPublish.Any())
            {
                _logger.LogInformation("Found {Count} scheduled events to publish", eventsToPublish.Count());
                
                foreach (var eventEntity in eventsToPublish)
                {
                    try
                    {
                        eventEntity.Publish();
                        await unitOfWork.SaveChangesAsync(cancellationToken);
                        
                        _logger.LogInformation(
                            "Successfully published scheduled event: {EventId} - {Title}",
                            eventEntity.Id,
                            eventEntity.Title
                        );
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex,
                            "Failed to publish scheduled event: {EventId} - {Title}",
                            eventEntity.Id,
                            eventEntity.Title
                        );
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while fetching scheduled content for publication");
        }
    }
}
