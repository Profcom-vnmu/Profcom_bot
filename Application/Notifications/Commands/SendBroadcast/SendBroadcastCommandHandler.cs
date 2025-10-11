using MediatR;
using Microsoft.Extensions.Logging;
using StudentUnionBot.Core.Results;
using StudentUnionBot.Domain.Interfaces;
using StudentUnionBot.Domain.Enums;

namespace StudentUnionBot.Application.Notifications.Commands.SendBroadcast;

/// <summary>
/// Handler для масової розсилки повідомлень
/// </summary>
public class SendBroadcastCommandHandler : IRequestHandler<SendBroadcastCommand, Result<BroadcastResultDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPushNotificationProvider _pushProvider;
    private readonly IEmailService _emailService;
    private readonly ILogger<SendBroadcastCommandHandler> _logger;

    public SendBroadcastCommandHandler(
        IUnitOfWork unitOfWork,
        IPushNotificationProvider pushProvider,
        IEmailService emailService,
        ILogger<SendBroadcastCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _pushProvider = pushProvider;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<Result<BroadcastResultDto>> Handle(
        SendBroadcastCommand request, 
        CancellationToken cancellationToken)
    {
        var startTime = DateTime.UtcNow;
        var result = new BroadcastResultDto
        {
            StartedAt = startTime
        };

        try
        {
            _logger.LogInformation(
                "Starting broadcast from admin {AdminId}, type: {Type}, audience: {Audience}",
                request.AdminTelegramId,
                request.NotificationType,
                request.TargetAudience?.ToString() ?? "All"
            );

            // Перевіряємо чи існує адмін
            var admin = await _unitOfWork.Users.GetByTelegramIdAsync(
                request.AdminTelegramId, 
                cancellationToken
            );

            if (admin == null)
            {
                _logger.LogWarning("Admin {AdminId} not found", request.AdminTelegramId);
                return Result<BroadcastResultDto>.Fail("Адміністратор не знайдений");
            }

            // Якщо запланована розсилка - поки що не підтримується
            if (!request.SendImmediately)
            {
                _logger.LogWarning("Scheduled broadcasts not yet implemented");
                return Result<BroadcastResultDto>.Fail(
                    "Запланована розсилка поки не реалізована. Використовуйте негайну розсилку."
                );
            }

            // Отримуємо список користувачів для розсилки
            var targetUsers = await GetTargetUsersAsync(request.TargetAudience, cancellationToken);

            if (!targetUsers.Any())
            {
                _logger.LogWarning("No target users found for broadcast");
                result.Message = "Не знайдено жодного користувача для розсилки";
                result.CompletedAt = DateTime.UtcNow;
                return Result<BroadcastResultDto>.Ok(result);
            }

            result.TotalAttempts = targetUsers.Count;
            _logger.LogInformation("Broadcasting to {Count} users", targetUsers.Count);

            // Відправляємо повідомлення кожному користувачу
            foreach (var user in targetUsers)
            {
                try
                {
                    // Поки що підтримуємо тільки Push-повідомлення
                    // Email розсилка може бути додана пізніше
                    if (request.NotificationType == NotificationType.Push || 
                        request.NotificationType == NotificationType.Both)
                    {
                        var message = $"📢 <b>Розсилка від профкому</b>\n\n{request.Message}";
                        await _pushProvider.SendPushAsync(
                            user.TelegramId,
                            message,
                            cancellationToken
                        );
                    }

                    result.SuccessCount++;

                    // Невелика затримка між відправками
                    await Task.Delay(50, cancellationToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, 
                        "Failed to send broadcast to user {UserId}", 
                        user.TelegramId
                    );
                    result.FailureCount++;
                }
            }

            result.CompletedAt = DateTime.UtcNow;
            result.Message = $"Розсилка завершена. Успішно: {result.SuccessCount}, Помилок: {result.FailureCount}";

            _logger.LogInformation(
                "Broadcast completed. Success: {Success}, Failed: {Failed}, Duration: {Duration}ms",
                result.SuccessCount,
                result.FailureCount,
                (result.CompletedAt - result.StartedAt).TotalMilliseconds
            );

            return Result<BroadcastResultDto>.Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during broadcast execution");
            result.CompletedAt = DateTime.UtcNow;
            result.Message = "Помилка під час виконання розсилки";
            return Result<BroadcastResultDto>.Fail("Помилка під час виконання розсилки");
        }
    }

    /// <summary>
    /// Отримує список користувачів для розсилки на основі цільової аудиторії
    /// </summary>
    private async Task<List<Domain.Entities.BotUser>> GetTargetUsersAsync(
        BroadcastAudience? audience, 
        CancellationToken cancellationToken)
    {
        var allActiveUsers = await _unitOfWork.Users.GetActiveUsersAsync(cancellationToken);

        return audience switch
        {
            BroadcastAudience.AllUsers or null => allActiveUsers,
            
            BroadcastAudience.StudentsOnly => allActiveUsers
                .Where(u => u.Role == UserRole.Student)
                .ToList(),
            
            BroadcastAudience.AdminsOnly => allActiveUsers
                .Where(u => u.Role == UserRole.Admin || u.Role == UserRole.SuperAdmin)
                .ToList(),
            
            BroadcastAudience.UsersWithEmail => allActiveUsers
                .Where(u => !string.IsNullOrEmpty(u.Email))
                .ToList(),
            
            BroadcastAudience.ByLanguage => allActiveUsers
                .Where(u => u.Language == Language.Ukrainian)
                .ToList(),
            
            _ => allActiveUsers
        };
    }
}
