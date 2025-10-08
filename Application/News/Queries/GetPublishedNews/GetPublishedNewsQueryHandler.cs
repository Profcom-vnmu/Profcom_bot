using MediatR;
using Microsoft.Extensions.Logging;
using StudentUnionBot.Application.News.DTOs;
using StudentUnionBot.Core.Results;
using StudentUnionBot.Domain.Enums;
using StudentUnionBot.Domain.Interfaces;

namespace StudentUnionBot.Application.News.Queries.GetPublishedNews;

/// <summary>
/// Обробник запиту для отримання опублікованих новин
/// </summary>
public class GetPublishedNewsQueryHandler : IRequestHandler<GetPublishedNewsQuery, Result<NewsListDto>>
{
    private readonly INewsRepository _newsRepository;
    private readonly ILogger<GetPublishedNewsQueryHandler> _logger;

    public GetPublishedNewsQueryHandler(
        INewsRepository newsRepository,
        ILogger<GetPublishedNewsQueryHandler> logger)
    {
        _newsRepository = newsRepository;
        _logger = logger;
    }

    public async Task<Result<NewsListDto>> Handle(GetPublishedNewsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation(
                "Отримання новин: категорія={Category}, сторінка={Page}, розмір={Size}",
                request.Category,
                request.PageNumber,
                request.PageSize);

            // Отримуємо новини
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

            _logger.LogInformation(
                "Знайдено {Count} новин (всього {Total})",
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

    private static NewsDto MapToDto(Domain.Entities.News news)
    {
        return new NewsDto
        {
            Id = news.Id,
            Title = news.Title,
            Content = news.Content,
            Summary = news.Summary,
            Category = news.Category.GetDisplayName(),
            CategoryEmoji = news.Category.GetEmoji(),
            AuthorName = news.AuthorName,
            PhotoFileId = news.PhotoFileId,
            DocumentFileId = news.DocumentFileId,
            IsPublished = news.IsPublished,
            IsPinned = news.IsPinned,
            CreatedAt = news.CreatedAt,
            PublishAt = news.PublishAt,
            ViewCount = news.ViewCount
        };
    }
}
