using MediatR;
using Microsoft.Extensions.Logging;
using StudentUnionBot.Application.Common.Interfaces;
using StudentUnionBot.Domain.Enums;
using StudentUnionBot.Domain.Interfaces;
using StudentUnionBot.Application.Contacts.Queries.GetAllContacts;
using StudentUnionBot.Application.Events.Queries.GetUpcomingEvents;
using StudentUnionBot.Application.News.Queries.GetPublishedNews;
using StudentUnionBot.Application.Partners.Queries.GetActivePartners;
using StudentUnionBot.Application.Users.Commands.RegisterUser;
using StudentUnionBot.Application.Appeals.Commands.CreateAppeal;
using StudentUnionBot.Application.Appeals.Queries.GetUserAppeals;
using StudentUnionBot.Application.Appeals.Queries.GetAppealById;
using StudentUnionBot.Application.Appeals.Queries.GetAdminAppeals;
using StudentUnionBot.Application.Appeals.Commands.ReplyToAppeal;
using StudentUnionBot.Application.Appeals.Commands.CloseAppeal;
using StudentUnionBot.Application.Appeals.Commands.AssignAppeal;
using StudentUnionBot.Application.Appeals.Commands.UpdatePriority;
using StudentUnionBot.Application.Users.Commands.SendVerificationEmail;
using StudentUnionBot.Application.Users.Commands.VerifyEmail;
using StudentUnionBot.Presentation.Bot.Keyboards;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace StudentUnionBot.Presentation.Bot.Handlers;

/// <summary>
/// Головний обробник оновлень від Telegram
/// </summary>
public class UpdateHandler : IBotUpdateHandler
{
    private readonly ILogger<UpdateHandler> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IUserStateManager _stateManager;

    public UpdateHandler(
        ILogger<UpdateHandler> logger,
        IServiceScopeFactory scopeFactory,
        IUserStateManager stateManager)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
        _stateManager = stateManager;
    }

    public async Task HandleUpdateAsync(
        ITelegramBotClient botClient,
        Update update,
        CancellationToken cancellationToken)
    {
        var handler = update.Type switch
        {
            UpdateType.Message => HandleMessageAsync(botClient, update.Message!, cancellationToken),
            UpdateType.CallbackQuery => HandleCallbackQueryAsync(botClient, update.CallbackQuery!, cancellationToken),
            UpdateType.EditedMessage => HandleEditedMessageAsync(botClient, update.EditedMessage!, cancellationToken),
            _ => HandleUnknownUpdateAsync(botClient, update, cancellationToken)
        };

        try
        {
            await handler;
        }
        catch (Exception ex)
        {
            await HandleErrorAsync(botClient, ex, cancellationToken);
        }
    }

    private async Task HandleMessageAsync(
        ITelegramBotClient botClient,
        Message message,
        CancellationToken cancellationToken)
    {
        if (message.Text is not { } messageText)
            return;

        var chatId = message.Chat.Id;
        var userId = message.From?.Id ?? 0;

        _logger.LogInformation(
            "Отримано повідомлення від {UserId} в чаті {ChatId}: {Text}",
            userId,
            chatId,
            messageText);

        // Реєструємо/оновлюємо користувача при кожній взаємодії
        await RegisterOrUpdateUserAsync(message.From, cancellationToken);

        // Обробка команд
        if (messageText.StartsWith('/'))
        {
            await HandleCommandAsync(botClient, message, cancellationToken);
            return;
        }

        // Обробка звичайних повідомлень
        await HandleTextMessageAsync(botClient, message, cancellationToken);
    }

    private async Task RegisterOrUpdateUserAsync(User? user, CancellationToken cancellationToken)
    {
        if (user == null) return;

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            
            var command = new RegisterUserCommand
            {
                TelegramId = user.Id,
                Username = user.Username,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Language = user.LanguageCode ?? "uk"
            };

            await mediator.Send(command, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Помилка при реєстрації користувача {UserId}", user.Id);
        }
    }

    private async Task HandleCommandAsync(
        ITelegramBotClient botClient,
        Message message,
        CancellationToken cancellationToken)
    {
        var command = message.Text!.Split(' ')[0].ToLower();
        var userId = message.From!.Id;

        _logger.LogInformation("Обробка команди: {Command}", command);

        // Команди /start та /appeal скасовують поточний процес
        if (command == "/start" || command == "/appeal")
        {
            await _stateManager.ClearStateAsync(userId, cancellationToken);
            await _stateManager.ClearAllDataAsync(userId, cancellationToken);
        }

        // Check if user is admin for menu display
        using var menuScope = _scopeFactory.CreateScope();
        var userRepoForMenu = menuScope.ServiceProvider.GetRequiredService<IUserRepository>();
        var userForMenu = await userRepoForMenu.GetByTelegramIdAsync(message.From.Id, cancellationToken);
        var isAdmin = userForMenu?.Role == UserRole.Admin;

        _logger.LogInformation(
            "Користувач {TelegramId} має роль {Role}, isAdmin={IsAdmin}",
            message.From.Id,
            userForMenu?.Role,
            isAdmin);

        var (responseText, keyboard) = command switch
        {
            "/start" => (
                "🎓 <b>Вітаємо в боті Студентського Профкому ВНМУ!</b>\n\n" +
                "Я допоможу вам:\n" +
                "📝 Створити звернення до профкому\n" +
                "📋 Відстежувати статус ваших звернень\n" +
                "📰 Дізнаватися останні новини\n" +
                "🎉 Бути в курсі майбутніх подій\n" +
                "🤝 Отримувати знижки від партнерів\n\n" +
                "Оберіть дію з меню нижче:",
                KeyboardFactory.GetMainMenuKeyboard(isAdmin) as IReplyMarkup),
            
            "/help" => (
                "📌 <b>Довідка по боту</b>\n\n" +
                "<b>Основні команди:</b>\n" +
                "/start - Головне меню\n" +
                "/appeal - Створити звернення\n" +
                "/myappeals - Мої звернення\n" +
                "/news - Новини\n" +
                "/events - Заходи\n" +
                "/profile - Мій профіль\n" +
                "/contacts - Контакти\n\n" +
                "<b>Як створити звернення?</b>\n" +
                "1. Натисніть '📝 Створити звернення'\n" +
                "2. Оберіть категорію\n" +
                "3. Опишіть вашу проблему\n" +
                "4. Очікуйте на відповідь від адміністрації\n\n" +
                "За питаннями звертайтесь: @vnmu_profkom",
                KeyboardFactory.GetBackToMainMenuKeyboard() as IReplyMarkup),
            
            "/appeal" => (
                "📝 <b>Створення звернення</b>\n\n" +
                "Оберіть категорію вашого звернення:",
                KeyboardFactory.GetAppealCategoriesKeyboard() as IReplyMarkup),
            
            "/myappeals" => (
                "📋 Завантаження ваших звернень...",
                null as IReplyMarkup),
            
            "/news" => (
                "📰 Завантаження новин...",
                null as IReplyMarkup),
            
            "/events" => (
                "🎉 Завантаження подій...",
                null as IReplyMarkup),
            
            "/profile" => (
                "👤 Завантаження профілю...",
                null as IReplyMarkup),
            
            "/contacts" => (
                "📞 <b>Контактна інформація</b>\n\n" +
                "🏛 <b>Студентський профспілковий комітет ВНМУ</b>\n\n" +
                "📧 Email: profkom@vnmu.edu.ua\n" +
                "📱 Telegram: @vnmu_profkom\n" +
                "📍 Адреса: вул. Пирогова, 56, Вінниця\n" +
                "🕐 Години роботи: ПН-ПТ 9:00-17:00\n\n" +
                "Ми завжди раді вам допомогти! 🤝",
                KeyboardFactory.GetBackToMainMenuKeyboard() as IReplyMarkup),
            
            _ => (
                "❓ Невідома команда. Використовуйте /help для перегляду списку команд.",
                KeyboardFactory.GetBackToMainMenuKeyboard() as IReplyMarkup)
        };

        await botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: responseText,
            parseMode: ParseMode.Html,
            replyMarkup: keyboard,
            cancellationToken: cancellationToken);
    }

    private async Task HandleTextMessageAsync(
        ITelegramBotClient botClient,
        Message message,
        CancellationToken cancellationToken)
    {
        var userId = message.From!.Id;
        var state = await _stateManager.GetStateAsync(userId, cancellationToken);

        // Обробка повідомлень на основі поточного стану
        switch (state)
        {
            case UserConversationState.WaitingAppealSubject:
                await HandleAppealSubjectInputAsync(botClient, message, cancellationToken);
                break;

            case UserConversationState.WaitingAppealMessage:
                await HandleAppealMessageInputAsync(botClient, message, cancellationToken);
                break;

            case UserConversationState.WaitingEmailInput:
                await HandleEmailInputAsync(botClient, message, cancellationToken);
                break;

            case UserConversationState.WaitingVerificationCode:
                await HandleVerificationCodeInputAsync(botClient, message, cancellationToken);
                break;

            default:
                // Стандартна відповідь для Idle стану
                await botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: "Я отримав ваше повідомлення. Використовуйте /help для перегляду доступних команд.",
                    replyMarkup: KeyboardFactory.GetBackToMainMenuKeyboard(),
                    cancellationToken: cancellationToken);
                break;
        }
    }

    private async Task HandleAppealSubjectInputAsync(
        ITelegramBotClient botClient,
        Message message,
        CancellationToken cancellationToken)
    {
        var userId = message.From!.Id;
        var subject = message.Text?.Trim();

        if (string.IsNullOrWhiteSpace(subject) || subject.Length < 5)
        {
            await botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: "❌ Тема звернення занадто коротка. Будь ласка, введіть щонайменше 5 символів.",
                cancellationToken: cancellationToken);
            return;
        }

        if (subject.Length > 200)
        {
            await botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: "❌ Тема звернення занадто довга. Максимум 200 символів.",
                cancellationToken: cancellationToken);
            return;
        }

        // Зберігаємо тему
        await _stateManager.SetDataAsync(userId, "appeal_subject", subject, cancellationToken);

        // Переходимо до наступного кроку
        await _stateManager.SetStateAsync(userId, UserConversationState.WaitingAppealMessage, cancellationToken);

        await botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: $"✅ Тема збережена: <b>{subject}</b>\n\n" +
                  "📝 Тепер опишіть вашу проблему детально.\n\n" +
                  "<i>Мінімум 10 символів, максимум 2000 символів.</i>",
            parseMode: ParseMode.Html,
            cancellationToken: cancellationToken);
    }

    private async Task HandleAppealMessageInputAsync(
        ITelegramBotClient botClient,
        Message message,
        CancellationToken cancellationToken)
    {
        var userId = message.From!.Id;
        var messageText = message.Text?.Trim();

        if (string.IsNullOrWhiteSpace(messageText) || messageText.Length < 10)
        {
            await botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: "❌ Повідомлення занадто коротке. Будь ласка, опишіть проблему детальніше (мінімум 10 символів).",
                cancellationToken: cancellationToken);
            return;
        }

        if (messageText.Length > 2000)
        {
            await botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: "❌ Повідомлення занадто довге. Максимум 2000 символів.",
                cancellationToken: cancellationToken);
            return;
        }

        // Отримуємо збережені дані
        var category = await _stateManager.GetDataAsync<AppealCategory>(userId, "appeal_category", cancellationToken);
        var subject = await _stateManager.GetDataAsync<string>(userId, "appeal_subject", cancellationToken);

        // AppealCategory є enum, тому перевіряємо subject
        if (string.IsNullOrEmpty(subject))
        {
            await botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: "❌ Помилка: дані звернення втрачено. Почніть спочатку /appeal",
                replyMarkup: KeyboardFactory.GetBackToMainMenuKeyboard(),
                cancellationToken: cancellationToken);

            await _stateManager.ClearStateAsync(userId, cancellationToken);
            await _stateManager.ClearAllDataAsync(userId, cancellationToken);
            return;
        }

        // Створюємо звернення через MediatR
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

            var command = new CreateAppealCommand
            {
                StudentId = userId,
                StudentName = $"{message.From.FirstName} {message.From.LastName}".Trim(),
                Category = category,
                Subject = subject,
                Message = messageText
            };

            var result = await mediator.Send(command, cancellationToken);

            if (result.IsSuccess)
            {
                await botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: $"✅ <b>Звернення успішно створено!</b>\n\n" +
                          $"📋 Номер: #{result.Value!.Id}\n" +
                          $"📂 Категорія: {result.Value.Category}\n" +
                          $"📌 Тема: {result.Value.Subject}\n" +
                          $"📊 Статус: {result.Value.Status}\n\n" +
                          $"Ми розглянемо ваше звернення найближчим часом.\n" +
                          $"Ви отримаєте сповіщення про зміну статусу.",
                    parseMode: ParseMode.Html,
                    replyMarkup: KeyboardFactory.GetBackToMainMenuKeyboard(),
                    cancellationToken: cancellationToken);

                // Очищаємо стан
                await _stateManager.ClearStateAsync(userId, cancellationToken);
                await _stateManager.ClearAllDataAsync(userId, cancellationToken);
            }
            else
            {
                await botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: $"❌ Помилка при створенні звернення:\n{result.Error}\n\nСпробуйте ще раз /appeal",
                    parseMode: ParseMode.Html,
                    replyMarkup: KeyboardFactory.GetBackToMainMenuKeyboard(),
                    cancellationToken: cancellationToken);

                await _stateManager.ClearStateAsync(userId, cancellationToken);
                await _stateManager.ClearAllDataAsync(userId, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Помилка при створенні звернення для користувача {UserId}", userId);

            await botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: "❌ Виникла технічна помилка. Спробуйте пізніше або зверніться до адміністратора.",
                replyMarkup: KeyboardFactory.GetBackToMainMenuKeyboard(),
                cancellationToken: cancellationToken);

            await _stateManager.ClearStateAsync(userId, cancellationToken);
            await _stateManager.ClearAllDataAsync(userId, cancellationToken);
        }
    }

    private async Task HandleEmailInputAsync(
        ITelegramBotClient botClient,
        Message message,
        CancellationToken cancellationToken)
    {
        var userId = message.From!.Id;
        var email = message.Text?.Trim();

        // Basic email validation
        if (string.IsNullOrWhiteSpace(email) || !email.Contains("@") || email.Length < 5)
        {
            await botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: "❌ Невірний формат email. Будь ласка, введіть коректну адресу.\n\n" +
                      "<i>Наприклад: student@vnmu.edu.ua</i>",
                parseMode: ParseMode.Html,
                cancellationToken: cancellationToken);
            return;
        }

        if (email.Length > 100)
        {
            await botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: "❌ Email занадто довгий. Максимум 100 символів.",
                cancellationToken: cancellationToken);
            return;
        }

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

            // Send verification email via MediatR
            var command = new SendVerificationEmailCommand
            {
                TelegramId = userId,
                Email = email
            };

            var result = await mediator.Send(command, cancellationToken);

            if (result.IsSuccess)
            {
                // Change state to waiting for verification code
                await _stateManager.SetStateAsync(userId, UserConversationState.WaitingVerificationCode, cancellationToken);

                await botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: $"✅ <b>Код верифікації відправлено!</b>\n\n" +
                          $"📧 На адресу <code>{email}</code> надіслано лист з 6-значним кодом.\n\n" +
                          $"⏰ Код дійсний протягом 15 хвилин.\n\n" +
                          $"Введіть код з листа:",
                    parseMode: ParseMode.Html,
                    replyMarkup: new InlineKeyboardMarkup(InlineKeyboardButton.WithCallbackData("❌ Скасувати", "profile_view")),
                    cancellationToken: cancellationToken);
            }
            else
            {
                await botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: $"❌ {result.Error}\n\nСпробуйте ще раз або скасуйте операцію /cancel",
                    replyMarkup: new InlineKeyboardMarkup(InlineKeyboardButton.WithCallbackData("❌ Скасувати", "profile_view")),
                    cancellationToken: cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Помилка при відправці verification email для користувача {UserId}", userId);

            await botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: "❌ Виникла технічна помилка при відправці коду. Спробуйте пізніше.",
                replyMarkup: KeyboardFactory.GetBackToMainMenuKeyboard(),
                cancellationToken: cancellationToken);

            await _stateManager.ClearStateAsync(userId, cancellationToken);
        }
    }

    private async Task HandleVerificationCodeInputAsync(
        ITelegramBotClient botClient,
        Message message,
        CancellationToken cancellationToken)
    {
        var userId = message.From!.Id;
        var code = message.Text?.Trim();

        // Validate code format (6 digits)
        if (string.IsNullOrWhiteSpace(code) || code.Length != 6 || !code.All(char.IsDigit))
        {
            await botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: "❌ Невірний формат коду. Будь ласка, введіть 6 цифр.\n\n" +
                      "<i>Перевірте код у листі та спробуйте ще раз.</i>",
                parseMode: ParseMode.Html,
                cancellationToken: cancellationToken);
            return;
        }

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

            // Verify email code via MediatR
            var command = new VerifyEmailCommand
            {
                TelegramId = userId,
                Code = code
            };

            var result = await mediator.Send(command, cancellationToken);

            if (result.IsSuccess && result.Value)
            {
                await botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: "✅ <b>Email успішно підтверджено!</b>\n\n" +
                          "🎉 Тепер ви можете отримувати сповіщення на вашу електронну пошту.\n\n" +
                          "Дякуємо за верифікацію!",
                    parseMode: ParseMode.Html,
                    replyMarkup: KeyboardFactory.GetBackToMainMenuKeyboard(),
                    cancellationToken: cancellationToken);

                // Clear state
                await _stateManager.ClearStateAsync(userId, cancellationToken);
            }
            else
            {
                await botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: $"❌ {result.Error}\n\n" +
                          "Перевірте код та спробуйте ще раз, або запитайте новий код повторно.",
                    replyMarkup: new InlineKeyboardMarkup(InlineKeyboardButton.WithCallbackData("❌ Скасувати", "profile_view")),
                    cancellationToken: cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Помилка при верифікації email для користувача {UserId}", userId);

            await botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: "❌ Виникла технічна помилка при перевірці коду. Спробуйте пізніше.",
                replyMarkup: KeyboardFactory.GetBackToMainMenuKeyboard(),
                cancellationToken: cancellationToken);

            await _stateManager.ClearStateAsync(userId, cancellationToken);
        }
    }

    private async Task HandleCallbackQueryAsync(
        ITelegramBotClient botClient,
        CallbackQuery callbackQuery,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Отримано callback від {UserId}: {Data}",
            callbackQuery.From.Id,
            callbackQuery.Data);

        if (callbackQuery.Data == null || callbackQuery.Message == null)
            return;

        var data = callbackQuery.Data;

        // Обробка різних callback'ів
        try
        {
            if (data == "back_to_main")
            {
                // Скасовуємо будь-який активний процес
                var userId = callbackQuery.From.Id;
                await _stateManager.ClearStateAsync(userId, cancellationToken);
                await _stateManager.ClearAllDataAsync(userId, cancellationToken);

                // Check if user is admin for menu display
                using var backScope = _scopeFactory.CreateScope();
                var userRepoForBack = backScope.ServiceProvider.GetRequiredService<IUserRepository>();
                var userForBack = await userRepoForBack.GetByTelegramIdAsync(callbackQuery.From.Id, cancellationToken);
                var isAdminBack = userForBack?.Role == UserRole.Admin;

                await botClient.EditMessageTextAsync(
                    chatId: callbackQuery.Message.Chat.Id,
                    messageId: callbackQuery.Message.MessageId,
                    text: "🎓 <b>Головне меню</b>\n\nОберіть дію:",
                    parseMode: ParseMode.Html,
                    replyMarkup: KeyboardFactory.GetMainMenuKeyboard(isAdminBack),
                    cancellationToken: cancellationToken);
            }
            else if (data == "appeal_create")
            {
                await botClient.EditMessageTextAsync(
                    chatId: callbackQuery.Message.Chat.Id,
                    messageId: callbackQuery.Message.MessageId,
                    text: "📝 <b>Створення звернення</b>\n\nОберіть категорію:",
                    parseMode: ParseMode.Html,
                    replyMarkup: KeyboardFactory.GetAppealCategoriesKeyboard(),
                    cancellationToken: cancellationToken);
            }
            else if (data.StartsWith("appeal_cat_"))
            {
                await HandleAppealCategorySelection(botClient, callbackQuery, data, cancellationToken);
            }
            else if (data.StartsWith("appeal_view_"))
            {
                await HandleAppealViewCallback(botClient, callbackQuery, data, cancellationToken);
            }
            else if (data == "appeal_list")
            {
                await HandleMyAppealsCallback(botClient, callbackQuery, cancellationToken);
            }
            else if (data == "news_list")
            {
                await HandleNewsListCallback(botClient, callbackQuery, cancellationToken);
            }
            else if (data == "events_list")
            {
                await HandleEventsListCallback(botClient, callbackQuery, cancellationToken);
            }
            else if (data == "partners_list")
            {
                await HandlePartnersListCallback(botClient, callbackQuery, cancellationToken);
            }
            else if (data == "contacts_list")
            {
                await HandleContactsListCallback(botClient, callbackQuery, cancellationToken);
            }
            else if (data == "profile_view")
            {
                await HandleProfileViewCallback(botClient, callbackQuery, cancellationToken);
            }
            else if (data == "profile_edit_email")
            {
                await HandleProfileEditEmailCallback(botClient, callbackQuery, cancellationToken);
            }
            else if (data == "help")
            {
                await botClient.EditMessageTextAsync(
                    chatId: callbackQuery.Message.Chat.Id,
                    messageId: callbackQuery.Message.MessageId,
                    text: "ℹ️ <b>Допомога</b>\n\n" +
                          "Використовуйте меню для навігації.\n" +
                          "Команди: /start, /help, /appeal, /contacts",
                    parseMode: ParseMode.Html,
                    replyMarkup: KeyboardFactory.GetBackToMainMenuKeyboard(),
                    cancellationToken: cancellationToken);
            }
            // ==================== ADMIN CALLBACKS ====================
            else if (data == "admin_panel")
            {
                await HandleAdminPanelCallback(botClient, callbackQuery, cancellationToken);
            }
            else if (data.StartsWith("admin_appeals_"))
            {
                await HandleAdminAppealsListCallback(botClient, callbackQuery, cancellationToken);
            }
            else if (data.StartsWith("admin_view_"))
            {
                await HandleAdminAppealViewCallback(botClient, callbackQuery, cancellationToken);
            }
            else if (data.StartsWith("admin_assign_me_"))
            {
                await HandleAdminAssignToMeCallback(botClient, callbackQuery, cancellationToken);
            }
            else if (data.StartsWith("admin_unassign_"))
            {
                await HandleAdminUnassignCallback(botClient, callbackQuery, cancellationToken);
            }
            else if (data.StartsWith("admin_priority_"))
            {
                await HandleAdminPriorityMenuCallback(botClient, callbackQuery, cancellationToken);
            }
            else if (data.StartsWith("admin_set_priority_"))
            {
                await HandleAdminSetPriorityCallback(botClient, callbackQuery, cancellationToken);
            }
            else if (data.StartsWith("admin_close_"))
            {
                await HandleAdminCloseAppealCallback(botClient, callbackQuery, cancellationToken);
            }

            await botClient.AnswerCallbackQueryAsync(
                callbackQueryId: callbackQuery.Id,
                cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Помилка при обробці callback: {Data}", data);
            await botClient.AnswerCallbackQueryAsync(
                callbackQueryId: callbackQuery.Id,
                text: "❌ Виникла помилка. Спробуйте ще раз.",
                showAlert: true,
                cancellationToken: cancellationToken);
        }
    }

    private async Task HandleAppealCategorySelection(
        ITelegramBotClient botClient,
        CallbackQuery callbackQuery,
        string data,
        CancellationToken cancellationToken)
    {
        var userId = callbackQuery.From.Id;
        var categoryId = int.Parse(data.Replace("appeal_cat_", ""));
        
        var categoryNames = new Dictionary<int, string>
        {
            { 1, "💰 Стипендія" },
            { 2, "🏠 Гуртожиток" },
            { 3, "🎉 Заходи" },
            { 4, "💡 Пропозиція" },
            { 5, "⚠️ Скарга" },
            { 6, "📝 Інше" }
        };

        var categoryName = categoryNames.GetValueOrDefault(categoryId, "Невідома категорія");
        var category = (AppealCategory)categoryId;

        // Зберігаємо обрану категорію
        await _stateManager.SetDataAsync(userId, "appeal_category", category, cancellationToken);

        // Встановлюємо стан очікування теми
        await _stateManager.SetStateAsync(userId, UserConversationState.WaitingAppealSubject, cancellationToken);

        await botClient.EditMessageTextAsync(
            chatId: callbackQuery.Message!.Chat.Id,
            messageId: callbackQuery.Message.MessageId,
            text: $"📝 <b>Створення звернення</b>\n\n" +
                  $"✅ Категорія: {categoryName}\n\n" +
                  $"Напишіть <b>тему</b> вашого звернення:\n\n" +
                  $"<i>Мінімум 5 символів, максимум 200 символів</i>",
            parseMode: ParseMode.Html,
            replyMarkup: KeyboardFactory.GetBackToMainMenuKeyboard(),
            cancellationToken: cancellationToken);
    }

    private async Task HandleMyAppealsCallback(
        ITelegramBotClient botClient,
        CallbackQuery callbackQuery,
        CancellationToken cancellationToken)
    {
        var userId = callbackQuery.From.Id;

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

            // Отримуємо звернення користувача
            var query = new GetUserAppealsQuery
            {
                UserId = userId,
                PageSize = 5,
                PageNumber = 1,
                OnlyActive = false // Показуємо всі звернення
            };

            var result = await mediator.Send(query, cancellationToken);

            if (!result.IsSuccess)
            {
                await botClient.EditMessageTextAsync(
                    chatId: callbackQuery.Message!.Chat.Id,
                    messageId: callbackQuery.Message.MessageId,
                    text: $"❌ Помилка: {result.Error}",
                    parseMode: ParseMode.Html,
                    replyMarkup: KeyboardFactory.GetBackToMainMenuKeyboard(),
                    cancellationToken: cancellationToken);
                return;
            }

            var appeals = result.Value ?? new List<StudentUnionBot.Application.Appeals.DTOs.AppealDto>();

            if (appeals.Count == 0)
            {
                await botClient.EditMessageTextAsync(
                    chatId: callbackQuery.Message!.Chat.Id,
                    messageId: callbackQuery.Message.MessageId,
                    text: "📋 <b>Мої звернення</b>\n\n" +
                          "У вас поки немає звернень.\n\n" +
                          "Натисніть \"📝 Створити звернення\" щоб подати нове звернення.",
                    parseMode: ParseMode.Html,
                    replyMarkup: KeyboardFactory.GetBackToMainMenuKeyboard(),
                    cancellationToken: cancellationToken);
                return;
            }

            // Формуємо текст зі списком звернень
            var text = "📋 <b>Мої звернення</b>\n\n";

            foreach (var appeal in appeals)
            {
                var statusEmoji = appeal.Status.GetEmoji();
                var categoryEmoji = appeal.Category.GetEmoji();
                var daysAgo = (DateTime.UtcNow - appeal.CreatedAt).Days;
                var timeAgo = daysAgo == 0 ? "сьогодні" : 
                              daysAgo == 1 ? "вчора" : 
                              $"{daysAgo} дн. тому";

                text += $"━━━━━━━━━━━━━━\n";
                text += $"<b>#{appeal.Id}</b> {categoryEmoji} {appeal.CategoryName}\n";
                text += $"📌 <b>{appeal.Subject}</b>\n";
                text += $"{statusEmoji} {appeal.StatusName}\n";
                text += $"📅 {appeal.CreatedAt:dd.MM.yyyy} ({timeAgo})\n";

                if (appeal.MessageCount > 0)
                {
                    text += $"💬 Повідомлень: {appeal.MessageCount}\n";
                }

                if (appeal.ClosedAt.HasValue)
                {
                    text += $"✅ Закрито: {appeal.ClosedAt.Value:dd.MM.yyyy HH:mm}\n";
                }

                text += "\n";
            }

            // Створюємо клавіатуру з кнопками для кожного звернення
            var buttons = new List<List<InlineKeyboardButton>>();

            foreach (var appeal in appeals)
            {
                buttons.Add(new List<InlineKeyboardButton>
                {
                    InlineKeyboardButton.WithCallbackData(
                        $"#{appeal.Id} - {appeal.Subject}",
                        $"appeal_view_{appeal.Id}"
                    )
                });
            }

            // Кнопка "Назад"
            buttons.Add(new List<InlineKeyboardButton>
            {
                InlineKeyboardButton.WithCallbackData("🔙 Назад до меню", "back_to_main")
            });

            var keyboard = new InlineKeyboardMarkup(buttons);

            await botClient.EditMessageTextAsync(
                chatId: callbackQuery.Message!.Chat.Id,
                messageId: callbackQuery.Message.MessageId,
                text: text,
                parseMode: ParseMode.Html,
                replyMarkup: keyboard,
                cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Помилка при отриманні звернень користувача {UserId}", userId);

            await botClient.EditMessageTextAsync(
                chatId: callbackQuery.Message!.Chat.Id,
                messageId: callbackQuery.Message.MessageId,
                text: "❌ Виникла помилка при завантаженні звернень. Спробуйте пізніше.",
                parseMode: ParseMode.Html,
                replyMarkup: KeyboardFactory.GetBackToMainMenuKeyboard(),
                cancellationToken: cancellationToken);
        }
    }

    private async Task HandleAppealViewCallback(
        ITelegramBotClient botClient,
        CallbackQuery callbackQuery,
        string callbackData,
        CancellationToken cancellationToken)
    {
        var userId = callbackQuery.From.Id;
        
        // Парсимо ID звернення з callback data (appeal_view_123)
        var appealIdStr = callbackData.Replace("appeal_view_", "");
        if (!int.TryParse(appealIdStr, out var appealId))
        {
            await botClient.AnswerCallbackQueryAsync(
                callbackQuery.Id,
                "❌ Невірний формат звернення",
                showAlert: true,
                cancellationToken: cancellationToken);
            return;
        }

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

            // Отримуємо деталі звернення
            var query = new StudentUnionBot.Application.Appeals.Queries.GetAppealById.GetAppealByIdQuery
            {
                AppealId = appealId,
                RequestUserId = userId
            };

            var result = await mediator.Send(query, cancellationToken);

            if (!result.IsSuccess)
            {
                await botClient.EditMessageTextAsync(
                    chatId: callbackQuery.Message!.Chat.Id,
                    messageId: callbackQuery.Message.MessageId,
                    text: $"❌ {result.Error}",
                    parseMode: ParseMode.Html,
                    replyMarkup: new InlineKeyboardMarkup(new[]
                    {
                        InlineKeyboardButton.WithCallbackData("🔙 До списку звернень", "appeal_list")
                    }),
                    cancellationToken: cancellationToken);
                return;
            }

            var appeal = result.Value!;

            // Формуємо текст з деталями звернення
            var statusEmoji = appeal.Status.GetEmoji();
            var categoryEmoji = appeal.Category.GetEmoji();
            
            var text = $"📋 <b>Звернення #{appeal.Id}</b>\n\n";
            text += $"{categoryEmoji} <b>Категорія:</b> {appeal.CategoryName}\n";
            text += $"{statusEmoji} <b>Статус:</b> {appeal.StatusName}\n";
            text += $"📊 <b>Пріоритет:</b> {appeal.PriorityName}\n";
            text += $"📅 <b>Створено:</b> {appeal.CreatedAt:dd.MM.yyyy HH:mm}\n";
            
            if (appeal.FirstResponseAt.HasValue)
            {
                text += $"⏱ <b>Перша відповідь:</b> {appeal.FirstResponseAt.Value:dd.MM.yyyy HH:mm}\n";
            }
            
            if (appeal.ClosedAt.HasValue)
            {
                text += $"✅ <b>Закрито:</b> {appeal.ClosedAt.Value:dd.MM.yyyy HH:mm}\n";
                if (!string.IsNullOrEmpty(appeal.ClosedReason))
                {
                    text += $"<b>Причина закриття:</b> {appeal.ClosedReason}\n";
                }
            }
            
            text += $"\n<b>Тема:</b>\n{appeal.Subject}\n\n";
            text += $"<b>Повідомлення:</b>\n{appeal.Message}\n";

            // Додаємо історію повідомлень, якщо вони є
            if (appeal.Messages.Count > 0)
            {
                text += "\n━━━━━━━━━━━━━━\n";
                text += "<b>📬 Історія повідомлень:</b>\n\n";

                foreach (var msg in appeal.Messages)
                {
                    var senderIcon = msg.IsFromAdmin ? "👨‍💼" : "👤";
                    var senderLabel = msg.IsFromAdmin ? "Адміністратор" : "Ви";
                    
                    text += $"{senderIcon} <b>{senderLabel}</b> ({msg.SentAt:dd.MM HH:mm})\n";
                    text += $"{msg.Text}\n\n";
                }
            }

            // Створюємо клавіатуру
            var buttons = new List<List<InlineKeyboardButton>>();

            // Якщо звернення активне - можна додати повідомлення
            if (appeal.Status != AppealStatus.Closed)
            {
                buttons.Add(new List<InlineKeyboardButton>
                {
                    InlineKeyboardButton.WithCallbackData(
                        "💬 Додати повідомлення",
                        $"appeal_add_msg_{appeal.Id}"
                    )
                });
            }

            // Кнопка назад
            buttons.Add(new List<InlineKeyboardButton>
            {
                InlineKeyboardButton.WithCallbackData("🔙 До списку", "appeal_list")
            });

            var keyboard = new InlineKeyboardMarkup(buttons);

            await botClient.EditMessageTextAsync(
                chatId: callbackQuery.Message!.Chat.Id,
                messageId: callbackQuery.Message.MessageId,
                text: text,
                parseMode: ParseMode.Html,
                replyMarkup: keyboard,
                cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Помилка при перегляді звернення {AppealId}", appealId);

            await botClient.EditMessageTextAsync(
                chatId: callbackQuery.Message!.Chat.Id,
                messageId: callbackQuery.Message.MessageId,
                text: "❌ Виникла помилка при завантаженні звернення",
                parseMode: ParseMode.Html,
                replyMarkup: new InlineKeyboardMarkup(new[]
                {
                    InlineKeyboardButton.WithCallbackData("🔙 До списку", "appeal_list")
                }),
                cancellationToken: cancellationToken);
        }
    }

    private async Task HandleProfileViewCallback(
        ITelegramBotClient botClient,
        CallbackQuery callbackQuery,
        CancellationToken cancellationToken)
    {
        var userId = callbackQuery.From.Id;
        
        using var scope = _scopeFactory.CreateScope();
        var userRepo = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        var user = await userRepo.GetByTelegramIdAsync(userId, cancellationToken);

        if (user == null)
        {
            await botClient.AnswerCallbackQueryAsync(
                callbackQuery.Id,
                "❌ Помилка: користувач не знайдений",
                showAlert: true,
                cancellationToken: cancellationToken);
            return;
        }

        var username = user.Username ?? "немає";
        var fullName = user.FullName ?? $"{callbackQuery.From.FirstName} {callbackQuery.From.LastName}".Trim();
        var email = user.Email ?? "<i>не вказано</i>";
        var emailStatus = user.IsEmailVerified ? "✅ Підтверджено" : "❌ Не підтверджено";

        var profileText = "👤 <b>Мій профіль</b>\n\n" +
                         $"<b>Ім'я:</b> {fullName}\n" +
                         $"<b>Username:</b> @{username}\n" +
                         $"<b>Email:</b> {email}\n" +
                         $"<b>Статус email:</b> {emailStatus}\n" +
                         $"<b>ID:</b> <code>{userId}</code>";

        // Кнопки для редагування
        var keyboard = new InlineKeyboardMarkup(new[]
        {
            new[]
            {
                InlineKeyboardButton.WithCallbackData("📧 Змінити email", "profile_edit_email")
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData("🔙 Головне меню", "back_to_main")
            }
        });

        await botClient.EditMessageTextAsync(
            chatId: callbackQuery.Message!.Chat.Id,
            messageId: callbackQuery.Message.MessageId,
            text: profileText,
            parseMode: ParseMode.Html,
            replyMarkup: keyboard,
            cancellationToken: cancellationToken);
    }

    private async Task HandleProfileEditEmailCallback(
        ITelegramBotClient botClient,
        CallbackQuery callbackQuery,
        CancellationToken cancellationToken)
    {
        var userId = callbackQuery.From.Id;

        // Set state to waiting for email input
        await _stateManager.SetStateAsync(userId, UserConversationState.WaitingEmailInput, cancellationToken);

        await botClient.EditMessageTextAsync(
            chatId: callbackQuery.Message!.Chat.Id,
            messageId: callbackQuery.Message.MessageId,
            text: "📧 <b>Зміна email</b>\n\n" +
                  "Введіть вашу корпоративну email адресу (бажано університетську):\n\n" +
                  "<i>Наприклад: student@vnmu.edu.ua</i>\n\n" +
                  "Надішліть /cancel щоб скасувати",
            parseMode: ParseMode.Html,
            replyMarkup: new InlineKeyboardMarkup(InlineKeyboardButton.WithCallbackData("❌ Скасувати", "profile_view")),
            cancellationToken: cancellationToken);
    }

    private async Task HandleNewsListCallback(
        ITelegramBotClient botClient,
        CallbackQuery callbackQuery,
        CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            
            // Отримуємо новини через MediatR
            var query = new GetPublishedNewsQuery
            {
                PageNumber = 1,
                PageSize = 5
            };

            var result = await mediator.Send(query, cancellationToken);

            if (!result.IsSuccess || result.Value == null)
            {
                await botClient.EditMessageTextAsync(
                    chatId: callbackQuery.Message!.Chat.Id,
                    messageId: callbackQuery.Message.MessageId,
                    text: "📰 <b>Новини</b>\n\n❌ Не вдалося завантажити новини. Спробуйте пізніше.",
                    parseMode: ParseMode.Html,
                    replyMarkup: KeyboardFactory.GetBackToMainMenuKeyboard(),
                    cancellationToken: cancellationToken);
                return;
            }

            var newsList = result.Value;
            if (newsList.Items.Count == 0)
            {
                await botClient.EditMessageTextAsync(
                    chatId: callbackQuery.Message!.Chat.Id,
                    messageId: callbackQuery.Message.MessageId,
                    text: "📰 <b>Новини</b>\n\n📭 Поки що немає опублікованих новин.",
                    parseMode: ParseMode.Html,
                    replyMarkup: KeyboardFactory.GetBackToMainMenuKeyboard(),
                    cancellationToken: cancellationToken);
                return;
            }

            // Формуємо текст з новинами
            var newsText = "📰 <b>Останні новини</b>\n\n";
            
            foreach (var news in newsList.Items.Take(5))
            {
                var pinnedMark = news.IsPinned ? "📌 " : "";
                newsText += $"{pinnedMark}{news.CategoryEmoji} <b>{news.Title}</b>\n";
                
                if (!string.IsNullOrEmpty(news.Summary))
                {
                    newsText += $"<i>{news.Summary}</i>\n";
                }
                else
                {
                    var preview = news.Content.Length > 100 
                        ? news.Content.Substring(0, 100) + "..." 
                        : news.Content;
                    newsText += $"{preview}\n";
                }
                
                newsText += $"📅 {news.CreatedAt:dd.MM.yyyy HH:mm}\n\n";
            }

            if (newsList.TotalCount > 5)
            {
                newsText += $"<i>Показано {newsList.Items.Count} з {newsList.TotalCount} новин</i>";
            }

            await botClient.EditMessageTextAsync(
                chatId: callbackQuery.Message!.Chat.Id,
                messageId: callbackQuery.Message.MessageId,
                text: newsText,
                parseMode: ParseMode.Html,
                replyMarkup: KeyboardFactory.GetBackToMainMenuKeyboard(),
                cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Помилка при отриманні новин для користувача {UserId}", callbackQuery.From.Id);
            
            await botClient.EditMessageTextAsync(
                chatId: callbackQuery.Message!.Chat.Id,
                messageId: callbackQuery.Message.MessageId,
                text: "📰 <b>Новини</b>\n\n❌ Виникла помилка при завантаженні новин.",
                parseMode: ParseMode.Html,
                replyMarkup: KeyboardFactory.GetBackToMainMenuKeyboard(),
                cancellationToken: cancellationToken);
        }
    }

    private async Task HandleEventsListCallback(
        ITelegramBotClient botClient,
        CallbackQuery callbackQuery,
        CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            
            // Отримуємо майбутні події через MediatR
            var query = new GetUpcomingEventsQuery
            {
                PageNumber = 1,
                PageSize = 5
            };

            var result = await mediator.Send(query, cancellationToken);

            if (!result.IsSuccess || result.Value == null)
            {
                await botClient.EditMessageTextAsync(
                    chatId: callbackQuery.Message!.Chat.Id,
                    messageId: callbackQuery.Message.MessageId,
                    text: "🎉 <b>Заходи</b>\n\n❌ Не вдалося завантажити події. Спробуйте пізніше.",
                    parseMode: ParseMode.Html,
                    replyMarkup: KeyboardFactory.GetBackToMainMenuKeyboard(),
                    cancellationToken: cancellationToken);
                return;
            }

            var eventsList = result.Value;
            if (eventsList.Items.Count == 0)
            {
                await botClient.EditMessageTextAsync(
                    chatId: callbackQuery.Message!.Chat.Id,
                    messageId: callbackQuery.Message.MessageId,
                    text: "🎉 <b>Заходи</b>\n\n📭 Наразі немає запланованих подій.\n\n" +
                          "<i>Слідкуйте за оновленнями!</i>",
                    parseMode: ParseMode.Html,
                    replyMarkup: KeyboardFactory.GetBackToMainMenuKeyboard(),
                    cancellationToken: cancellationToken);
                return;
            }

            // Формуємо текст з подіями
            var eventsText = "🎉 <b>Майбутні заходи</b>\n\n";
            
            foreach (var ev in eventsList.Items.Take(5))
            {
                var featuredMark = ev.IsFeatured ? "⭐ " : "";
                eventsText += $"{featuredMark}{ev.TypeEmoji} <b>{ev.Title}</b>\n";
                
                eventsText += $"📅 {ev.StartDate:dd.MM.yyyy HH:mm}";
                if (ev.EndDate.HasValue)
                {
                    eventsText += $" - {ev.EndDate.Value:HH:mm}";
                }
                eventsText += "\n";
                
                if (!string.IsNullOrEmpty(ev.Location))
                {
                    eventsText += $"📍 {ev.Location}\n";
                }
                
                if (ev.RequiresRegistration)
                {
                    var spotsLeft = ev.MaxParticipants.HasValue 
                        ? $"{ev.MaxParticipants.Value - ev.CurrentParticipants}" 
                        : "∞";
                    eventsText += $"👥 Реєстрація: {ev.CurrentParticipants}/{(ev.MaxParticipants?.ToString() ?? "∞")} (вільно: {spotsLeft})\n";
                    
                    if (ev.RegistrationDeadline.HasValue)
                    {
                        eventsText += $"⏰ Дедлайн: {ev.RegistrationDeadline.Value:dd.MM.yyyy HH:mm}\n";
                    }
                }
                
                eventsText += "\n";
            }

            if (eventsList.TotalCount > 5)
            {
                eventsText += $"<i>Показано {eventsList.Items.Count} з {eventsList.TotalCount} подій</i>";
            }

            await botClient.EditMessageTextAsync(
                chatId: callbackQuery.Message!.Chat.Id,
                messageId: callbackQuery.Message.MessageId,
                text: eventsText,
                parseMode: ParseMode.Html,
                replyMarkup: KeyboardFactory.GetBackToMainMenuKeyboard(),
                cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Помилка при отриманні подій для користувача {UserId}", callbackQuery.From.Id);
            
            await botClient.EditMessageTextAsync(
                chatId: callbackQuery.Message!.Chat.Id,
                messageId: callbackQuery.Message.MessageId,
                text: "🎉 <b>Заходи</b>\n\n❌ Виникла помилка при завантаженні подій.",
                parseMode: ParseMode.Html,
                replyMarkup: KeyboardFactory.GetBackToMainMenuKeyboard(),
                cancellationToken: cancellationToken);
        }
    }

    private async Task HandlePartnersListCallback(
        ITelegramBotClient botClient,
        CallbackQuery callbackQuery,
        CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            
            // Отримуємо партнерів через MediatR
            var query = new GetActivePartnersQuery();
            var result = await mediator.Send(query, cancellationToken);

            if (!result.IsSuccess || result.Value == null)
            {
                await botClient.EditMessageTextAsync(
                    chatId: callbackQuery.Message!.Chat.Id,
                    messageId: callbackQuery.Message.MessageId,
                    text: "🤝 <b>Партнери</b>\n\n❌ Не вдалося завантажити партнерів. Спробуйте пізніше.",
                    parseMode: ParseMode.Html,
                    replyMarkup: KeyboardFactory.GetBackToMainMenuKeyboard(),
                    cancellationToken: cancellationToken);
                return;
            }

            var partnersList = result.Value;
            if (partnersList.Items.Count == 0)
            {
                await botClient.EditMessageTextAsync(
                    chatId: callbackQuery.Message!.Chat.Id,
                    messageId: callbackQuery.Message.MessageId,
                    text: "🤝 <b>Партнери</b>\n\n📭 Наразі немає активних партнерів.\n\n" +
                          "<i>Ми працюємо над новими партнерствами!</i>",
                    parseMode: ParseMode.Html,
                    replyMarkup: KeyboardFactory.GetBackToMainMenuKeyboard(),
                    cancellationToken: cancellationToken);
                return;
            }

            // Формуємо текст з партнерами
            var partnersText = "🤝 <b>Наші партнери</b>\n\n";
            partnersText += "<i>Пропонуємо знижки та привілеї для членів профспілки:</i>\n\n";
            
            foreach (var partner in partnersList.Items)
            {
                var featuredMark = partner.IsFeatured ? "⭐ " : "";
                partnersText += $"{featuredMark}{partner.TypeEmoji} <b>{partner.Name}</b>\n";
                
                if (!string.IsNullOrEmpty(partner.Description))
                {
                    partnersText += $"{partner.Description}\n";
                }
                
                if (!string.IsNullOrEmpty(partner.DiscountInfo))
                {
                    partnersText += $"💰 <b>Знижка:</b> {partner.DiscountInfo}\n";
                }
                
                if (!string.IsNullOrEmpty(partner.Address))
                {
                    partnersText += $"📍 {partner.Address}\n";
                }
                
                if (!string.IsNullOrEmpty(partner.PhoneNumber))
                {
                    partnersText += $"📞 {partner.PhoneNumber}\n";
                }
                
                partnersText += "\n";
            }

            partnersText += $"<i>Всього партнерів: {partnersList.TotalCount}</i>";

            await botClient.EditMessageTextAsync(
                chatId: callbackQuery.Message!.Chat.Id,
                messageId: callbackQuery.Message.MessageId,
                text: partnersText,
                parseMode: ParseMode.Html,
                replyMarkup: KeyboardFactory.GetBackToMainMenuKeyboard(),
                cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Помилка при отриманні партнерів для користувача {UserId}", callbackQuery.From.Id);
            
            await botClient.EditMessageTextAsync(
                chatId: callbackQuery.Message!.Chat.Id,
                messageId: callbackQuery.Message.MessageId,
                text: "🤝 <b>Партнери</b>\n\n❌ Виникла помилка при завантаженні партнерів.",
                parseMode: ParseMode.Html,
                replyMarkup: KeyboardFactory.GetBackToMainMenuKeyboard(),
                cancellationToken: cancellationToken);
        }
    }

    private async Task HandleContactsListCallback(
        ITelegramBotClient botClient,
        CallbackQuery callbackQuery,
        CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            
            // Отримуємо контакти через MediatR
            var query = new GetAllContactsQuery();
            var result = await mediator.Send(query, cancellationToken);

            if (!result.IsSuccess || result.Value == null)
            {
                await botClient.EditMessageTextAsync(
                    chatId: callbackQuery.Message!.Chat.Id,
                    messageId: callbackQuery.Message.MessageId,
                    text: "📞 <b>Контакти</b>\n\n❌ Не вдалося завантажити контакти. Спробуйте пізніше.",
                    parseMode: ParseMode.Html,
                    replyMarkup: KeyboardFactory.GetBackToMainMenuKeyboard(),
                    cancellationToken: cancellationToken);
                return;
            }

            var contactsList = result.Value;
            if (contactsList.Items.Count == 0)
            {
                await botClient.EditMessageTextAsync(
                    chatId: callbackQuery.Message!.Chat.Id,
                    messageId: callbackQuery.Message.MessageId,
                    text: "📞 <b>Контактна інформація</b>\n\n" +
                          "🏛 <b>Студентський профспілковий комітет</b>\n\n" +
                          "📧 Email: profkom@vnmu.edu.ua\n" +
                          "📱 Telegram: @vnmu_profkom\n" +
                          "📍 Адреса: вул. Пирогова, 56, Вінниця\n" +
                          "🕐 Години роботи: ПН-ПТ 9:00-17:00",
                    parseMode: ParseMode.Html,
                    replyMarkup: KeyboardFactory.GetBackToMainMenuKeyboard(),
                    cancellationToken: cancellationToken);
                return;
            }

            // Формуємо текст з контактами
            var contactsText = "📞 <b>Контактна інформація</b>\n\n";
            
            foreach (var contact in contactsList.Items)
            {
                contactsText += $"🏛 <b>{contact.Title}</b>\n";
                
                if (!string.IsNullOrEmpty(contact.Description))
                {
                    contactsText += $"<i>{contact.Description}</i>\n";
                }
                
                if (!string.IsNullOrEmpty(contact.PhoneNumber))
                {
                    contactsText += $"📞 {contact.PhoneNumber}\n";
                }
                
                if (!string.IsNullOrEmpty(contact.Email))
                {
                    contactsText += $"📧 {contact.Email}\n";
                }
                
                if (!string.IsNullOrEmpty(contact.TelegramUsername))
                {
                    contactsText += $"📱 @{contact.TelegramUsername}\n";
                }
                
                if (!string.IsNullOrEmpty(contact.Address))
                {
                    contactsText += $"📍 {contact.Address}\n";
                }
                
                if (!string.IsNullOrEmpty(contact.WorkingHours))
                {
                    contactsText += $"🕐 {contact.WorkingHours}\n";
                }
                
                contactsText += "\n";
            }

            await botClient.EditMessageTextAsync(
                chatId: callbackQuery.Message!.Chat.Id,
                messageId: callbackQuery.Message.MessageId,
                text: contactsText.TrimEnd(),
                parseMode: ParseMode.Html,
                replyMarkup: KeyboardFactory.GetBackToMainMenuKeyboard(),
                cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Помилка при отриманні контактів для користувача {UserId}", callbackQuery.From.Id);
            
            await botClient.EditMessageTextAsync(
                chatId: callbackQuery.Message!.Chat.Id,
                messageId: callbackQuery.Message.MessageId,
                text: "📞 <b>Контакти</b>\n\n❌ Виникла помилка при завантаженні контактів.",
                parseMode: ParseMode.Html,
                replyMarkup: KeyboardFactory.GetBackToMainMenuKeyboard(),
                cancellationToken: cancellationToken);
        }
    }

    #region Admin Panel Handlers

    private async Task HandleAdminPanelCallback(
        ITelegramBotClient botClient,
        CallbackQuery callbackQuery,
        CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var userRepo = scope.ServiceProvider.GetRequiredService<IUserRepository>();

        var user = await userRepo.GetByTelegramIdAsync(callbackQuery.From.Id, cancellationToken);
        if (user?.Role != Domain.Enums.UserRole.Admin)
        {
            await botClient.AnswerCallbackQueryAsync(
                callbackQuery.Id,
                "⛔ У вас немає прав адміністратора",
                showAlert: true,
                cancellationToken: cancellationToken);
            return;
        }

        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        
        // Get statistics
        var allAppealsResult = await mediator.Send(new GetAdminAppealsQuery
        {
            AdminId = user.TelegramId,
            PageNumber = 1,
            PageSize = 1
        }, cancellationToken);

        var newAppealsResult = await mediator.Send(new GetAdminAppealsQuery
        {
            AdminId = user.TelegramId,
            Status = Domain.Enums.AppealStatus.New,
            PageNumber = 1,
            PageSize = 1
        }, cancellationToken);

        var myAppealsResult = await mediator.Send(new GetAdminAppealsQuery
        {
            AdminId = user.TelegramId,
            OnlyMy = true,
            PageNumber = 1,
            PageSize = 1
        }, cancellationToken);

        var unassignedResult = await mediator.Send(new GetAdminAppealsQuery
        {
            AdminId = user.TelegramId,
            OnlyUnassigned = true,
            PageNumber = 1,
            PageSize = 1
        }, cancellationToken);

        var statsText = $"👨‍💼 <b>Адмін панель</b>\n\n" +
                       $"📊 <b>Статистика:</b>\n" +
                       $"📋 Всього звернень: {allAppealsResult.Value?.TotalCount ?? 0}\n" +
                       $"🆕 Нових: {newAppealsResult.Value?.TotalCount ?? 0}\n" +
                       $"👤 Моїх: {myAppealsResult.Value?.TotalCount ?? 0}\n" +
                       $"❓ Непризначених: {unassignedResult.Value?.TotalCount ?? 0}\n\n" +
                       $"Оберіть дію:";

        await botClient.EditMessageTextAsync(
            chatId: callbackQuery.Message!.Chat.Id,
            messageId: callbackQuery.Message.MessageId,
            text: statsText,
            parseMode: ParseMode.Html,
            replyMarkup: KeyboardFactory.GetAdminPanelKeyboard(),
            cancellationToken: cancellationToken);
    }

    private async Task HandleAdminAppealsListCallback(
        ITelegramBotClient botClient,
        CallbackQuery callbackQuery,
        CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var userRepo = scope.ServiceProvider.GetRequiredService<IUserRepository>();

        var user = await userRepo.GetByTelegramIdAsync(callbackQuery.From.Id, cancellationToken);
        if (user?.Role != Domain.Enums.UserRole.Admin)
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
            query.Status = Domain.Enums.AppealStatus.New;
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
                "📭 Звернень не знайдено",
                cancellationToken: cancellationToken);
            return;
        }

        var appealsText = "📋 <b>Звернення:</b>\n\n";
        foreach (var appeal in result.Value.Appeals)
        {
            var statusEmoji = appeal.Status switch
            {
                Domain.Enums.AppealStatus.New => "🆕",
                Domain.Enums.AppealStatus.InProgress => "⏳",
                Domain.Enums.AppealStatus.Closed => "✅",
                _ => "❓"
            };

            var priorityEmoji = appeal.Priority switch
            {
                Domain.Enums.AppealPriority.Low => "🟢",
                Domain.Enums.AppealPriority.Normal => "🟡",
                Domain.Enums.AppealPriority.High => "🟠",
                Domain.Enums.AppealPriority.Urgent => "🔴",
                _ => "⚪"
            };

            var assignedText = appeal.AssignedToAdminId.HasValue ? "👤" : "❓";

            appealsText += $"{statusEmoji} {priorityEmoji} #{appeal.Id} | {appeal.Category.GetDisplayName()}\n" +
                          $"<b>{appeal.Subject}</b>\n" +
                          $"{assignedText} Статус: {appeal.Status.GetDisplayName()}\n\n";
        }

        appealsText += $"Сторінка 1 з {Math.Ceiling((double)result.Value.TotalCount / 10)}";

        var keyboard = new InlineKeyboardMarkup(new[]
        {
            result.Value.Appeals.Select(a => 
                InlineKeyboardButton.WithCallbackData($"#{a.Id}", $"admin_view_{a.Id}")
            ).ToArray(),
            new[] { InlineKeyboardButton.WithCallbackData("« Назад до адмін панелі", "admin_panel") }
        });

        await botClient.EditMessageTextAsync(
            chatId: callbackQuery.Message!.Chat.Id,
            messageId: callbackQuery.Message.MessageId,
            text: appealsText,
            parseMode: ParseMode.Html,
            replyMarkup: keyboard,
            cancellationToken: cancellationToken);
    }

    private async Task HandleAdminAppealViewCallback(
        ITelegramBotClient botClient,
        CallbackQuery callbackQuery,
        CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var userRepo = scope.ServiceProvider.GetRequiredService<IUserRepository>();

        var user = await userRepo.GetByTelegramIdAsync(callbackQuery.From.Id, cancellationToken);
        if (user?.Role != Domain.Enums.UserRole.Admin)
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
                "❌ Невірний ID звернення",
                showAlert: true,
                cancellationToken: cancellationToken);
            return;
        }

        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var result = await mediator.Send(new GetAppealByIdQuery { AppealId = appealId, RequestUserId = user.TelegramId }, cancellationToken);

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
        var isClosed = appeal.Status == Domain.Enums.AppealStatus.Closed;

        var appealText = $"📋 <b>Звернення #{appeal.Id}</b>\n\n" +
                        $"📂 Категорія: {appeal.CategoryName}\n" +
                        $"📌 Тема: {appeal.Subject}\n" +
                        $"📝 Опис:\n{appeal.Message}\n\n" +
                        $"📊 Статус: {appeal.StatusName}\n" +
                        $"🎯 Пріоритет: {appeal.PriorityName}\n" +
                        $"👤 Призначено: {(appeal.AssignedToAdminId.HasValue ? $"Адмін #{appeal.AssignedToAdminId}" : "Непризначено")}\n" +
                        $"📅 Створено: {appeal.CreatedAt:dd.MM.yyyy HH:mm}\n";

        if (appeal.FirstResponseAt.HasValue)
        {
            appealText += $"⏱️ Перша відповідь: {appeal.FirstResponseAt:dd.MM.yyyy HH:mm}\n";
        }

        if (appeal.ClosedAt.HasValue)
        {
            appealText += $"✅ Закрито: {appeal.ClosedAt:dd.MM.yyyy HH:mm}\n";
            if (!string.IsNullOrEmpty(appeal.ClosedReason))
            {
                appealText += $"📝 Причина закриття: {appeal.ClosedReason}\n";
            }
        }

        if (appeal.Messages.Any())
        {
            appealText += $"\n💬 <b>Історія ({appeal.Messages.Count}):</b>\n";
            foreach (var msg in appeal.Messages.OrderBy(m => m.SentAt).Take(3))
            {
                var senderType = msg.IsFromAdmin ? "👨‍💼 Адмін" : "👤 Користувач";
                appealText += $"{senderType} ({msg.SentAt:dd.MM HH:mm}):\n{msg.Text}\n\n";
            }
            if (appeal.Messages.Count > 3)
            {
                appealText += $"... та ще {appeal.Messages.Count - 3} повідомлень\n";
            }
        }

        await botClient.EditMessageTextAsync(
            chatId: callbackQuery.Message!.Chat.Id,
            messageId: callbackQuery.Message.MessageId,
            text: appealText,
            parseMode: ParseMode.Html,
            replyMarkup: KeyboardFactory.GetAdminAppealActionsKeyboard(appealId, isAssignedToMe, isClosed),
            cancellationToken: cancellationToken);
    }

    private async Task HandleAdminAssignToMeCallback(
        ITelegramBotClient botClient,
        CallbackQuery callbackQuery,
        CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var userRepo = scope.ServiceProvider.GetRequiredService<IUserRepository>();

        var user = await userRepo.GetByTelegramIdAsync(callbackQuery.From.Id, cancellationToken);
        if (user?.Role != Domain.Enums.UserRole.Admin)
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
                "❌ Невірний ID звернення",
                showAlert: true,
                cancellationToken: cancellationToken);
            return;
        }

        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var result = await mediator.Send(new AssignAppealCommand
        {
            AppealId = appealId,
            RequestAdminId = user.TelegramId,
            AssignToAdminId = user.TelegramId
        }, cancellationToken);

        if (result.IsSuccess)
        {
            await botClient.AnswerCallbackQueryAsync(
                callbackQuery.Id,
                "✅ Звернення призначено вам",
                cancellationToken: cancellationToken);

            // Refresh the view
            await HandleAdminAppealViewCallback(botClient, new CallbackQuery
            {
                Id = callbackQuery.Id,
                From = callbackQuery.From,
                Message = callbackQuery.Message,
                Data = $"admin_view_{appealId}"
            }, cancellationToken);
        }
        else
        {
            await botClient.AnswerCallbackQueryAsync(
                callbackQuery.Id,
                $"❌ {result.Error}",
                showAlert: true,
                cancellationToken: cancellationToken);
        }
    }

    private async Task HandleAdminUnassignCallback(
        ITelegramBotClient botClient,
        CallbackQuery callbackQuery,
        CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var userRepo = scope.ServiceProvider.GetRequiredService<IUserRepository>();

        var user = await userRepo.GetByTelegramIdAsync(callbackQuery.From.Id, cancellationToken);
        if (user?.Role != Domain.Enums.UserRole.Admin)
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
                "❌ Невірний ID звернення",
                showAlert: true,
                cancellationToken: cancellationToken);
            return;
        }

        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var result = await mediator.Send(new AssignAppealCommand
        {
            AppealId = appealId,
            RequestAdminId = user.TelegramId,
            AssignToAdminId = null
        }, cancellationToken);

        if (result.IsSuccess)
        {
            await botClient.AnswerCallbackQueryAsync(
                callbackQuery.Id,
                "✅ Призначення знято",
                cancellationToken: cancellationToken);

            // Refresh the view
            await HandleAdminAppealViewCallback(botClient, new CallbackQuery
            {
                Id = callbackQuery.Id,
                From = callbackQuery.From,
                Message = callbackQuery.Message,
                Data = $"admin_view_{appealId}"
            }, cancellationToken);
        }
        else
        {
            await botClient.AnswerCallbackQueryAsync(
                callbackQuery.Id,
                $"❌ {result.Error}",
                showAlert: true,
                cancellationToken: cancellationToken);
        }
    }

    private async Task HandleAdminPriorityMenuCallback(
        ITelegramBotClient botClient,
        CallbackQuery callbackQuery,
        CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var userRepo = scope.ServiceProvider.GetRequiredService<IUserRepository>();

        var user = await userRepo.GetByTelegramIdAsync(callbackQuery.From.Id, cancellationToken);
        if (user?.Role != Domain.Enums.UserRole.Admin)
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
                "❌ Невірний ID звернення",
                showAlert: true,
                cancellationToken: cancellationToken);
            return;
        }

        await botClient.EditMessageReplyMarkupAsync(
            chatId: callbackQuery.Message!.Chat.Id,
            messageId: callbackQuery.Message.MessageId,
            replyMarkup: KeyboardFactory.GetPrioritySelectionKeyboard(appealId),
            cancellationToken: cancellationToken);

        await botClient.AnswerCallbackQueryAsync(
            callbackQuery.Id,
            "Оберіть пріоритет:",
            cancellationToken: cancellationToken);
    }

    private async Task HandleAdminSetPriorityCallback(
        ITelegramBotClient botClient,
        CallbackQuery callbackQuery,
        CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var userRepo = scope.ServiceProvider.GetRequiredService<IUserRepository>();

        var user = await userRepo.GetByTelegramIdAsync(callbackQuery.From.Id, cancellationToken);
        if (user?.Role != Domain.Enums.UserRole.Admin)
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
                "❌ Невірні параметри",
                showAlert: true,
                cancellationToken: cancellationToken);
            return;
        }

        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var result = await mediator.Send(new UpdatePriorityCommand
        {
            AppealId = appealId,
            AdminId = user.TelegramId,
            Priority = (Domain.Enums.AppealPriority)priorityValue
        }, cancellationToken);

        if (result.IsSuccess)
        {
            await botClient.AnswerCallbackQueryAsync(
                callbackQuery.Id,
                "✅ Пріоритет оновлено",
                cancellationToken: cancellationToken);

            // Refresh the view
            await HandleAdminAppealViewCallback(botClient, new CallbackQuery
            {
                Id = callbackQuery.Id,
                From = callbackQuery.From,
                Message = callbackQuery.Message,
                Data = $"admin_view_{appealId}"
            }, cancellationToken);
        }
        else
        {
            await botClient.AnswerCallbackQueryAsync(
                callbackQuery.Id,
                $"❌ {result.Error}",
                showAlert: true,
                cancellationToken: cancellationToken);
        }
    }

    private async Task HandleAdminCloseAppealCallback(
        ITelegramBotClient botClient,
        CallbackQuery callbackQuery,
        CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var userRepo = scope.ServiceProvider.GetRequiredService<IUserRepository>();

        var user = await userRepo.GetByTelegramIdAsync(callbackQuery.From.Id, cancellationToken);
        if (user?.Role != Domain.Enums.UserRole.Admin)
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
                "❌ Невірний ID звернення",
                showAlert: true,
                cancellationToken: cancellationToken);
            return;
        }

        // TODO: Implement state management for close reason input
        // For now, close with default reason
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var result = await mediator.Send(new CloseAppealCommand
        {
            AppealId = appealId,
            AdminId = user.TelegramId,
            Reason = "Розглянуто та вирішено адміністратором"
        }, cancellationToken);

        if (result.IsSuccess)
        {
            await botClient.AnswerCallbackQueryAsync(
                callbackQuery.Id,
                "✅ Звернення закрито",
                cancellationToken: cancellationToken);

            // Refresh the view
            await HandleAdminAppealViewCallback(botClient, new CallbackQuery
            {
                Id = callbackQuery.Id,
                From = callbackQuery.From,
                Message = callbackQuery.Message,
                Data = $"admin_view_{appealId}"
            }, cancellationToken);
        }
        else
        {
            await botClient.AnswerCallbackQueryAsync(
                callbackQuery.Id,
                $"❌ {result.Error}",
                showAlert: true,
                cancellationToken: cancellationToken);
        }
    }

    #endregion

    private Task HandleEditedMessageAsync(
        ITelegramBotClient botClient,
        Message message,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Отримано відредаговане повідомлення від {UserId}", message.From?.Id);
        return Task.CompletedTask;
    }

    private Task HandleUnknownUpdateAsync(
        ITelegramBotClient botClient,
        Update update,
        CancellationToken cancellationToken)
    {
        _logger.LogWarning("Невідомий тип оновлення: {UpdateType}", update.Type);
        return Task.CompletedTask;
    }

    public Task HandleErrorAsync(
        ITelegramBotClient botClient,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var errorMessage = exception switch
        {
            ApiRequestException apiEx => $"Telegram API Error:\n[{apiEx.ErrorCode}]\n{apiEx.Message}",
            _ => exception.ToString()
        };

        _logger.LogError(exception, "Помилка при обробці оновлення: {ErrorMessage}", errorMessage);
        return Task.CompletedTask;
    }
}
