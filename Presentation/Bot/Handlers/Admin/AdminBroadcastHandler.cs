using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StudentUnionBot.Application.Common.Interfaces;
using StudentUnionBot.Domain.Interfaces;
// using StudentUnionBot.Application.Notifications.Commands.SendBroadcast;
// using StudentUnionBot.Application.Users.Queries.GetAllActiveUsers;
using StudentUnionBot.Domain.Enums;
using StudentUnionBot.Presentation.Bot.Handlers.Common;
using StudentUnionBot.Presentation.Bot.Handlers.Interfaces;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace StudentUnionBot.Presentation.Bot.Handlers.Admin;

/// <summary>
/// Обробник адміністративних розсилок повідомлень
/// </summary>
public class AdminBroadcastHandler : BaseHandler, IAdminBroadcastHandler
{
    public AdminBroadcastHandler(
        IServiceScopeFactory scopeFactory,
        ILogger<AdminBroadcastHandler> logger,
        IMediator mediator)
        : base(scopeFactory, logger, mediator)
    {
    }

    /// <summary>
    /// Показує меню розсилок
    /// </summary>
    public async Task HandleAdminBroadcastMenuCallback(ITelegramBotClient botClient, CallbackQuery callbackQuery, CancellationToken cancellationToken)
    {
        var userId = callbackQuery.From.Id;
        
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var stateManager = scope.ServiceProvider.GetRequiredService<IUserStateManager>();
            
            // Встановлюємо стан очікування тексту розсилки
            await stateManager.SetStateAsync(userId, UserConversationState.WaitingBroadcastMessage, cancellationToken);

            await botClient.EditMessageTextAsync(
                chatId: callbackQuery.Message!.Chat.Id,
                messageId: callbackQuery.Message.MessageId,
                text: "📢 <b>Створення розсилки</b>\n\n" +
                      "Введіть текст повідомлення для розсилки всім користувачам:\n\n" +
                      "⚠️ <i>Будьте обережні! Розсилка піде всім активним користувачам бота.</i>",
                parseMode: ParseMode.Html,
                replyMarkup: GetCancelBroadcastKeyboard(),
                cancellationToken: cancellationToken);

            await botClient.AnswerCallbackQueryAsync(
                callbackQuery.Id, 
                "Введіть текст розсилки", 
                cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Помилка при ініціалізації розсилки для адміна {UserId}", userId);
            
            await SendErrorMessageAsync(
                botClient, 
                callbackQuery.Message!.Chat.Id,
                "❌ Виникла помилка при ініціалізації розсилки.",
                cancellationToken);
        }
    }

    /// <summary>
    /// Обробляє введення повідомлення розсилки
    /// </summary>
    public async Task HandleBroadcastMessageInputAsync(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
    {
        var userId = message.From!.Id;
        var broadcastText = message.Text?.Trim();

        if (string.IsNullOrWhiteSpace(broadcastText))
        {
            await botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: "❌ Текст розсилки не може бути порожнім. Спробуйте ще раз:",
                parseMode: ParseMode.Html,
                replyMarkup: GetCancelBroadcastKeyboard(),
                cancellationToken: cancellationToken);
            return;
        }

        if (broadcastText.Length > 4000)
        {
            await botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: "❌ Текст розсилки занадто довгий (максимум 4000 символів). Спробуйте ще раз:",
                parseMode: ParseMode.Html,
                replyMarkup: GetCancelBroadcastKeyboard(),
                cancellationToken: cancellationToken);
            return;
        }

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var stateManager = scope.ServiceProvider.GetRequiredService<IUserStateManager>();
            
            // Зберігаємо текст розсилки
            await stateManager.SetDataAsync(userId, "broadcast_text", broadcastText, cancellationToken);
            
            // Переходимо до підтвердження
            await stateManager.SetStateAsync(userId, UserConversationState.WaitingBroadcastConfirmation, cancellationToken);

            // Отримуємо кількість активних користувачів
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var activeUsers = await unitOfWork.Users.GetActiveUsersAsync(cancellationToken);
            var usersCount = activeUsers.Count;

            await botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: "📢 <b>Підтвердження розсилки</b>\n\n" +
                      $"<b>Текст повідомлення:</b>\n{broadcastText}\n\n" +
                      $"<b>Кількість отримувачів:</b> {usersCount} користувачів\n\n" +
                      "⚠️ <b>Ви впевнені, що хочете відправити цю розсилку?</b>",
                parseMode: ParseMode.Html,
                replyMarkup: GetConfirmBroadcastKeyboard(),
                cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Помилка при обробці тексту розсилки від адміна {UserId}", userId);
            
            using var scope = _scopeFactory.CreateScope();
            var stateManager = scope.ServiceProvider.GetRequiredService<IUserStateManager>();
            await stateManager.ClearStateAsync(userId, cancellationToken);
            
            await SendErrorMessageAsync(
                botClient,
                message.Chat.Id,
                "❌ Виникла помилка при обробці розсилки.\nСпробуйте пізніше.",
                cancellationToken);
        }
    }

    /// <summary>
    /// Підтверджує розсилку
    /// </summary>
    public async Task HandleBroadcastConfirmCallback(ITelegramBotClient botClient, CallbackQuery callbackQuery, CancellationToken cancellationToken)
    {
        var userId = callbackQuery.From.Id;
        
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var stateManager = scope.ServiceProvider.GetRequiredService<IUserStateManager>();
            
            // Отримуємо збережений текст розсилки
            var broadcastText = await stateManager.GetDataAsync<string>(userId, "broadcast_text", cancellationToken);
            
            if (string.IsNullOrWhiteSpace(broadcastText))
            {
                await botClient.AnswerCallbackQueryAsync(
                    callbackQuery.Id,
                    "❌ Текст розсилки не знайдено",
                    cancellationToken: cancellationToken);
                return;
            }

            // Очищуємо стан
            await stateManager.ClearStateAsync(userId, cancellationToken);
            await stateManager.ClearAllDataAsync(userId, cancellationToken);

            // Відправляємо команду на розсилку через MediatR
            var broadcastCommand = new Application.Notifications.Commands.SendBroadcast.SendBroadcastCommand
            {
                AdminTelegramId = userId,
                Message = broadcastText,
                NotificationType = NotificationType.Push,
                SendImmediately = true
            };
            
            var result = await _mediator.Send(broadcastCommand, cancellationToken);

            // Обробляємо результат
            if (result.IsSuccess && result.Value != null)
            {
                var broadcastResult = result.Value;
                
                await botClient.EditMessageTextAsync(
                    chatId: callbackQuery.Message!.Chat.Id,
                    messageId: callbackQuery.Message.MessageId,
                    text: "✅ <b>Розсилка успішно завершена!</b>\n\n" +
                          $"📊 <b>Статистика:</b>\n" +
                          $"• Успішно: {broadcastResult.SuccessCount}\n" +
                          $"• Помилок: {broadcastResult.FailureCount}\n" +
                          $"• Загалом спроб: {broadcastResult.TotalAttempts}\n\n" +
                          $"⏱️ Час виконання: {(broadcastResult.CompletedAt - broadcastResult.StartedAt).TotalSeconds:F1}с\n\n" +
                          $"📝 <b>Текст розсилки:</b>\n{broadcastText}",
                    parseMode: ParseMode.Html,
                    replyMarkup: GetBackToAdminPanelKeyboard(),
                    cancellationToken: cancellationToken);

                _logger.LogInformation(
                    "Адмін {AdminId} успішно відправив розсилку. Доставлено {Success}/{Total} повідомлень",
                    userId, 
                    broadcastResult.SuccessCount,
                    broadcastResult.TotalAttempts
                );
            }
            else
            {
                await botClient.EditMessageTextAsync(
                    chatId: callbackQuery.Message!.Chat.Id,
                    messageId: callbackQuery.Message.MessageId,
                    text: $"❌ <b>Помилка при відправці розсилки</b>\n\n" +
                          $"Деталі: {result.Error}",
                    parseMode: ParseMode.Html,
                    replyMarkup: GetBackToAdminPanelKeyboard(),
                    cancellationToken: cancellationToken);

                _logger.LogWarning(
                    "Помилка при розсилці від адміна {AdminId}: {Error}",
                    userId,
                    result.Error
                );
            }

            await botClient.AnswerCallbackQueryAsync(callbackQuery.Id, cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Помилка при підтвердженні розсилки адміном {UserId}", userId);
            
            using var scope = _scopeFactory.CreateScope();
            var stateManager = scope.ServiceProvider.GetRequiredService<IUserStateManager>();
            await stateManager.ClearStateAsync(userId, cancellationToken);
            await stateManager.ClearAllDataAsync(userId, cancellationToken);
            
            await botClient.AnswerCallbackQueryAsync(
                callbackQuery.Id,
                "❌ Виникла помилка при розсилці",
                cancellationToken: cancellationToken);
        }
    }

    /// <summary>
    /// Скасовує розсилку
    /// </summary>
    public async Task HandleBroadcastCancelCallback(ITelegramBotClient botClient, CallbackQuery callbackQuery, CancellationToken cancellationToken)
    {
        var userId = callbackQuery.From.Id;
        
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var stateManager = scope.ServiceProvider.GetRequiredService<IUserStateManager>();
            
            // Очищуємо стан і дані
            await stateManager.ClearStateAsync(userId, cancellationToken);
            await stateManager.ClearAllDataAsync(userId, cancellationToken);

            await botClient.EditMessageTextAsync(
                chatId: callbackQuery.Message!.Chat.Id,
                messageId: callbackQuery.Message.MessageId,
                text: "❌ <b>Розсилка скасована</b>\n\nВи повернулися до адміністративної панелі.",
                parseMode: ParseMode.Html,
                replyMarkup: GetBackToAdminPanelKeyboard(),
                cancellationToken: cancellationToken);

            await botClient.AnswerCallbackQueryAsync(
                callbackQuery.Id,
                "Розсилка скасована",
                cancellationToken: cancellationToken);

            _logger.LogInformation("Адмін {AdminId} скасував розсилку", userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Помилка при скасуванні розсилки адміном {UserId}", userId);
            
            await botClient.AnswerCallbackQueryAsync(
                callbackQuery.Id,
                "❌ Виникла помилка",
                cancellationToken: cancellationToken);
        }
    }

    /// <summary>
    /// Обробляє вибір аудиторії для розсилки
    /// </summary>
    public async Task HandleBroadcastAudienceCallback(ITelegramBotClient botClient, CallbackQuery callbackQuery, CancellationToken cancellationToken)
    {
        // TODO: Реалізувати вибір аудиторії
        await botClient.AnswerCallbackQueryAsync(
            callbackQuery.Id,
            "Функція у розробці",
            cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Обробляє введення списку email адрес для кастомної розсилки
    /// </summary>
    public async Task HandleBroadcastCustomEmailsInputAsync(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
    {
        // TODO: Реалізувати введення кастомних email
        await botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: "Функція у розробці",
            cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Обробляє текстові повідомлення (реалізація абстрактного методу)
    /// </summary>
    public override async Task HandleTextMessageAsync(ITelegramBotClient botClient, Message message, UserConversationState state, CancellationToken cancellationToken)
    {
        switch (state)
        {
            case UserConversationState.WaitingBroadcastMessage:
                await HandleBroadcastMessageInputAsync(botClient, message, cancellationToken);
                break;
        }
    }

    #region Private Methods

    /// <summary>
    /// Створює клавіатуру для скасування розсилки
    /// </summary>
    private static InlineKeyboardMarkup GetCancelBroadcastKeyboard()
    {
        return new InlineKeyboardMarkup(new[]
        {
            new[]
            {
                InlineKeyboardButton.WithCallbackData("❌ Скасувати", "admin_cancel_broadcast")
            }
        });
    }

    /// <summary>
    /// Створює клавіатуру для підтвердження розсилки
    /// </summary>
    private static InlineKeyboardMarkup GetConfirmBroadcastKeyboard()
    {
        return new InlineKeyboardMarkup(new[]
        {
            new[]
            {
                InlineKeyboardButton.WithCallbackData("✅ Відправити", "admin_confirm_broadcast"),
                InlineKeyboardButton.WithCallbackData("❌ Скасувати", "admin_cancel_broadcast")
            }
        });
    }

    /// <summary>
    /// Створює клавіатуру для повернення до адміністративної панелі
    /// </summary>
    private static InlineKeyboardMarkup GetBackToAdminPanelKeyboard()
    {
        return new InlineKeyboardMarkup(new[]
        {
            new[]
            {
                InlineKeyboardButton.WithCallbackData("🔙 До адміністративної панелі", "admin_panel")
            }
        });
    }

    #endregion
}