using MediatR;
using Microsoft.Extensions.Logging;
using StudentUnionBot.Application.News.DTOs;
using StudentUnionBot.Core.Results;
using StudentUnionBot.Domain.Enums;
using StudentUnionBot.Domain.Interfaces;

namespace StudentUnionBot.Application.News.Queries.GetPublishedNews;

/// <summary>
/// Обробник запиту для отримання опублікованих новин з кешуванням
/// </summary>
public class GetPublishedNewsQueryHandler : IRequestHandler<GetPublishedNewsQuery, Result<NewsListDto>>
{
    private readonly INewsRepository _newsRepository;
    private readonly IStudentUnionCacheService _cacheService;
    private readonly ILogger<GetPublishedNewsQueryHandler> _logger;

    public GetPublishedNewsQueryHandler(
        INewsRepository newsRepository,
        IStudentUnionCacheService cacheService,
        ILogger<GetPublishedNewsQueryHandler> logger)
    {
        _newsRepository = newsRepository;
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task<Result<NewsListDto>> Handle(GetPublishedNewsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation(
                "Отримання новин: категорія={Category}, сторінка={Page}, розмір={Size}, закріплені={OnlyPinned}",
                request.Category,
                request.PageNumber,
                request.PageSize,
                request.OnlyPinned);

            // Генеруємо ключ кешу з урахуванням всіх параметрів
            var cacheKey = GenerateCacheKey(request);
            
            // Спробуємо отримати з кешу
            var cachedResult = await _cacheService.GetNewsListAsync<NewsListDto>(
                request.PageNumber, 
                request.PageSize, 
                cancellationToken);

            if (cachedResult != null)
            {
                _logger.LogDebug("Новини отримано з кешу для ключа: {CacheKey}", cacheKey);
                return Result<NewsListDto>.Ok(cachedResult);
            }

            // Отримуємо новини з БД
            var news = await _newsRepository.GetPublishedNewsAsync(
                category: request.Category,
                onlyPinned: request.OnlyPinned,
                pageNumber: request.PageNumber,
                pageSize: request.PageSize,
                cancellationToken: cancellationToken);

            // Отримуємо загальну кількість
            var totalCount = await _newsRepository.GetPublishedNewsCountAsync(
                category: request.Category,
                onlyPinned: request.OnlyPinned,
                cancellationToken: cancellationToken);

            // Маппінг на DTO
            var newsDto = news.Select(MapToDto).ToList();

            var result = new NewsListDto
            {
                Items = newsDto,
                TotalCount = totalCount,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize
            };

            // Кешуємо результат
            await _cacheService.SetNewsListAsync(
                request.PageNumber,
                request.PageSize,
                result,
                cancellationToken);

            _logger.LogInformation(
                "Знайдено {Count} новин (всього {Total}), результат закешовано",
                newsDto.Count,
                totalCount);

            return Result<NewsListDto>.Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Помилка при отриманні новин");
            return Result<NewsListDto>.Fail("Виникла помилка при завантаженні новин");
        }
    }

    private static string GenerateCacheKey(GetPublishedNewsQuery request)
    {
        var categoryPart = request.Category?.ToString() ?? "all";
        var pinnedPart = request.OnlyPinned ? "pinned" : "all";
        return $"news_list:{categoryPart}:{pinnedPart}:{request.PageNumber}:{request.PageSize}";
    }

    private static NewsDto MapToDto(Domain.Entities.News news)
    {
        return new NewsDto
        {
            Id = news.Id,
            Title = news.Title,
            Content = news.Content,
            Summary = news.Summary,
            Category = news.Category,
            AuthorName = news.AuthorName,
            PhotoFileId = news.PhotoFileId,
            DocumentFileId = news.DocumentFileId,
            IsPublished = news.IsPublished,
            IsPinned = news.IsPinned,
            CreatedAt = news.CreatedAt,
            PublishAt = news.PublishAt,
            ViewCount = news.ViewCount,
            // Додаткові поля з Domain Entity
            AuthorId = news.AuthorId,
            UpdatedAt = news.UpdatedAt
        };
    }
}
