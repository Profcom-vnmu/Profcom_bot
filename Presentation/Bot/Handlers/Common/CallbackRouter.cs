using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StudentUnionBot.Presentation.Bot.Handlers.Interfaces;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace StudentUnionBot.Presentation.Bot.Handlers.Common;

/// <summary>
/// Маршрутизатор для callback запитів, направляє їх до відповідних обробників
/// </summary>
public class CallbackRouter
{
    private readonly ILogger<CallbackRouter> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly Dictionary<string, Func<ITelegramBotClient, CallbackQuery, CancellationToken, Task>> _routes;

    public CallbackRouter(
        ILogger<CallbackRouter> logger,
        IServiceScopeFactory scopeFactory,
        IAppealHandler appealHandler,
        IUserHandler userHandler,
        IAdminHandler adminHandler,
        IAdminAppealHandler adminAppealHandler,
        IAdminBackupHandler adminBackupHandler,
        IAdminBroadcastHandler adminBroadcastHandler,
        INewsManagementHandler newsManagementHandler,
        IEventsManagementHandler eventsManagementHandler,
        IContentHandler contentHandler)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
        
        _routes = new Dictionary<string, Func<ITelegramBotClient, CallbackQuery, CancellationToken, Task>>
        {
            // Appeal routes
            ["appeal_create"] = appealHandler.HandleAppealCreateCallback,
            ["appeal_cat_"] = appealHandler.HandleAppealCategorySelectionAsync,
            ["appeal_view_"] = appealHandler.HandleAppealViewCallbackAsync,
            ["appeal_list"] = appealHandler.HandleMyAppealsCallbackAsync,
            ["my_appeals"] = appealHandler.HandleMyAppealsCallbackAsync, // Alias
            
            // User routes  
            ["profile_view"] = userHandler.HandleProfileViewCallback,
            ["profile_edit_info"] = userHandler.HandleProfileEditInfoCallback,
            ["profile_edit_email"] = userHandler.HandleProfileEditEmailCallback,
            ["profile_change_language"] = userHandler.HandleProfileChangeLanguageCallback,
            ["set_lang_"] = userHandler.HandleSetLanguageCallback,
            
            // Admin routes
            ["admin_panel"] = adminHandler.HandleAdminPanelCallback,
            ["admin_stats"] = adminHandler.HandleAdminStatisticsCallback,
            ["admin_statistics"] = adminHandler.HandleAdminStatisticsCallback, // Alias
            ["admin_backup"] = adminHandler.HandleAdminBackupMenuCallback,
            
            // Admin appeal routes
            ["admin_appeals_new"] = adminAppealHandler.HandleAdminAppealsListCallback,
            ["admin_appeals_my"] = adminAppealHandler.HandleAdminAppealsListCallback,
            ["admin_appeals_unassigned"] = adminAppealHandler.HandleAdminAppealsListCallback,
            ["admin_appeals_"] = adminAppealHandler.HandleAdminAppealsListCallback,
            ["admin_view_"] = adminAppealHandler.HandleAdminAppealViewCallback,
            ["admin_assign_me_"] = adminAppealHandler.HandleAdminAssignToMeCallback,
            ["admin_unassign_"] = adminAppealHandler.HandleAdminUnassignCallback,
            ["admin_priority_"] = adminAppealHandler.HandleAdminPriorityMenuCallback,
            ["admin_set_priority_"] = adminAppealHandler.HandleAdminSetPriorityCallback,
            ["admin_reply_"] = adminAppealHandler.HandleAdminReplyCallback,
            ["admin_close_"] = adminAppealHandler.HandleAdminCloseAppealCallback,

            // Admin backup routes
            ["admin_backup_create"] = adminBackupHandler.HandleAdminBackupCreateCallback,
            ["admin_backup_list"] = adminBackupHandler.HandleAdminBackupListCallback,
            ["admin_backup_restore_"] = adminBackupHandler.HandleAdminBackupRestoreCallback,
            ["admin_restore_confirm_"] = adminBackupHandler.HandleAdminRestoreConfirmCallback,

            // Admin broadcast routes
            ["admin_broadcast"] = adminBroadcastHandler.HandleAdminBroadcastMenuCallback,
            ["broadcast_audience_"] = adminBroadcastHandler.HandleBroadcastAudienceCallback,
            ["broadcast_confirm"] = adminBroadcastHandler.HandleBroadcastConfirmCallback,
            ["broadcast_cancel"] = adminBroadcastHandler.HandleBroadcastCancelCallback,
            ["admin_confirm_broadcast"] = adminBroadcastHandler.HandleBroadcastConfirmCallback, // Alias
            ["admin_cancel_broadcast"] = adminBroadcastHandler.HandleBroadcastCancelCallback, // Alias

            // News management routes
            ["news_management"] = (botClient, query, ct) => newsManagementHandler.ShowNewsManagementMenuAsync(botClient, query.Message!.Chat.Id, ct),
            ["news_create"] = (botClient, query, ct) => newsManagementHandler.StartCreateNewsAsync(botClient, query.Message!.Chat.Id, ct),
            ["news_list_all"] = (botClient, query, ct) => newsManagementHandler.ShowNewsListAsync(botClient, query.Message!.Chat.Id, 1, null, ct),
            ["news_list_draft"] = (botClient, query, ct) => newsManagementHandler.ShowNewsListAsync(botClient, query.Message!.Chat.Id, 1, Domain.Enums.NewsStatus.Draft, ct),
            ["news_list_published"] = (botClient, query, ct) => newsManagementHandler.ShowNewsListAsync(botClient, query.Message!.Chat.Id, 1, Domain.Enums.NewsStatus.Published, ct),
            
            // Events management routes
            ["events_management"] = (botClient, query, ct) => eventsManagementHandler.ShowEventsManagementMenuAsync(botClient, query.Message!.Chat.Id, ct),
            ["events_create"] = (botClient, query, ct) => eventsManagementHandler.StartCreateEventAsync(botClient, query.Message!.Chat.Id, ct),
            ["events_list_all"] = (botClient, query, ct) => eventsManagementHandler.ShowEventsListAsync(botClient, query.Message!.Chat.Id, 1, null, null, ct),
            ["events_list_draft"] = (botClient, query, ct) => eventsManagementHandler.ShowEventsListAsync(botClient, query.Message!.Chat.Id, 1, Domain.Enums.EventStatus.Draft, null, ct),
            ["events_list_planned"] = (botClient, query, ct) => eventsManagementHandler.ShowEventsListAsync(botClient, query.Message!.Chat.Id, 1, Domain.Enums.EventStatus.Planned, null, ct),
            ["events_list_completed"] = (botClient, query, ct) => eventsManagementHandler.ShowEventsListAsync(botClient, query.Message!.Chat.Id, 1, Domain.Enums.EventStatus.Completed, null, ct),
            
            // Content routes
            ["news_list"] = contentHandler.HandleNewsListCallback,
            ["events_list"] = contentHandler.HandleEventsListCallback,
            ["partners_list"] = contentHandler.HandlePartnersListCallback,
            ["contacts_list"] = contentHandler.HandleContactsListCallback,
            ["event_details_"] = contentHandler.HandleEventDetailsCallback,
            ["event_register_"] = contentHandler.HandleEventRegisterCallback,
            ["event_unregister_"] = contentHandler.HandleEventUnregisterCallback,
            
            // Common navigation
            ["back_to_main"] = HandleBackToMainCallback,
            ["help"] = HandleHelpCallback,
            

        };
    }

    /// <summary>
    /// Маршрутизує callback запит до відповідного обробника
    /// </summary>
    public async Task RouteCallbackAsync(
        ITelegramBotClient botClient, 
        CallbackQuery callbackQuery, 
        CancellationToken cancellationToken)
    {
        try
        {
            var data = callbackQuery.Data;
            if (string.IsNullOrEmpty(data))
            {
                _logger.LogWarning("Отримано callback з порожніми даними");
                return;
            }

            _logger.LogInformation("Маршрутизація callback: {CallbackData}", data);

            // Точний збіг для окремих команд
            if (_routes.ContainsKey(data))
            {
                await _routes[data](botClient, callbackQuery, cancellationToken);
                return;
            }

            // Пошук за префіксом для команд з параметрами
            var route = _routes.Keys.FirstOrDefault(key => key.EndsWith("_") && data.StartsWith(key));
            
            if (route != null)
            {
                await _routes[route](botClient, callbackQuery, cancellationToken);
            }
            else
            {
                _logger.LogWarning("Не знайдено маршрут для callback: {CallbackData}", data);
                
                // Відправляємо повідомлення про невідомий callback
                await botClient.AnswerCallbackQueryAsync(
                    callbackQueryId: callbackQuery.Id,
                    text: "❌ Невідома дія. Спробуйте ще раз.",
                    showAlert: true,
                    cancellationToken: cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Помилка при маршрутизації callback: {CallbackData}", callbackQuery.Data);
            
            await botClient.AnswerCallbackQueryAsync(
                callbackQueryId: callbackQuery.Id,
                text: "❌ Виникла помилка. Спробуйте пізніше.",
                showAlert: true,
                cancellationToken: cancellationToken);
        }
    }

    /// <summary>
    /// Обробляє повернення до головного меню
    /// </summary>
    private async Task HandleBackToMainCallback(ITelegramBotClient botClient, CallbackQuery callbackQuery, CancellationToken cancellationToken)
    {
        try
        {
            // Перевіряємо роль користувача через MediatR
            var isAdmin = false;
            
            using var scope = _scopeFactory.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            
            var getUserQuery = new StudentUnionBot.Application.Users.Queries.GetUserByTelegramId.GetUserByTelegramIdQuery 
            { 
                TelegramId = callbackQuery.From.Id 
            };
            
            var userResult = await mediator.Send(getUserQuery, cancellationToken);
            if (userResult.IsSuccess && userResult.Value != null)
            {
                isAdmin = userResult.Value.Role == StudentUnionBot.Domain.Enums.UserRole.Admin;
            }
            
            var mainMenu = GetMainMenu(isAdmin);
            
            await botClient.EditMessageTextAsync(
                chatId: callbackQuery.Message!.Chat.Id,
                messageId: callbackQuery.Message.MessageId,
                text: "🏠 <b>Головне меню</b>\n\nОберіть дію:",
                parseMode: ParseMode.Html,
                replyMarkup: mainMenu,
                cancellationToken: cancellationToken);

            await botClient.AnswerCallbackQueryAsync(
                callbackQueryId: callbackQuery.Id,
                cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Помилка в HandleBackToMainCallback");
            
            await botClient.AnswerCallbackQueryAsync(
                callbackQueryId: callbackQuery.Id,
                text: "❌ Виникла помилка. Спробуйте пізніше.",
                showAlert: true,
                cancellationToken: cancellationToken);
        }
    }

    /// <summary>
    /// Обробляє відображення довідки
    /// </summary>
    private async Task HandleHelpCallback(ITelegramBotClient botClient, CallbackQuery callbackQuery, CancellationToken cancellationToken)
    {
        try
        {
            var helpText = "❓ <b>Допомога</b>\n\n" +
                          "Використовуйте меню для навігації.\n" +
                          "Команди: /start, /help, /appeal, /contacts";
            
            var backButton = new InlineKeyboardMarkup(
                InlineKeyboardButton.WithCallbackData("🔙 Назад", "back_to_main"));
            
            await botClient.EditMessageTextAsync(
                chatId: callbackQuery.Message!.Chat.Id,
                messageId: callbackQuery.Message.MessageId,
                text: helpText,
                parseMode: ParseMode.Html,
                replyMarkup: backButton,
                cancellationToken: cancellationToken);

            await botClient.AnswerCallbackQueryAsync(
                callbackQueryId: callbackQuery.Id,
                cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Помилка в HandleHelpCallback");
            
            await botClient.AnswerCallbackQueryAsync(
                callbackQueryId: callbackQuery.Id,
                text: "❌ Виникла помилка. Спробуйте пізніше.",
                showAlert: true,
                cancellationToken: cancellationToken);
        }
    }

    /// <summary>
    /// Обробляє callbacks для неімплементованих функцій
    /// </summary>
    private async Task HandleNotImplementedCallback(ITelegramBotClient botClient, CallbackQuery callbackQuery, CancellationToken cancellationToken)
    {
        try
        {
            await botClient.AnswerCallbackQueryAsync(
                callbackQueryId: callbackQuery.Id,
                text: "🚧 Функція в розробці. Скоро буде доступна!",
                showAlert: true,
                cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Помилка в HandleNotImplementedCallback");
        }
    }

    /// <summary>
    /// Створює головне меню
    /// </summary>
    private InlineKeyboardMarkup GetMainMenu(bool isAdmin)
    {
        var buttons = new List<InlineKeyboardButton[]>
        {
            new[]
            {
                InlineKeyboardButton.WithCallbackData("📩 Звернення", "appeal_create"),
                InlineKeyboardButton.WithCallbackData("📋 Мої звернення", "my_appeals")
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData("📰 Новини", "news_list"),
                InlineKeyboardButton.WithCallbackData("🎉 Події", "events_list")
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData("🤝 Партнери", "partners_list"),
                InlineKeyboardButton.WithCallbackData("📞 Контакти", "contacts_list")
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData("👤 Профіль", "profile_view"),
                InlineKeyboardButton.WithCallbackData("❓ Допомога", "help")
            }
        };

        if (isAdmin)
        {
            buttons.Add(new[]
            {
                InlineKeyboardButton.WithCallbackData("🔧 Адмін панель", "admin_panel")
            });
        }

        return new InlineKeyboardMarkup(buttons);
    }

    /// <summary>
    /// Додає новий маршрут для callback
    /// </summary>
    public void AddRoute(string prefix, Func<ITelegramBotClient, CallbackQuery, CancellationToken, Task> handler)
    {
        _routes[prefix] = handler;
        _logger.LogInformation("Додано новий маршрут: {Prefix}", prefix);
    }

    /// <summary>
    /// Видаляє маршрут
    /// </summary>
    public void RemoveRoute(string prefix)
    {
        if (_routes.Remove(prefix))
        {
            _logger.LogInformation("Видалено маршрут: {Prefix}", prefix);
        }
    }

    /// <summary>
    /// Отримує список всіх зареєстрованих маршрутів
    /// </summary>
    public IReadOnlyCollection<string> GetRoutes()
    {
        return _routes.Keys.ToList().AsReadOnly();
    }
}