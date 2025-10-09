using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StudentUnionBot.Application.Common.Interfaces;
using StudentUnionBot.Application.Events.Commands.CreateEvent;
using StudentUnionBot.Application.Events.Queries.GetAllEvents;
using StudentUnionBot.Application.Events.Queries.GetEventById;
using StudentUnionBot.Domain.Enums;
using StudentUnionBot.Presentation.Bot.Handlers.Common;
using StudentUnionBot.Presentation.Bot.Handlers.Interfaces;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace StudentUnionBot.Presentation.Bot.Handlers.Admin;

/// <summary>
/// Обробник управління подіями для адміністраторів
/// </summary>
public class EventsManagementHandler : BaseHandler, IEventsManagementHandler
{
    public EventsManagementHandler(
        IServiceScopeFactory scopeFactory,
        ILogger<EventsManagementHandler> logger,
        IMediator mediator)
        : base(scopeFactory, logger, mediator)
    {
    }

    public override async Task HandleTextMessageAsync(
        ITelegramBotClient botClient,
        Message message,
        UserConversationState state,
        CancellationToken cancellationToken)
    {
        var userId = message.From!.Id;

        switch (state)
        {
            case UserConversationState.CreatingEventTitle:
                await HandleEventTitle(botClient, message, cancellationToken);
                break;

            case UserConversationState.CreatingEventDescription:
                await HandleEventDescription(botClient, message, cancellationToken);
                break;

            case UserConversationState.CreatingEventLocation:
                await HandleEventLocation(botClient, message, cancellationToken);
                break;

            case UserConversationState.CreatingEventDateTime:
                await HandleEventDateTime(botClient, message, cancellationToken);
                break;

            default:
                await SendLocalizedErrorAsync(
                    botClient,
                    userId,
                    "error.unknown_state",
                    cancellationToken);
                break;
        }
    }

    /// <summary>
    /// Показати головне меню управління подіями
    /// </summary>
    public async Task ShowEventsManagementMenuAsync(
        ITelegramBotClient botClient,
        long chatId,
        CancellationToken cancellationToken)
    {
        var menuText = await GetLocalizedTextForUserAsync(chatId, "admin.events_management_menu", cancellationToken);
        var keyboard = await GetEventsManagementKeyboard(chatId, cancellationToken);

        await botClient.SendTextMessageAsync(
            chatId: chatId,
            text: menuText,
            parseMode: ParseMode.Html,
            replyMarkup: keyboard,
            cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Показати список подій
    /// </summary>
    public async Task ShowEventsListAsync(
        ITelegramBotClient botClient,
        long chatId,
        int page = 1,
        EventStatus? status = null,
        EventType? type = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var query = new GetAllEventsQuery
            {
                PageNumber = page,
                PageSize = 5,
                Status = status,
                Type = type
            };

            var result = await _mediator.Send(query, cancellationToken);

            if (!result.IsSuccess || result.Value == null)
            {
                await SendLocalizedErrorAsync(
                    botClient,
                    chatId,
                    "admin.events_load_error",
                    cancellationToken);
                return;
            }

            var eventsList = result.Value;
            var messageText = await BuildEventsListText(chatId, eventsList, cancellationToken);
            var keyboard = await BuildEventsListKeyboard(chatId, eventsList, status, type, cancellationToken);

            await botClient.SendTextMessageAsync(
                chatId: chatId,
                text: messageText,
                parseMode: ParseMode.Html,
                replyMarkup: keyboard,
                cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            await HandleErrorAsync(botClient, chatId, ex, "ShowEventsListAsync", cancellationToken);
        }
    }

    /// <summary>
    /// Почати створення події
    /// </summary>
    public async Task StartCreateEventAsync(
        ITelegramBotClient botClient,
        long chatId,
        CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var stateManager = scope.ServiceProvider.GetRequiredService<IUserStateManager>();

        await stateManager.SetStateAsync(chatId, UserConversationState.CreatingEventTitle, cancellationToken);

        var promptText = await GetLocalizedTextForUserAsync(chatId, "admin.event_create_title_prompt", cancellationToken);

        await botClient.SendTextMessageAsync(
            chatId: chatId,
            text: promptText,
            parseMode: ParseMode.Html,
            replyMarkup: await GetBackToMainMenuAsync(chatId, cancellationToken),
            cancellationToken: cancellationToken);
    }

    #region Private Helper Methods

    /// <summary>
    /// Отримати клавіатуру управління подіями
    /// </summary>
    private async Task<InlineKeyboardMarkup> GetEventsManagementKeyboard(long userId, CancellationToken cancellationToken)
    {
        var buttons = new List<List<InlineKeyboardButton>>
        {
            new()
            {
                InlineKeyboardButton.WithCallbackData(
                    await GetLocalizedTextForUserAsync(userId, "button.create_event", cancellationToken),
                    "events_create")
            },
            new()
            {
                InlineKeyboardButton.WithCallbackData(
                    await GetLocalizedTextForUserAsync(userId, "button.all_events", cancellationToken),
                    "events_list_all")
            },
            new()
            {
                InlineKeyboardButton.WithCallbackData(
                    await GetLocalizedTextForUserAsync(userId, "button.draft_events", cancellationToken),
                    "events_list_draft"),
                InlineKeyboardButton.WithCallbackData(
                    await GetLocalizedTextForUserAsync(userId, "button.planned_events", cancellationToken),
                    "events_list_planned")
            },
            new()
            {
                InlineKeyboardButton.WithCallbackData(
                    await GetLocalizedTextForUserAsync(userId, "button.completed_events", cancellationToken),
                    "events_list_completed")
            },
            new()
            {
                InlineKeyboardButton.WithCallbackData(
                    await GetLocalizedTextForUserAsync(userId, "button.back_to_admin", cancellationToken),
                    "admin_panel")
            }
        };

        return new InlineKeyboardMarkup(buttons);
    }

    /// <summary>
    /// Обробити введення назви події
    /// </summary>
    private async Task HandleEventTitle(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
    {
        var userId = message.From!.Id;
        var title = message.Text?.Trim();

        if (string.IsNullOrWhiteSpace(title))
        {
            await SendLocalizedErrorAsync(botClient, userId, "validation.event_title_empty", cancellationToken);
            return;
        }

        if (title.Length < 5 || title.Length > 100)
        {
            await SendLocalizedErrorAsync(botClient, userId, "validation.event_title_length", cancellationToken, new object[] { 5, 100 });
            return;
        }

        // Зберігаємо назву
        using var scope = _scopeFactory.CreateScope();
        var stateManager = scope.ServiceProvider.GetRequiredService<IUserStateManager>();
        await stateManager.SetDataAsync(userId, "event_title", title, cancellationToken);
        await stateManager.SetStateAsync(userId, UserConversationState.CreatingEventDescription, cancellationToken);

        var promptText = await GetLocalizedTextForUserAsync(userId, "admin.event_create_description_prompt", cancellationToken);

        await botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: promptText,
            parseMode: ParseMode.Html,
            replyMarkup: await GetBackToMainMenuAsync(userId, cancellationToken),
            cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Обробити введення опису події
    /// </summary>
    private async Task HandleEventDescription(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
    {
        var userId = message.From!.Id;
        var description = message.Text?.Trim();

        if (string.IsNullOrWhiteSpace(description))
        {
            await SendLocalizedErrorAsync(botClient, userId, "validation.event_description_empty", cancellationToken);
            return;
        }

        if (description.Length < 20)
        {
            await SendLocalizedErrorAsync(botClient, userId, "validation.event_description_short", cancellationToken, new object[] { 20 });
            return;
        }

        // Зберігаємо опис
        using var scope = _scopeFactory.CreateScope();
        var stateManager = scope.ServiceProvider.GetRequiredService<IUserStateManager>();
        await stateManager.SetDataAsync(userId, "event_description", description, cancellationToken);
        await stateManager.SetStateAsync(userId, UserConversationState.CreatingEventLocation, cancellationToken);

        var promptText = await GetLocalizedTextForUserAsync(userId, "admin.event_create_location_prompt", cancellationToken);

        await botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: promptText,
            parseMode: ParseMode.Html,
            replyMarkup: await GetBackToMainMenuAsync(userId, cancellationToken),
            cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Обробити введення місця події
    /// </summary>
    private async Task HandleEventLocation(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
    {
        var userId = message.From!.Id;
        var location = message.Text?.Trim();

        if (string.IsNullOrWhiteSpace(location))
        {
            await SendLocalizedErrorAsync(botClient, userId, "validation.event_location_empty", cancellationToken);
            return;
        }

        // Зберігаємо місце
        using var scope = _scopeFactory.CreateScope();
        var stateManager = scope.ServiceProvider.GetRequiredService<IUserStateManager>();
        await stateManager.SetDataAsync(userId, "event_location", location, cancellationToken);
        await stateManager.SetStateAsync(userId, UserConversationState.CreatingEventDateTime, cancellationToken);

        var promptText = await GetLocalizedTextForUserAsync(userId, "admin.event_create_datetime_prompt", cancellationToken);

        await botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: promptText,
            parseMode: ParseMode.Html,
            replyMarkup: await GetBackToMainMenuAsync(userId, cancellationToken),
            cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Обробити введення дати та часу події
    /// </summary>
    private async Task HandleEventDateTime(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
    {
        var userId = message.From!.Id;
        var dateTimeStr = message.Text?.Trim();

        if (string.IsNullOrWhiteSpace(dateTimeStr))
        {
            await SendLocalizedErrorAsync(botClient, userId, "validation.event_datetime_empty", cancellationToken);
            return;
        }

        // Спроба парсингу дати
        if (!DateTime.TryParse(dateTimeStr, out var eventDateTime))
        {
            await SendLocalizedErrorAsync(botClient, userId, "validation.event_datetime_invalid", cancellationToken);
            return;
        }

        if (eventDateTime <= DateTime.Now)
        {
            await SendLocalizedErrorAsync(botClient, userId, "validation.event_datetime_past", cancellationToken);
            return;
        }

        // Отримуємо збережені дані та створюємо подію
        using var scope = _scopeFactory.CreateScope();
        var stateManager = scope.ServiceProvider.GetRequiredService<IUserStateManager>();
        
        var title = await stateManager.GetDataAsync<string>(userId, "event_title", cancellationToken);
        var description = await stateManager.GetDataAsync<string>(userId, "event_description", cancellationToken);
        var location = await stateManager.GetDataAsync<string>(userId, "event_location", cancellationToken);

        if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(description) || string.IsNullOrWhiteSpace(location))
        {
            await SendLocalizedErrorAsync(botClient, userId, "error.session_expired", cancellationToken);
            await ClearUserStateAsync(userId, cancellationToken);
            return;
        }

        // Створюємо подію
        var createCommand = new CreateEventCommand
        {
            Title = title,
            Description = description,
            Location = location,
            EventDate = eventDateTime,
            Type = EventType.Other,
            Category = EventCategory.Other,
            OrganizerId = userId
        };

        var result = await _mediator.Send(createCommand, cancellationToken);

        await ClearUserStateAsync(userId, cancellationToken);

        if (result.IsSuccess)
        {
            var successText = await GetLocalizedTextForUserAsync(
                userId, 
                "admin.event_created_success", 
                cancellationToken, 
                title, eventDateTime.ToString("dd.MM.yyyy HH:mm"));

            await botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: successText,
                parseMode: ParseMode.Html,
                replyMarkup: await GetBackToMainMenuAsync(userId, cancellationToken),
                cancellationToken: cancellationToken);
        }
        else
        {
            await SendLocalizedErrorAsync(botClient, userId, "admin.event_create_error", cancellationToken);
        }
    }

    /// <summary>
    /// Створити текст списку подій
    /// </summary>
    private async Task<string> BuildEventsListText(long userId, Application.Events.DTOs.EventListDto eventsList, CancellationToken cancellationToken)
    {
        var headerText = await GetLocalizedTextForUserAsync(
            userId, 
            "admin.events_list_header", 
            cancellationToken, 
            eventsList.TotalCount, 
            eventsList.PageNumber, 
            eventsList.TotalPages);

        if (!eventsList.Items.Any())
        {
            var emptyText = await GetLocalizedTextForUserAsync(userId, "admin.events_list_empty", cancellationToken);
            return $"{headerText}\n\n{emptyText}";
        }

        var eventItems = new List<string>();
        foreach (var eventItem in eventsList.Items)
        {
            var statusEmoji = eventItem.Status.GetEmoji();
            var typeEmoji = eventItem.Type.GetEmoji();
            var dateStr = eventItem.StartDate.ToString("dd.MM.yyyy HH:mm");

            eventItems.Add($"{statusEmoji} {typeEmoji} <b>{eventItem.Title}</b>\n📅 {dateStr} | 📍 {eventItem.Location}");
        }

        return $"{headerText}\n\n{string.Join("\n\n", eventItems)}";
    }

    /// <summary>
    /// Створити клавіатуру списку подій
    /// </summary>
    private async Task<InlineKeyboardMarkup> BuildEventsListKeyboard(
        long userId, 
        Application.Events.DTOs.EventListDto eventsList, 
        EventStatus? currentStatusFilter,
        EventType? currentTypeFilter,
        CancellationToken cancellationToken)
    {
        var buttons = new List<List<InlineKeyboardButton>>();

        // Кнопки для окремих подій
        foreach (var eventItem in eventsList.Items)
        {
            buttons.Add(new List<InlineKeyboardButton>
            {
                InlineKeyboardButton.WithCallbackData(
                    $"✏️ {eventItem.Title.Substring(0, Math.Min(20, eventItem.Title.Length))}...",
                    $"events_edit_{eventItem.Id}")
            });
        }

        // Навігація сторінками
        if (eventsList.TotalPages > 1)
        {
            var navButtons = new List<InlineKeyboardButton>();

            if (eventsList.PageNumber > 1)
            {
                var prevPage = eventsList.PageNumber - 1;
                var callback = BuildPageCallback(currentStatusFilter, currentTypeFilter, prevPage);
                navButtons.Add(InlineKeyboardButton.WithCallbackData("◀️", callback));
            }

            navButtons.Add(InlineKeyboardButton.WithCallbackData(
                $"{eventsList.PageNumber}/{eventsList.TotalPages}",
                "events_pagination_info"));

            if (eventsList.PageNumber < eventsList.TotalPages)
            {
                var nextPage = eventsList.PageNumber + 1;
                var callback = BuildPageCallback(currentStatusFilter, currentTypeFilter, nextPage);
                navButtons.Add(InlineKeyboardButton.WithCallbackData("▶️", callback));
            }

            buttons.Add(navButtons);
        }

        // Кнопка назад
        buttons.Add(new List<InlineKeyboardButton>
        {
            InlineKeyboardButton.WithCallbackData(
                await GetLocalizedTextForUserAsync(userId, "button.back_to_events_menu", cancellationToken),
                "events_management")
        });

        return new InlineKeyboardMarkup(buttons);
    }

    /// <summary>
    /// Створити callback для навігації сторінками
    /// </summary>
    private string BuildPageCallback(EventStatus? status, EventType? type, int page)
    {
        if (status.HasValue)
        {
            return $"events_list_{status.ToString()!.ToLower()}_page_{page}";
        }
        if (type.HasValue)
        {
            return $"events_list_{type.ToString()!.ToLower()}_page_{page}";
        }
        return $"events_list_all_page_{page}";
    }

    #endregion
}