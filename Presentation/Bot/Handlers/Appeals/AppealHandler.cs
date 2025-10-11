using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StudentUnionBot.Application.Appeals.Commands.CreateAppeal;
using StudentUnionBot.Application.Appeals.Queries.GetUserAppeals;
using StudentUnionBot.Application.Appeals.Queries.GetAppealById;
using StudentUnionBot.Application.Common.Interfaces;
using StudentUnionBot.Domain.Enums;
using StudentUnionBot.Presentation.Bot.Handlers.Common;
using StudentUnionBot.Presentation.Bot.Handlers.Interfaces;
using StudentUnionBot.Presentation.Bot.Helpers;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace StudentUnionBot.Presentation.Bot.Handlers.Appeals;

/// <summary>
/// Обробник функціоналу звернень користувачів
/// </summary>
public class AppealHandler : BaseHandler, IAppealHandler
{
    // In-memory storage для фільтрів звернень користувачів
    private static readonly Dictionary<long, AppealStatus?> _appealStatusFilters = new();
    private static readonly Dictionary<long, AppealCategory?> _appealCategoryFilters = new();

    public AppealHandler(
        IServiceScopeFactory scopeFactory,
        ILogger<AppealHandler> logger,
        IMediator mediator)
        : base(scopeFactory, logger, mediator)
    {
    }

    /// <summary>
    /// Обробляє текстові повідомлення для звернень
    /// </summary>
    public override async Task HandleTextMessageAsync(
        ITelegramBotClient botClient,
        Message message,
        UserConversationState state,
        CancellationToken cancellationToken)
    {
        switch (state)
        {
            case UserConversationState.WaitingAppealSubject:
                await HandleAppealSubjectInputAsync(botClient, message, cancellationToken);
                break;
            
            case UserConversationState.WaitingAppealMessage:
                await HandleAppealMessageInputAsync(botClient, message, cancellationToken);
                break;
            
            default:
                _logger.LogWarning("Неочікуваний стан {State} для AppealHandler", state);
                await SendErrorMessageAsync(
                    botClient,
                    message.Chat.Id,
                    "Неочікуваний стан. Спробуйте почати знову з /appeal",
                    cancellationToken);
                break;
        }
    }

    /// <summary>
    /// Обробляє вибір категорії звернення
    /// </summary>
    public async Task HandleAppealCategorySelectionAsync(
        ITelegramBotClient botClient,
        CallbackQuery callbackQuery,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = callbackQuery.From.Id;
            var data = callbackQuery.Data!;
            var categoryId = int.Parse(data.Replace("appeal_cat_", ""));
            
            var categoryNames = new Dictionary<int, string>
            {
                { 1, "🏥 Стипендія" },
                { 2, "🏠 Гуртожиток" },
                { 3, "🍽️ Заходи" },
                { 4, "💡 Пропозиція" },
                { 5, "😡 Скарга" },
                { 6, "❓ Інше" }
            };

            var categoryName = categoryNames.GetValueOrDefault(categoryId, "Невідома категорія");
            var category = (AppealCategory)categoryId;

            // Зберігаємо обрану категорію
            using var scope = _scopeFactory.CreateScope();
            var stateManager = scope.ServiceProvider.GetRequiredService<IUserStateManager>();
            await stateManager.SetDataAsync(userId, "appeal_category", category, cancellationToken);

            // Встановлюємо стан очікування теми
            await stateManager.SetStateAsync(userId, UserConversationState.WaitingAppealSubject, cancellationToken);

            await botClient.EditMessageTextAsync(
                chatId: callbackQuery.Message!.Chat.Id,
                messageId: callbackQuery.Message.MessageId,
                text: $"📩 <b>Створення звернення</b>\n\n" +
                      $"📁 Категорія: {categoryName}\n\n" +
                      $"Напишіть <b>тему</b> вашого звернення:\n\n" +
                      $"<i>Мінімум 5 символів, максимум 200 символів</i>\n\n" +
                      $"💡 Для скасування натисніть кнопку нижче",
                parseMode: ParseMode.Html,
                replyMarkup: await GetCancelKeyboardAsync(userId, cancellationToken),
                cancellationToken: cancellationToken);

            await botClient.AnswerCallbackQueryAsync(
                callbackQueryId: callbackQuery.Id,
                text: $"Обрано категорію: {categoryName}",
                cancellationToken: cancellationToken);

            _logger.LogInformation("Користувач {UserId} обрав категорію звернення: {Category}", userId, categoryName);
        }
        catch (Exception ex)
        {
            await HandleErrorAsync(
                botClient,
                callbackQuery.Message!.Chat.Id,
                ex,
                "HandleAppealCategorySelectionAsync",
                cancellationToken);
        }
    }

    /// <summary>
    /// Обробляє перегляд власних звернень
    /// </summary>
    public async Task HandleMyAppealsCallbackAsync(
        ITelegramBotClient botClient,
        CallbackQuery callbackQuery,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = callbackQuery.From.Id;
            
            // Парсимо номер сторінки з callback
            var pageNumber = PaginationHelper.ParsePageNumber(callbackQuery.Data!, "my_appeals_page") ?? 1;
            const int pageSize = 5;
            
            // Отримуємо активні фільтри користувача (якщо є)
            _appealStatusFilters.TryGetValue(userId, out var selectedStatus);
            _appealCategoryFilters.TryGetValue(userId, out var selectedCategory);
            
            var query = new GetUserAppealsQuery 
            { 
                UserId = userId,
                PageNumber = pageNumber,
                PageSize = pageSize,
                Status = selectedStatus,      // Застосовуємо фільтр статусу
                Category = selectedCategory    // Застосовуємо фільтр категорії
            };

            var result = await _mediator.Send(query, cancellationToken);

            if (!result.IsSuccess)
            {
                await botClient.EditMessageTextAsync(
                    chatId: callbackQuery.Message!.Chat.Id,
                    messageId: callbackQuery.Message.MessageId,
                    text: $"❌ Помилка завантаження звернень: {result.Error}",
                    replyMarkup: await GetBackToMainMenuAsync(userId, cancellationToken),
                    cancellationToken: cancellationToken);
                return;
            }

            var appeals = result.Value;
            if (appeals == null || !appeals.Any())
            {
                await botClient.EditMessageTextAsync(
                    chatId: callbackQuery.Message!.Chat.Id,
                    messageId: callbackQuery.Message.MessageId,
                    text: "📋 <b>Ваші звернення</b>\n\n" +
                          "У вас поки немає звернень.\n" +
                          "Створіть перше звернення кнопкою нижче! 👇",
                    parseMode: ParseMode.Html,
                    replyMarkup: await GetBackToMainMenuAsync(userId, cancellationToken),
                    cancellationToken: cancellationToken);
                return;
            }

            // Підраховуємо загальну кількість (тут використовуємо Count для прикладу, в ідеалі отримувати з query)
            var totalCount = appeals.Count(); // TODO: отримувати TotalCount з DTO
            var totalPages = PaginationHelper.CalculateTotalPages(totalCount, pageSize);
            var validPage = PaginationHelper.ValidatePageNumber(pageNumber, totalPages);

            // Формуємо заголовок зі списком звернень
            var filterParts = new List<string>();
            if (selectedStatus.HasValue)
                filterParts.Add($"{selectedStatus.Value.GetEmoji()} {selectedStatus.Value.GetDisplayName()}");
            if (selectedCategory.HasValue)
                filterParts.Add($"{selectedCategory.Value.GetEmoji()} {selectedCategory.Value.GetDisplayName()}");
            
            var filterInfo = filterParts.Count > 0 ? $" ({string.Join(", ", filterParts)})" : "";
            
            var messageText = PaginationHelper.FormatListHeader(
                $"Ваші звернення{filterInfo}",
                totalCount,
                validPage,
                totalPages,
                pageSize);

            foreach (var appeal in appeals)
            {
                var statusIcon = appeal.Status switch
                {
                    AppealStatus.New => "🆕",
                    AppealStatus.InProgress => "⏳",
                    AppealStatus.Closed => "✅",
                    _ => "❓"
                };

                messageText += $"{statusIcon} <b>#{appeal.Id}</b> - {appeal.Subject}\n" +
                              $"   📅 {appeal.CreatedAt:dd.MM.yyyy}\n" +
                              $"   📊 {appeal.Status}\n\n";
            }

            // Створюємо кнопки для кожного звернення
            var itemButtons = PaginationHelper.CreateItemButtons(
                appeals,
                buttonsPerRow: 2,
                itemButtonFactory: a => InlineKeyboardButton.WithCallbackData(
                    $"#{a.Id} - {a.Subject.Substring(0, Math.Min(20, a.Subject.Length))}...",
                    $"appeal_view_{a.Id}"));

            // Створюємо список кнопок для клавіатури
            var buttons = itemButtons.ToList();
            
            // Додаємо кнопку фільтрів на початок
            var hasActiveFilters = selectedStatus.HasValue || selectedCategory.HasValue;
            FilterHelper.AddFilterButton(
                buttons, 
                "appeals_filters_menu", 
                hasActiveFilters: hasActiveFilters);

            // Додаємо навігаційні кнопки
            var navButtons = PaginationHelper.GetNavigationButtons(validPage, totalPages, "my_appeals_page");
            if (navButtons.Count > 0)
            {
                buttons.Add(navButtons);
            }

            // Кнопка "Назад до головного меню"
            buttons.Add(new List<InlineKeyboardButton>
            {
                InlineKeyboardButton.WithCallbackData("🔙 Головне меню", "back_to_main")
            });

            var keyboard = new InlineKeyboardMarkup(buttons);

            await botClient.EditMessageTextAsync(
                chatId: callbackQuery.Message!.Chat.Id,
                messageId: callbackQuery.Message.MessageId,
                text: messageText,
                parseMode: ParseMode.Html,
                replyMarkup: keyboard,
                cancellationToken: cancellationToken);

            await botClient.AnswerCallbackQueryAsync(
                callbackQueryId: callbackQuery.Id,
                text: $"Сторінка {validPage} з {totalPages}",
                cancellationToken: cancellationToken);

            _logger.LogInformation("Користувач {UserId} переглянув свої звернення ({Count})", userId, appeals.Count());
        }
        catch (Exception ex)
        {
            await HandleErrorAsync(
                botClient,
                callbackQuery.Message!.Chat.Id,
                ex,
                "HandleMyAppealsCallbackAsync",
                cancellationToken);
        }
    }

    /// <summary>
    /// Обробляє перегляд конкретного звернення
    /// </summary>
    public async Task HandleAppealViewCallbackAsync(
        ITelegramBotClient botClient,
        CallbackQuery callbackQuery,
        CancellationToken cancellationToken)
    {
        try
        {
            var data = callbackQuery.Data!;
            var appealId = int.Parse(data.Replace("appeal_view_", ""));
            
            var query = new GetAppealByIdQuery { AppealId = appealId };
            var result = await _mediator.Send(query, cancellationToken);

            if (!result.IsSuccess || result.Value == null)
            {
                await botClient.EditMessageTextAsync(
                    chatId: callbackQuery.Message!.Chat.Id,
                    messageId: callbackQuery.Message.MessageId,
                    text: $"❌ Помилка завантаження звернення: {result.Error ?? "Звернення не знайдено"}",
                    replyMarkup: GetBackToMainMenu(),
                    cancellationToken: cancellationToken);
                return;
            }

            var appeal = result.Value;
            var statusIcon = appeal.Status switch
            {
                AppealStatus.New => "🆕",
                AppealStatus.InProgress => "⏳", 
                AppealStatus.Closed => "✅",
                _ => "❓"
            };

            var messageText = $"📄 <b>Звернення #{appeal.Id}</b>\n\n" +
                             $"{statusIcon} <b>Статус:</b> {appeal.Status}\n" +
                             $"📁 <b>Категорія:</b> {appeal.Category}\n" +
                             $"📝 <b>Тема:</b> {appeal.Subject}\n" +
                             $"📅 <b>Створено:</b> {appeal.CreatedAt:dd.MM.yyyy HH:mm}\n\n" +
                             $"<b>Повідомлення:</b>\n{appeal.Message}";

            await botClient.EditMessageTextAsync(
                chatId: callbackQuery.Message!.Chat.Id,
                messageId: callbackQuery.Message.MessageId,
                text: messageText,
                parseMode: ParseMode.Html,
                replyMarkup: GetBackToMainMenu(),
                cancellationToken: cancellationToken);

            await botClient.AnswerCallbackQueryAsync(
                callbackQueryId: callbackQuery.Id,
                text: $"Звернення #{appealId}",
                cancellationToken: cancellationToken);

            _logger.LogInformation("Користувач переглянув звернення #{AppealId}", appealId);
        }
        catch (Exception ex)
        {
            await HandleErrorAsync(
                botClient,
                callbackQuery.Message!.Chat.Id,
                ex,
                "HandleAppealViewCallbackAsync",
                cancellationToken);
        }
    }

    #region Private Methods

    /// <summary>
    /// Обробляє введення теми звернення
    /// </summary>
    private async Task HandleAppealSubjectInputAsync(
        ITelegramBotClient botClient,
        Message message,
        CancellationToken cancellationToken)
    {
        try
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
            using var scope = _scopeFactory.CreateScope();
            var stateManager = scope.ServiceProvider.GetRequiredService<IUserStateManager>();
            await stateManager.SetDataAsync(userId, "appeal_subject", subject, cancellationToken);

            // Переходимо до наступного кроку
            await stateManager.SetStateAsync(userId, UserConversationState.WaitingAppealMessage, cancellationToken);

            await botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: $"📝 Тема збережена: <b>{subject}</b>\n\n" +
                      "📖 Тепер опишіть вашу проблему детально.\n\n" +
                      "<i>Мінімум 10 символів, максимум 2000 символів.</i>\n\n" +
                      "💡 Для скасування натисніть кнопку нижче",
                parseMode: ParseMode.Html,
                replyMarkup: await GetCancelKeyboardAsync(userId, cancellationToken),
                cancellationToken: cancellationToken);

            _logger.LogInformation("Користувач {UserId} ввів тему звернення: {Subject}", userId, subject);
        }
        catch (Exception ex)
        {
            await HandleErrorAsync(
                botClient,
                message.Chat.Id,
                ex,
                "HandleAppealSubjectInputAsync",
                cancellationToken);
        }
    }

    /// <summary>
    /// Обробляє введення повідомлення звернення та створює звернення
    /// </summary>
    private async Task HandleAppealMessageInputAsync(
        ITelegramBotClient botClient,
        Message message,
        CancellationToken cancellationToken)
    {
        try
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
            using var scope = _scopeFactory.CreateScope();
            var stateManager = scope.ServiceProvider.GetRequiredService<IUserStateManager>();
            var rateLimiter = scope.ServiceProvider.GetRequiredService<IRateLimiter>();

            // Перевіряємо rate limiting для створення звернень
            if (!await rateLimiter.AllowAsync(userId, "CreateAppeal", cancellationToken))
            {
                var remainingTime = await rateLimiter.GetTimeUntilResetAsync(userId, "CreateAppeal", cancellationToken);
                var waitMessage = remainingTime.HasValue 
                    ? $"⏳ Ви можете створити звернення лише раз на 10 хвилин.\nНаступне звернення можна створити через {remainingTime.Value.TotalMinutes:F0} хвилин."
                    : "⏳ Ви можете створити звернення лише раз на 10 хвилин. Спробуйте пізніше.";
                    
                await botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: waitMessage,
                    replyMarkup: GetMainMenu(),
                    cancellationToken: cancellationToken);

                await ClearUserStateAsync(userId, cancellationToken);
                return;
            }
            var category = await stateManager.GetDataAsync<AppealCategory>(userId, "appeal_category", cancellationToken);
            var subject = await stateManager.GetDataAsync<string>(userId, "appeal_subject", cancellationToken);

            if (string.IsNullOrEmpty(subject))
            {
                await SendErrorMessageAsync(
                    botClient,
                    message.Chat.Id,
                    "Помилка: дані звернення втрачено. Почніть спочатку /appeal",
                    cancellationToken);

                await ClearUserStateAsync(userId, cancellationToken);
                return;
            }

            // Створюємо звернення через MediatR
            var command = new CreateAppealCommand
            {
                StudentId = userId,
                StudentName = $"{message.From.FirstName} {message.From.LastName}".Trim(),
                Category = category,
                Subject = subject,
                Message = messageText
            };

            var result = await _mediator.Send(command, cancellationToken);

            if (result.IsSuccess)
            {
                await botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: $"✅ <b>Звернення успішно створено!</b>\n\n" +
                          $"🆔 Номер: #{result.Value!.Id}\n" +
                          $"📁 Категорія: {result.Value.Category}\n" +
                          $"📝 Тема: {result.Value.Subject}\n" +
                          $"📊 Статус: {result.Value.Status}\n\n" +
                          $"Ми розглянемо ваше звернення найближчим часом.\n" +
                          $"Ви отримаєте сповіщення про зміну статусу.",
                    parseMode: ParseMode.Html,
                    replyMarkup: GetBackToMainMenu(),
                    cancellationToken: cancellationToken);

                _logger.LogInformation("Створено нове звернення #{AppealId} від користувача {UserId}", result.Value.Id, userId);
            }
            else
            {
                await SendErrorMessageAsync(
                    botClient,
                    message.Chat.Id,
                    $"Помилка при створенні звернення:\n{result.Error}\n\nСпробуйте ще раз /appeal",
                    cancellationToken);
            }

            // Очищення стану та даних
            await ClearUserStateAsync(userId, cancellationToken);
        }
        catch (Exception ex)
        {
            await HandleErrorAsync(
                botClient,
                message.Chat.Id,
                ex,
                "HandleAppealMessageInputAsync",
                cancellationToken);

            await ClearUserStateAsync(message.From!.Id, cancellationToken);
        }
    }

    /// <summary>
    /// Обробляє створення нового звернення
    /// </summary>
    public async Task HandleAppealCreateCallback(ITelegramBotClient botClient, CallbackQuery callbackQuery, CancellationToken cancellationToken)
    {
        try
        {
            await botClient.EditMessageTextAsync(
                chatId: callbackQuery.Message!.Chat.Id,
                messageId: callbackQuery.Message.MessageId,
                text: "📩 <b>Створення звернення</b>\n\n" +
                      "Оберіть категорію вашого звернення:",
                parseMode: ParseMode.Html,
                replyMarkup: GetAppealCategories(),
                cancellationToken: cancellationToken);

            await botClient.AnswerCallbackQueryAsync(
                callbackQueryId: callbackQuery.Id,
                cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Помилка в HandleAppealCreateCallback");
            
            await botClient.AnswerCallbackQueryAsync(
                callbackQueryId: callbackQuery.Id,
                text: "❌ Виникла помилка. Спробуйте пізніше.",
                showAlert: true,
                cancellationToken: cancellationToken);
        }
    }

    /// <summary>
    /// Відображає меню вибору фільтрів для звернень
    /// </summary>
    public async Task HandleAppealsFiltersMenuCallback(
        ITelegramBotClient botClient,
        CallbackQuery callbackQuery,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = callbackQuery.From.Id;
            
            var buttons = new List<List<InlineKeyboardButton>>
            {
                new List<InlineKeyboardButton>
                {
                    InlineKeyboardButton.WithCallbackData("📊 Фільтр за статусом", "appeals_filter_status_menu")
                },
                new List<InlineKeyboardButton>
                {
                    InlineKeyboardButton.WithCallbackData("📂 Фільтр за категорією", "appeals_filter_category_menu")
                },
                new List<InlineKeyboardButton>
                {
                    InlineKeyboardButton.WithCallbackData("🔄 Скинути всі фільтри", "appeals_filter_clear_all")
                },
                new List<InlineKeyboardButton>
                {
                    InlineKeyboardButton.WithCallbackData("🔙 Назад до списку", "my_appeals")
                }
            };

            await botClient.EditMessageTextAsync(
                chatId: callbackQuery.Message!.Chat.Id,
                messageId: callbackQuery.Message.MessageId,
                text: "🔍 <b>Фільтри звернень</b>\n\nОберіть тип фільтру:",
                parseMode: ParseMode.Html,
                replyMarkup: new InlineKeyboardMarkup(buttons),
                cancellationToken: cancellationToken);

            await botClient.AnswerCallbackQueryAsync(
                callbackQueryId: callbackQuery.Id,
                cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Помилка при відображенні меню фільтрів звернень для користувача {UserId}", callbackQuery.From.Id);
        }
    }

    /// <summary>
    /// Відображає меню вибору статусу для фільтрації
    /// </summary>
    public async Task HandleAppealsFilterStatusMenuCallback(
        ITelegramBotClient botClient,
        CallbackQuery callbackQuery,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = callbackQuery.From.Id;
            _appealStatusFilters.TryGetValue(userId, out var selectedStatus);

            var keyboard = FilterHelper.CreateAppealStatusFilterKeyboard(
                selectedStatus,
                backCallbackData: "appeals_filters_menu");

            await botClient.EditMessageTextAsync(
                chatId: callbackQuery.Message!.Chat.Id,
                messageId: callbackQuery.Message.MessageId,
                text: "🔍 <b>Фільтр за статусом</b>\n\nОберіть статус звернення:",
                parseMode: ParseMode.Html,
                replyMarkup: keyboard,
                cancellationToken: cancellationToken);

            await botClient.AnswerCallbackQueryAsync(
                callbackQueryId: callbackQuery.Id,
                cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Помилка при відображенні фільтру статусів для користувача {UserId}", callbackQuery.From.Id);
        }
    }

    /// <summary>
    /// Відображає меню вибору категорії для фільтрації
    /// </summary>
    public async Task HandleAppealsFilterCategoryMenuCallback(
        ITelegramBotClient botClient,
        CallbackQuery callbackQuery,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = callbackQuery.From.Id;
            _appealCategoryFilters.TryGetValue(userId, out var selectedCategory);

            var keyboard = FilterHelper.CreateAppealCategoryFilterKeyboard(
                selectedCategory,
                backCallbackData: "appeals_filters_menu");

            await botClient.EditMessageTextAsync(
                chatId: callbackQuery.Message!.Chat.Id,
                messageId: callbackQuery.Message.MessageId,
                text: "🔍 <b>Фільтр за категорією</b>\n\nОберіть категорію звернення:",
                parseMode: ParseMode.Html,
                replyMarkup: keyboard,
                cancellationToken: cancellationToken);

            await botClient.AnswerCallbackQueryAsync(
                callbackQueryId: callbackQuery.Id,
                cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Помилка при відображенні фільтру категорій для користувача {UserId}", callbackQuery.From.Id);
        }
    }

    /// <summary>
    /// Застосовує фільтр статусу звернення
    /// </summary>
    public async Task HandleAppealsFilterStatusCallback(
        ITelegramBotClient botClient,
        CallbackQuery callbackQuery,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = callbackQuery.From.Id;
            var status = FilterHelper.ParseAppealStatusFromCallback(callbackQuery.Data!);

            if (status.HasValue)
            {
                _appealStatusFilters[userId] = status.Value;
                await botClient.AnswerCallbackQueryAsync(
                    callbackQueryId: callbackQuery.Id,
                    text: $"✅ Фільтр: {status.Value.GetDisplayName()}",
                    cancellationToken: cancellationToken);
            }

            // Повертаємось до списку звернень з застосованим фільтром
            await HandleMyAppealsCallbackAsync(botClient, callbackQuery, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Помилка при застосуванні фільтру статусу для користувача {UserId}", callbackQuery.From.Id);
        }
    }

    /// <summary>
    /// Застосовує фільтр категорії звернення
    /// </summary>
    public async Task HandleAppealsFilterCategoryCallback(
        ITelegramBotClient botClient,
        CallbackQuery callbackQuery,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = callbackQuery.From.Id;
            var category = FilterHelper.ParseAppealCategoryFromCallback(callbackQuery.Data!);

            if (category.HasValue)
            {
                _appealCategoryFilters[userId] = category.Value;
                await botClient.AnswerCallbackQueryAsync(
                    callbackQueryId: callbackQuery.Id,
                    text: $"✅ Фільтр: {category.Value.GetDisplayName()}",
                    cancellationToken: cancellationToken);
            }

            // Повертаємось до списку звернень з застосованим фільтром
            await HandleMyAppealsCallbackAsync(botClient, callbackQuery, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Помилка при застосуванні фільтру категорії для користувача {UserId}", callbackQuery.From.Id);
        }
    }

    /// <summary>
    /// Очищає фільтр статусу звернень
    /// </summary>
    public async Task HandleAppealsClearStatusFilterCallback(
        ITelegramBotClient botClient,
        CallbackQuery callbackQuery,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = callbackQuery.From.Id;
            _appealStatusFilters.Remove(userId);

            await botClient.AnswerCallbackQueryAsync(
                callbackQueryId: callbackQuery.Id,
                text: "✅ Фільтр статусу скинуто",
                cancellationToken: cancellationToken);

            // Повертаємось до списку звернень
            await HandleMyAppealsCallbackAsync(botClient, callbackQuery, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Помилка при скиданні фільтру статусу для користувача {UserId}", callbackQuery.From.Id);
        }
    }

    /// <summary>
    /// Очищає фільтр категорії звернень
    /// </summary>
    public async Task HandleAppealsClearCategoryFilterCallback(
        ITelegramBotClient botClient,
        CallbackQuery callbackQuery,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = callbackQuery.From.Id;
            _appealCategoryFilters.Remove(userId);

            await botClient.AnswerCallbackQueryAsync(
                callbackQueryId: callbackQuery.Id,
                text: "✅ Фільтр категорії скинуто",
                cancellationToken: cancellationToken);

            // Повертаємось до списку звернень
            await HandleMyAppealsCallbackAsync(botClient, callbackQuery, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Помилка при скиданні фільтру категорії для користувача {UserId}", callbackQuery.From.Id);
        }
    }

    /// <summary>
    /// Очищає всі фільтри звернень
    /// </summary>
    public async Task HandleAppealsClearAllFiltersCallback(
        ITelegramBotClient botClient,
        CallbackQuery callbackQuery,
        CancellationToken cancellationToken)
    {
        try
        {
            var userId = callbackQuery.From.Id;
            _appealStatusFilters.Remove(userId);
            _appealCategoryFilters.Remove(userId);

            await botClient.AnswerCallbackQueryAsync(
                callbackQueryId: callbackQuery.Id,
                text: "✅ Всі фільтри скинуто",
                cancellationToken: cancellationToken);

            // Повертаємось до списку звернень
            await HandleMyAppealsCallbackAsync(botClient, callbackQuery, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Помилка при скиданні всіх фільтрів для користувача {UserId}", callbackQuery.From.Id);
        }
    }

    #endregion
}