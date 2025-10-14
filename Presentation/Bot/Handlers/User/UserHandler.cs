using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StudentUnionBot.Application.Common.Interfaces;
using StudentUnionBot.Application.Users.Commands.RegisterUser;
using StudentUnionBot.Application.Users.Commands.SendVerificationEmail;
using StudentUnionBot.Application.Users.Commands.VerifyEmail;
using StudentUnionBot.Application.Users.Commands.UpdateProfile;
using StudentUnionBot.Application.Users.Commands.ChangeLanguage;
using StudentUnionBot.Application.Users.Queries.GetUserByTelegramId;
using StudentUnionBot.Domain.Enums;
using StudentUnionBot.Presentation.Bot.Handlers.Common;
using StudentUnionBot.Presentation.Bot.Handlers.Interfaces;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramUser = Telegram.Bot.Types.User;

namespace StudentUnionBot.Presentation.Bot.Handlers.User;

/// <summary>
/// Обробник користувацьких функцій: реєстрація, профіль, верифікація email
/// </summary>
public class UserHandler : BaseHandler, IUserHandler
{
    public UserHandler(
        IServiceScopeFactory scopeFactory,
        ILogger<UserHandler> logger,
        IMediator mediator)
        : base(scopeFactory, logger, mediator)
    {
    }

    /// <summary>
    /// Реєструє або оновлює інформацію про користувача
    /// </summary>
    public async Task RegisterOrUpdateUserAsync(TelegramUser? user, CancellationToken cancellationToken)
    {
        if (user == null) return;

        try
        {
            var command = new RegisterUserCommand
            {
                TelegramId = user.Id,
                Username = user.Username,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Language = Language.Ukrainian
            };

            await _mediator.Send(command, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Помилка при реєстрації користувача {UserId}", user.Id);
        }
    }

    /// <summary>
    /// Обробляє введення email для верифікації
    /// </summary>
    public async Task HandleEmailInputAsync(
        ITelegramBotClient botClient,
        Message message,
        CancellationToken cancellationToken)
    {
        var userId = message.From!.Id;
        var email = message.Text?.Trim();

        // Базова валідація email
        if (string.IsNullOrWhiteSpace(email) || !email.Contains("@") || email.Length < 5)
        {
            await SendErrorMessageAsync(
                botClient,
                message.Chat.Id,
                "❌ Некоректний формат email адреси.\n\n" +
                "Будь ласка, введіть дійсну email адресу (наприклад: user@vnmu.edu.ua)",
                cancellationToken);
            return;
        }

        if (email.Length > 100)
        {
            await SendErrorMessageAsync(
                botClient,
                message.Chat.Id,
                "❌ Email адреса занадто довга (максимум 100 символів)",
                cancellationToken);
            return;
        }

        try
        {
            // Відправляємо верифікаційний email через MediatR
            var command = new SendVerificationEmailCommand
            {
                TelegramId = userId,
                Email = email
            };

            var result = await _mediator.Send(command, cancellationToken);

            if (result.IsSuccess)
            {
                using var scope = _scopeFactory.CreateScope();
                var stateManager = scope.ServiceProvider.GetRequiredService<IUserStateManager>();
                await stateManager.SetStateAsync(userId, UserConversationState.WaitingVerificationCode, cancellationToken);

                await botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: "📧 Верифікаційний код надіслано на вашу email адресу!\n\n" +
                          "Перевірте вашу пошту та введіть 6-значний код для підтвердження:",
                    parseMode: ParseMode.Html,
                    replyMarkup: GetBackToMainMenu(),
                    cancellationToken: cancellationToken);
            }
            else
            {
                await SendErrorMessageAsync(
                    botClient,
                    message.Chat.Id,
                    "❌ Помилка при відправці верифікаційного коду.\n" +
                    "Спробуйте пізніше або зверніться до адміністрації.",
                    cancellationToken);

                using var scope = _scopeFactory.CreateScope();
                var stateManager = scope.ServiceProvider.GetRequiredService<IUserStateManager>();
                await stateManager.ClearStateAsync(userId, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Помилка при відправці verification email для користувача {UserId}", userId);

            await SendErrorMessageAsync(
                botClient,
                message.Chat.Id,
                "❌ Виникла помилка при відправці верифікаційного коду.\nСпробуйте пізніше.",
                cancellationToken);

            using var scope = _scopeFactory.CreateScope();
            var stateManager = scope.ServiceProvider.GetRequiredService<IUserStateManager>();
            await stateManager.ClearStateAsync(userId, cancellationToken);
        }
    }

    /// <summary>
    /// Обробляє введення коду верифікації email
    /// </summary>
    public async Task HandleVerificationCodeInputAsync(
        ITelegramBotClient botClient,
        Message message,
        CancellationToken cancellationToken)
    {
        var userId = message.From!.Id;
        var code = message.Text?.Trim();

        // Валідація формату коду (6 цифр)
        if (string.IsNullOrWhiteSpace(code) || code.Length != 6 || !code.All(char.IsDigit))
        {
            await SendErrorMessageAsync(
                botClient,
                message.Chat.Id,
                "❌ Некоректний формат коду.\n\n" +
                "Код має складатися з 6 цифр. Спробуйте ще раз:",
                cancellationToken);
            return;
        }

        try
        {
            // Верифікуємо email код через MediatR
            var command = new VerifyEmailCommand
            {
                TelegramId = userId,
                Code = code
            };

            var result = await _mediator.Send(command, cancellationToken);

            if (result.IsSuccess && result.Value)
            {
                using var scope = _scopeFactory.CreateScope();
                var stateManager = scope.ServiceProvider.GetRequiredService<IUserStateManager>();
                await stateManager.ClearStateAsync(userId, cancellationToken);

                await botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: "✅ <b>Email успішно підтверджено!</b>\n\n" +
                          "Тепер ви будете отримувати важливі повідомлення на вашу email адресу.",
                    parseMode: ParseMode.Html,
                    replyMarkup: GetMainMenu(),
                    cancellationToken: cancellationToken);
            }
            else
            {
                await SendErrorMessageAsync(
                    botClient,
                    message.Chat.Id,
                    "❌ Невірний або застарілий код верифікації.\n\n" +
                    "Спробуйте ввести код ще раз або почніть процес верифікації знову:",
                    cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Помилка при верифікації email для користувача {UserId}", userId);

            await SendErrorMessageAsync(
                botClient,
                message.Chat.Id,
                "❌ Виникла помилка при верифікації.\nСпробуйте пізніше.",
                cancellationToken);

            using var scope = _scopeFactory.CreateScope();
            var stateManager = scope.ServiceProvider.GetRequiredService<IUserStateManager>();
            await stateManager.ClearStateAsync(userId, cancellationToken);
        }
    }

    /// <summary>
    /// Обробляє введення повного імені
    /// </summary>
    public async Task HandleFullNameInputAsync(
        ITelegramBotClient botClient,
        Message message,
        CancellationToken cancellationToken)
    {
        var userId = message.From!.Id;
        var fullName = message.Text?.Trim();

        if (string.IsNullOrWhiteSpace(fullName) || fullName.Length < 2)
        {
            await SendErrorMessageAsync(
                botClient,
                message.Chat.Id,
                "❌ Ім'я занадто коротке (мінімум 2 символи)",
                cancellationToken);
            return;
        }

        if (fullName.Length > 100)
        {
            await SendErrorMessageAsync(
                botClient,
                message.Chat.Id,
                "❌ Ім'я занадто довге (максимум 100 символів)",
                cancellationToken);
            return;
        }

        // Зберігаємо повне ім'я та переходимо до введення факультету
        using var scope = _scopeFactory.CreateScope();
        var stateManager = scope.ServiceProvider.GetRequiredService<IUserStateManager>();
        await stateManager.SetDataAsync(userId, "profile_fullname", fullName, cancellationToken);
        await stateManager.SetStateAsync(userId, UserConversationState.WaitingFacultyInput, cancellationToken);

        await botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: $"✅ Ім'я збережено: <b>{fullName}</b>\n\n" +
                  "📚 Тепер введіть назву свого факультету:",
            parseMode: ParseMode.Html,
            cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Обробляє введення факультету
    /// </summary>
    public async Task HandleFacultyInputAsync(
        ITelegramBotClient botClient,
        Message message,
        CancellationToken cancellationToken)
    {
        var userId = message.From!.Id;
        var faculty = message.Text?.Trim();

        if (string.IsNullOrWhiteSpace(faculty) || faculty.Length < 2)
        {
            await SendErrorMessageAsync(
                botClient,
                message.Chat.Id,
                "❌ Назва факультету занадто коротка (мінімум 2 символи)",
                cancellationToken);
            return;
        }

        if (faculty.Length > 100)
        {
            await SendErrorMessageAsync(
                botClient,
                message.Chat.Id,
                "❌ Назва факультету занадто довга (максимум 100 символів)",
                cancellationToken);
            return;
        }

        // Зберігаємо факультет та переходимо до введення курсу
        using var scope = _scopeFactory.CreateScope();
        var stateManager = scope.ServiceProvider.GetRequiredService<IUserStateManager>();
        await stateManager.SetDataAsync(userId, "profile_faculty", faculty, cancellationToken);
        await stateManager.SetStateAsync(userId, UserConversationState.WaitingCourseInput, cancellationToken);

        await botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: $"✅ Факультет збережено: <b>{faculty}</b>\n\n" +
                  "🎓 Тепер введіть номер курсу (1-6):",
            parseMode: ParseMode.Html,
            cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Обробляє введення курсу
    /// </summary>
    public async Task HandleCourseInputAsync(
        ITelegramBotClient botClient,
        Message message,
        CancellationToken cancellationToken)
    {
        var userId = message.From!.Id;
        var courseText = message.Text?.Trim();

        if (!int.TryParse(courseText, out int course) || course < 1 || course > 6)
        {
            await SendErrorMessageAsync(
                botClient,
                message.Chat.Id,
                "❌ Некоректний номер курсу. Введіть число від 1 до 6",
                cancellationToken);
            return;
        }

        // Зберігаємо курс та переходимо до введення групи
        using var scope = _scopeFactory.CreateScope();
        var stateManager = scope.ServiceProvider.GetRequiredService<IUserStateManager>();
        await stateManager.SetDataAsync(userId, "profile_course", course, cancellationToken);
        await stateManager.SetStateAsync(userId, UserConversationState.WaitingGroupInput, cancellationToken);

        await botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: $"✅ Курс збережено: <b>{course}</b>\n\n" +
                  "👥 І наостанок, введіть назву вашої групи:",
            parseMode: ParseMode.Html,
            cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Обробляє введення групи та завершує оновлення профілю
    /// </summary>
    public async Task HandleGroupInputAsync(
        ITelegramBotClient botClient,
        Message message,
        CancellationToken cancellationToken)
    {
        var userId = message.From!.Id;
        var group = message.Text?.Trim();

        if (string.IsNullOrWhiteSpace(group) || group.Length < 1)
        {
            await SendErrorMessageAsync(
                botClient,
                message.Chat.Id,
                "❌ Назва групи не може бути порожньою",
                cancellationToken);
            return;
        }

        if (group.Length > 50)
        {
            await SendErrorMessageAsync(
                botClient,
                message.Chat.Id,
                "❌ Назва групи занадто довга (максимум 50 символів)",
                cancellationToken);
            return;
        }

        try
        {
            // Отримуємо збережені дані
            using var scope = _scopeFactory.CreateScope();
            var stateManager = scope.ServiceProvider.GetRequiredService<IUserStateManager>();
            var fullName = await stateManager.GetDataAsync<string>(userId, "profile_fullname", cancellationToken);
            var faculty = await stateManager.GetDataAsync<string>(userId, "profile_faculty", cancellationToken);
            var course = await stateManager.GetDataAsync<int>(userId, "profile_course", cancellationToken);

            // Оновлюємо профіль
            var command = new UpdateProfileCommand
            {
                TelegramId = userId,
                FullName = fullName,
                Faculty = faculty,
                Course = course,
                Group = group
            };

            var result = await _mediator.Send(command, cancellationToken);

            if (result.IsSuccess)
            {
                using var clearScope = _scopeFactory.CreateScope();
                var clearStateManager = clearScope.ServiceProvider.GetRequiredService<IUserStateManager>();
                await clearStateManager.ClearStateAsync(userId, cancellationToken);
                await clearStateManager.ClearAllDataAsync(userId, cancellationToken);

                await botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: "✅ <b>Профіль успішно оновлено!</b>\n\n" +
                          $"👤 <b>Ім'я:</b> {fullName}\n" +
                          $"🏛️ <b>Факультет:</b> {faculty}\n" +
                          $"🎓 <b>Курс:</b> {course}\n" +
                          $"👥 <b>Група:</b> {group}",
                    parseMode: ParseMode.Html,
                    replyMarkup: GetMainMenu(),
                    cancellationToken: cancellationToken);
            }
            else
            {
                await SendErrorMessageAsync(
                    botClient,
                    message.Chat.Id,
                    "❌ Помилка при оновленні профілю.\nСпробуйте пізніше.",
                    cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Помилка при оновленні профілю для користувача {UserId}", userId);

            await SendErrorMessageAsync(
                botClient,
                message.Chat.Id,
                "❌ Виникла помилка при оновленні профілю.\nСпробуйте пізніше.",
                cancellationToken);

            using var errorScope = _scopeFactory.CreateScope();
            var errorStateManager = errorScope.ServiceProvider.GetRequiredService<IUserStateManager>();
            await errorStateManager.ClearStateAsync(userId, cancellationToken);
            await errorStateManager.ClearAllDataAsync(userId, cancellationToken);
        }
    }

    /// <summary>
    /// Показує профіль користувача
    /// </summary>
    public async Task HandleProfileViewCallback(
        ITelegramBotClient botClient,
        CallbackQuery callbackQuery,
        CancellationToken cancellationToken)
    {
        var userId = callbackQuery.From.Id;

        var getUserQuery = new GetUserByTelegramIdQuery { TelegramId = userId };
        var userResult = await _mediator.Send(getUserQuery, cancellationToken);
        var user = userResult.Value;

        if (user == null)
        {
            await SendCallbackErrorAsync(
                botClient,
                callbackQuery,
                "❌ Профіль не знайдено",
                cancellationToken);
            return;
        }

        var username = user.Username ?? "немає";
        var fullName = user.FullName ?? $"{callbackQuery.From.FirstName} {callbackQuery.From.LastName}".Trim();
        var email = user.Email ?? "<i>не вказано</i>";
        var emailStatus = user.IsEmailVerified ? "✅ Підтверджено" : "⚠️ Не підтверджено";
        var languageDisplay = user.Language == "uk" ? "🇺🇦 Українська" : "🇬🇧 English";

        var profileText = "👤 <b>Мій профіль</b>\n\n" +
                         $"🆔 <b>ID:</b> {user.TelegramId}\n" +
                         $"👤 <b>Ім'я:</b> {fullName}\n" +
                         $"📱 <b>Username:</b> @{username}\n" +
                         $"📧 <b>Email:</b> {email} ({emailStatus})\n" +
                         $"🌐 <b>Мова:</b> {languageDisplay}\n" +
                         $"🏛️ <b>Факультет:</b> {user.Faculty ?? "<i>не вказано</i>"}\n" +
                         $"🎓 <b>Курс:</b> {(user.Course?.ToString() ?? "<i>не вказано</i>")}\n" +
                         $"👥 <b>Група:</b> {user.Group ?? "<i>не вказано</i>"}\n" +
                         $"📅 <b>Дата реєстрації:</b> {user.JoinedAt:dd.MM.yyyy}";

        // Кнопки для редагування
        var keyboard = new InlineKeyboardMarkup(new[]
        {
            new[]
            {
                InlineKeyboardButton.WithCallbackData("✏️ Редагувати дані", "profile_edit_info"),
                InlineKeyboardButton.WithCallbackData("📧 Змінити Email", "profile_edit_email")
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData("🌐 Змінити мову", "profile_change_language")
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData("🔙 Назад до меню", "back_to_main")
            }
        });

        await botClient.EditMessageTextAsync(
            chatId: callbackQuery.Message!.Chat.Id,
            messageId: callbackQuery.Message.MessageId,
            text: profileText,
            parseMode: ParseMode.Html,
            replyMarkup: keyboard,
            cancellationToken: cancellationToken);

        await botClient.AnswerCallbackQueryAsync(
            callbackQueryId: callbackQuery.Id,
            cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Розпочинає редагування інформації профілю
    /// </summary>
    public async Task HandleProfileEditInfoCallback(
        ITelegramBotClient botClient,
        CallbackQuery callbackQuery,
        CancellationToken cancellationToken)
    {
        var userId = callbackQuery.From.Id;

        // Встановлюємо стан очікування введення повного імені
        using var scope = _scopeFactory.CreateScope();
        var stateManager = scope.ServiceProvider.GetRequiredService<IUserStateManager>();
        await stateManager.SetStateAsync(userId, UserConversationState.WaitingFullNameInput, cancellationToken);

        await botClient.EditMessageTextAsync(
            chatId: callbackQuery.Message!.Chat.Id,
            messageId: callbackQuery.Message.MessageId,
            text: "✏️ <b>Редагування профілю</b>\n\n" +
                  "Давайте оновимо вашу інформацію. Спочатку введіть ваше повне ім'я:\n\n" +
                  "<i>Приклад: Іванов Іван Іванович</i>",
            parseMode: ParseMode.Html,
            replyMarkup: GetBackToMainMenu(),
            cancellationToken: cancellationToken);

        await botClient.AnswerCallbackQueryAsync(
            callbackQueryId: callbackQuery.Id,
            cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Розпочинає редагування email адреси
    /// </summary>
    public async Task HandleProfileEditEmailCallback(
        ITelegramBotClient botClient,
        CallbackQuery callbackQuery,
        CancellationToken cancellationToken)
    {
        var userId = callbackQuery.From.Id;

        // Встановлюємо стан очікування введення email
        using var scope = _scopeFactory.CreateScope();
        var stateManager = scope.ServiceProvider.GetRequiredService<IUserStateManager>();
        await stateManager.SetStateAsync(userId, UserConversationState.WaitingEmailInput, cancellationToken);

        await botClient.EditMessageTextAsync(
            chatId: callbackQuery.Message!.Chat.Id,
            messageId: callbackQuery.Message.MessageId,
            text: "📧 <b>Зміна Email адреси</b>\n\n" +
                  "Введіть нову email адресу для отримання повідомлень:\n\n" +
                  "<i>Приклад: student@vnmu.edu.ua</i>\n\n" +
                  "💡 Для скасування натисніть кнопку нижче",
            parseMode: ParseMode.Html,
            replyMarkup: await GetCancelKeyboardAsync(userId, cancellationToken),
            cancellationToken: cancellationToken);

        await botClient.AnswerCallbackQueryAsync(
            callbackQueryId: callbackQuery.Id,
            cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Показує меню вибору мови
    /// </summary>
    public async Task HandleProfileChangeLanguageCallback(
        ITelegramBotClient botClient,
        CallbackQuery callbackQuery,
        CancellationToken cancellationToken)
    {
        var keyboard = new InlineKeyboardMarkup(new[]
        {
            new[]
            {
                InlineKeyboardButton.WithCallbackData("🇺🇦 Українська", "set_lang_uk"),
                InlineKeyboardButton.WithCallbackData("🇬🇧 English", "set_lang_en")
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData("🔙 Назад до профілю", "profile_view")
            }
        });

        await botClient.EditMessageTextAsync(
            chatId: callbackQuery.Message!.Chat.Id,
            messageId: callbackQuery.Message.MessageId,
            text: "🌐 <b>Вибір мови інтерфейсу</b>\n\n" +
                  "Оберіть мову для відображення повідомлень бота:",
            parseMode: ParseMode.Html,
            replyMarkup: keyboard,
            cancellationToken: cancellationToken);

        await botClient.AnswerCallbackQueryAsync(
            callbackQueryId: callbackQuery.Id,
            cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Встановлює мову користувача
    /// </summary>
    public async Task HandleSetLanguageCallback(
        ITelegramBotClient botClient,
        CallbackQuery callbackQuery,
        CancellationToken cancellationToken)
    {
        var userId = callbackQuery.From.Id;
        var data = callbackQuery.Data!;

        var language = data == "set_lang_uk" ? Language.Ukrainian : Language.English;
        var languageName = data == "set_lang_uk" ? "🇺🇦 Українську" : "🇬🇧 English";

        try
        {
            var command = new ChangeLanguageCommand
            {
                TelegramId = userId,
                Language = language
            };

            var result = await _mediator.Send(command, cancellationToken);

            if (result.IsSuccess)
            {
                // Отримуємо локалізаційний сервіс для нової мови
                using var scope = _scopeFactory.CreateScope();
                var localizationService = scope.ServiceProvider.GetRequiredService<StudentUnionBot.Domain.Interfaces.ILocalizationService>();
                
                var successMessage = await localizationService.GetLocalizedStringAsync(
                    "profile.language_changed", language, cancellationToken);
                var buttonProfile = await localizationService.GetLocalizedStringAsync(
                    "button.profile", language, cancellationToken);
                var buttonMainMenu = await localizationService.GetLocalizedStringAsync(
                    "button.main_menu", language, cancellationToken);

                await botClient.EditMessageTextAsync(
                    chatId: callbackQuery.Message!.Chat.Id,
                    messageId: callbackQuery.Message.MessageId,
                    text: successMessage,
                    parseMode: ParseMode.Html,
                    replyMarkup: new InlineKeyboardMarkup(new[]
                    {
                        new[]
                        {
                            InlineKeyboardButton.WithCallbackData($"👤 {buttonProfile}", "profile_view"),
                            InlineKeyboardButton.WithCallbackData($"🏠 {buttonMainMenu}", "back_to_main")
                        }
                    }),
                    cancellationToken: cancellationToken);
            }
            else
            {
                await SendCallbackErrorAsync(
                    botClient,
                    callbackQuery,
                    "❌ Помилка при зміні мови",
                    cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Помилка при зміні мови для користувача {UserId}", userId);
            
            await SendCallbackErrorAsync(
                botClient,
                callbackQuery,
                "❌ Помилка при зміні мови",
                cancellationToken);
        }

        await botClient.AnswerCallbackQueryAsync(
            callbackQueryId: callbackQuery.Id,
            cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Реалізація абстрактного методу HandleTextMessageAsync
    /// </summary>
    public override async Task HandleTextMessageAsync(
        ITelegramBotClient botClient, 
        Message message, 
        UserConversationState state, 
        CancellationToken cancellationToken)
    {
        var userId = message.From!.Id;

        switch (state)
        {
            case UserConversationState.WaitingVerificationCode:
                await HandleVerificationCodeInputAsync(botClient, message, cancellationToken);
                break;
            case UserConversationState.WaitingFullNameInput:
                await HandleFullNameInputAsync(botClient, message, cancellationToken);
                break;
            case UserConversationState.WaitingFacultyInput:
                await HandleFacultyInputAsync(botClient, message, cancellationToken);
                break;
            case UserConversationState.WaitingCourseInput:
                await HandleCourseInputAsync(botClient, message, cancellationToken);
                break;
            case UserConversationState.WaitingGroupInput:
                await HandleGroupInputAsync(botClient, message, cancellationToken);
                break;
            case UserConversationState.WaitingEmailInput:
                await HandleEmailInputAsync(botClient, message, cancellationToken);
                break;
            default:
                // Для інших станів нічого не робимо
                break;
        }
    }

    /// <summary>
    /// Обробка callback для перегляду профілю
    /// </summary>
    public async Task HandleProfileViewCallbackAsync(
        ITelegramBotClient botClient,
        CallbackQuery callbackQuery,
        CancellationToken cancellationToken)
    {
        await HandleProfileViewCallback(botClient, callbackQuery, cancellationToken);
    }

    /// <summary>
    /// Обробка callback для редагування профілю
    /// </summary>
    public async Task HandleProfileEditCallbackAsync(
        ITelegramBotClient botClient,
        CallbackQuery callbackQuery,
        CancellationToken cancellationToken)
    {
        await HandleProfileEditInfoCallback(botClient, callbackQuery, cancellationToken);
    }

    /// <summary>
    /// Обробка callback для зміни мови
    /// </summary>
    public async Task HandleLanguageChangeCallbackAsync(
        ITelegramBotClient botClient,
        CallbackQuery callbackQuery,
        CancellationToken cancellationToken)
    {
        await HandleProfileChangeLanguageCallback(botClient, callbackQuery, cancellationToken);
    }

    /// <summary>
    /// Допоміжний метод для відправки помилки callback
    /// </summary>
    private async Task SendCallbackErrorAsync(
        ITelegramBotClient botClient,
        CallbackQuery callbackQuery,
        string errorMessage,
        CancellationToken cancellationToken)
    {
        await botClient.AnswerCallbackQueryAsync(
            callbackQueryId: callbackQuery.Id,
            text: errorMessage,
            showAlert: true,
            cancellationToken: cancellationToken);
    }
}