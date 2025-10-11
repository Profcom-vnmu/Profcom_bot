using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StudentUnionBot.Application.Appeals.Commands.AssignAppeal;
using StudentUnionBot.Application.Appeals.Commands.CloseAppeal;
using StudentUnionBot.Application.Appeals.Commands.ReplyToAppeal;
using StudentUnionBot.Application.Appeals.Commands.UpdatePriority;
using StudentUnionBot.Application.Appeals.Queries.GetAdminAppeals;
using StudentUnionBot.Application.Appeals.Queries.GetAppealById;
using StudentUnionBot.Application.Common.Interfaces;
using StudentUnionBot.Domain.Enums;
using StudentUnionBot.Domain.Interfaces;
using StudentUnionBot.Presentation.Bot.Handlers.Common;
using StudentUnionBot.Presentation.Bot.Handlers.Interfaces;
using StudentUnionBot.Presentation.Bot.Helpers;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace StudentUnionBot.Presentation.Bot.Handlers.Admin;

/// <summary>
/// Обробник адміністративного управління зверненнями
/// </summary>
public class AdminAppealHandler : BaseHandler, IAdminAppealHandler
{
    public AdminAppealHandler(
        IServiceScopeFactory scopeFactory,
        ILogger<AdminAppealHandler> logger,
        IMediator mediator)
        : base(scopeFactory, logger, mediator)
    {
    }

    /// <summary>
    /// Показує список звернень для адміна
    /// </summary>
    public async Task HandleAdminAppealsListCallback(
        ITelegramBotClient botClient,
        CallbackQuery callbackQuery,
        CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var userRepo = scope.ServiceProvider.GetRequiredService<IUserRepository>();

        var user = await userRepo.GetByTelegramIdAsync(callbackQuery.From.Id, cancellationToken);
        if (user?.Role != UserRole.Admin)
        {
            await botClient.AnswerCallbackQueryAsync(
                callbackQuery.Id,
                "⛔ У вас немає прав адміністратора",
                showAlert: true,
                cancellationToken: cancellationToken);
            return;
        }

        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var callbackData = callbackQuery.Data!;

        var query = new GetAdminAppealsQuery
        {
            AdminId = user.TelegramId,
            PageNumber = 1,
            PageSize = 10,
            SortBy = "CreatedAt",
            Descending = true
        };

        if (callbackData.Contains("_new"))
        {
            query.Status = AppealStatus.New;
        }
        else if (callbackData.Contains("_my"))
        {
            query.OnlyMy = true;
        }
        else if (callbackData.Contains("_unassigned"))
        {
            query.OnlyUnassigned = true;
        }

        var result = await mediator.Send(query, cancellationToken);

        if (!result.IsSuccess || result.Value == null || !result.Value.Appeals.Any())
        {
            await botClient.AnswerCallbackQueryAsync(
                callbackQuery.Id,
                "📄 Звернень не знайдено",
                cancellationToken: cancellationToken);
            return;
        }

        var appealsText = "📋 <b>Звернення:</b>\n\n";
        foreach (var appeal in result.Value.Appeals)
        {
            var statusEmoji = appeal.Status switch
            {
                AppealStatus.New => "🆕",
                AppealStatus.InProgress => "⚙️",
                AppealStatus.Closed => "✅",
                _ => "❓"
            };

            var priorityEmoji = appeal.Priority switch
            {
                AppealPriority.Low => "🟢",
                AppealPriority.Normal => "🟡",
                AppealPriority.High => "🟠",
                AppealPriority.Urgent => "🔴",
                _ => "⚪"
            };

            var assignedText = appeal.AssignedToAdminId.HasValue ? "👤" : "❌";

            appealsText += $"{statusEmoji} {priorityEmoji} #{appeal.Id} | {appeal.CategoryName}\n" +
                          $"<b>{appeal.Subject}</b>\n" +
                          $"{assignedText} Статус: {appeal.StatusName}\n\n";
        }

        appealsText += $"Сторінка 1 з {Math.Ceiling((double)result.Value.TotalCount / 10)}";

        var buttons = new List<List<InlineKeyboardButton>>();
        
        // Кнопки для кожного звернення
        var appealButtons = result.Value.Appeals
            .Select(a => InlineKeyboardButton.WithCallbackData($"#{a.Id}", $"admin_view_{a.Id}"))
            .ToArray();
        
        // Розбиваємо кнопки по рядках (по 3 в рядку)
        for (int i = 0; i < appealButtons.Length; i += 3)
        {
            buttons.Add(appealButtons.Skip(i).Take(3).ToList());
        }

        // Кнопка назад
        buttons.Add(new List<InlineKeyboardButton>
        {
            InlineKeyboardButton.WithCallbackData("🔙 Назад до адмін панелі", "admin_panel")
        });

        var keyboard = new InlineKeyboardMarkup(buttons);

        await botClient.EditMessageTextAsync(
            chatId: callbackQuery.Message!.Chat.Id,
            messageId: callbackQuery.Message.MessageId,
            text: appealsText,
            parseMode: ParseMode.Html,
            replyMarkup: keyboard,
            cancellationToken: cancellationToken);

        await botClient.AnswerCallbackQueryAsync(
            callbackQueryId: callbackQuery.Id,
            cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Показує деталі звернення для адміна
    /// </summary>
    public async Task HandleAdminAppealViewCallback(
        ITelegramBotClient botClient,
        CallbackQuery callbackQuery,
        CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var userRepo = scope.ServiceProvider.GetRequiredService<IUserRepository>();

        var user = await userRepo.GetByTelegramIdAsync(callbackQuery.From.Id, cancellationToken);
        if (user?.Role != UserRole.Admin)
        {
            await botClient.AnswerCallbackQueryAsync(
                callbackQuery.Id,
                "⛔ У вас немає прав адміністратора",
                showAlert: true,
                cancellationToken: cancellationToken);
            return;
        }

        var appealIdStr = callbackQuery.Data!.Replace("admin_view_", "");
        if (!int.TryParse(appealIdStr, out var appealId))
        {
            await botClient.AnswerCallbackQueryAsync(
                callbackQuery.Id,
                "❌ Некоректний ID звернення",
                showAlert: true,
                cancellationToken: cancellationToken);
            return;
        }

        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var result = await mediator.Send(new GetAppealByIdQuery 
        { 
            AppealId = appealId, 
            RequestUserId = user.TelegramId 
        }, cancellationToken);

        if (!result.IsSuccess || result.Value == null)
        {
            await botClient.AnswerCallbackQueryAsync(
                callbackQuery.Id,
                "❌ Звернення не знайдено",
                showAlert: true,
                cancellationToken: cancellationToken);
            return;
        }

        var appeal = result.Value;
        var isAssignedToMe = appeal.AssignedToAdminId == user.TelegramId;
        var isClosed = appeal.Status == AppealStatus.Closed;

        var appealText = $"📋 <b>Звернення #{appeal.Id}</b>\n\n" +
                        $"📂 <b>Категорія:</b> {appeal.CategoryName}\n" +
                        $"📊 <b>Статус:</b> {appeal.StatusName}\n" +
                        $"🎯 <b>Пріоритет:</b> {appeal.PriorityName}\n" +
                        $"📅 <b>Створено:</b> {appeal.CreatedAt:dd.MM.yyyy HH:mm}\n";

        if (appeal.FirstResponseAt.HasValue)
        {
            appealText += $"⏰ <b>Перша відповідь:</b> {appeal.FirstResponseAt:dd.MM.yyyy HH:mm}\n";
        }

        if (appeal.ClosedAt.HasValue)
        {
            appealText += $"✅ <b>Закрито:</b> {appeal.ClosedAt:dd.MM.yyyy HH:mm}\n";
            if (!string.IsNullOrEmpty(appeal.ClosedReason))
            {
                appealText += $"📝 <b>Причина закриття:</b> {appeal.ClosedReason}\n";
            }
        }

        appealText += $"\n<b>Тема:</b>\n{appeal.Subject}\n\n";
        appealText += $"<b>Повідомлення:</b>\n{appeal.Message}\n";

        if (appeal.Messages.Any())
        {
            appealText += "\n<b>Історія повідомлень:</b>\n";
            foreach (var msg in appeal.Messages.OrderBy(m => m.SentAt))
            {
                var senderType = msg.IsFromAdmin ? "👨‍💼 Адмін" : "👤 Користувач";
                appealText += $"{senderType} ({msg.SentAt:dd.MM HH:mm}): {msg.Text}\n";
            }
        }

        await botClient.EditMessageTextAsync(
            chatId: callbackQuery.Message!.Chat.Id,
            messageId: callbackQuery.Message.MessageId,
            text: appealText,
            parseMode: ParseMode.Html,
            replyMarkup: GetAdminAppealActionsKeyboard(appealId, isAssignedToMe, isClosed),
            cancellationToken: cancellationToken);

        await botClient.AnswerCallbackQueryAsync(
            callbackQueryId: callbackQuery.Id,
            cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Призначає звернення собі
    /// </summary>
    public async Task HandleAdminAssignToMeCallback(
        ITelegramBotClient botClient,
        CallbackQuery callbackQuery,
        CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var userRepo = scope.ServiceProvider.GetRequiredService<IUserRepository>();

        var user = await userRepo.GetByTelegramIdAsync(callbackQuery.From.Id, cancellationToken);
        if (user?.Role != UserRole.Admin)
        {
            await botClient.AnswerCallbackQueryAsync(
                callbackQuery.Id,
                "⛔ У вас немає прав адміністратора",
                showAlert: true,
                cancellationToken: cancellationToken);
            return;
        }

        var appealIdStr = callbackQuery.Data!.Replace("admin_assign_me_", "");
        if (!int.TryParse(appealIdStr, out var appealId))
        {
            await botClient.AnswerCallbackQueryAsync(
                callbackQuery.Id,
                "❌ Некоректний ID звернення",
                showAlert: true,
                cancellationToken: cancellationToken);
            return;
        }

        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var result = await mediator.Send(new AssignAppealCommand(
            appealId,
            user.TelegramId,
            user.TelegramId,
            "Адмін призначив звернення собі"
        ), cancellationToken);

        if (result.IsSuccess)
        {
            await botClient.AnswerCallbackQueryAsync(
                callbackQuery.Id,
                "✅ Звернення призначено вам",
                cancellationToken: cancellationToken);

            // Оновлюємо відображення звернення
            var newCallbackQuery1 = new CallbackQuery
            {
                Id = callbackQuery.Id,
                From = callbackQuery.From,
                Message = callbackQuery.Message,
                Data = $"admin_view_{appealId}"
            };
            await HandleAdminAppealViewCallback(botClient, newCallbackQuery1, cancellationToken);
        }
        else
        {
            await botClient.AnswerCallbackQueryAsync(
                callbackQuery.Id,
                $"❌ Помилка: {result.Error}",
                showAlert: true,
                cancellationToken: cancellationToken);
        }
    }

    /// <summary>
    /// Відміняє призначення звернення
    /// </summary>
    public async Task HandleAdminUnassignCallback(
        ITelegramBotClient botClient,
        CallbackQuery callbackQuery,
        CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var userRepo = scope.ServiceProvider.GetRequiredService<IUserRepository>();

        var user = await userRepo.GetByTelegramIdAsync(callbackQuery.From.Id, cancellationToken);
        if (user?.Role != UserRole.Admin)
        {
            await botClient.AnswerCallbackQueryAsync(
                callbackQuery.Id,
                "⛔ У вас немає прав адміністратора",
                showAlert: true,
                cancellationToken: cancellationToken);
            return;
        }

        var appealIdStr = callbackQuery.Data!.Replace("admin_unassign_", "");
        if (!int.TryParse(appealIdStr, out var appealId))
        {
            await botClient.AnswerCallbackQueryAsync(
                callbackQuery.Id,
                "❌ Некоректний ID звернення",
                showAlert: true,
                cancellationToken: cancellationToken);
            return;
        }

        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var result = await mediator.Send(new AssignAppealCommand(
            appealId,
            user.TelegramId
        ), cancellationToken);

        if (result.IsSuccess)
        {
            await botClient.AnswerCallbackQueryAsync(
                callbackQuery.Id,
                "✅ Призначення скасовано",
                cancellationToken: cancellationToken);

            // Оновлюємо відображення звернення
            var newCallbackQuery2 = new CallbackQuery
            {
                Id = callbackQuery.Id,
                From = callbackQuery.From,
                Message = callbackQuery.Message,
                Data = $"admin_view_{appealId}"
            };
            await HandleAdminAppealViewCallback(botClient, newCallbackQuery2, cancellationToken);
        }
        else
        {
            await botClient.AnswerCallbackQueryAsync(
                callbackQuery.Id,
                $"❌ Помилка: {result.Error}",
                showAlert: true,
                cancellationToken: cancellationToken);
        }
    }

    /// <summary>
    /// Показує меню зміни пріоритету
    /// </summary>
    public async Task HandleAdminPriorityMenuCallback(
        ITelegramBotClient botClient,
        CallbackQuery callbackQuery,
        CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var userRepo = scope.ServiceProvider.GetRequiredService<IUserRepository>();

        var user = await userRepo.GetByTelegramIdAsync(callbackQuery.From.Id, cancellationToken);
        if (user?.Role != UserRole.Admin)
        {
            await botClient.AnswerCallbackQueryAsync(
                callbackQuery.Id,
                "⛔ У вас немає прав адміністратора",
                showAlert: true,
                cancellationToken: cancellationToken);
            return;
        }

        var appealIdStr = callbackQuery.Data!.Replace("admin_priority_", "");
        if (!int.TryParse(appealIdStr, out var appealId))
        {
            await botClient.AnswerCallbackQueryAsync(
                callbackQuery.Id,
                "❌ Некоректний ID звернення",
                showAlert: true,
                cancellationToken: cancellationToken);
            return;
        }

        await botClient.EditMessageReplyMarkupAsync(
            chatId: callbackQuery.Message!.Chat.Id,
            messageId: callbackQuery.Message.MessageId,
            replyMarkup: GetPrioritySelectionKeyboard(appealId),
            cancellationToken: cancellationToken);

        await botClient.AnswerCallbackQueryAsync(
            callbackQueryId: callbackQuery.Id,
            text: "Оберіть новий пріоритет:",
            cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Встановлює пріоритет звернення
    /// </summary>
    public async Task HandleAdminSetPriorityCallback(
        ITelegramBotClient botClient,
        CallbackQuery callbackQuery,
        CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var userRepo = scope.ServiceProvider.GetRequiredService<IUserRepository>();

        var user = await userRepo.GetByTelegramIdAsync(callbackQuery.From.Id, cancellationToken);
        if (user?.Role != UserRole.Admin)
        {
            await botClient.AnswerCallbackQueryAsync(
                callbackQuery.Id,
                "⛔ У вас немає прав адміністратора",
                showAlert: true,
                cancellationToken: cancellationToken);
            return;
        }

        var parts = callbackQuery.Data!.Replace("admin_set_priority_", "").Split('_');
        if (parts.Length != 2 || !int.TryParse(parts[0], out var appealId) || !int.TryParse(parts[1], out var priorityValue))
        {
            await botClient.AnswerCallbackQueryAsync(
                callbackQuery.Id,
                "❌ Некоректні параметри",
                showAlert: true,
                cancellationToken: cancellationToken);
            return;
        }

        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var result = await mediator.Send(new UpdatePriorityCommand
        {
            AppealId = appealId,
            AdminId = user.TelegramId,
            Priority = (AppealPriority)priorityValue
        }, cancellationToken);

        if (result.IsSuccess)
        {
            var priorityName = ((AppealPriority)priorityValue) switch
            {
                AppealPriority.Low => "Низький",
                AppealPriority.Normal => "Звичайний", 
                AppealPriority.High => "Високий",
                AppealPriority.Urgent => "Терміновий",
                _ => "Невизначений"
            };

            await botClient.AnswerCallbackQueryAsync(
                callbackQuery.Id,
                $"✅ Пріоритет змінено на: {priorityName}",
                cancellationToken: cancellationToken);

            // Оновлюємо відображення звернення
            var newCallbackQuery3 = new CallbackQuery
            {
                Id = callbackQuery.Id,
                From = callbackQuery.From,
                Message = callbackQuery.Message,
                Data = $"admin_view_{appealId}"
            };
            await HandleAdminAppealViewCallback(botClient, newCallbackQuery3, cancellationToken);
        }
        else
        {
            await botClient.AnswerCallbackQueryAsync(
                callbackQuery.Id,
                $"❌ Помилка: {result.Error}",
                showAlert: true,
                cancellationToken: cancellationToken);
        }
    }

    /// <summary>
    /// Встановлює статус звернення
    /// </summary>
    public async Task HandleAdminSetStatusCallback(
        ITelegramBotClient botClient,
        CallbackQuery callbackQuery,
        CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var userRepo = scope.ServiceProvider.GetRequiredService<IUserRepository>();

        var user = await userRepo.GetByTelegramIdAsync(callbackQuery.From.Id, cancellationToken);
        if (user?.Role != UserRole.Admin)
        {
            await botClient.AnswerCallbackQueryAsync(
                callbackQuery.Id,
                "⛔ У вас немає прав адміністратора",
                showAlert: true,
                cancellationToken: cancellationToken);
            return;
        }

        var parts = callbackQuery.Data!.Replace("admin_set_status_", "").Split('_');
        if (parts.Length != 2 || !int.TryParse(parts[0], out var appealId) || !int.TryParse(parts[1], out var statusValue))
        {
            await botClient.AnswerCallbackQueryAsync(
                callbackQuery.Id,
                "❌ Некоректні параметри",
                showAlert: true,
                cancellationToken: cancellationToken);
            return;
        }

        var newStatus = (AppealStatus)statusValue;

        // TODO: Створити UpdateAppealStatusCommand для зміни статусу
        // var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        // var result = await mediator.Send(new UpdateAppealStatusCommand(...), cancellationToken);

        await botClient.AnswerCallbackQueryAsync(
            callbackQuery.Id,
            $"⚠️ Зміна статусу в розробці. Обраний статус: {newStatus.GetDisplayName()}",
            showAlert: true,
            cancellationToken: cancellationToken);

        // Повертаємось до перегляду звернення
        var newCallbackQuery = new CallbackQuery
        {
            Id = callbackQuery.Id,
            From = callbackQuery.From,
            Message = callbackQuery.Message,
            Data = $"admin_view_{appealId}"
        };
        await HandleAdminAppealViewCallback(botClient, newCallbackQuery, cancellationToken);
    }

    /// <summary>
    /// Розпочинає процес відповіді на звернення
    /// </summary>
    public async Task HandleAdminReplyCallback(
        ITelegramBotClient botClient,
        CallbackQuery callbackQuery,
        CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var userRepo = scope.ServiceProvider.GetRequiredService<IUserRepository>();

        var user = await userRepo.GetByTelegramIdAsync(callbackQuery.From.Id, cancellationToken);
        if (user?.Role != UserRole.Admin)
        {
            await botClient.AnswerCallbackQueryAsync(
                callbackQuery.Id,
                "⛔ У вас немає прав адміністратора",
                showAlert: true,
                cancellationToken: cancellationToken);
            return;
        }

        var appealIdStr = callbackQuery.Data!.Replace("admin_reply_", "");
        if (!int.TryParse(appealIdStr, out var appealId))
        {
            await botClient.AnswerCallbackQueryAsync(
                callbackQuery.Id,
                "❌ Некоректний ID звернення",
                showAlert: true,
                cancellationToken: cancellationToken);
            return;
        }

        // Зберігаємо ID звернення та встановлюємо стан
        using var stateScope = _scopeFactory.CreateScope();
        var stateManager = stateScope.ServiceProvider.GetRequiredService<IUserStateManager>();
        await stateManager.SetDataAsync(user.TelegramId, "reply_appeal_id", appealId, cancellationToken);
        await stateManager.SetStateAsync(user.TelegramId, UserConversationState.WaitingAdminReply, cancellationToken);

        await botClient.AnswerCallbackQueryAsync(
            callbackQueryId: callbackQuery.Id,
            text: "Введіть відповідь:",
            cancellationToken: cancellationToken);

        // Створюємо keyboard з кнопкою шаблонів
        var keyboard = new InlineKeyboardMarkup(new[]
        {
            new[]
            {
                InlineKeyboardButton.WithCallbackData("📝 Використати шаблон", $"admin_templates_{appealId}"),
                InlineKeyboardButton.WithCallbackData("🔙 Скасувати", $"admin_view_{appealId}")
            }
        });

        await botClient.SendTextMessageAsync(
            chatId: callbackQuery.Message!.Chat.Id,
            text: $"✍️ <b>Відповідь на звернення #{appealId}</b>\n\n" +
                  "Введіть текст вашої відповіді або оберіть шаблон:\n\n" +
                  "<i>Мінімум 5 символів, максимум 2000 символів</i>",
            parseMode: ParseMode.Html,
            replyMarkup: keyboard,
            cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Розпочинає процес закриття звернення
    /// </summary>
    public async Task HandleAdminCloseAppealCallback(
        ITelegramBotClient botClient,
        CallbackQuery callbackQuery,
        CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var userRepo = scope.ServiceProvider.GetRequiredService<IUserRepository>();

        var user = await userRepo.GetByTelegramIdAsync(callbackQuery.From.Id, cancellationToken);
        if (user?.Role != UserRole.Admin)
        {
            await botClient.AnswerCallbackQueryAsync(
                callbackQuery.Id,
                "⛔ У вас немає прав адміністратора",
                showAlert: true,
                cancellationToken: cancellationToken);
            return;
        }

        var appealIdStr = callbackQuery.Data!.Replace("admin_close_", "");
        if (!int.TryParse(appealIdStr, out var appealId))
        {
            await botClient.AnswerCallbackQueryAsync(
                callbackQuery.Id,
                "❌ Некоректний ID звернення",
                showAlert: true,
                cancellationToken: cancellationToken);
            return;
        }

        // Зберігаємо ID звернення та встановлюємо стан
        using var stateScope = _scopeFactory.CreateScope();
        var stateManager = stateScope.ServiceProvider.GetRequiredService<IUserStateManager>();
        await stateManager.SetDataAsync(user.TelegramId, "close_appeal_id", appealId, cancellationToken);
        await stateManager.SetStateAsync(user.TelegramId, UserConversationState.WaitingCloseReason, cancellationToken);

        await botClient.AnswerCallbackQueryAsync(
            callbackQueryId: callbackQuery.Id,
            text: "Введіть причину закриття:",
            cancellationToken: cancellationToken);

        await botClient.SendTextMessageAsync(
            chatId: callbackQuery.Message!.Chat.Id,
            text: $"❌ <b>Закриття звернення #{appealId}</b>\n\n" +
                  "Введіть причину закриття звернення:\n\n" +
                  "<i>Мінімум 5 символів, максимум 500 символів</i>",
            parseMode: ParseMode.Html,
            replyMarkup: GetBackToMainMenu(),
            cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Обробляє введення тексту відповіді адміном
    /// </summary>
    public async Task HandleAdminReplyInputAsync(
        ITelegramBotClient botClient,
        Message message,
        CancellationToken cancellationToken)
    {
        var userId = message.From!.Id;
        var replyText = message.Text?.Trim();

        // Валідація довжини відповіді
        if (string.IsNullOrWhiteSpace(replyText) || replyText.Length < 5)
        {
            await SendErrorMessageAsync(
                botClient,
                message.Chat.Id,
                "❌ Відповідь занадто коротка (мінімум 5 символів)",
                cancellationToken);
            return;
        }

        if (replyText.Length > 2000)
        {
            await SendErrorMessageAsync(
                botClient,
                message.Chat.Id,
                "❌ Відповідь занадто довга (максимум 2000 символів)",
                cancellationToken);
            return;
        }

        try
        {
            using var stateScope = _scopeFactory.CreateScope();
            var stateManager = stateScope.ServiceProvider.GetRequiredService<IUserStateManager>();
            
            // Отримуємо збережений ID звернення
            var appealId = await stateManager.GetDataAsync<int>(userId, "reply_appeal_id", cancellationToken);

            if (appealId == 0)
            {
                await SendErrorMessageAsync(
                    botClient,
                    message.Chat.Id,
                    "❌ Помилка: ID звернення не знайдено",
                    cancellationToken);

                await stateManager.ClearStateAsync(userId, cancellationToken);
                await stateManager.ClearAllDataAsync(userId, cancellationToken);
                return;
            }

            // Надсилаємо відповідь на звернення
            var result = await _mediator.Send(new ReplyToAppealCommand
            {
                AppealId = appealId,
                AdminId = userId,
                Text = replyText
            }, cancellationToken);

            if (result.IsSuccess)
            {
                await stateManager.ClearStateAsync(userId, cancellationToken);
                await stateManager.ClearAllDataAsync(userId, cancellationToken);

                await botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: "✅ <b>Відповідь надіслана!</b>\n\n" +
                          "Користувач отримає повідомлення про вашу відповідь.",
                    parseMode: ParseMode.Html,
                    replyMarkup: GetMainMenu(),
                    cancellationToken: cancellationToken);
            }
            else
            {
                await SendErrorMessageAsync(
                    botClient,
                    message.Chat.Id,
                    $"❌ Помилка при відправці відповіді: {result.Error}",
                    cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Помилка при обробці відповіді адміна для звернення користувача {UserId}", userId);

            await SendErrorMessageAsync(
                botClient,
                message.Chat.Id,
                "❌ Виникла помилка при відправці відповіді. Спробуйте пізніше.",
                cancellationToken);

            using var errorStateScope = _scopeFactory.CreateScope();
            var errorStateManager = errorStateScope.ServiceProvider.GetRequiredService<IUserStateManager>();
            await errorStateManager.ClearStateAsync(userId, cancellationToken);
            await errorStateManager.ClearAllDataAsync(userId, cancellationToken);
        }
    }

    /// <summary>
    /// Обробляє введення причини закриття звернення
    /// </summary>
    public async Task HandleCloseReasonInputAsync(
        ITelegramBotClient botClient,
        Message message,
        CancellationToken cancellationToken)
    {
        var userId = message.From!.Id;
        var reason = message.Text?.Trim();

        // Валідація довжини причини
        if (string.IsNullOrWhiteSpace(reason) || reason.Length < 5)
        {
            await SendErrorMessageAsync(
                botClient,
                message.Chat.Id,
                "❌ Причина закриття занадто коротка (мінімум 5 символів)",
                cancellationToken);
            return;
        }

        if (reason.Length > 500)
        {
            await SendErrorMessageAsync(
                botClient,
                message.Chat.Id,
                "❌ Причина закриття занадто довга (максимум 500 символів)",
                cancellationToken);
            return;
        }

        try
        {
            using var stateScope = _scopeFactory.CreateScope();
            var stateManager = stateScope.ServiceProvider.GetRequiredService<IUserStateManager>();
            
            // Отримуємо збережений ID звернення
            var appealId = await stateManager.GetDataAsync<int>(userId, "close_appeal_id", cancellationToken);

            if (appealId == 0)
            {
                await SendErrorMessageAsync(
                    botClient,
                    message.Chat.Id,
                    "❌ Помилка: ID звернення не знайдено",
                    cancellationToken);

                await stateManager.ClearStateAsync(userId, cancellationToken);
                await stateManager.ClearAllDataAsync(userId, cancellationToken);
                return;
            }

            // Закриваємо звернення з вказаною причиною
            var result = await _mediator.Send(new CloseAppealCommand
            {
                AppealId = appealId,
                AdminId = userId,
                Reason = reason
            }, cancellationToken);

            if (result.IsSuccess)
            {
                await stateManager.ClearStateAsync(userId, cancellationToken);
                await stateManager.ClearAllDataAsync(userId, cancellationToken);

                await botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: "✅ <b>Звернення закрито!</b>\n\n" +
                          $"Причина: {reason}\n\n" +
                          "Користувач отримає повідомлення про закриття звернення.",
                    parseMode: ParseMode.Html,
                    replyMarkup: GetMainMenu(),
                    cancellationToken: cancellationToken);
            }
            else
            {
                await SendErrorMessageAsync(
                    botClient,
                    message.Chat.Id,
                    $"❌ Помилка при закритті звернення: {result.Error}",
                    cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Помилка при обробці причини закриття звернення для користувача {UserId}", userId);

            await SendErrorMessageAsync(
                botClient,
                message.Chat.Id,
                "❌ Виникла помилка при закritті звернення. Спробуйте пізніше.",
                cancellationToken);

            using var errorStateScope = _scopeFactory.CreateScope();
            var errorStateManager = errorStateScope.ServiceProvider.GetRequiredService<IUserStateManager>();
            await errorStateManager.ClearStateAsync(userId, cancellationToken);
            await errorStateManager.ClearAllDataAsync(userId, cancellationToken);
        }
    }

    /// <summary>
    /// Створює клавіатуру дій для звернення адміністратора
    /// </summary>
    private InlineKeyboardMarkup GetAdminAppealActionsKeyboard(int appealId, bool isAssignedToMe, bool isClosed)
    {
        var buttons = new List<List<InlineKeyboardButton>>();

        if (!isClosed)
        {
            // ⚡ QUICK ACTIONS - Перший рядок (найчастіші дії)
            if (isAssignedToMe)
            {
                // Якщо вже призначено - показуємо Відповісти та Закрити
                buttons.Add(new List<InlineKeyboardButton>
                {
                    InlineKeyboardButton.WithCallbackData("💬 Відповісти", $"admin_reply_{appealId}"),
                    InlineKeyboardButton.WithCallbackData("✅ Закрити", $"admin_close_{appealId}")
                });
            }
            else
            {
                // Якщо не призначено - показуємо Прийняти та Відповісти
                buttons.Add(new List<InlineKeyboardButton>
                {
                    InlineKeyboardButton.WithCallbackData("✅ Прийняти", $"admin_assign_me_{appealId}"),
                    InlineKeyboardButton.WithCallbackData("💬 Відповісти", $"admin_reply_{appealId}")
                });
            }

            // Другий рядок - додаткові дії
            var secondRow = new List<InlineKeyboardButton>();
            
            if (isAssignedToMe)
            {
                secondRow.Add(InlineKeyboardButton.WithCallbackData("🔓 Відмінити призначення", $"admin_unassign_{appealId}"));
            }
            
            secondRow.Add(InlineKeyboardButton.WithCallbackData("🔺 Ескалювати", $"admin_escalate_{appealId}"));
            
            if (secondRow.Count > 0)
            {
                buttons.Add(secondRow);
            }

            // Третій рядок - пріоритет та статус
            buttons.Add(new List<InlineKeyboardButton>
            {
                InlineKeyboardButton.WithCallbackData("🎯 Пріоритет", $"admin_priority_{appealId}"),
                InlineKeyboardButton.WithCallbackData("📊 Статус", $"admin_status_{appealId}")
            });
        }
        else
        {
            // Для закритих звернень - тільки можливість переглянути
            buttons.Add(new List<InlineKeyboardButton>
            {
                InlineKeyboardButton.WithCallbackData("🔄 Відкрити знову", $"admin_reopen_{appealId}")
            });
        }

        // Кнопка назад
        buttons.Add(new List<InlineKeyboardButton>
        {
            InlineKeyboardButton.WithCallbackData("🔙 Список звернень", "admin_appeals"),
            InlineKeyboardButton.WithCallbackData("🏠 Адмін панель", "admin_panel")
        });

        return new InlineKeyboardMarkup(buttons);
    }

    /// <summary>
    /// Створює клавіатуру вибору пріоритету
    /// </summary>
    private InlineKeyboardMarkup GetPrioritySelectionKeyboard(int appealId)
    {
        return new InlineKeyboardMarkup(new[]
        {
            new[]
            {
                InlineKeyboardButton.WithCallbackData("🟢 Низький", $"admin_set_priority_{appealId}_{(int)AppealPriority.Low}"),
                InlineKeyboardButton.WithCallbackData("🟡 Звичайний", $"admin_set_priority_{appealId}_{(int)AppealPriority.Normal}")
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData("🟠 Високий", $"admin_set_priority_{appealId}_{(int)AppealPriority.High}"),
                InlineKeyboardButton.WithCallbackData("🔴 Терміновий", $"admin_set_priority_{appealId}_{(int)AppealPriority.Urgent}")
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData("🔙 Назад", $"admin_view_{appealId}")
            }
        });
    }

    /// <summary>
    /// Ескалює звернення (підвищує пріоритет та змінює статус)
    /// </summary>
    public async Task HandleAdminEscalateCallback(
        ITelegramBotClient botClient,
        CallbackQuery callbackQuery,
        CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var userRepo = scope.ServiceProvider.GetRequiredService<IUserRepository>();

        var user = await userRepo.GetByTelegramIdAsync(callbackQuery.From.Id, cancellationToken);
        if (user?.Role != UserRole.Admin)
        {
            await botClient.AnswerCallbackQueryAsync(
                callbackQuery.Id,
                "⛔ У вас немає прав адміністратора",
                showAlert: true,
                cancellationToken: cancellationToken);
            return;
        }

        var appealIdStr = callbackQuery.Data!.Replace("admin_escalate_", "");
        if (!int.TryParse(appealIdStr, out var appealId))
        {
            await botClient.AnswerCallbackQueryAsync(
                callbackQuery.Id,
                "❌ Некоректний ID звернення",
                showAlert: true,
                cancellationToken: cancellationToken);
            return;
        }

        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        
        // Отримуємо звернення
        var appealResult = await mediator.Send(new GetAppealByIdQuery 
        { 
            AppealId = appealId, 
            RequestUserId = user.TelegramId 
        }, cancellationToken);

        if (!appealResult.IsSuccess || appealResult.Value == null)
        {
            await botClient.AnswerCallbackQueryAsync(
                callbackQuery.Id,
                "❌ Звернення не знайдено",
                showAlert: true,
                cancellationToken: cancellationToken);
            return;
        }

        var appeal = appealResult.Value;

        // Змінюємо пріоритет на Urgent
        var priorityResult = await mediator.Send(new UpdatePriorityCommand
        {
            AppealId = appealId,
            AdminId = user.TelegramId,
            Priority = AppealPriority.Urgent
        }, cancellationToken);

        if (!priorityResult.IsSuccess)
        {
            await botClient.AnswerCallbackQueryAsync(
                callbackQuery.Id,
                $"❌ Помилка ескалації: {priorityResult.Error}",
                showAlert: true,
                cancellationToken: cancellationToken);
            return;
        }

        // TODO: Додати зміну статусу на Escalated, коли буде відповідна команда
        // var statusResult = await mediator.Send(new ChangeAppealStatusCommand(...), cancellationToken);

        await botClient.AnswerCallbackQueryAsync(
            callbackQuery.Id,
            "🔺 Звернення ескальовано (пріоритет: ТЕРМІНОВИЙ)",
            cancellationToken: cancellationToken);

        // Оновлюємо відображення звернення
        var newCallbackQuery = new CallbackQuery
        {
            Id = callbackQuery.Id,
            From = callbackQuery.From,
            Message = callbackQuery.Message,
            Data = $"admin_view_{appealId}"
        };

        await HandleAdminAppealViewCallback(botClient, newCallbackQuery, cancellationToken);
    }

    /// <summary>
    /// Показує меню зміни статусу звернення
    /// </summary>
    public async Task HandleAdminStatusMenuCallback(
        ITelegramBotClient botClient,
        CallbackQuery callbackQuery,
        CancellationToken cancellationToken)
    {
        var appealIdStr = callbackQuery.Data!.Replace("admin_status_", "");
        if (!int.TryParse(appealIdStr, out var appealId))
        {
            await botClient.AnswerCallbackQueryAsync(
                callbackQuery.Id,
                "❌ Некоректний ID звернення",
                showAlert: true,
                cancellationToken: cancellationToken);
            return;
        }

        var keyboard = GetStatusSelectionKeyboard(appealId);

        await botClient.EditMessageReplyMarkupAsync(
            chatId: callbackQuery.Message!.Chat.Id,
            messageId: callbackQuery.Message.MessageId,
            replyMarkup: keyboard,
            cancellationToken: cancellationToken);

        await botClient.AnswerCallbackQueryAsync(
            callbackQueryId: callbackQuery.Id,
            text: "Оберіть новий статус",
            cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Повторно відкриває закрите звернення
    /// </summary>
    public async Task HandleAdminReopenCallback(
        ITelegramBotClient botClient,
        CallbackQuery callbackQuery,
        CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var userRepo = scope.ServiceProvider.GetRequiredService<IUserRepository>();

        var user = await userRepo.GetByTelegramIdAsync(callbackQuery.From.Id, cancellationToken);
        if (user?.Role != UserRole.Admin)
        {
            await botClient.AnswerCallbackQueryAsync(
                callbackQuery.Id,
                "⛔ У вас немає прав адміністратора",
                showAlert: true,
                cancellationToken: cancellationToken);
            return;
        }

        var appealIdStr = callbackQuery.Data!.Replace("admin_reopen_", "");
        if (!int.TryParse(appealIdStr, out var appealId))
        {
            await botClient.AnswerCallbackQueryAsync(
                callbackQuery.Id,
                "❌ Некоректний ID звернення",
                showAlert: true,
                cancellationToken: cancellationToken);
            return;
        }

        // TODO: Створити ReopenAppealCommand
        // var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        // var result = await mediator.Send(new ReopenAppealCommand(appealId, user.TelegramId), cancellationToken);

        await botClient.AnswerCallbackQueryAsync(
            callbackQuery.Id,
            "⚠️ Функція повторного відкриття в розробці",
            showAlert: true,
            cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Показує меню вибору шаблонів відповідей
    /// </summary>
    public async Task HandleAdminTemplatesMenuCallback(
        ITelegramBotClient botClient,
        CallbackQuery callbackQuery,
        CancellationToken cancellationToken)
    {
        var appealIdStr = callbackQuery.Data!.Replace("admin_templates_", "");
        if (!int.TryParse(appealIdStr, out var appealId))
        {
            await botClient.AnswerCallbackQueryAsync(
                callbackQuery.Id,
                "❌ Некоректний ID звернення",
                showAlert: true,
                cancellationToken: cancellationToken);
            return;
        }

        // Отримуємо інформацію про звернення для визначення категорії
        using var scope = _scopeFactory.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var appealResult = await mediator.Send(new GetAppealByIdQuery 
        { 
            AppealId = appealId, 
            RequestUserId = callbackQuery.From.Id 
        }, cancellationToken);

        AppealCategory? appealCategory = null;
        if (appealResult.IsSuccess && appealResult.Value != null)
        {
            appealCategory = appealResult.Value.Category;
        }

        var keyboard = AdminReplyTemplatesHelper.CreateTemplatesKeyboard(appealId, appealCategory);

        await botClient.EditMessageTextAsync(
            chatId: callbackQuery.Message!.Chat.Id,
            messageId: callbackQuery.Message.MessageId,
            text: "📝 <b>Шаблони відповідей</b>\n\nОберіть категорію шаблону:",
            parseMode: ParseMode.Html,
            replyMarkup: keyboard,
            cancellationToken: cancellationToken);

        await botClient.AnswerCallbackQueryAsync(
            callbackQueryId: callbackQuery.Id,
            cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Показує список шаблонів обраної категорії
    /// </summary>
    public async Task HandleAdminTemplateCategoryCallback(
        ITelegramBotClient botClient,
        CallbackQuery callbackQuery,
        CancellationToken cancellationToken)
    {
        // admin_template_ack_123, admin_template_progress_123, etc.
        var parts = callbackQuery.Data!.Split('_');
        if (parts.Length < 4 || !int.TryParse(parts[3], out var appealId))
        {
            await botClient.AnswerCallbackQueryAsync(
                callbackQuery.Id,
                "❌ Некоректні параметри",
                showAlert: true,
                cancellationToken: cancellationToken);
            return;
        }

        var categoryStr = parts[2]; // ack, progress, needinfo, resolved, special
        
        if (categoryStr == "special")
        {
            // Обробка спеціальних шаблонів
            using var scope = _scopeFactory.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            var appealResult = await mediator.Send(new GetAppealByIdQuery 
            { 
                AppealId = appealId, 
                RequestUserId = callbackQuery.From.Id 
            }, cancellationToken);

            if (appealResult.IsSuccess && appealResult.Value != null)
            {
                var keyboard = AdminReplyTemplatesHelper.CreateSpecialTemplatesKeyboard(appealId, appealResult.Value.Category);
                
                await botClient.EditMessageTextAsync(
                    chatId: callbackQuery.Message!.Chat.Id,
                    messageId: callbackQuery.Message.MessageId,
                    text: $"⭐ <b>Спеціальні шаблони для {appealResult.Value.Category.GetDisplayName()}</b>\n\nОберіть шаблон:",
                    parseMode: ParseMode.Html,
                    replyMarkup: keyboard,
                    cancellationToken: cancellationToken);
            }
        }
        else
        {
            var category = categoryStr switch
            {
                "ack" => AdminReplyTemplatesHelper.TemplateCategory.Acknowledgment,
                "progress" => AdminReplyTemplatesHelper.TemplateCategory.InProgress,
                "needinfo" => AdminReplyTemplatesHelper.TemplateCategory.NeedInfo,
                "resolved" => AdminReplyTemplatesHelper.TemplateCategory.Resolved,
                _ => AdminReplyTemplatesHelper.TemplateCategory.Acknowledgment
            };

            var keyboard = AdminReplyTemplatesHelper.CreateCategoryTemplatesKeyboard(appealId, category);
            var categoryName = AdminReplyTemplatesHelper.TemplateCategory.Acknowledgment.ToString(); // TODO: Add method to get name

            await botClient.EditMessageTextAsync(
                chatId: callbackQuery.Message!.Chat.Id,
                messageId: callbackQuery.Message.MessageId,
                text: $"📝 <b>Шаблони категорії</b>\n\nОберіть шаблон:",
                parseMode: ParseMode.Html,
                replyMarkup: keyboard,
                cancellationToken: cancellationToken);
        }

        await botClient.AnswerCallbackQueryAsync(
            callbackQueryId: callbackQuery.Id,
            cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Використовує обраний шаблон для відповіді
    /// </summary>
    public async Task HandleAdminUseTemplateCallback(
        ITelegramBotClient botClient,
        CallbackQuery callbackQuery,
        CancellationToken cancellationToken)
    {
        // admin_use_template_123_1_2 (appealId_categoryId_templateIndex)
        // admin_use_special_template_123_1_0 (appealId_appealCategoryId_templateIndex)
        var isSpecial = callbackQuery.Data!.Contains("special");
        var parts = callbackQuery.Data!.Split('_');
        
        int appealIdIndex = isSpecial ? 4 : 3;
        int categoryIndex = isSpecial ? 5 : 4;
        int templateIndex_idx = isSpecial ? 6 : 5;

        if (parts.Length <= templateIndex_idx || 
            !int.TryParse(parts[appealIdIndex], out var appealId) ||
            !int.TryParse(parts[categoryIndex], out var categoryId) ||
            !int.TryParse(parts[templateIndex_idx], out var templateIndex))
        {
            await botClient.AnswerCallbackQueryAsync(
                callbackQuery.Id,
                "❌ Некоректні параметри",
                showAlert: true,
                cancellationToken: cancellationToken);
            return;
        }

        string? templateText = null;
        
        if (isSpecial)
        {
            templateText = AdminReplyTemplatesHelper.GetSpecialTemplateText((AppealCategory)categoryId, templateIndex);
        }
        else
        {
            templateText = AdminReplyTemplatesHelper.GetTemplateText((AdminReplyTemplatesHelper.TemplateCategory)categoryId, templateIndex);
        }

        if (string.IsNullOrEmpty(templateText))
        {
            await botClient.AnswerCallbackQueryAsync(
                callbackQuery.Id,
                "❌ Шаблон не знайдено",
                showAlert: true,
                cancellationToken: cancellationToken);
            return;
        }

        // Автоматично відправляємо відповідь з використанням шаблону
        using var scope = _scopeFactory.CreateScope();
        var userRepo = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        var user = await userRepo.GetByTelegramIdAsync(callbackQuery.From.Id, cancellationToken);

        if (user?.Role != UserRole.Admin)
        {
            await botClient.AnswerCallbackQueryAsync(
                callbackQuery.Id,
                "⛔ У вас немає прав адміністратора",
                showAlert: true,
                cancellationToken: cancellationToken);
            return;
        }

        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var result = await mediator.Send(new ReplyToAppealCommand
        {
            AppealId = appealId,
            AdminId = user.TelegramId,
            AdminName = user.FullName ?? user.Username ?? "Адміністратор",
            Text = templateText
        }, cancellationToken);

        if (result.IsSuccess)
        {
            await botClient.AnswerCallbackQueryAsync(
                callbackQuery.Id,
                "✅ Відповідь відправлено",
                cancellationToken: cancellationToken);

            // Повертаємось до перегляду звернення
            var newCallbackQuery = new CallbackQuery
            {
                Id = callbackQuery.Id,
                From = callbackQuery.From,
                Message = callbackQuery.Message,
                Data = $"admin_view_{appealId}"
            };
            await HandleAdminAppealViewCallback(botClient, newCallbackQuery, cancellationToken);
        }
        else
        {
            await botClient.AnswerCallbackQueryAsync(
                callbackQuery.Id,
                $"❌ Помилка: {result.Error}",
                showAlert: true,
                cancellationToken: cancellationToken);
        }
    }

    /// <summary>
    /// Створює клавіатуру вибору статусу
    /// </summary>
    private InlineKeyboardMarkup GetStatusSelectionKeyboard(int appealId)
    {
        return new InlineKeyboardMarkup(new[]
        {
            new[]
            {
                InlineKeyboardButton.WithCallbackData("🆕 Нове", $"admin_set_status_{appealId}_{(int)AppealStatus.New}"),
                InlineKeyboardButton.WithCallbackData("⏳ В роботі", $"admin_set_status_{appealId}_{(int)AppealStatus.InProgress}")
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData("⌛ Очікує студента", $"admin_set_status_{appealId}_{(int)AppealStatus.WaitingForStudent}"),
                InlineKeyboardButton.WithCallbackData("⏰ Очікує адміна", $"admin_set_status_{appealId}_{(int)AppealStatus.WaitingForAdmin}")
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData("🔺 Ескальовано", $"admin_set_status_{appealId}_{(int)AppealStatus.Escalated}"),
                InlineKeyboardButton.WithCallbackData("✅ Вирішено", $"admin_set_status_{appealId}_{(int)AppealStatus.Resolved}")
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData("🔙 Назад", $"admin_view_{appealId}")
            }
        });
    }

    /// <summary>
    /// Обробляє текстові повідомлення адміна
    /// </summary>
    public override async Task HandleTextMessageAsync(
        ITelegramBotClient botClient,
        Message message,
        UserConversationState state,
        CancellationToken cancellationToken)
    {
        switch (state)
        {
            case UserConversationState.WaitingAdminReply:
                await HandleAdminReplyInputAsync(botClient, message, cancellationToken);
                break;

            case UserConversationState.WaitingCloseReason:
                await HandleCloseReasonInputAsync(botClient, message, cancellationToken);
                break;

            default:
                // Інші стани не обробляються цим хендлером
                break;
        }
    }
}