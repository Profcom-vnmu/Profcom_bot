using MediatR;
using Microsoft.Extensions.Logging;
using StudentUnionBot.Application.Tutorial.Commands.UpdateTutorialProgress;
using StudentUnionBot.Application.Users.Queries.GetUserByTelegramId;
using StudentUnionBot.Domain.Enums;
using StudentUnionBot.Domain.Interfaces;
using StudentUnionBot.Presentation.Bot.Keyboards;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace StudentUnionBot.Presentation.Bot.Handlers.Tutorial;

/// <summary>
/// Обробник callback'ів туторіалу
/// </summary>
public class TutorialHandler
{
    private readonly IMediator _mediator;
    private readonly ILogger<TutorialHandler> _logger;
    private readonly ILocalizationService _localizationService;

    public TutorialHandler(
        IMediator mediator,
        ILogger<TutorialHandler> logger,
        ILocalizationService localizationService)
    {
        _mediator = mediator;
        _logger = logger;
        _localizationService = localizationService;
    }

    /// <summary>
    /// Обробка початку туторіалу
    /// </summary>
    public async Task HandleTutorialStartAsync(
        ITelegramBotClient botClient,
        CallbackQuery callbackQuery,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = callbackQuery.From.Id;
            
            // Отримуємо мову користувача
            var getUserQuery = new GetUserByTelegramIdQuery { TelegramId = userId };
            var userResult = await _mediator.Send(getUserQuery, cancellationToken);
            var userLanguage = Language.Ukrainian;
            
            if (userResult.IsSuccess && userResult.Value != null)
            {
                Enum.TryParse<Language>(userResult.Value.Language, out userLanguage);
            }
            
            // Оновлюємо прогрес
            var command = new UpdateTutorialProgressCommand
            {
                TelegramId = userId,
                Step = TutorialStep.Welcome
            };
            
            var result = await _mediator.Send(command, cancellationToken);
            
            if (!result.IsSuccess)
            {
                await botClient.AnswerCallbackQueryAsync(
                    callbackQuery.Id,
                    "❌ Помилка при запуску туторіалу",
                    showAlert: true,
                    cancellationToken: cancellationToken);
                return;
            }

            // Показуємо перший крок
            var tutorialText = GetTutorialStepText(TutorialStep.Welcome);
            var keyboard = await KeyboardFactory.GetTutorialKeyboardAsync(
                _localizationService,
                userLanguage,
                1, // currentStep
                5, // totalSteps (Welcome, Appeals, Events, News, Profile)
                cancellationToken);
            
            await botClient.EditMessageTextAsync(
                chatId: callbackQuery.Message!.Chat.Id,
                messageId: callbackQuery.Message.MessageId,
                text: tutorialText,
                parseMode: ParseMode.Html,
                replyMarkup: keyboard,
                cancellationToken: cancellationToken);

            await botClient.AnswerCallbackQueryAsync(
                callbackQuery.Id,
                cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Помилка в HandleTutorialStartAsync");
            await botClient.AnswerCallbackQueryAsync(
                callbackQuery.Id,
                "❌ Виникла помилка",
                showAlert: true,
                cancellationToken: cancellationToken);
        }
    }

    /// <summary>
    /// Обробка переходу до наступного кроку
    /// </summary>
    public async Task HandleTutorialStepAsync(
        ITelegramBotClient botClient,
        CallbackQuery callbackQuery,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = callbackQuery.From.Id;
            var data = callbackQuery.Data!;
            
            // Витягуємо номер кроку з callback_data (tutorial_step_2)
            if (!int.TryParse(data.Replace("tutorial_step_", ""), out int stepNumber))
            {
                _logger.LogWarning("Невалідний формат callback_data: {Data}", data);
                return;
            }

            var step = (TutorialStep)stepNumber;
            
            // Отримуємо мову користувача
            var getUserQuery = new GetUserByTelegramIdQuery { TelegramId = userId };
            var userResult = await _mediator.Send(getUserQuery, cancellationToken);
            var userLanguage = Language.Ukrainian;
            
            if (userResult.IsSuccess && userResult.Value != null)
            {
                Enum.TryParse<Language>(userResult.Value.Language, out userLanguage);
            }
            
            // Оновлюємо прогрес
            var command = new UpdateTutorialProgressCommand
            {
                TelegramId = userId,
                Step = step
            };
            
            var result = await _mediator.Send(command, cancellationToken);
            
            if (!result.IsSuccess)
            {
                await botClient.AnswerCallbackQueryAsync(
                    callbackQuery.Id,
                    "❌ Помилка при оновленні прогресу",
                    showAlert: true,
                    cancellationToken: cancellationToken);
                return;
            }

            // Показуємо відповідний крок
            var tutorialText = GetTutorialStepText(step);
            var keyboard = await KeyboardFactory.GetTutorialKeyboardAsync(
                _localizationService,
                userLanguage,
                stepNumber, // currentStep
                5, // totalSteps (Welcome, Appeals, Events, News, Profile)
                cancellationToken);
            
            await botClient.EditMessageTextAsync(
                chatId: callbackQuery.Message!.Chat.Id,
                messageId: callbackQuery.Message.MessageId,
                text: tutorialText,
                parseMode: ParseMode.Html,
                replyMarkup: keyboard,
                cancellationToken: cancellationToken);

            await botClient.AnswerCallbackQueryAsync(
                callbackQuery.Id,
                cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Помилка в HandleTutorialStepAsync");
            await botClient.AnswerCallbackQueryAsync(
                callbackQuery.Id,
                "❌ Виникла помилка",
                showAlert: true,
                cancellationToken: cancellationToken);
        }
    }

    /// <summary>
    /// Обробка завершення туторіалу
    /// </summary>
    public async Task HandleTutorialCompleteAsync(
        ITelegramBotClient botClient,
        CallbackQuery callbackQuery,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = callbackQuery.From.Id;
            
            // Оновлюємо прогрес
            var command = new UpdateTutorialProgressCommand
            {
                TelegramId = userId,
                Step = TutorialStep.Completed
            };
            
            var result = await _mediator.Send(command, cancellationToken);
            
            if (!result.IsSuccess)
            {
                await botClient.AnswerCallbackQueryAsync(
                    callbackQuery.Id,
                    "❌ Помилка при завершенні туторіалу",
                    showAlert: true,
                    cancellationToken: cancellationToken);
                return;
            }

            // Показуємо фінальне повідомлення
            var completionText = "🎉 <b>Вітаємо! Туторіал завершено!</b>\n\n" +
                               "Тепер ви знаєте як користуватись усіма можливостями бота.\n\n" +
                               "Приємного користування! 😊";
            
            var mainMenuKeyboard = new Telegram.Bot.Types.ReplyMarkups.InlineKeyboardMarkup(new[]
            {
                new[]
                {
                    Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton.WithCallbackData("🏠 Головне меню", "back_to_main")
                }
            });
            
            await botClient.EditMessageTextAsync(
                chatId: callbackQuery.Message!.Chat.Id,
                messageId: callbackQuery.Message.MessageId,
                text: completionText,
                parseMode: ParseMode.Html,
                replyMarkup: mainMenuKeyboard,
                cancellationToken: cancellationToken);

            await botClient.AnswerCallbackQueryAsync(
                callbackQuery.Id,
                "✅ Туторіал завершено!",
                cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Помилка в HandleTutorialCompleteAsync");
            await botClient.AnswerCallbackQueryAsync(
                callbackQuery.Id,
                "❌ Виникла помилка",
                showAlert: true,
                cancellationToken: cancellationToken);
        }
    }

    /// <summary>
    /// Обробка пропуску туторіалу
    /// </summary>
    public async Task HandleTutorialSkipAsync(
        ITelegramBotClient botClient,
        CallbackQuery callbackQuery,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = callbackQuery.From.Id;
            
            // Позначаємо туторіал як завершений
            var command = new UpdateTutorialProgressCommand
            {
                TelegramId = userId,
                Step = TutorialStep.Completed
            };
            
            var result = await _mediator.Send(command, cancellationToken);
            
            if (!result.IsSuccess)
            {
                await botClient.AnswerCallbackQueryAsync(
                    callbackQuery.Id,
                    "❌ Помилка",
                    showAlert: true,
                    cancellationToken: cancellationToken);
                return;
            }

            // Повертаємо до головного меню
            var skipText = "⏭️ <b>Туторіал пропущено</b>\n\n" +
                          "Ви завжди можете повторно пройти туторіал з головного меню.";
            
            var mainMenuKeyboard = new Telegram.Bot.Types.ReplyMarkups.InlineKeyboardMarkup(new[]
            {
                new[]
                {
                    Telegram.Bot.Types.ReplyMarkups.InlineKeyboardButton.WithCallbackData("🏠 Головне меню", "back_to_main")
                }
            });
            
            await botClient.EditMessageTextAsync(
                chatId: callbackQuery.Message!.Chat.Id,
                messageId: callbackQuery.Message.MessageId,
                text: skipText,
                parseMode: ParseMode.Html,
                replyMarkup: mainMenuKeyboard,
                cancellationToken: cancellationToken);

            await botClient.AnswerCallbackQueryAsync(
                callbackQuery.Id,
                cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Помилка в HandleTutorialSkipAsync");
            await botClient.AnswerCallbackQueryAsync(
                callbackQuery.Id,
                "❌ Виникла помилка",
                showAlert: true,
                cancellationToken: cancellationToken);
        }
    }

    /// <summary>
    /// Отримання тексту для кожного кроку туторіалу
    /// </summary>
    private string GetTutorialStepText(TutorialStep step)
    {
        return step switch
        {
            TutorialStep.Welcome => 
                "👋 <b>Вітаємо в StudentUnionBot!</b>\n\n" +
                "Я допоможу вам:\n" +
                "📩 Подавати звернення до профкому\n" +
                "📰 Дізнаватись новини та оновлення\n" +
                "🎉 Реєструватись на події\n" +
                "👤 Керувати своїм профілем\n\n" +
                "Давайте швидко ознайомимось з основними можливостями!",
            
            TutorialStep.Appeals => 
                "📩 <b>Створення звернень</b>\n\n" +
                "Ви можете створити звернення до профкому з будь-якого питання:\n" +
                "• Академічні питання\n" +
                "• Соціальні питання\n" +
                "• Фінансові питання\n\n" +
                "Просто натисніть <b>\"Створити звернення\"</b> в головному меню, " +
                "оберіть категорію та опишіть вашу проблему.\n\n" +
                "Ви завжди можете переглянути статус ваших звернень в розділі <b>\"Мої звернення\"</b>.",
            
            TutorialStep.Events => 
                "🎉 <b>Події та реєстрація</b>\n\n" +
                "В розділі <b>\"Події\"</b> ви знайдете:\n" +
                "• Майбутні заходи\n" +
                "• Інформацію про час та місце\n" +
                "• Можливість зареєструватись\n\n" +
                "Натисніть на подію щоб дізнатись більше деталей або зареєструватись на участь.",
            
            TutorialStep.News => 
                "📰 <b>Новини та оновлення</b>\n\n" +
                "Залишайтесь в курсі останніх новин:\n" +
                "• Оголошення від профкому\n" +
                "• Важливі оновлення\n" +
                "• Корисна інформація\n\n" +
                "Всі новини доступні в розділі <b>\"Новини\"</b>.",
            
            TutorialStep.Profile => 
                "👤 <b>Ваш профіль</b>\n\n" +
                "В розділі <b>\"Профіль\"</b> ви можете:\n" +
                "• Переглянути вашу інформацію\n" +
                "• Вказати email для зв'язку\n" +
                "• Додати групу та факультет\n" +
                "• Змінити мову інтерфейсу\n\n" +
                "Заповнений профіль допоможе нам швидше обробляти ваші звернення!",
            
            _ => "❓ Невідомий крок туторіалу"
        };
    }
}
