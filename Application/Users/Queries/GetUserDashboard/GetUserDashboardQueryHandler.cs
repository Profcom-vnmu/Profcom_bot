using MediatR;
using Microsoft.Extensions.Logging;
using StudentUnionBot.Application.Users.DTOs;
using StudentUnionBot.Core.Results;
using StudentUnionBot.Domain.Enums;
using StudentUnionBot.Domain.Interfaces;

namespace StudentUnionBot.Application.Users.Queries.GetUserDashboard;

/// <summary>
/// Handler для отримання персоналізованого dashboard користувача
/// </summary>
public class GetUserDashboardQueryHandler : IRequestHandler<GetUserDashboardQuery, Result<UserDashboardDto>>
{
    private readonly IUserRepository _userRepository;
    private readonly IAppealRepository _appealRepository;
    private readonly IEventRepository _eventRepository;
    private readonly ILogger<GetUserDashboardQueryHandler> _logger;

    public GetUserDashboardQueryHandler(
        IUserRepository userRepository,
        IAppealRepository appealRepository,
        IEventRepository eventRepository,
        ILogger<GetUserDashboardQueryHandler> logger)
    {
        _userRepository = userRepository;
        _appealRepository = appealRepository;
        _eventRepository = eventRepository;
        _logger = logger;
    }

    public async Task<Result<UserDashboardDto>> Handle(
        GetUserDashboardQuery request, 
        CancellationToken cancellationToken)
    {
        try
        {
            // Отримуємо користувача
            var user = await _userRepository.GetByTelegramIdAsync(request.TelegramId, cancellationToken);

            if (user == null)
            {
                return Result<UserDashboardDto>.Fail("Користувача не знайдено");
            }

            // Визначаємо, чи користувач новий (без email або перший вхід за останні 7 днів)
            var isNewUser = string.IsNullOrWhiteSpace(user.Email) || 
                            user.JoinedAt > DateTime.UtcNow.AddDays(-7);

            // Збираємо статистику користувача
            var statistics = await GetUserStatisticsAsync(user.TelegramId, cancellationToken);

            // Генеруємо персоналізовані Quick Actions
            var quickActions = await GenerateQuickActionsAsync(user, statistics, isNewUser, cancellationToken);

            // Отримуємо останні нотифікації
            var recentNotifications = await GetRecentNotificationsAsync(user.TelegramId, cancellationToken);

            // Створюємо UserDto
            var userDto = new UserDto
            {
                TelegramId = user.TelegramId,
                Username = user.Username,
                FirstName = user.FirstName,
                LastName = user.LastName,
                FullName = user.FullName,
                Faculty = user.Faculty,
                Course = user.Course,
                Group = user.Group,
                Email = user.Email,
                IsEmailVerified = user.IsEmailVerified,
                Language = user.Language.ToString().ToLower(),
                Role = user.Role,
                IsActive = user.IsActive,
                JoinedAt = user.JoinedAt,
                RoleName = user.Role.ToString()
            };

            var dashboard = new UserDashboardDto
            {
                User = userDto,
                QuickActions = quickActions,
                RecentNotifications = recentNotifications,
                Statistics = statistics,
                IsNewUser = isNewUser
            };

            return Result<UserDashboardDto>.Ok(dashboard);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Помилка при отриманні dashboard для користувача {TelegramId}", request.TelegramId);
            return Result<UserDashboardDto>.Fail("Помилка при завантаженні дашборда");
        }
    }

    /// <summary>
    /// Отримання статистики користувача
    /// </summary>
    private async Task<UserStatisticsDto> GetUserStatisticsAsync(long telegramId, CancellationToken cancellationToken)
    {
        // Отримуємо активні звернення
        var userAppeals = await _appealRepository.GetUserAppealsAsync(telegramId, cancellationToken);
        
        var activeAppeals = userAppeals.Count(a => 
            a.Status == AppealStatus.New || 
            a.Status == AppealStatus.InProgress ||
            a.Status == AppealStatus.WaitingForStudent ||
            a.Status == AppealStatus.WaitingForAdmin ||
            a.Status == AppealStatus.Escalated);
        
        // Підраховуємо нові відповіді (звернення з повідомленнями від адміна після останнього повідомлення користувача)
        var appealsWithNewReplies = 0;
        foreach (var appeal in userAppeals.Where(a => a.Status != AppealStatus.Closed))
        {
            var appealWithMessages = await _appealRepository.GetByIdWithMessagesAsync(appeal.Id, cancellationToken);
            if (appealWithMessages?.Messages != null && appealWithMessages.Messages.Any())
            {
                var lastAdminMessage = appealWithMessages.Messages
                    .Where(m => m.IsFromAdmin)
                    .OrderByDescending(m => m.SentAt)
                    .FirstOrDefault();
                
                var lastUserMessage = appealWithMessages.Messages
                    .Where(m => !m.IsFromAdmin)
                    .OrderByDescending(m => m.SentAt)
                    .FirstOrDefault();

                if (lastAdminMessage != null && 
                    (lastUserMessage == null || lastAdminMessage.SentAt > lastUserMessage.SentAt))
                {
                    appealsWithNewReplies++;
                }
            }
        }

        // Отримуємо майбутні події
        var upcomingEvents = await _eventRepository.GetUpcomingEventsAsync(
            type: null,
            onlyFeatured: false,
            pageNumber: 1,
            pageSize: 100,
            cancellationToken: cancellationToken);

        // Підраховуємо події на найближчі 7 днів
        var upcomingEventsCount = upcomingEvents.Count(e => 
            e.StartDate <= DateTime.UtcNow.AddDays(7));

        return new UserStatisticsDto
        {
            ActiveAppeals = activeAppeals,
            TotalAppeals = userAppeals.Count,
            NewReplies = appealsWithNewReplies,
            UpcomingEvents = upcomingEventsCount,
            RegisteredEvents = 0 // TODO: Додати коли буде реалізована реєстрація на події
        };
    }

    /// <summary>
    /// Генерація персоналізованих Quick Actions на основі активності користувача
    /// </summary>
    private async Task<List<QuickActionDto>> GenerateQuickActionsAsync(
        Domain.Entities.BotUser user, 
        UserStatisticsDto statistics, 
        bool isNewUser,
        CancellationToken cancellationToken)
    {
        var actions = new List<(QuickActionDto Action, int Priority)>();

        // 🎓 Tutorial для нових користувачів (найвищий пріоритет)
        if (isNewUser)
        {
            actions.Add((new QuickActionDto
            {
                Title = "Почати навчання",
                CallbackData = "tutorial_start",
                Description = "Дізнайся, як користуватись ботом",
                Type = QuickActionType.Tutorial,
                Priority = 100,
                Emoji = "🎓"
            }, 100));
        }

        // ⚠️ Нові відповіді на звернення (високий пріоритет)
        if (statistics.NewReplies > 0)
        {
            actions.Add((new QuickActionDto
            {
                Title = $"Нові відповіді ({statistics.NewReplies})",
                CallbackData = "view_my_appeals",
                Description = "У вас є нові відповіді від адміністраторів",
                Type = QuickActionType.ViewAppeals,
                Priority = 90,
                Emoji = "💬"
            }, 90));
        }

        // 📋 Мої звернення
        if (statistics.ActiveAppeals > 0)
        {
            actions.Add((new QuickActionDto
            {
                Title = $"Мої звернення ({statistics.ActiveAppeals})",
                CallbackData = "view_my_appeals",
                Description = "Переглянути активні звернення",
                Type = QuickActionType.ViewAppeals,
                Priority = 80,
                Emoji = "📋"
            }, 80));
        }
        else
        {
            actions.Add((new QuickActionDto
            {
                Title = "Створити звернення",
                CallbackData = "create_appeal",
                Description = "Написати профкому",
                Type = QuickActionType.CreateAppeal,
                Priority = 70,
                Emoji = "✍️"
            }, 70));
        }

        // 📅 Майбутні події
        if (statistics.UpcomingEvents > 0)
        {
            actions.Add((new QuickActionDto
            {
                Title = $"Події ({statistics.UpcomingEvents})",
                CallbackData = "view_events",
                Description = "Переглянути найближчі події",
                Type = QuickActionType.ViewEvents,
                Priority = 60,
                Emoji = "📅"
            }, 60));
        }

        //  Профіль (якщо не заповнений)
        if (string.IsNullOrWhiteSpace(user.Faculty) || user.Course == null)
        {
            actions.Add((new QuickActionDto
            {
                Title = "Заповнити профіль",
                CallbackData = "edit_profile",
                Description = "Вкажіть факультет та курс",
                Type = QuickActionType.Profile,
                Priority = 65,
                Emoji = "👤"
            }, 65));
        }

        // Сортуємо за пріоритетом і беремо топ-4
        var topActions = actions
            .OrderByDescending(a => a.Priority)
            .Take(4)
            .Select(a => a.Action)
            .ToList();

        return await Task.FromResult(topActions);
    }

    /// <summary>
    /// Отримання останніх нотифікацій користувача
    /// </summary>
    private async Task<List<NotificationItemDto>> GetRecentNotificationsAsync(
        long telegramId, 
        CancellationToken cancellationToken)
    {
        var notifications = new List<NotificationItemDto>();

        // Нотифікації про нові відповіді на звернення
        var userAppeals = await _appealRepository.GetUserAppealsAsync(telegramId, cancellationToken);
        
        foreach (var appeal in userAppeals.Where(a => a.Status != AppealStatus.Closed).Take(3))
        {
            var appealWithMessages = await _appealRepository.GetByIdWithMessagesAsync(appeal.Id, cancellationToken);
            if (appealWithMessages?.Messages != null && appealWithMessages.Messages.Any())
            {
                var lastAdminMessage = appealWithMessages.Messages
                    .Where(m => m.IsFromAdmin)
                    .OrderByDescending(m => m.SentAt)
                    .FirstOrDefault();

                if (lastAdminMessage != null)
                {
                    notifications.Add(new NotificationItemDto
                    {
                        Message = $"Нова відповідь на звернення #{appeal.Id}: " + 
                                  (lastAdminMessage.Text.Length > 50 
                                      ? lastAdminMessage.Text.Substring(0, 47) + "..." 
                                      : lastAdminMessage.Text),
                        Icon = "💬",
                        CallbackData = $"appeal_view_{appeal.Id}",
                        Timestamp = lastAdminMessage.SentAt,
                        IsRead = false
                    });
                }
            }
        }

        // Нотифікації про майбутні події (в найближчі 3 дні)
        var upcomingEvents = await _eventRepository.GetUpcomingEventsAsync(
            type: null,
            onlyFeatured: false,
            pageNumber: 1,
            pageSize: 5,
            cancellationToken: cancellationToken);

        foreach (var evt in upcomingEvents.Where(e => e.StartDate <= DateTime.UtcNow.AddDays(3)).Take(2))
        {
            notifications.Add(new NotificationItemDto
            {
                Message = $"Подія: {evt.Title} - Початок: {evt.StartDate:dd.MM.yyyy HH:mm}",
                Icon = "📅",
                CallbackData = $"event_view_{evt.Id}",
                Timestamp = evt.CreatedAt,
                IsRead = false
            });
        }

        return notifications
            .OrderByDescending(n => n.Timestamp)
            .Take(5)
            .ToList();
    }
}
