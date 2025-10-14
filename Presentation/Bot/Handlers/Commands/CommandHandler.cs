using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StudentUnionBot.Application.Common.Interfaces;
using StudentUnionBot.Application.Users.Queries.GetUserByTelegramId;
using StudentUnionBot.Application.Users.Queries.GetUserDashboard;
using StudentUnionBot.Core.Constants;
using StudentUnionBot.Domain.Enums;
using StudentUnionBot.Domain.Interfaces;
using StudentUnionBot.Presentation.Bot.Handlers.Common;
using StudentUnionBot.Presentation.Bot.Handlers.Interfaces;
using StudentUnionBot.Presentation.Bot.Keyboards;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace StudentUnionBot.Presentation.Bot.Handlers.Commands;

/// <summary>
/// Обробник команд бота (/start, /help, /appeal, /contacts тощо)
/// </summary>
public class CommandHandler : BaseHandler, ICommandHandler
{
    public CommandHandler(
        IServiceScopeFactory scopeFactory,
        ILogger<CommandHandler> logger,
        IMediator mediator)
        : base(scopeFactory, logger, mediator)
    {
    }

    /// <summary>
    /// Цей метод не використовується в CommandHandler, оскільки команди обробляються безпосередньо
    /// </summary>
    public override Task HandleTextMessageAsync(
        ITelegramBotClient botClient, 
        Message message, 
        UserConversationState state, 
        CancellationToken cancellationToken)
    {
        // CommandHandler обробляє команди безпосередньо через HandleCommandAsync
        throw new NotImplementedException("Використовуйте HandleCommandAsync для обробки команд");
    }

    /// <summary>
    /// Обробляє команди бота
    /// </summary>
    public async Task HandleCommandAsync(
        ITelegramBotClient botClient,
        Message message,
        CancellationToken cancellationToken)
    {
        try
        {
            var command = message.Text!.Split(' ')[0].ToLower();
            var userId = message.From!.Id;

            _logger.LogInformation("Обробка команди: {Command} від користувача {UserId}", command, userId);

            // Команди /start та /appeal скасовують поточний процес
            if (command == "/start" || command == "/appeal")
            {
                await ClearUserStateAsync(userId, cancellationToken);
            }

            // Перевіряємо, чи є користувач адміністратором
            var isAdmin = await IsUserAdminAsync(message.From.Id, cancellationToken);

            var (responseText, keyboard) = await GetCommandResponseAsync(command, isAdmin, userId, cancellationToken);

            await botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: responseText,
                parseMode: ParseMode.Html,
                replyMarkup: keyboard,
                cancellationToken: cancellationToken);

            _logger.LogInformation("Команда {Command} успішно оброблена", command);
        }
        catch (Exception ex)
        {
            await HandleErrorAsync(
                botClient, 
                message.Chat.Id, 
                ex, 
                $"HandleCommandAsync - команда: {message.Text}",
                cancellationToken);
        }
    }

    #region Private Methods

    /// <summary>
    /// Перевіряє, чи є користувач адміністратором
    /// </summary>
    private async Task<bool> IsUserAdminAsync(long telegramId, CancellationToken cancellationToken)
    {
        try
        {
            var getUserQuery = new GetUserByTelegramIdQuery { TelegramId = telegramId };
            var userResult = await _mediator.Send(getUserQuery, cancellationToken);
            var isAdmin = userResult.IsSuccess && userResult.Value?.Role == UserRole.Admin;

            _logger.LogInformation(
                "Користувач {TelegramId} має роль {Role}, isAdmin={IsAdmin}",
                telegramId,
                userResult.Value?.Role,
                isAdmin);

            return isAdmin;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Помилка при перевірці ролі користувача {TelegramId}", telegramId);
            return false;
        }
    }

    /// <summary>
    /// Формує відповідь на команду /start з привітальним повідомленням та персоналізованим dashboard
    /// </summary>
    private async Task<(string responseText, IReplyMarkup? keyboard)> GetStartCommandResponseAsync(
        long userId, 
        bool isAdmin, 
        CancellationToken cancellationToken)
    {
        try
        {
            // Отримуємо персоналізований dashboard
            var dashboardQuery = new GetUserDashboardQuery { TelegramId = userId };
            var dashboardResult = await _mediator.Send(dashboardQuery, cancellationToken);

            if (!dashboardResult.IsSuccess || dashboardResult.Value == null)
            {
                // Fallback - стандартне меню
                return await GetFallbackStartResponseAsync(userId, isAdmin, cancellationToken);
            }

            var dashboard = dashboardResult.Value;
            var language = dashboard.User.Language?.ToLower() == "en" 
                ? Language.English 
                : Language.Ukrainian;

            // Формуємо персоналізоване привітальне повідомлення
            var welcomeMessage = BuildPersonalizedWelcomeMessage(dashboard, language);

            // Створюємо персоналізовану клавіатуру
            using var scope = _scopeFactory.CreateScope();
            var localizationService = scope.ServiceProvider.GetRequiredService<ILocalizationService>();
            
            var keyboard = await KeyboardFactory.GetPersonalizedMainMenuKeyboardAsync(
                localizationService,
                language,
                dashboard,
                cancellationToken);

            return (welcomeMessage, keyboard);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Помилка при формуванні персоналізованого dashboard для користувача {UserId}", userId);
            return await GetFallbackStartResponseAsync(userId, isAdmin, cancellationToken);
        }
    }

    /// <summary>
    /// Fallback метод для /start (якщо dashboard не працює)
    /// </summary>
    private async Task<(string responseText, IReplyMarkup? keyboard)> GetFallbackStartResponseAsync(
        long userId,
        bool isAdmin,
        CancellationToken cancellationToken)
    {
        var getUserQuery = new GetUserByTelegramIdQuery { TelegramId = userId };
        var userResult = await _mediator.Send(getUserQuery, cancellationToken);

        var language = Language.Ukrainian;
        var isNewUser = true;

        if (userResult.IsSuccess && userResult.Value != null)
        {
            language = userResult.Value.Language?.ToLower() == "en" 
                ? Language.English 
                : Language.Ukrainian;
            isNewUser = string.IsNullOrWhiteSpace(userResult.Value.Email);
        }

        var welcomeMessage = WelcomeMessages.GetWelcomeMessage(language, isNewUser);
        var keyboard = await GetMainMenuAsync(userId, isAdmin, cancellationToken);

        return (welcomeMessage, keyboard);
    }

    /// <summary>
    /// Будує персоналізоване привітальне повідомлення
    /// </summary>
    private string BuildPersonalizedWelcomeMessage(
        Application.Users.DTOs.UserDashboardDto dashboard, 
        Language language)
    {
        var greeting = language == Language.English ? "Hello" : "Привіт";
        var userName = !string.IsNullOrWhiteSpace(dashboard.User.FirstName) 
            ? dashboard.User.FirstName 
            : "користувач";

        var message = $"🏠 {greeting}, {userName}! 👋\n\n";

        // Якщо новий користувач - спеціальне вітання
        if (dashboard.IsNewUser)
        {
            message += language == Language.English
                ? "Welcome to VNMU Student Union Bot! 🎓\n\n"
                : "Вітаємо у боті Профкому ВНМУ! 🎓\n\n";
            
            message += language == Language.English
                ? "I can help you with:\n"
                : "Я допоможу тобі:\n";
            
            message += language == Language.English
                ? "✅ Submit appeals\n✅ Stay informed with news\n✅ Register for events\n✅ Contact student union\n\n"
                : "✅ Створювати звернення\n✅ Дізнаватись новини\n✅ Реєструватись на події\n✅ Зв'язатись з профкомом\n\n";
        }
        else
        {
            // Показуємо сповіщення якщо є
            if (dashboard.RecentNotifications.Any())
            {
                var count = dashboard.RecentNotifications.Count;
                message += language == Language.English
                    ? $"🔔 NEW FOR YOU ({count}):\n"
                    : $"🔔 НОВЕ ДЛЯ ТЕБЕ ({count}):\n";

                foreach (var notification in dashboard.RecentNotifications.Take(3))
                {
                    message += $"{notification.Icon} {notification.Message}\n";
                }
                message += "\n";
            }
        }

        // Додаємо статистику якщо є звернення
        if (dashboard.Statistics.TotalAppeals > 0)
        {
            message += language == Language.English
                ? "📊 Your activity:\n"
                : "📊 Твоя активність:\n";
            
            message += $"• {dashboard.Statistics.TotalAppeals} " + 
                      (language == Language.English ? "appeals" : "звернень") + "\n";
            
            if (dashboard.Statistics.ActiveAppeals > 0)
            {
                message += $"• {dashboard.Statistics.ActiveAppeals} " +
                          (language == Language.English ? "active" : "активних") + "\n";
            }
            
            if (dashboard.Statistics.UpcomingEvents > 0)
            {
                message += $"• {dashboard.Statistics.UpcomingEvents} " +
                          (language == Language.English ? "upcoming events" : "майбутніх подій") + "\n";
            }
            
            message += "\n";
        }

        message += language == Language.English
            ? "Choose an action from the menu:"
            : "Обери дію з меню:";

        return message;
    }

    /// <summary>
    /// Отримує відповідь на команду
    /// </summary>
    private async Task<(string responseText, IReplyMarkup? keyboard)> GetCommandResponseAsync(string command, bool isAdmin, long userId, CancellationToken cancellationToken)
    {
        return command switch
        {
            "/start" => await GetStartCommandResponseAsync(userId, isAdmin, cancellationToken),
            
            "/help" => (
                await GetLocalizedTextForUserAsync(userId, "command.help_text", cancellationToken),
                await GetBackToMainMenuAsync(userId, cancellationToken)),
            
            "/appeal" => (
                await GetLocalizedTextForUserAsync(userId, "command.appeal_create", cancellationToken),
                await GetAppealCategoriesAsync(userId, cancellationToken)),
            
            "/myappeals" => (
                await GetLocalizedTextForUserAsync(userId, "command.loading_appeals", cancellationToken),
                null),
            
            "/news" => (
                await GetLocalizedTextForUserAsync(userId, "command.loading_news", cancellationToken),
                null),
            
            "/events" => (
                await GetLocalizedTextForUserAsync(userId, "command.loading_events", cancellationToken),
                null),
            
            "/profile" => (
                await GetLocalizedTextForUserAsync(userId, "command.loading_profile", cancellationToken),
                null),
            
            "/contacts" => (
                await GetLocalizedTextForUserAsync(userId, "command.contacts_info", cancellationToken),
                await GetBackToMainMenuAsync(userId, cancellationToken)),
            
            _ => (
                await GetLocalizedTextForUserAsync(userId, "command.unknown_command", cancellationToken),
                await GetBackToMainMenuAsync(userId, cancellationToken))
        };
    }




    #endregion

    #region ICommandHandler Implementation

    /// <summary>
    /// Обробляє команду /start
    /// </summary>
    public async Task HandleStartCommandAsync(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
    {
        await HandleCommandAsync(botClient, message, cancellationToken);
    }

    /// <summary>
    /// Обробляє команду /help
    /// </summary>
    public async Task HandleHelpCommandAsync(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
    {
        await HandleCommandAsync(botClient, message, cancellationToken);
    }

    /// <summary>
    /// Обробляє команду /appeal
    /// </summary>
    public async Task HandleAppealCommandAsync(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
    {
        await HandleCommandAsync(botClient, message, cancellationToken);
    }

    /// <summary>
    /// Обробляє команду /contacts
    /// </summary>
    public async Task HandleContactsCommandAsync(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
    {
        await HandleCommandAsync(botClient, message, cancellationToken);
    }

    #endregion
}