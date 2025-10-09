using MediatR;
using Microsoft.Extensions.Logging;
using StudentUnionBot.Application.Appeals.DTOs;
using StudentUnionBot.Core.Results;
using StudentUnionBot.Domain.Enums;
using StudentUnionBot.Domain.Interfaces;

namespace StudentUnionBot.Application.Appeals.Queries.GetAppealById;

/// <summary>
/// Обробник запиту для отримання детальної інформації про звернення
/// </summary>
public class GetAppealByIdQueryHandler : IRequestHandler<GetAppealByIdQuery, Result<AppealDetailsDto>>
{
    private readonly IAppealRepository _appealRepository;
    private readonly IUserRepository _userRepository;
    private readonly ILogger<GetAppealByIdQueryHandler> _logger;

    public GetAppealByIdQueryHandler(
        IAppealRepository appealRepository,
        IUserRepository userRepository,
        ILogger<GetAppealByIdQueryHandler> logger)
    {
        _appealRepository = appealRepository;
        _userRepository = userRepository;
        _logger = logger;
    }

    public async Task<Result<AppealDetailsDto>> Handle(
        GetAppealByIdQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation(
                "Отримання звернення {AppealId} для користувача {UserId}",
                request.AppealId,
                request.RequestUserId);

            // Отримуємо звернення з повідомленнями
            var appeal = await _appealRepository.GetByIdWithMessagesAsync(
                request.AppealId,
                cancellationToken);

            if (appeal == null)
            {
                _logger.LogWarning(
                    "Звернення {AppealId} не знайдено",
                    request.AppealId);
                return Result<AppealDetailsDto>.Fail("Звернення не знайдено");
            }

            // Перевіряємо доступ: власник або адміністратор
            var isOwner = appeal.StudentId == request.RequestUserId;
            var isAdmin = false;

            if (!isOwner)
            {
                var user = await _userRepository.GetByTelegramIdAsync(request.RequestUserId, cancellationToken);
                isAdmin = user?.Role == UserRole.Admin;
            }

            if (!isOwner && !isAdmin)
            {
                _logger.LogWarning(
                    "Користувач {UserId} намагається отримати доступ до чужого звернення {AppealId}",
                    request.RequestUserId,
                    request.AppealId);
                return Result<AppealDetailsDto>.Fail("У вас немає доступу до цього звернення");
            }

            // Маппінг на DTO
            var dto = new AppealDetailsDto
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
                ClosedBy = appeal.ClosedBy,
                ClosedReason = appeal.ClosedReason,
                Messages = appeal.Messages.Select(m => new AppealMessageDto
                {
                    Id = m.Id,
                    AppealId = m.AppealId,
                    SenderId = m.SenderId,
                    SenderName = m.SenderName,
                    IsFromAdmin = m.IsFromAdmin,
                    Text = m.Text,
                    PhotoFileId = m.PhotoFileId,
                    DocumentFileId = m.DocumentFileId,
                    DocumentFileName = m.DocumentFileName,
                    SentAt = m.SentAt
                }).ToList()
            };

            return Result<AppealDetailsDto>.Ok(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Помилка при отриманні звернення {AppealId}", request.AppealId);
            return Result<AppealDetailsDto>.Fail("Виникла помилка при завантаженні звернення");
        }
    }
}
