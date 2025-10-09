using MediatR;
using Microsoft.Extensions.Logging;
using StudentUnionBot.Application.Appeals.DTOs;
using StudentUnionBot.Core.Results;
using StudentUnionBot.Domain.Enums;
using StudentUnionBot.Domain.Interfaces;

namespace StudentUnionBot.Application.Appeals.Queries.GetAdminAppeals;

/// <summary>
/// Обробник запиту для отримання звернень адміністратором
/// </summary>
public class GetAdminAppealsQueryHandler : IRequestHandler<GetAdminAppealsQuery, Result<AppealListDto>>
{
    private readonly IAppealRepository _appealRepository;
    private readonly IUserRepository _userRepository;
    private readonly ILogger<GetAdminAppealsQueryHandler> _logger;

    public GetAdminAppealsQueryHandler(
        IAppealRepository appealRepository,
        IUserRepository userRepository,
        ILogger<GetAdminAppealsQueryHandler> logger)
    {
        _appealRepository = appealRepository;
        _userRepository = userRepository;
        _logger = logger;
    }

    public async Task<Result<AppealListDto>> Handle(
        GetAdminAppealsQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            // Перевіряємо чи є користувач адміністратором
            var admin = await _userRepository.GetByTelegramIdAsync(request.AdminId, cancellationToken);
            if (admin == null || admin.Role != UserRole.Admin)
            {
                _logger.LogWarning(
                    "Користувач {AdminId} не є адміністратором",
                    request.AdminId);
                return Result<AppealListDto>.Fail("У вас немає прав адміністратора");
            }

            _logger.LogInformation(
                "Отримання звернень адміністратором {AdminId} з фільтрами: Status={Status}, Category={Category}, Priority={Priority}",
                request.AdminId,
                request.Status,
                request.Category,
                request.Priority);

            // Отримуємо всі звернення
            var query = await _appealRepository.GetAllAsync(cancellationToken);

            // Застосовуємо фільтри
            if (request.Status.HasValue)
            {
                query = query.Where(a => a.Status == request.Status.Value).ToList();
            }

            if (request.Category.HasValue)
            {
                query = query.Where(a => a.Category == request.Category.Value).ToList();
            }

            if (request.Priority.HasValue)
            {
                query = query.Where(a => a.Priority == request.Priority.Value).ToList();
            }

            if (request.AssignedToAdminId.HasValue)
            {
                query = query.Where(a => a.AssignedToAdminId == request.AssignedToAdminId.Value).ToList();
            }

            if (request.OnlyUnassigned)
            {
                query = query.Where(a => a.AssignedToAdminId == null).ToList();
            }

            if (request.OnlyMy)
            {
                query = query.Where(a => a.AssignedToAdminId == request.AdminId).ToList();
            }

            if (!string.IsNullOrWhiteSpace(request.SearchText))
            {
                var searchLower = request.SearchText.ToLower();
                query = query.Where(a =>
                    a.Subject.ToLower().Contains(searchLower) ||
                    a.Message.ToLower().Contains(searchLower) ||
                    a.StudentName.ToLower().Contains(searchLower)
                ).ToList();
            }

            // Сортування
            query = request.SortBy.ToLower() switch
            {
                "updatedat" => request.Descending
                    ? query.OrderByDescending(a => a.UpdatedAt).ToList()
                    : query.OrderBy(a => a.UpdatedAt).ToList(),
                "priority" => request.Descending
                    ? query.OrderByDescending(a => a.Priority).ToList()
                    : query.OrderBy(a => a.Priority).ToList(),
                "status" => request.Descending
                    ? query.OrderByDescending(a => a.Status).ToList()
                    : query.OrderBy(a => a.Status).ToList(),
                _ => request.Descending
                    ? query.OrderByDescending(a => a.CreatedAt).ToList()
                    : query.OrderBy(a => a.CreatedAt).ToList()
            };

            var totalCount = query.Count;

            // Пагінація
            var appeals = query
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToList();

            _logger.LogInformation(
                "Знайдено {Count} звернень для адміністратора {AdminId} (сторінка {Page})",
                totalCount,
                request.AdminId,
                request.PageNumber);

            // Маппінг на DTO
            var appealsDto = appeals.Select(a => new AppealDto
            {
                Id = a.Id,
                StudentId = a.StudentId,
                StudentName = a.StudentName,
                Category = a.Category,
                CategoryName = a.Category.GetDisplayName(),
                Subject = a.Subject,
                Message = a.Message,
                Status = a.Status,
                StatusName = a.Status.GetDisplayName(),
                Priority = a.Priority,
                PriorityName = a.Priority.GetDisplayName(),
                AssignedToAdminId = a.AssignedToAdminId,
                CreatedAt = a.CreatedAt,
                UpdatedAt = a.UpdatedAt,
                FirstResponseAt = a.FirstResponseAt,
                ClosedAt = a.ClosedAt,
                MessageCount = a.Messages?.Count ?? 0
            }).ToList();

            var result = new AppealListDto
            {
                Appeals = appealsDto,
                TotalCount = totalCount,
                PageNumber = request.PageNumber,
                PageSize = request.PageSize
            };

            return Result<AppealListDto>.Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Помилка при отриманні звернень адміністратором {AdminId}",
                request.AdminId);
            return Result<AppealListDto>.Fail("Виникла помилка при завантаженні звернень");
        }
    }
}
