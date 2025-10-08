using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using StudentUnionBot.Application.Appeals.DTOs;
using StudentUnionBot.Core.Results;
using StudentUnionBot.Domain.Entities;
using StudentUnionBot.Domain.Enums;
using StudentUnionBot.Domain.Interfaces;

namespace StudentUnionBot.Application.Appeals.Queries.GetUserAppeals;

/// <summary>
/// Обробник запиту для отримання звернень користувача
/// </summary>
public class GetUserAppealsQueryHandler : IRequestHandler<GetUserAppealsQuery, Result<List<AppealDto>>>
{
    private readonly IAppealRepository _appealRepository;
    private readonly ILogger<GetUserAppealsQueryHandler> _logger;

    public GetUserAppealsQueryHandler(
        IAppealRepository appealRepository,
        ILogger<GetUserAppealsQueryHandler> logger)
    {
        _appealRepository = appealRepository;
        _logger = logger;
    }

    public async Task<Result<List<AppealDto>>> Handle(GetUserAppealsQuery request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation(
                "Отримання звернень користувача {UserId}, сторінка {Page}, тільки активні: {OnlyActive}",
                request.UserId,
                request.PageNumber,
                request.OnlyActive);

            // Отримуємо звернення користувача
            var appeals = await _appealRepository.GetUserAppealsAsync(request.UserId, cancellationToken);

            // Фільтр по активним зверненням
            if (request.OnlyActive)
            {
                appeals = appeals.Where(a => a.Status != AppealStatus.Closed).ToList();
            }

            // Сортування (нові перші)
            appeals = appeals.OrderByDescending(a => a.CreatedAt).ToList();

            // Пагінація
            var skip = (request.PageNumber - 1) * request.PageSize;
            var pagedAppeals = appeals
                .Skip(skip)
                .Take(request.PageSize)
                .ToList();

            // Маппінг на DTO
            var dtos = pagedAppeals.Select(MapToDto).ToList();

            _logger.LogInformation(
                "Знайдено {Count} звернень для користувача {UserId}",
                dtos.Count,
                request.UserId);

            return Result<List<AppealDto>>.Ok(dtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Помилка при отриманні звернень користувача {UserId}", request.UserId);
            return Result<List<AppealDto>>.Fail("Виникла помилка при отриманні звернень");
        }
    }

    private static AppealDto MapToDto(Appeal appeal)
    {
        return new AppealDto
        {
            Id = appeal.Id,
            StudentId = appeal.StudentId,
            StudentName = appeal.StudentName,
            Category = appeal.Category,
            CategoryName = appeal.Category.GetDisplayName(),
            Subject = appeal.Subject,
            Message = appeal.Message,
            Status = appeal.Status,
            StatusName = appeal.Status.GetDisplayName(),
            Priority = appeal.Priority,
            PriorityName = appeal.Priority.GetDisplayName(),
            AssignedToAdminId = appeal.AssignedToAdminId,
            CreatedAt = appeal.CreatedAt,
            UpdatedAt = appeal.UpdatedAt,
            FirstResponseAt = appeal.FirstResponseAt,
            ClosedAt = appeal.ClosedAt,
            Rating = appeal.Rating,
            RatingComment = appeal.RatingComment,
            MessageCount = appeal.Messages?.Count ?? 0
        };
    }
}
