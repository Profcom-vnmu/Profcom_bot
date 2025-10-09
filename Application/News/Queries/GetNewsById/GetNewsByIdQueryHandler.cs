using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StudentUnionBot.Application.News.DTOs;
using StudentUnionBot.Core.Results;
using StudentUnionBot.Domain.Interfaces;

namespace StudentUnionBot.Application.News.Queries.GetNewsById;

/// <summary>
/// Обробник запиту для отримання новини за ID
/// </summary>
public class GetNewsByIdQueryHandler : IRequestHandler<GetNewsByIdQuery, Result<NewsDto>>
{
    private readonly INewsRepository _newsRepository;
    private readonly ILogger<GetNewsByIdQueryHandler> _logger;

    public GetNewsByIdQueryHandler(
        INewsRepository newsRepository,
        ILogger<GetNewsByIdQueryHandler> logger)
    {
        _newsRepository = newsRepository;
        _logger = logger;
    }

    public async Task<Result<NewsDto>> Handle(GetNewsByIdQuery request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Отримання новини з ID: {NewsId}", request.NewsId);

            var newsEntity = await _newsRepository.GetByIdAsync(request.NewsId, cancellationToken);

            if (newsEntity == null)
            {
                _logger.LogWarning("Новина з ID {NewsId} не знайдена", request.NewsId);
                return Result<NewsDto>.Fail($"Новина з ID {request.NewsId} не знайдена");
            }

            var news = new NewsDto
            {
                Id = newsEntity.Id,
                Title = newsEntity.Title,
                Content = newsEntity.Content,
                Category = newsEntity.Category,
                IsPinned = newsEntity.IsPinned,
                AuthorId = newsEntity.AuthorId,
                AuthorName = newsEntity.AuthorName,
                CreatedAt = newsEntity.CreatedAt,
                UpdatedAt = newsEntity.UpdatedAt,
                IsPublished = newsEntity.IsPublished,
                PublishAt = newsEntity.PublishAt,
                ViewCount = newsEntity.ViewCount,
                Summary = newsEntity.Summary,
                PhotoFileId = newsEntity.PhotoFileId,
                DocumentFileId = newsEntity.DocumentFileId
            };

            _logger.LogInformation("Новина знайдена: {Title}", news.Title);
            return Result<NewsDto>.Ok(news);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Помилка при отриманні новини з ID: {NewsId}", request.NewsId);
            return Result<NewsDto>.Fail("Не вдалося завантажити новину");
        }
    }
}