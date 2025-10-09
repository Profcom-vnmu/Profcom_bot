using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MediatR;
using StudentUnionBot.Application.Common.Interfaces;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using StudentUnionBot.Application.News.Queries.GetPublishedNews;
using StudentUnionBot.Application.Events.Queries.GetUpcomingEvents;
using StudentUnionBot.Application.Events.Queries.GetEventById;
using StudentUnionBot.Application.Events.Commands.RegisterForEvent;
using StudentUnionBot.Application.Events.Commands.UnregisterFromEvent;
using StudentUnionBot.Application.Partners.Queries.GetActivePartners;
using StudentUnionBot.Application.Contacts.Queries.GetAllContacts;
using StudentUnionBot.Domain.Enums;
using StudentUnionBot.Presentation.Bot.Handlers.Common;
using StudentUnionBot.Presentation.Bot.Handlers.Interfaces;

namespace StudentUnionBot.Presentation.Bot.Handlers.Content;

public class ContentHandler : BaseHandler, IContentHandler
{
    public ContentHandler(IServiceScopeFactory scopeFactory, ILogger<ContentHandler> logger, IMediator mediator)
        : base(scopeFactory, logger, mediator)
    {
    }

    /// <summary>
    /// Обробляє текстові повідомлення (ContentHandler не обробляє текстові повідомлення)
    /// </summary>
    public override async Task HandleTextMessageAsync(ITelegramBotClient botClient, Message message, UserConversationState state, CancellationToken cancellationToken)
    {
        // ContentHandler не обробляє текстові повідомлення
        await Task.CompletedTask;
    }

    /// <summary>
    /// Обробка callback'у для списку новин
    /// </summary>
    public async Task HandleNewsListCallback(
        ITelegramBotClient botClient,
        CallbackQuery callbackQuery,
        CancellationToken cancellationToken)
    {
        try
        {
            // Отримуємо новини через MediatR
            var query = new GetPublishedNewsQuery
            {
                PageNumber = 1,
                PageSize = 5
            };

            var result = await _mediator.Send(query, cancellationToken);

            if (!result.IsSuccess || result.Value == null)
            {
                await botClient.EditMessageTextAsync(
                    chatId: callbackQuery.Message!.Chat.Id,
                    messageId: callbackQuery.Message.MessageId,
                    text: "📰 <b>Новини</b>\n\n❌ Не вдалося завантажити новини. Спробуйте пізніше.",
                    parseMode: ParseMode.Html,
                    replyMarkup: GetBackToMainMenu(),
                    cancellationToken: cancellationToken);
                return;
            }

            var newsList = result.Value;
            if (newsList.Items.Count == 0)
            {
                await botClient.EditMessageTextAsync(
                    chatId: callbackQuery.Message!.Chat.Id,
                    messageId: callbackQuery.Message.MessageId,
                    text: "📰 <b>Новини</b>\n\n📝 Поки що немає опублікованих новин.",
                    parseMode: ParseMode.Html,
                    replyMarkup: GetBackToMainMenu(),
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
                replyMarkup: GetBackToMainMenu(),
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
                replyMarkup: GetBackToMainMenu(),
                cancellationToken: cancellationToken);
        }
    }

    /// <summary>
    /// Обробка callback'у для списку подій
    /// </summary>
    public async Task HandleEventsListCallback(
        ITelegramBotClient botClient,
        CallbackQuery callbackQuery,
        CancellationToken cancellationToken)
    {
        try
        {
            // Отримуємо майбутні події через MediatR
            var query = new GetUpcomingEventsQuery
            {
                PageNumber = 1,
                PageSize = 5
            };

            var result = await _mediator.Send(query, cancellationToken);

            if (!result.IsSuccess || result.Value == null)
            {
                await botClient.EditMessageTextAsync(
                    chatId: callbackQuery.Message!.Chat.Id,
                    messageId: callbackQuery.Message.MessageId,
                    text: "🎫 <b>Заходи</b>\n\n❌ Не вдалося завантажити заходи. Спробуйте пізніше.",
                    parseMode: ParseMode.Html,
                    replyMarkup: GetBackToMainMenu(),
                    cancellationToken: cancellationToken);
                return;
            }

            var eventsList = result.Value;
            if (eventsList.Items.Count == 0)
            {
                await botClient.EditMessageTextAsync(
                    chatId: callbackQuery.Message!.Chat.Id,
                    messageId: callbackQuery.Message.MessageId,
                    text: "🎫 <b>Заходи</b>\n\n📝 Наразі немає запланованих подій.\n\n" +
                          "<i>Слідкуйте за оновленнями!</i>",
                    parseMode: ParseMode.Html,
                    replyMarkup: GetBackToMainMenu(),
                    cancellationToken: cancellationToken);
                return;
            }

            // Формуємо текст з подіями
            var eventsText = "🎫 <b>Майбутні заходи</b>\n\n";
            
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
                replyMarkup: GetBackToMainMenu(),
                cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Помилка при отриманні подій для користувача {UserId}", callbackQuery.From.Id);
            
            await botClient.EditMessageTextAsync(
                chatId: callbackQuery.Message!.Chat.Id,
                messageId: callbackQuery.Message.MessageId,
                text: "🎫 <b>Заходи</b>\n\n❌ Виникла помилка при завантаженні подій.",
                parseMode: ParseMode.Html,
                replyMarkup: GetBackToMainMenu(),
                cancellationToken: cancellationToken);
        }
    }

    /// <summary>
    /// Обробка callback'у для списку партнерів
    /// </summary>
    public async Task HandlePartnersListCallback(
        ITelegramBotClient botClient,
        CallbackQuery callbackQuery,
        CancellationToken cancellationToken)
    {
        try
        {
            // Отримуємо партнерів через MediatR
            var query = new GetActivePartnersQuery();
            var result = await _mediator.Send(query, cancellationToken);

            if (!result.IsSuccess || result.Value == null)
            {
                await botClient.EditMessageTextAsync(
                    chatId: callbackQuery.Message!.Chat.Id,
                    messageId: callbackQuery.Message.MessageId,
                    text: "🤝 <b>Партнери</b>\n\n❌ Не вдалося завантажити партнерів. Спробуйте пізніше.",
                    parseMode: ParseMode.Html,
                    replyMarkup: GetBackToMainMenu(),
                    cancellationToken: cancellationToken);
                return;
            }

            var partnersList = result.Value;
            if (partnersList.Items.Count == 0)
            {
                await botClient.EditMessageTextAsync(
                    chatId: callbackQuery.Message!.Chat.Id,
                    messageId: callbackQuery.Message.MessageId,
                    text: "🤝 <b>Партнери</b>\n\n📝 Наразі немає активних партнерів.\n\n" +
                          "<i>Ми працюємо над новими партнерствами!</i>",
                    parseMode: ParseMode.Html,
                    replyMarkup: GetBackToMainMenu(),
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
                    partnersText += $"🎯 <b>Знижка:</b> {partner.DiscountInfo}\n";
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
                replyMarkup: GetBackToMainMenu(),
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
                replyMarkup: GetBackToMainMenu(),
                cancellationToken: cancellationToken);
        }
    }

    /// <summary>
    /// Обробка callback'у для списку контактів
    /// </summary>
    public async Task HandleContactsListCallback(
        ITelegramBotClient botClient,
        CallbackQuery callbackQuery,
        CancellationToken cancellationToken)
    {
        try
        {
            // Отримуємо контакти через MediatR
            var query = new GetAllContactsQuery();
            var result = await _mediator.Send(query, cancellationToken);

            if (!result.IsSuccess || result.Value == null)
            {
                await botClient.EditMessageTextAsync(
                    chatId: callbackQuery.Message!.Chat.Id,
                    messageId: callbackQuery.Message.MessageId,
                    text: "📞 <b>Контакти</b>\n\n❌ Не вдалося завантажити контакти. Спробуйте пізніше.",
                    parseMode: ParseMode.Html,
                    replyMarkup: GetBackToMainMenu(),
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
                          "🏢 <b>Студентський профспілковий комітет</b>\n\n" +
                          "📧 Email: profkom@vnmu.edu.ua\n" +
                          "📱 Telegram: @vnmu_profkom\n" +
                          "📍 Адреса: вул. Пирогова, 56, Вінниця\n" +
                          "🕒 Години роботи: ПН-ПТ 9:00-17:00",
                    parseMode: ParseMode.Html,
                    replyMarkup: GetBackToMainMenu(),
                    cancellationToken: cancellationToken);
                return;
            }

            // Формуємо текст з контактами
            var contactsText = "📞 <b>Контактна інформація</b>\n\n";
            
            foreach (var contact in contactsList.Items)
            {
                contactsText += $"🏢 <b>{contact.Title}</b>\n";
                
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
                    contactsText += $"🕒 {contact.WorkingHours}\n";
                }
                
                contactsText += "\n";
            }

            await botClient.EditMessageTextAsync(
                chatId: callbackQuery.Message!.Chat.Id,
                messageId: callbackQuery.Message.MessageId,
                text: contactsText.TrimEnd(),
                parseMode: ParseMode.Html,
                replyMarkup: GetBackToMainMenu(),
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
                replyMarkup: GetBackToMainMenu(),
                cancellationToken: cancellationToken);
        }
    }

    /// <summary>
    /// Обробка callback'у для деталей події
    /// </summary>
    public async Task HandleEventDetailsCallback(
        ITelegramBotClient botClient,
        CallbackQuery callbackQuery,
        CancellationToken cancellationToken)
    {
        var eventIdStr = callbackQuery.Data!.Replace("event_details_", "");
        if (!int.TryParse(eventIdStr, out var eventId))
        {
            await botClient.AnswerCallbackQueryAsync(
                callbackQuery.Id,
                "❌ Невірний ID події",
                showAlert: true,
                cancellationToken: cancellationToken);
            return;
        }

        try
        {
            var query = new GetEventByIdQuery(eventId, callbackQuery.From.Id);
            var result = await _mediator.Send(query, cancellationToken);

            if (!result.IsSuccess || result.Value == null)
            {
                await botClient.AnswerCallbackQueryAsync(
                    callbackQuery.Id,
                    "❌ Подію не знайдено",
                    showAlert: true,
                    cancellationToken: cancellationToken);
                return;
            }

            var ev = result.Value;
            
            var text = $"{ev.TypeEmoji} <b>{ev.Title}</b>\n\n";
            text += $"<b>Опис:</b>\n{ev.Description}\n\n";
            text += $"📅 <b>Початок:</b> {ev.StartDate:dd.MM.yyyy HH:mm}\n";
            
            if (ev.EndDate.HasValue)
            {
                text += $"🏁 <b>Завершення:</b> {ev.EndDate.Value:dd.MM.yyyy HH:mm}\n";
            }
            
            if (!string.IsNullOrEmpty(ev.Location))
            {
                text += $"📍 <b>Місце:</b> {ev.Location}\n";
            }
            
            text += $"\n📋 <b>Тип:</b> {ev.TypeDisplayName}\n";
            text += $"🏷️ <b>Статус:</b> {ev.Status.GetDisplayName()}\n";
            
            if (ev.RequiresRegistration)
            {
                text += $"\n👥 <b>Реєстрація:</b>\n";
                text += $"• Зареєстровано: {ev.CurrentParticipants}";
                
                if (ev.MaxParticipants.HasValue)
                {
                    var spotsLeft = ev.MaxParticipants.Value - ev.CurrentParticipants;
                    text += $" / {ev.MaxParticipants.Value}\n";
                    text += $"• Вільних місць: {spotsLeft}\n";
                }
                else
                {
                    text += " (без обмежень)\n";
                }
                
                if (ev.RegistrationDeadline.HasValue)
                {
                    text += $"⏰ <b>Реєстрація до:</b> {ev.RegistrationDeadline.Value:dd.MM.yyyy HH:mm}\n";
                }
                
                if (ev.IsUserRegistered)
                {
                    text += "\n✅ <b>Ви зареєстровані на цю подію</b>";
                }
            }

            var buttons = new List<InlineKeyboardButton[]>();
            
            if (ev.RequiresRegistration)
            {
                if (ev.IsUserRegistered)
                {
                    buttons.Add(new[]
                    {
                        InlineKeyboardButton.WithCallbackData("❌ Скасувати реєстрацію", $"event_unregister_{eventId}")
                    });
                }
                else if (ev.CanRegister)
                {
                    buttons.Add(new[]
                    {
                        InlineKeyboardButton.WithCallbackData("✅ Зареєструватися", $"event_register_{eventId}")
                    });
                }
                else
                {
                    text += "\n\n⚠️ <i>Реєстрація недоступна (немає місць або минув дедлайн)</i>";
                }
            }
            
            buttons.Add(new[]
            {
                InlineKeyboardButton.WithCallbackData("🔙 До списку подій", "events_list")
            });

            var keyboard = new InlineKeyboardMarkup(buttons);

            await botClient.EditMessageTextAsync(
                chatId: callbackQuery.Message!.Chat.Id,
                messageId: callbackQuery.Message.MessageId,
                text: text,
                parseMode: ParseMode.Html,
                replyMarkup: keyboard,
                cancellationToken: cancellationToken);

            await botClient.AnswerCallbackQueryAsync(
                callbackQuery.Id,
                cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Помилка при отриманні деталей події {EventId}", eventId);
            
            await botClient.AnswerCallbackQueryAsync(
                callbackQuery.Id,
                "❌ Виникла помилка",
                showAlert: true,
                cancellationToken: cancellationToken);
        }
    }

    /// <summary>
    /// Обробка callback'у для реєстрації на подію
    /// </summary>
    public async Task HandleEventRegisterCallback(
        ITelegramBotClient botClient,
        CallbackQuery callbackQuery,
        CancellationToken cancellationToken)
    {
        var eventIdStr = callbackQuery.Data!.Replace("event_register_", "");
        if (!int.TryParse(eventIdStr, out var eventId))
        {
            await botClient.AnswerCallbackQueryAsync(
                callbackQuery.Id,
                "❌ Невірний ID події",
                showAlert: true,
                cancellationToken: cancellationToken);
            return;
        }

        try
        {
            // Перевіряємо rate limiting для реєстрації на події
            using var scope = _scopeFactory.CreateScope();
            var rateLimiter = scope.ServiceProvider.GetRequiredService<IRateLimiter>();
            var userId = callbackQuery.From.Id;

            if (!await rateLimiter.AllowAsync(userId, "RegisterEvent", cancellationToken))
            {
                var remainingTime = await rateLimiter.GetTimeUntilResetAsync(userId, "RegisterEvent", cancellationToken);
                var waitMessage = remainingTime.HasValue 
                    ? $"⏳ Занадто багато реєстрацій! Спробуйте через {remainingTime.Value.TotalMinutes:F0} хвилин."
                    : "⏳ Занадто багато реєстрацій! Спробуйте пізніше.";
                    
                await botClient.AnswerCallbackQueryAsync(
                    callbackQuery.Id,
                    waitMessage,
                    showAlert: true,
                    cancellationToken: cancellationToken);
                return;
            }

            var command = new RegisterForEventCommand(callbackQuery.From.Id, eventId);
            var result = await _mediator.Send(command, cancellationToken);

            if (result.IsSuccess)
            {
                await botClient.AnswerCallbackQueryAsync(
                    callbackQuery.Id,
                    "✅ Ви успішно зареєструвалися на подію!",
                    showAlert: true,
                    cancellationToken: cancellationToken);
                
                // Оновлюємо деталі події
                await HandleEventDetailsCallback(botClient, new CallbackQuery 
                { 
                    Id = callbackQuery.Id,
                    From = callbackQuery.From,
                    Message = callbackQuery.Message,
                    Data = $"event_details_{eventId}"
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Помилка при реєстрації на подію {EventId}", eventId);
            
            await botClient.AnswerCallbackQueryAsync(
                callbackQuery.Id,
                "❌ Виникла помилка при реєстрації",
                showAlert: true,
                cancellationToken: cancellationToken);
        }
    }

    /// <summary>
    /// Обробка callback'у для скасування реєстрації на подію
    /// </summary>
    public async Task HandleEventUnregisterCallback(
        ITelegramBotClient botClient,
        CallbackQuery callbackQuery,
        CancellationToken cancellationToken)
    {
        var eventIdStr = callbackQuery.Data!.Replace("event_unregister_", "");
        if (!int.TryParse(eventIdStr, out var eventId))
        {
            await botClient.AnswerCallbackQueryAsync(
                callbackQuery.Id,
                "❌ Невірний ID події",
                showAlert: true,
                cancellationToken: cancellationToken);
            return;
        }

        try
        {
            var command = new UnregisterFromEventCommand(callbackQuery.From.Id, eventId);
            var result = await _mediator.Send(command, cancellationToken);

            if (result.IsSuccess)
            {
                await botClient.AnswerCallbackQueryAsync(
                    callbackQuery.Id,
                    "✅ Реєстрацію скасовано",
                    showAlert: true,
                    cancellationToken: cancellationToken);
                
                // Оновлюємо деталі події
                await HandleEventDetailsCallback(botClient, new CallbackQuery 
                { 
                    Id = callbackQuery.Id,
                    From = callbackQuery.From,
                    Message = callbackQuery.Message,
                    Data = $"event_details_{eventId}"
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Помилка при скасуванні реєстрації на подію {EventId}", eventId);
            
            await botClient.AnswerCallbackQueryAsync(
                callbackQuery.Id,
                "❌ Виникла помилка при скасуванні",
                showAlert: true,
                cancellationToken: cancellationToken);
        }
    }
}