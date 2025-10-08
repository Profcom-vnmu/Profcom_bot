using MediatR;
using Microsoft.Extensions.Logging;
using StudentUnionBot.Application.Appeals.DTOs;
using StudentUnionBot.Application.Common.Interfaces;
using StudentUnionBot.Core.Results;
using StudentUnionBot.Domain.Entities;
using StudentUnionBot.Domain.Enums;
using StudentUnionBot.Domain.Interfaces;

namespace StudentUnionBot.Application.Appeals.Commands.CreateAppeal;

/// <summary>
/// Обробник команди створення звернення
/// </summary>
public class CreateAppealCommandHandler : IRequestHandler<CreateAppealCommand, Result<AppealDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IRateLimiter _rateLimiter;
    private readonly ILogger<CreateAppealCommandHandler> _logger;

    public CreateAppealCommandHandler(
        IUnitOfWork unitOfWork,
        IRateLimiter rateLimiter,
        ILogger<CreateAppealCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _rateLimiter = rateLimiter;
        _logger = logger;
    }

    public async Task<Result<AppealDto>> Handle(CreateAppealCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation(
                "Створення звернення для студента {StudentId} з темою {Subject}",
                request.StudentId,
                request.Subject);

            // Перевірка rate limit
            var isAllowed = await _rateLimiter.AllowAsync(request.StudentId, "CreateAppeal", cancellationToken);
            if (!isAllowed)
            {
                var timeUntilReset = await _rateLimiter.GetTimeUntilResetAsync(request.StudentId, "CreateAppeal", cancellationToken);
                var waitMinutes = timeUntilReset?.TotalMinutes ?? 10;
                
                _logger.LogWarning(
                    "Rate limit exceeded для користувача {StudentId} при створенні звернення",
                    request.StudentId);

                return Result<AppealDto>.Fail(
                    $"⏱ Ви перевищили ліміт створення звернень.\n" +
                    $"Зачекайте {Math.Ceiling(waitMinutes)} хв. перед наступним зверненням.\n\n" +
                    $"💡 Це обмеження захищає систему від спаму.");
            }

            // Перевіряємо чи існує користувач
            var user = await _unitOfWork.Users.GetByTelegramIdAsync(request.StudentId, cancellationToken);
            if (user == null)
            {
                _logger.LogWarning("Користувач {StudentId} не знайдений", request.StudentId);
                return Result<AppealDto>.Fail("Користувач не знайдений у системі");
            }

            // Перевіряємо чи користувач не заблокований
            if (user.IsBanned)
            {
                _logger.LogWarning("Спроба створити звернення заблокованим користувачем {StudentId}", request.StudentId);
                return Result<AppealDto>.Fail($"Ваш обліковий запис заблоковано. Причина: {user.BanReason}");
            }

            // Створюємо звернення через factory method
            var appeal = Appeal.Create(
                studentId: request.StudentId,
                studentName: request.StudentName,
                category: request.Category,
                subject: request.Subject,
                message: request.Message);

            // Зберігаємо в БД
            await _unitOfWork.Appeals.AddAsync(appeal, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Звернення {AppealId} успішно створено для студента {StudentId}",
                appeal.Id,
                request.StudentId);

            // Маппінг на DTO
            var dto = MapToDto(appeal);

            return Result<AppealDto>.Ok(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Помилка при створенні звернення для студента {StudentId}", request.StudentId);
            return Result<AppealDto>.Fail("Виникла помилка при створенні звернення. Спробуйте пізніше.");
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
            MessageCount = 0 // Нове звернення без додаткових повідомлень
        };
    }
}
