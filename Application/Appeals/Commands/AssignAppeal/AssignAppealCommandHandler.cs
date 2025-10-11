using MediatR;
using Microsoft.Extensions.Logging;
using StudentUnionBot.Application.Appeals.DTOs;
using StudentUnionBot.Core.Results;
using StudentUnionBot.Domain.Entities;
using StudentUnionBot.Domain.Enums;
using StudentUnionBot.Domain.Interfaces;

namespace StudentUnionBot.Application.Appeals.Commands.AssignAppeal;

/// <summary>
/// Обробник команди призначення апела
/// </summary>
public class AssignAppealCommandHandler : IRequestHandler<AssignAppealCommand, Result<AppealDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAppealAssignmentService _assignmentService;
    private readonly INotificationService _notificationService;
    private readonly ILogger<AssignAppealCommandHandler> _logger;

    public AssignAppealCommandHandler(
        IUnitOfWork unitOfWork,
        IAppealAssignmentService assignmentService,
        INotificationService notificationService,
        ILogger<AssignAppealCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _assignmentService = assignmentService;
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task<Result<AppealDto>> Handle(AssignAppealCommand request, CancellationToken cancellationToken)
    {
        try
        {
            // Перевіряємо чи існує апел
            var appeal = await _unitOfWork.Appeals.GetByIdAsync(request.AppealId, cancellationToken);
            if (appeal == null)
            {
                return Result<AppealDto>.Fail("Апел не знайдено");
            }

            // Перевіряємо права користувача
            var assignedByUser = await _unitOfWork.Users.GetByTelegramIdAsync(request.AssignedByUserId, cancellationToken);
            if (assignedByUser == null || (assignedByUser.Role != UserRole.Admin && assignedByUser.Role != UserRole.SuperAdmin))
            {
                return Result<AppealDto>.Fail("Тільки адміністратори можуть призначати апели");
            }

            BotUser? assignedAdmin = null;

            if (request.AdminId.HasValue)
            {
                // Ручне призначення
                var manualAssignResult = await _assignmentService.AssignAppealToAdminAsync(
                    appeal, request.AdminId.Value, cancellationToken);
                
                if (!manualAssignResult.IsSuccess)
                {
                    return Result<AppealDto>.Fail(manualAssignResult.Error);
                }

                assignedAdmin = await _unitOfWork.Users.GetByTelegramIdAsync(request.AdminId.Value, cancellationToken);
                
                _logger.LogInformation(
                    "Апел {AppealId} вручну призначено адміністратору {AdminId} користувачем {AssignedBy}. Причина: {Reason}",
                    request.AppealId, request.AdminId.Value, request.AssignedByUserId, request.Reason ?? "Не вказано");
            }
            else
            {
                // Автоматичне призначення
                var autoAssignResult = await _assignmentService.AssignAppealAsync(appeal, cancellationToken);
                
                if (!autoAssignResult.IsSuccess)
                {
                    return Result<AppealDto>.Fail(autoAssignResult.Error);
                }

                assignedAdmin = autoAssignResult.Value!;
                
                _logger.LogInformation(
                    "Апел {AppealId} автоматично призначено адміністратору {AdminId}",
                    request.AppealId, assignedAdmin.TelegramId);
            }

            // Оновлюємо апел
            appeal.AssignTo(assignedAdmin!.TelegramId);

            // Зберігаємо зміни
            _unitOfWork.Appeals.Update(appeal);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Відправляємо сповіщення призначеному адміністратору
            var notificationResult = await _notificationService.CreateAndSendNotificationAsync(
                userId: assignedAdmin.TelegramId,
                notificationEvent: NotificationEvent.AppealAssigned,
                type: NotificationType.Push,
                title: "👤 Нове призначення",
                message: $"Вам призначено звернення\n" +
                        $"Категорія: {appeal.Category.GetDisplayName()}\n" +
                        $"Тема: {appeal.Subject}\n" +
                        $"Пріоритет: {appeal.Priority.GetDisplayName()}\n" +
                        $"ID: #{appeal.Id}",
                priority: appeal.Priority == AppealPriority.Urgent ? NotificationPriority.Critical :
                         appeal.Priority == AppealPriority.High ? NotificationPriority.High :
                         NotificationPriority.Normal,
                relatedAppealId: appeal.Id,
                cancellationToken: cancellationToken);

            if (notificationResult.IsSuccess)
            {
                _logger.LogInformation(
                    "Відправлено сповіщення адміністратору {AdminId} про призначення звернення {AppealId}",
                    assignedAdmin.TelegramId,
                    appeal.Id);
            }
            else
            {
                _logger.LogWarning(
                    "Не вдалося відправити сповіщення адміністратору {AdminId} про призначення: {Error}",
                    assignedAdmin.TelegramId,
                    notificationResult.Error);
            }

            return Result<AppealDto>.Ok(MapToDto(appeal));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Помилка при призначенні апела {AppealId}", request.AppealId);
            return Result<AppealDto>.Fail("Виникла помилка при призначенні апела. Спробуйте пізніше.");
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
            MessageCount = 0 // Will be calculated elsewhere if needed
        };
    }
}
