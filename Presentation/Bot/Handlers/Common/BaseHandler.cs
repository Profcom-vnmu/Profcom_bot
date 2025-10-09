using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StudentUnionBot.Application.Common.Interfaces;
using StudentUnionBot.Domain.Enums;
using StudentUnionBot.Domain.Interfaces;
using StudentUnionBot.Presentation.Bot.Handlers.Interfaces;
using StudentUnionBot.Presentation.Bot.Keyboards;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace StudentUnionBot.Presentation.Bot.Handlers.Common;

/// <summary>
/// Базовий клас для всіх обробників бота з спільною функціональністю
/// </summary>
public abstract class BaseHandler : IBaseHandler
{
    protected readonly ILogger _logger;
    protected readonly IMediator _mediator;
    protected readonly IServiceScopeFactory _scopeFactory;

    protected BaseHandler(
        IServiceScopeFactory scopeFactory,
        ILogger logger, 
        IMediator mediator)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _mediator = mediator;
    }

    /// <summary>
    /// Абстрактний метод для обробки текстових повідомлень
    /// Має бути реалізований в кожному конкретному хендлері
    /// </summary>
    public abstract Task HandleTextMessageAsync(
        ITelegramBotClient botClient, 
        Message message, 
        UserConversationState state, 
        CancellationToken cancellationToken);

    #region Protected Helper Methods

    /// <summary>
    /// Отримати головне меню
    /// </summary>
    protected InlineKeyboardMarkup GetMainMenu(bool isAdmin = false)
    {
        using var scope = _scopeFactory.CreateScope();
        var localizationService = scope.ServiceProvider.GetRequiredService<ILocalizationService>();
        return KeyboardFactory.GetMainMenuKeyboard(localizationService, Language.Ukrainian, isAdmin);
    }

    /// <summary>
    /// Отримати головне меню з урахуванням мови користувача
    /// </summary>
    protected async Task<InlineKeyboardMarkup> GetMainMenuAsync(long userId, bool isAdmin = false, CancellationToken cancellationToken = default)
    {
        var language = await GetUserLanguageAsync(userId, cancellationToken);
        using var scope = _scopeFactory.CreateScope();
        var localizationService = scope.ServiceProvider.GetRequiredService<ILocalizationService>();
        return KeyboardFactory.GetMainMenuKeyboard(localizationService, language, isAdmin);
    }

    /// <summary>
    /// Отримати кнопку повернення в головне меню
    /// </summary>
    protected InlineKeyboardMarkup GetBackToMainMenu()
    {
        using var scope = _scopeFactory.CreateScope();
        var localizationService = scope.ServiceProvider.GetRequiredService<ILocalizationService>();
        return KeyboardFactory.GetBackToMainMenuKeyboard(localizationService, Language.Ukrainian);
    }

    /// <summary>
    /// Отримати кнопку повернення в головне меню з урахуванням мови користувача
    /// </summary>
    protected async Task<InlineKeyboardMarkup> GetBackToMainMenuAsync(long userId, CancellationToken cancellationToken = default)
    {
        var language = await GetUserLanguageAsync(userId, cancellationToken);
        using var scope = _scopeFactory.CreateScope();
        var localizationService = scope.ServiceProvider.GetRequiredService<ILocalizationService>();
        return KeyboardFactory.GetBackToMainMenuKeyboard(localizationService, language);
    }

    /// <summary>
    /// Отримати категорії звернень
    /// </summary>
    protected InlineKeyboardMarkup GetAppealCategories()
    {
        using var scope = _scopeFactory.CreateScope();
        var localizationService = scope.ServiceProvider.GetRequiredService<ILocalizationService>();
        return KeyboardFactory.GetAppealCategoriesKeyboard(localizationService, Language.Ukrainian);
    }

    /// <summary>
    /// Отримати категорії звернень з урахуванням мови користувача
    /// </summary>
    protected async Task<InlineKeyboardMarkup> GetAppealCategoriesAsync(long userId, CancellationToken cancellationToken = default)
    {
        var language = await GetUserLanguageAsync(userId, cancellationToken);
        using var scope = _scopeFactory.CreateScope();
        var localizationService = scope.ServiceProvider.GetRequiredService<ILocalizationService>();
        return KeyboardFactory.GetAppealCategoriesKeyboard(localizationService, language);
    }

    /// <summary>
    /// Отримати адміністративну панель
    /// </summary>
    protected InlineKeyboardMarkup GetAdminPanel()
    {
        using var scope = _scopeFactory.CreateScope();
        var localizationService = scope.ServiceProvider.GetRequiredService<ILocalizationService>();
        return KeyboardFactory.GetAdminPanelKeyboard(localizationService, Language.Ukrainian);
    }

    /// <summary>
    /// Отримати адміністративну панель з урахуванням мови користувача
    /// </summary>
    protected async Task<InlineKeyboardMarkup> GetAdminPanelAsync(long userId, CancellationToken cancellationToken = default)
    {
        var language = await GetUserLanguageAsync(userId, cancellationToken);
        using var scope = _scopeFactory.CreateScope();
        var localizationService = scope.ServiceProvider.GetRequiredService<ILocalizationService>();
        return KeyboardFactory.GetAdminPanelKeyboard(localizationService, language);
    }

    /// <summary>
    /// Отримати дії для звернень в адмін панелі
    /// </summary>
    protected InlineKeyboardMarkup GetAdminAppealActions(int appealId, bool isAssignedToMe, bool isClosed)
    {
        using var scope = _scopeFactory.CreateScope();
        var localizationService = scope.ServiceProvider.GetRequiredService<ILocalizationService>();
        return KeyboardFactory.GetAdminAppealActionsKeyboard(
            localizationService, Language.Ukrainian, appealId, isAssignedToMe, isClosed);
    }

    /// <summary>
    /// Отримати дії для звернень в адмін панелі з урахуванням мови користувача
    /// </summary>
    protected async Task<InlineKeyboardMarkup> GetAdminAppealActionsAsync(long userId, int appealId, bool isAssignedToMe, bool isClosed, CancellationToken cancellationToken = default)
    {
        var language = await GetUserLanguageAsync(userId, cancellationToken);
        using var scope = _scopeFactory.CreateScope();
        var localizationService = scope.ServiceProvider.GetRequiredService<ILocalizationService>();
        return KeyboardFactory.GetAdminAppealActionsKeyboard(
            localizationService, language, appealId, isAssignedToMe, isClosed);
    }

    /// <summary>
    /// Отримати вибір пріоритету
    /// </summary>
    protected InlineKeyboardMarkup GetPrioritySelection(int appealId)
    {
        using var scope = _scopeFactory.CreateScope();
        var localizationService = scope.ServiceProvider.GetRequiredService<ILocalizationService>();
        return KeyboardFactory.GetPrioritySelectionKeyboard(localizationService, Language.Ukrainian, appealId);
    }

    /// <summary>
    /// Отримати вибір пріоритету з урахуванням мови користувача
    /// </summary>
    protected async Task<InlineKeyboardMarkup> GetPrioritySelectionAsync(long userId, int appealId, CancellationToken cancellationToken = default)
    {
        var language = await GetUserLanguageAsync(userId, cancellationToken);
        using var scope = _scopeFactory.CreateScope();
        var localizationService = scope.ServiceProvider.GetRequiredService<ILocalizationService>();
        return KeyboardFactory.GetPrioritySelectionKeyboard(localizationService, language, appealId);
    }

    /// <summary>
    /// Очистити стан користувача
    /// </summary>
    protected async Task ClearUserStateAsync(long userId, CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var stateManager = scope.ServiceProvider.GetRequiredService<IUserStateManager>();
        await stateManager.ClearStateAsync(userId, cancellationToken);
        await stateManager.ClearAllDataAsync(userId, cancellationToken);
    }

    /// <summary>
    /// Відправити повідомлення про помилку
    /// </summary>
    protected async Task SendErrorMessageAsync(
        ITelegramBotClient botClient, 
        long chatId, 
        string errorMessage, 
        CancellationToken cancellationToken)
    {
        await botClient.SendTextMessageAsync(
            chatId: chatId,
            text: $"❌ {errorMessage}",
            replyMarkup: GetBackToMainMenu(),
            cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Відправити локалізоване повідомлення
    /// </summary>
    protected async Task SendLocalizedMessageAsync(
        ITelegramBotClient botClient,
        long chatId,
        string messageKey,
        CancellationToken cancellationToken,
        InlineKeyboardMarkup? replyMarkup = null,
        object[]? args = null)
    {
        var message = await GetLocalizedTextForUserAsync(chatId, messageKey, cancellationToken, args ?? Array.Empty<object>());
        
        await botClient.SendTextMessageAsync(
            chatId: chatId,
            text: message,
            replyMarkup: replyMarkup,
            cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Відправити локалізоване повідомлення про помилку
    /// </summary>
    protected async Task SendLocalizedErrorAsync(
        ITelegramBotClient botClient,
        long chatId,
        string errorKey,
        CancellationToken cancellationToken,
        object[]? args = null)
    {
        var errorMessage = await GetLocalizedTextForUserAsync(chatId, errorKey, cancellationToken, args ?? Array.Empty<object>());
        var backMenu = await GetBackToMainMenuAsync(chatId, cancellationToken);
        
        await botClient.SendTextMessageAsync(
            chatId: chatId,
            text: $"❌ {errorMessage}",
            replyMarkup: backMenu,
            cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Логувати помилку та відправити повідомлення користувачу
    /// </summary>
    protected async Task HandleErrorAsync(
        ITelegramBotClient botClient,
        long chatId,
        Exception ex,
        string context,
        CancellationToken cancellationToken)
    {
        _logger.LogError(ex, "Помилка в {Context}", context);
        
        var errorMessage = await GetLocalizedTextAsync("error.technical_error", Language.Ukrainian, cancellationToken: cancellationToken);
        await SendErrorMessageAsync(
            botClient, 
            chatId, 
            errorMessage,
            cancellationToken);
    }

    /// <summary>
    /// Отримати локалізований текст за ключем
    /// </summary>
    protected async Task<string> GetLocalizedTextAsync(string key, Language language = Language.Ukrainian, object[]? args = null, CancellationToken cancellationToken = default)
    {
        using var scope = _scopeFactory.CreateScope();
        var localizationService = scope.ServiceProvider.GetRequiredService<ILocalizationService>();
        
        if (args?.Length > 0)
        {
            return await localizationService.GetLocalizedStringAsync(key, language, args, cancellationToken);
        }
        
        return await localizationService.GetLocalizedStringAsync(key, language, cancellationToken);
    }

    /// <summary>
    /// Отримати мову користувача (з БД або за замовчуванням Ukrainian)
    /// </summary>
    protected async Task<Language> GetUserLanguageAsync(long userId, CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
            var user = await userRepository.GetByTelegramIdAsync(userId, cancellationToken);
            return user?.Language ?? Language.Ukrainian;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Не вдалося отримати мову користувача {UserId}, використовуємо українську", userId);
            return Language.Ukrainian;
        }
    }

    /// <summary>
    /// Отримати локалізований текст з урахуванням мови користувача
    /// </summary>
    protected async Task<string> GetLocalizedTextForUserAsync(long userId, string key, CancellationToken cancellationToken, params object[] args)
    {
        var language = await GetUserLanguageAsync(userId, cancellationToken);
        return await GetLocalizedTextAsync(key, language, args, cancellationToken);
    }

    #endregion
}