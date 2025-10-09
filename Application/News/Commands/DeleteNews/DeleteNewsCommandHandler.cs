using MediatR;
using Microsoft.Extensions.Logging;
using StudentUnionBot.Core.Results;
using StudentUnionBot.Domain.Interfaces;

namespace StudentUnionBot.Application.News.Commands.DeleteNews;

/// <summary>
/// Handler для видалення новини
/// </summary>
public class DeleteNewsCommandHandler : IRequestHandler<DeleteNewsCommand, Result<bool>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DeleteNewsCommandHandler> _logger;

    public DeleteNewsCommandHandler(
        IUnitOfWork unitOfWork,
        ILogger<DeleteNewsCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Result<bool>> Handle(DeleteNewsCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation(
                "Deleting news article {NewsId} by deleter {DeleterId} (archive: {Archive})",
                request.NewsId,
                request.DeleterId,
                request.ArchiveInsteadOfDelete
            );

            // Перевіряємо чи існує новина
            var news = await _unitOfWork.News.GetByIdAsync(request.NewsId, cancellationToken);
            if (news == null)
            {
                _logger.LogWarning("News article {NewsId} not found", request.NewsId);
                return Result<bool>.Fail("Новину не знайдено");
            }

            // Перевіряємо права того, хто видаляє
            var deleter = await _unitOfWork.Users.GetByTelegramIdAsync(request.DeleterId, cancellationToken);
            if (deleter == null)
            {
                _logger.LogWarning("Deleter {DeleterId} not found", request.DeleterId);
                return Result<bool>.Fail("Користувач не знайдений");
            }

            // TODO: Додати перевірку прав доступу для видалення
            // if (news.AuthorId != request.DeleterId && !deleter.CanDeleteNews)

            // Логуємо причину видалення, якщо вказана
            if (!string.IsNullOrEmpty(request.DeletionReason))
            {
                _logger.LogInformation(
                    "News article {NewsId} deletion reason: {Reason}",
                    request.NewsId,
                    request.DeletionReason
                );
            }

            if (request.ArchiveInsteadOfDelete)
            {
                // Архівуємо новину (знімаємо з публікації)
                news.Unpublish();
                
                // TODO: Додати поле IsArchived до ентіті News
                // news.Archive(request.DeletionReason);
                
                _unitOfWork.News.Update(news);
                
                _logger.LogInformation(
                    "News article {NewsId} has been archived instead of deleted",
                    request.NewsId
                );
            }
            else
            {
                // Повністю видаляємо новину з бази даних
                _unitOfWork.News.Delete(news);
                
                _logger.LogWarning(
                    "News article {NewsId} has been permanently deleted by {DeleterId}",
                    request.NewsId,
                    request.DeleterId
                );
            }

            // Зберігаємо зміни
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Successfully processed deletion request for news article {NewsId}",
                request.NewsId
            );

            return Result<bool>.Ok(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Error deleting news article {NewsId}",
                request.NewsId
            );
            return Result<bool>.Fail("Помилка при видаленні новини");
        }
    }
}