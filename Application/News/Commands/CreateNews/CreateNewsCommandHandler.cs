using MediatR;
using Microsoft.Extensions.Logging;
using StudentUnionBot.Application.News.DTOs;
using StudentUnionBot.Core.Results;
using StudentUnionBot.Domain.Entities;
using StudentUnionBot.Domain.Interfaces;

namespace StudentUnionBot.Application.News.Commands.CreateNews;

/// <summary>
/// Handler для створення нової новини з інвалідацією кешу
/// </summary>
public class CreateNewsCommandHandler : IRequestHandler<CreateNewsCommand, Result<NewsDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IStudentUnionCacheService _cacheService;
    private readonly INotificationService _notificationService;
    private readonly ILogger<CreateNewsCommandHandler> _logger;

    public CreateNewsCommandHandler(
        IUnitOfWork unitOfWork,
        IStudentUnionCacheService cacheService,
        INotificationService notificationService,
        ILogger<CreateNewsCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _cacheService = cacheService;
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task<Result<NewsDto>> Handle(CreateNewsCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation(
                "Creating news article with title: {Title} by author: {AuthorId}",
                request.Title,
                request.AuthorId
            );

            // Отримуємо інформацію про автора для збереження імені
            var author = await _unitOfWork.Users.GetByTelegramIdAsync(request.AuthorId, cancellationToken);
            if (author == null)
            {
                _logger.LogWarning("Author with ID {AuthorId} not found", request.AuthorId);
                return Result<NewsDto>.Fail("Автор не знайдений");
            }

            // Створюємо новину через domain factory
            var news = Domain.Entities.News.Create(
                title: request.Title,
                content: request.Content,
                category: request.Category,
                authorId: request.AuthorId,
                authorName: author.FirstName ?? author.Username ?? "Адміністратор",
                summary: request.Summary,
                // Legacy fields - залишаємо для зворотної сумісності
                photoFileId: request.Attachments.FirstOrDefault(a => a.FileType == Domain.Enums.FileType.Image)?.FileId,
                documentFileId: request.Attachments.FirstOrDefault(a => a.FileType == Domain.Enums.FileType.Document)?.FileId,
                publishImmediately: request.PublishImmediately,
                publishAt: request.ScheduledPublishDate
            );

            // Додаємо всі attachments до новини
            foreach (var attachment in request.Attachments)
            {
                news.AddNewsAttachment(attachment.FileId, attachment.FileType, attachment.FileName);
            }

            // Зберігаємо в базі
            await _unitOfWork.News.AddAsync(news, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Інвалідуємо кеш новин
            await _cacheService.InvalidateNewsAsync(cancellationToken: cancellationToken);

            _logger.LogInformation(
                "Successfully created news article with ID: {NewsId}, cache invalidated",
                news.Id
            );

            // Відправляємо push-повідомлення якщо новина опублікована одразу
            if (request.SendPushNotification && news.IsPublished)
            {
                try
                {
                    _logger.LogInformation(
                        "Sending push notification for newly created published news: {NewsId}",
                        news.Id
                    );
                    
                    await _notificationService.SendNewsPublishedNotificationAsync(
                        news.Id,
                        news.Title,
                        news.Summary ?? news.Content.Substring(0, Math.Min(100, news.Content.Length)),
                        news.Category,
                        news.PhotoFileId,
                        cancellationToken
                    );
                    
                    _logger.LogInformation(
                        "Push notification sent successfully for news: {NewsId}",
                        news.Id
                    );
                }
                catch (Exception ex)
                {
                    // Не повертаємо помилку, бо новину вже створено - просто логуємо
                    _logger.LogError(ex,
                        "Failed to send push notification for news: {NewsId}",
                        news.Id
                    );
                }
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
                IsPinned = news.IsPinned,
                Tags = request.Tags ?? string.Empty,
                Language = request.Language
            };

            return Result<NewsDto>.Ok(newsDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, 
                "Error creating news article with title: {Title}", 
                request.Title
            );
            return Result<NewsDto>.Fail("Помилка при створенні новини");
        }
    }

}