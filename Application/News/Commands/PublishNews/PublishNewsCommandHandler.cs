using MediatR;
using Microsoft.Extensions.Logging;
using StudentUnionBot.Application.News.DTOs;
using StudentUnionBot.Core.Results;
using StudentUnionBot.Domain.Interfaces;

namespace StudentUnionBot.Application.News.Commands.PublishNews;

/// <summary>
/// Handler для публікації новини з інвалідацією кешу
/// </summary>
public class PublishNewsCommandHandler : IRequestHandler<PublishNewsCommand, Result<NewsDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IStudentUnionCacheService _cacheService;
    private readonly ILogger<PublishNewsCommandHandler> _logger;
    // TODO: Додати INotificationService для відправки повідомлень

    public PublishNewsCommandHandler(
        IUnitOfWork unitOfWork,
        IStudentUnionCacheService cacheService,
        ILogger<PublishNewsCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task<Result<NewsDto>> Handle(PublishNewsCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation(
                "Publishing news article {NewsId} by publisher {PublisherId}",
                request.NewsId,
                request.PublisherId
            );

            // Перевіряємо чи існує новина
            var news = await _unitOfWork.News.GetByIdAsync(request.NewsId, cancellationToken);
            if (news == null)
            {
                _logger.LogWarning("News article {NewsId} not found", request.NewsId);
                return Result<NewsDto>.Fail("Новину не знайдено");
            }

            // Перевіряємо чи новина вже опублікована
            if (news.IsPublished)
            {
                _logger.LogWarning("News article {NewsId} is already published", request.NewsId);
                return Result<NewsDto>.Fail("Новина вже опублікована");
            }

            // Перевіряємо права публікувача
            var publisher = await _unitOfWork.Users.GetByTelegramIdAsync(request.PublisherId, cancellationToken);
            if (publisher == null)
            {
                _logger.LogWarning("Publisher {PublisherId} not found", request.PublisherId);
                return Result<NewsDto>.Fail("Публікувач не знайдений");
            }

            // TODO: Додати перевірку прав доступу для публікації
            // if (!publisher.CanPublishNews)

            // Якщо є запланована дата публікації
            if (request.ScheduledPublishDate.HasValue)
            {
                _logger.LogInformation(
                    "Scheduling news article {NewsId} for publication at {ScheduledDate}",
                    request.NewsId,
                    request.ScheduledPublishDate.Value
                );
                
                // TODO: Додати в чергу запланованих завдань
                // await _scheduledTaskService.ScheduleNewsPublication(request.NewsId, request.ScheduledPublishDate.Value);
                
                return Result<NewsDto>.Fail("Запланована публікація поки не реалізована");
            }

            // Публікуємо новину зараз
            news.Publish();

            // Закріплюємо, якщо потрібно
            if (request.PinNews)
            {
                news.Pin();
                _logger.LogInformation("News article {NewsId} has been pinned", request.NewsId);
            }

            // Зберігаємо зміни
            _unitOfWork.News.Update(news);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Інвалідуємо кеш новин
            await _cacheService.InvalidateNewsAsync(request.NewsId, cancellationToken);

            _logger.LogInformation(
                "Successfully published news article {NewsId}, cache invalidated",
                request.NewsId
            );

            // Відправляємо push-повідомлення, якщо потрібно
            if (request.SendPushNotification)
            {
                _logger.LogInformation(
                    "Sending push notification for published news {NewsId}",
                    request.NewsId
                );

                // TODO: Реалізувати через NotificationService
                /*
                await _notificationService.SendNewsNotificationAsync(new NewsNotificationRequest
                {
                    NewsId = news.Id,
                    Title = news.Title,
                    Summary = news.Summary ?? news.ContentPreview,
                    Category = news.Category
                }, cancellationToken);
                */
            }

            // Конвертуємо в DTO
            var newsDto = new NewsDto
            {
                Id = news.Id,
                Title = news.Title,
                Content = news.Content,
                Summary = news.Summary,
                Category = news.Category,
                AuthorId = news.AuthorId,
                AuthorName = news.AuthorName,
                PhotoFileId = news.PhotoFileId,
                DocumentFileId = news.DocumentFileId,
                IsPublished = news.IsPublished,
                PublishAt = news.PublishAt,
                CreatedAt = news.CreatedAt,
                UpdatedAt = news.UpdatedAt,
                ViewCount = news.ViewCount,
                IsPinned = news.IsPinned
            };

            return Result<NewsDto>.Ok(newsDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error publishing news article {NewsId}",
                request.NewsId
            );
            return Result<NewsDto>.Fail("Помилка при публікації новини");
        }
    }
}