using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StudentUnionBot.Application.Common.Interfaces;
using StudentUnionBot.Domain.Enums;
using StudentUnionBot.Presentation.Bot.Handlers.Common;
using StudentUnionBot.Presentation.Bot.Handlers.Interfaces;
using StudentUnionBot.Presentation.Bot.Interfaces;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace StudentUnionBot.Presentation.Bot;

/// <summary>
/// Головний обробник оновлень бота (спрощений роутер)
/// </summary>
public class UpdateHandler : IBotUpdateHandler
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<UpdateHandler> _logger;
    private readonly CallbackRouter _callbackRouter;

    public UpdateHandler(
        IServiceScopeFactory scopeFactory,
        ILogger<UpdateHandler> logger,
        CallbackRouter callbackRouter)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _callbackRouter = callbackRouter;
    }

    /// <summary>
    /// Обробляє оновлення від Telegram
    /// </summary>
    public async Task HandleUpdateAsync(Update update, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Отримано оновлення: {UpdateType}", update.Type);

            switch (update.Type)
            {
                case UpdateType.Message:
                    await HandleMessageAsync(update.Message!, cancellationToken);
                    break;

                case UpdateType.CallbackQuery:
                    await HandleCallbackQueryAsync(update.CallbackQuery!, cancellationToken);
                    break;

                default:
                    _logger.LogInformation("Невідомий тип оновлення: {UpdateType}", update.Type);
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Помилка при обробці оновлення: {UpdateType}", update.Type);
        }
    }

    /// <summary>
    /// Обробляє повідомлення
    /// </summary>
    private async Task HandleMessageAsync(Message message, CancellationToken cancellationToken)
    {
        if (message.From?.IsBot == true)
            return;

        using var scope = _scopeFactory.CreateScope();
        var botClient = scope.ServiceProvider.GetRequiredService<ITelegramBotClient>();
        var rateLimiter = scope.ServiceProvider.GetRequiredService<IRateLimiter>();
        
        // Перевіряємо rate limiting для загальних повідомлень
        var userId = message.From!.Id;
        if (!await rateLimiter.AllowAsync(userId, "SendMessage", cancellationToken))
        {
            var remainingTime = await rateLimiter.GetTimeUntilResetAsync(userId, "SendMessage", cancellationToken);
            var waitMessage = remainingTime.HasValue 
                ? $"⏳ Занадто багато повідомлень! Спробуйте через {remainingTime.Value.TotalSeconds:F0} секунд."
                : "⏳ Занадто багато повідомлень! Спробуйте пізніше.";
                
            await botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: waitMessage,
                cancellationToken: cancellationToken);
            return;
        }
        
        try
        {
            // Реєстрація/оновлення користувача
            var userHandler = scope.ServiceProvider.GetRequiredService<IUserHandler>();
            await userHandler.RegisterOrUpdateUserAsync(message.From, cancellationToken);

            // Обробка команд
            if (message.Text?.StartsWith("/") == true)
            {
                var commandHandler = scope.ServiceProvider.GetRequiredService<ICommandHandler>();
                await commandHandler.HandleCommandAsync(botClient, message, cancellationToken);
                return;
            }

            // Обробка текстових повідомлень залежно від стану користувача
            await HandleUserStateMessageAsync(botClient, message, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Помилка при обробці повідомлення від користувача {UserId}", message.From?.Id);
            
            await botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: "❌ Виникла помилка при обробці повідомлення. Спробуйте пізніше.",
                cancellationToken: cancellationToken);
        }
    }

    /// <summary>
    /// Обробляє текстові повідомлення залежно від стану користувача
    /// </summary>
    private async Task HandleUserStateMessageAsync(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var stateManager = scope.ServiceProvider.GetRequiredService<IUserStateManager>();
        
        var userId = message.From!.Id;
        var userState = await stateManager.GetStateAsync(userId, cancellationToken);

        _logger.LogInformation("Обробка повідомлення у стані {State} від користувача {UserId}", userState, userId);

        // Визначаємо, який handler має обробити повідомлення
        var handler = GetHandlerForState(scope, userState);
        if (handler != null)
        {
            await handler.HandleTextMessageAsync(botClient, message, userState, cancellationToken);
        }
        else
        {
            // Повідомлення поза контекстом - відправляємо до головного меню
            await botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: "Скористайтесь меню нижче або введіть команду /start",
                cancellationToken: cancellationToken);
        }
    }

    /// <summary>
    /// Визначає handler для обробки стану
    /// </summary>
    private IBaseHandler? GetHandlerForState(IServiceScope scope, UserConversationState state)
    {
        return state switch
        {
            // Appeal states
            UserConversationState.WaitingAppealSubject or
            UserConversationState.WaitingAppealMessage or
            UserConversationState.WaitingAppealConfirmation => 
                scope.ServiceProvider.GetRequiredService<IAppealHandler>(),

            // User states  
            UserConversationState.WaitingEmailInput or
            UserConversationState.WaitingVerificationCode or
            UserConversationState.WaitingFullNameInput or
            UserConversationState.WaitingFacultyInput or
            UserConversationState.WaitingCourseInput or
            UserConversationState.WaitingGroupInput => 
                scope.ServiceProvider.GetRequiredService<IUserHandler>(),

            // Admin states
            UserConversationState.WaitingAdminReply or
            UserConversationState.WaitingCloseReason => 
                scope.ServiceProvider.GetRequiredService<IAdminAppealHandler>(),

            // Broadcast states
            UserConversationState.WaitingBroadcastMessage or
            UserConversationState.WaitingBroadcastConfirmation or
            UserConversationState.WaitingBroadcastCustomEmails => 
                scope.ServiceProvider.GetRequiredService<IAdminBroadcastHandler>(),

            // News Management states
            UserConversationState.CreatingNewsTitle or
            UserConversationState.CreatingNewsContent => 
                scope.ServiceProvider.GetRequiredService<INewsManagementHandler>(),

            // Events Management states
            UserConversationState.CreatingEventTitle or
            UserConversationState.CreatingEventDescription or
            UserConversationState.CreatingEventLocation or
            UserConversationState.CreatingEventDateTime => 
                scope.ServiceProvider.GetRequiredService<IEventsManagementHandler>(),

            // Default state - no handler
            UserConversationState.Idle or _ => null
        };
    }

    /// <summary>
    /// Обробляє callback запити
    /// </summary>
    private async Task HandleCallbackQueryAsync(CallbackQuery callbackQuery, CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var botClient = scope.ServiceProvider.GetRequiredService<ITelegramBotClient>();
        var rateLimiter = scope.ServiceProvider.GetRequiredService<IRateLimiter>();

        // Перевіряємо rate limiting для callback queries
        var userId = callbackQuery.From.Id;
        if (!await rateLimiter.AllowAsync(userId, "SendMessage", cancellationToken))
        {
            var remainingTime = await rateLimiter.GetTimeUntilResetAsync(userId, "SendMessage", cancellationToken);
            var waitMessage = remainingTime.HasValue 
                ? $"⏳ Занадто багато дій! Спробуйте через {remainingTime.Value.TotalSeconds:F0} секунд."
                : "⏳ Занадто багато дій! Спробуйте пізніше.";
                
            await botClient.AnswerCallbackQueryAsync(
                callbackQueryId: callbackQuery.Id,
                text: waitMessage,
                showAlert: true,
                cancellationToken: cancellationToken);
            return;
        }

        try
        {
            // Реєстрація/оновлення користувача
            var userHandler = scope.ServiceProvider.GetRequiredService<IUserHandler>();
            await userHandler.RegisterOrUpdateUserAsync(callbackQuery.From, cancellationToken);

            // Маршрутизація callback через CallbackRouter
            await _callbackRouter.RouteCallbackAsync(botClient, callbackQuery, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Помилка при обробці callback від користувача {UserId}: {CallbackData}", 
                callbackQuery.From?.Id, callbackQuery.Data);
            
            await botClient.AnswerCallbackQueryAsync(
                callbackQuery.Id,
                "❌ Виникла помилка при обробці запиту",
                showAlert: true,
                cancellationToken: cancellationToken);
        }
    }

    /// <summary>
    /// Обробляє помилки polling
    /// </summary>
    public async Task HandleErrorAsync(Exception exception, CancellationToken cancellationToken)
    {
        _logger.LogError(exception, "Помилка polling Telegram бота");
        await Task.CompletedTask;
    }
}