using MediatR;
using Microsoft.Extensions.Logging;
using StudentUnionBot.Application.News.DTOs;
using StudentUnionBot.Core.Results;
using StudentUnionBot.Domain.Interfaces;

namespace StudentUnionBot.Application.News.Commands.UpdateNews;

/// <summary>
/// Handler для оновлення існуючої новини
/// </summary>
public class UpdateNewsCommandHandler : IRequestHandler<UpdateNewsCommand, Result<NewsDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UpdateNewsCommandHandler> _logger;

    public UpdateNewsCommandHandler(
        IUnitOfWork unitOfWork,
        ILogger<UpdateNewsCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<NewsDto>> Handle(UpdateNewsCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation(
                "Updating news article {NewsId} by editor {EditorId}",
                request.NewsId,
                request.EditorId
            );

            // Перевіряємо чи існує новина
            var news = await _unitOfWork.News.GetByIdAsync(request.NewsId, cancellationToken);
            if (news == null)
            {
                _logger.LogWarning("News article {NewsId} not found", request.NewsId);
                return Result<NewsDto>.Fail("Новину не знайдено");
            }

            // Перевіряємо права редактора (може бути автором або мати відповідні права)
            var editor = await _unitOfWork.Users.GetByTelegramIdAsync(request.EditorId, cancellationToken);
            if (editor == null)
            {
                _logger.LogWarning("Editor {EditorId} not found", request.EditorId);
                return Result<NewsDto>.Fail("Редактор не знайдений");
            }

            // Оновлюємо поля, якщо вони вказані
            if (request.Title != null || request.Content != null || 
                request.Summary != null || request.Category.HasValue)
            {
                news.Update(
                    title: request.Title ?? news.Title,
                    content: request.Content ?? news.Content,
                    category: request.Category ?? news.Category,
                    summary: request.Summary ?? news.Summary,
                    // TODO: Додати підтримку множинних файлів
                    photoFileId: GetFirstImageFile(request.AttachmentFileIds) ?? news.PhotoFileId,
                    documentFileId: GetFirstDocumentFile(request.AttachmentFileIds) ?? news.DocumentFileId
                );
            }

            // Зберігаємо зміни
            _unitOfWork.News.Update(news);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Successfully updated news article {NewsId}",
                request.NewsId
            );

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
                Tags = request.Tags ?? string.Empty
            };

            return Result<NewsDto>.Ok(newsDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error updating news article {NewsId}",
                request.NewsId
            );
            return Result<NewsDto>.Fail("Помилка при оновленні новини");
        }
    }

    /// <summary>
    /// Отримує перший файл зображення з прикріплених файлів
    /// </summary>
    private string? GetFirstImageFile(List<string>? fileIds)
    {
        return fileIds?.FirstOrDefault();
    }

    /// <summary>
    /// Отримує перший файл документа з прикріплених файлів
    /// </summary>
    private string? GetFirstDocumentFile(List<string>? fileIds)
    {
        return fileIds?.Skip(1).FirstOrDefault();
    }
}