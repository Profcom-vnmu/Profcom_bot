using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StudentUnionBot.Application.News.DTOs;
using StudentUnionBot.Core.Results;
using StudentUnionBot.Domain.Interfaces;
using StudentUnionBot.Domain.Enums;

namespace StudentUnionBot.Application.News.Queries.GetAllNews;

/// <summary>
/// Обробник запиту для отримання всіх новин (для адміністраторів)
/// </summary>
public class GetAllNewsQueryHandler : IRequestHandler<GetAllNewsQuery, Result<NewsListDto>>
{
    private readonly INewsRepository _newsRepository;
    private readonly ILogger<GetAllNewsQueryHandler> _logger;

    public GetAllNewsQueryHandler(
        INewsRepository newsRepository,
        ILogger<GetAllNewsQueryHandler> logger)
    {
        _newsRepository = newsRepository;
        _logger = logger;
    }

    public async Task<Result<NewsListDto>> Handle(GetAllNewsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation(
                "Отримання всіх новин: Category={Category}, Status={Status}, Page={Page}, PageSize={PageSize}",
                request.Category, request.Status, request.PageNumber, request.PageSize);

            // Мапування NewsStatus на параметри методу репозиторію
            bool? isPublished = null;
            bool? isArchived = null;

            if (request.Status.HasValue)
            {
                switch (request.Status.Value)
                {
                    case NewsStatus.Draft:
                        isPublished = false;
                        isArchived = false;
                        break;
                    case NewsStatus.Published:
                        isPublished = true;
                        isArchived = false;
                        break;
                    case NewsStatus.Archived:
                        isArchived = true;
                        break;
                }
            }

            // Отримання новин з репозиторію
            var news = await _newsRepository.GetAllNewsAsync(
                category: request.Category,
                isPublished: isPublished,
                isArchived: isArchived,
                sortByDateDesc: request.SortByDateDesc,
                pageNumber: request.PageNumber,
                pageSize: request.PageSize,
                cancellationToken: cancellationToken);

            // Підрахунок загальної кількості
            var totalCount = await _newsRepository.GetAllNewsCountAsync(
                category: request.Category,
                isPublished: isPublished,
                isArchived: isArchived,
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

            _logger.LogInformation("Знайдено {Count} новин з {TotalCount}", newsDto.Count, totalCount);

            return Result<NewsListDto>.Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Помилка при отриманні всіх новин");
            return Result<NewsListDto>.Fail("Не вдалося завантажити новини");
        }
    }

    private static NewsDto MapToDto(Domain.Entities.News news)
    {
        return new NewsDto
        {
            Id = news.Id,
            Title = news.Title,
            Content = news.Content,
            Category = news.Category,
            IsPinned = news.IsPinned,
            AuthorId = news.AuthorId,
            AuthorName = news.AuthorName,
            CreatedAt = news.CreatedAt,
            UpdatedAt = news.UpdatedAt,
            IsPublished = news.IsPublished,
            PublishAt = news.PublishAt,
            ViewCount = news.ViewCount,
            Summary = news.Summary,
            PhotoFileId = news.PhotoFileId,
            DocumentFileId = news.DocumentFileId
        };
    }
}