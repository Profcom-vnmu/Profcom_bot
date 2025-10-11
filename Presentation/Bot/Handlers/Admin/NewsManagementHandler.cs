using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StudentUnionBot.Application.Common.Interfaces;
using StudentUnionBot.Application.News.Commands.CreateNews;
using StudentUnionBot.Application.News.Commands.UpdateNews;
using StudentUnionBot.Application.News.Commands.DeleteNews;
using StudentUnionBot.Application.News.Commands.PublishNews;
using StudentUnionBot.Application.News.Queries.GetAllNews;
using StudentUnionBot.Application.News.Queries.GetNewsById;
using StudentUnionBot.Domain.Enums;
using StudentUnionBot.Presentation.Bot.Handlers.Common;
using StudentUnionBot.Presentation.Bot.Handlers.Interfaces;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace StudentUnionBot.Presentation.Bot.Handlers.Admin;

/// <summary>
/// Обробник управління новинами для адміністраторів
/// </summary>
public class NewsManagementHandler : BaseHandler, INewsManagementHandler
{
    public NewsManagementHandler(
        IServiceScopeFactory scopeFactory,
        ILogger<NewsManagementHandler> logger,
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
            case UserConversationState.CreatingNewsTitle:
                await HandleNewsTitle(botClient, message, cancellationToken);
                break;

            case UserConversationState.CreatingNewsContent:
                await HandleNewsContent(botClient, message, cancellationToken);
                break;

            case UserConversationState.EditingNewsTitle:
                await HandleEditNewsTitle(botClient, message, cancellationToken);
                break;

            case UserConversationState.EditingNewsContent:
                await HandleEditNewsContent(botClient, message, cancellationToken);
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
    /// Показати головне меню управління новинами
    /// </summary>
    public async Task ShowNewsManagementMenuAsync(
        ITelegramBotClient botClient,
        long chatId,
        CancellationToken cancellationToken)
    {
        var menuText = await GetLocalizedTextForUserAsync(chatId, "admin.news_management_menu", cancellationToken);
        var keyboard = await GetNewsManagementKeyboard(chatId, cancellationToken);

        await botClient.SendTextMessageAsync(
            chatId: chatId,
            text: menuText,
            parseMode: ParseMode.Html,
            replyMarkup: keyboard,
            cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Показати список новин
    /// </summary>
    public async Task ShowNewsListAsync(
        ITelegramBotClient botClient,
        long chatId,
        int page = 1,
        NewsStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var query = new GetAllNewsQuery
            {
                PageNumber = page,
                PageSize = 5,
                Status = status
            };

            var result = await _mediator.Send(query, cancellationToken);

            if (!result.IsSuccess || result.Value == null)
            {
                await SendLocalizedErrorAsync(
                    botClient,
                    chatId,
                    "admin.news_load_error",
                    cancellationToken);
                return;
            }

            var newsList = result.Value;
            var messageText = await BuildNewsListText(chatId, newsList, cancellationToken);
            var keyboard = await BuildNewsListKeyboard(chatId, newsList, status, cancellationToken);

            await botClient.SendTextMessageAsync(
                chatId: chatId,
                text: messageText,
                parseMode: ParseMode.Html,
                replyMarkup: keyboard,
                cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            await HandleErrorAsync(botClient, chatId, ex, "ShowNewsListAsync", cancellationToken);
        }
    }

    /// <summary>
    /// Почати створення новини
    /// </summary>
    public async Task StartCreateNewsAsync(
        ITelegramBotClient botClient,
        long chatId,
        CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var stateManager = scope.ServiceProvider.GetRequiredService<IUserStateManager>();

        await stateManager.SetStateAsync(chatId, UserConversationState.CreatingNewsTitle, cancellationToken);

        var promptText = await GetLocalizedTextForUserAsync(chatId, "admin.news_create_title_prompt", cancellationToken);

        await botClient.SendTextMessageAsync(
            chatId: chatId,
            text: promptText,
            parseMode: ParseMode.Html,
            replyMarkup: await GetBackToMainMenuAsync(chatId, cancellationToken),
            cancellationToken: cancellationToken);
    }

    #region Private Helper Methods

    /// <summary>
    /// Отримати клавіатуру управління новинами
    /// </summary>
    private async Task<InlineKeyboardMarkup> GetNewsManagementKeyboard(long userId, CancellationToken cancellationToken)
    {
        var buttons = new List<List<InlineKeyboardButton>>
        {
            new()
            {
                InlineKeyboardButton.WithCallbackData(
                    await GetLocalizedTextForUserAsync(userId, "button.create_news", cancellationToken),
                    "news_create")
            },
            new()
            {
                InlineKeyboardButton.WithCallbackData(
                    await GetLocalizedTextForUserAsync(userId, "button.all_news", cancellationToken),
                    "news_list_all")
            },
            new()
            {
                InlineKeyboardButton.WithCallbackData(
                    await GetLocalizedTextForUserAsync(userId, "button.draft_news", cancellationToken),
                    "news_list_draft"),
                InlineKeyboardButton.WithCallbackData(
                    await GetLocalizedTextForUserAsync(userId, "button.published_news", cancellationToken),
                    "news_list_published")
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
    /// Обробити введення заголовку новини
    /// </summary>
    private async Task HandleNewsTitle(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
    {
        var userId = message.From!.Id;
        var title = message.Text?.Trim();

        if (string.IsNullOrWhiteSpace(title))
        {
            await SendLocalizedErrorAsync(botClient, userId, "validation.news_title_empty", cancellationToken);
            return;
        }

        if (title.Length < 10 || title.Length > 200)
        {
            await SendLocalizedErrorAsync(botClient, userId, "validation.news_title_length", cancellationToken, new object[] { 10, 200 });
            return;
        }

        // Зберігаємо заголовок
        using var scope = _scopeFactory.CreateScope();
        var stateManager = scope.ServiceProvider.GetRequiredService<IUserStateManager>();
        await stateManager.SetDataAsync(userId, "news_title", title, cancellationToken);
        await stateManager.SetStateAsync(userId, UserConversationState.CreatingNewsContent, cancellationToken);

        var promptText = await GetLocalizedTextForUserAsync(userId, "admin.news_create_content_prompt", cancellationToken);

        await botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: promptText,
            parseMode: ParseMode.Html,
            replyMarkup: await GetBackToMainMenuAsync(userId, cancellationToken),
            cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Обробити введення вмісту новини
    /// </summary>
    private async Task HandleNewsContent(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
    {
        var userId = message.From!.Id;
        var content = message.Text?.Trim();

        if (string.IsNullOrWhiteSpace(content))
        {
            await SendLocalizedErrorAsync(botClient, userId, "validation.news_content_empty", cancellationToken);
            return;
        }

        if (content.Length < 50)
        {
            await SendLocalizedErrorAsync(botClient, userId, "validation.news_content_short", cancellationToken, new object[] { 50 });
            return;
        }

        // Отримуємо збережений заголовок
        using var scope = _scopeFactory.CreateScope();
        var stateManager = scope.ServiceProvider.GetRequiredService<IUserStateManager>();
        var title = await stateManager.GetDataAsync<string>(userId, "news_title", cancellationToken);

        if (string.IsNullOrWhiteSpace(title))
        {
            await SendLocalizedErrorAsync(botClient, userId, "error.session_expired", cancellationToken);
            await ClearUserStateAsync(userId, cancellationToken);
            return;
        }

        // Створюємо новину
        var createCommand = new CreateNewsCommand
        {
            Title = title,
            Content = content,
            Category = NewsCategory.Important,
            AuthorId = userId
        };

        var result = await _mediator.Send(createCommand, cancellationToken);

        await ClearUserStateAsync(userId, cancellationToken);

        if (result.IsSuccess)
        {
            var successText = await GetLocalizedTextForUserAsync(
                userId, 
                "admin.news_created_success", 
                cancellationToken, 
                title);

            await botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: successText,
                parseMode: ParseMode.Html,
                replyMarkup: await GetBackToMainMenuAsync(userId, cancellationToken),
                cancellationToken: cancellationToken);
        }
        else
        {
            await SendLocalizedErrorAsync(botClient, userId, "admin.news_create_error", cancellationToken);
        }
    }

    /// <summary>
    /// Створити текст списку новин
    /// </summary>
    private async Task<string> BuildNewsListText(long userId, Application.News.DTOs.NewsListDto newsList, CancellationToken cancellationToken)
    {
        var headerText = await GetLocalizedTextForUserAsync(
            userId, 
            "admin.news_list_header", 
            cancellationToken, 
            newsList.TotalCount, 
            newsList.PageNumber, 
            newsList.TotalPages);

        if (!newsList.Items.Any())
        {
            var emptyText = await GetLocalizedTextForUserAsync(userId, "admin.news_list_empty", cancellationToken);
            return $"{headerText}\n\n{emptyText}";
        }

        var newsItems = new List<string>();
        foreach (var news in newsList.Items)
        {
            var statusEmoji = news.IsPublished ? "✅" : "📝";
            var pinnedEmoji = news.IsPinned ? "📌 " : "";
            var dateStr = news.PublishAt?.ToString("dd.MM.yyyy HH:mm") ?? news.CreatedAt.ToString("dd.MM.yyyy HH:mm");

            newsItems.Add($"{statusEmoji} {pinnedEmoji}<b>{news.Title}</b>\n📅 {dateStr}");
        }

        return $"{headerText}\n\n{string.Join("\n\n", newsItems)}";
    }

    /// <summary>
    /// Створити клавіатуру списку новин
    /// </summary>
    private async Task<InlineKeyboardMarkup> BuildNewsListKeyboard(
        long userId, 
        Application.News.DTOs.NewsListDto newsList, 
        NewsStatus? currentFilter,
        CancellationToken cancellationToken)
    {
        var buttons = new List<List<InlineKeyboardButton>>();

        // Кнопки для окремих новин
        foreach (var news in newsList.Items)
        {
            buttons.Add(new List<InlineKeyboardButton>
            {
                InlineKeyboardButton.WithCallbackData(
                    $"✏️ {news.Title.Substring(0, Math.Min(20, news.Title.Length))}...",
                    $"news_edit_{news.Id}")
            });
        }

        // Навігація сторінками
        if (newsList.TotalPages > 1)
        {
            var navButtons = new List<InlineKeyboardButton>();

            if (newsList.PageNumber > 1)
            {
                var prevPage = newsList.PageNumber - 1;
                var callback = currentFilter.HasValue ? 
                    $"news_list_{currentFilter.ToString()?.ToLower()}_page_{prevPage}" :
                    $"news_list_all_page_{prevPage}";
                
                navButtons.Add(InlineKeyboardButton.WithCallbackData("◀️", callback));
            }

            navButtons.Add(InlineKeyboardButton.WithCallbackData(
                $"{newsList.PageNumber}/{newsList.TotalPages}",
                "news_pagination_info"));

            if (newsList.PageNumber < newsList.TotalPages)
            {
                var nextPage = newsList.PageNumber + 1;
                var callback = currentFilter.HasValue ? 
                    $"news_list_{currentFilter.ToString()?.ToLower()}_page_{nextPage}" :
                    $"news_list_all_page_{nextPage}";
                
                navButtons.Add(InlineKeyboardButton.WithCallbackData("▶️", callback));
            }

            buttons.Add(navButtons);
        }

        // Кнопка назад
        buttons.Add(new List<InlineKeyboardButton>
        {
            InlineKeyboardButton.WithCallbackData(
                await GetLocalizedTextForUserAsync(userId, "button.back_to_news_menu", cancellationToken),
                "news_management")
        });

        return new InlineKeyboardMarkup(buttons);
    }

    // Методи редагування новин
    private async Task HandleEditNewsTitle(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
    {
        var userId = message.From!.Id;
        var newTitle = message.Text?.Trim();

        if (string.IsNullOrWhiteSpace(newTitle))
        {
            await botClient.SendTextMessageAsync(
                chatId: userId,
                text: "❌ Заголовок не може бути порожнім. Спробуйте ще раз:",
                parseMode: ParseMode.Html,
                cancellationToken: cancellationToken);
            return;
        }

        if (newTitle.Length < 10 || newTitle.Length > 200)
        {
            await botClient.SendTextMessageAsync(
                chatId: userId,
                text: "❌ Заголовок має бути від 10 до 200 символів. Спробуйте ще раз:",
                parseMode: ParseMode.Html,
                cancellationToken: cancellationToken);
            return;
        }

        using var scope = _scopeFactory.CreateScope();
        var stateManager = scope.ServiceProvider.GetRequiredService<IUserStateManager>();

        // Отримуємо ID новини зі збереженого стану
        var newsIdStr = await stateManager.GetDataAsync<string>(userId, "editing_news_id", cancellationToken);
        
        if (string.IsNullOrEmpty(newsIdStr) || !int.TryParse(newsIdStr, out var newsId))
        {
            await botClient.SendTextMessageAsync(
                chatId: userId,
                text: "❌ Помилка: новина не знайдена. Спробуйте знову з початку.",
                parseMode: ParseMode.Html,
                cancellationToken: cancellationToken);
            
            await stateManager.ClearStateAsync(userId, cancellationToken);
            return;
        }

        // Оновлюємо заголовок через UpdateNewsCommand
        var updateCommand = new Application.News.Commands.UpdateNews.UpdateNewsCommand
        {
            NewsId = newsId,
            Title = newTitle,
            EditorId = userId
        };

        var result = await _mediator.Send(updateCommand, cancellationToken);

        if (result.IsSuccess)
        {
            await botClient.SendTextMessageAsync(
                chatId: userId,
                text: $"✅ <b>Заголовок успішно оновлено!</b>\n\n" +
                      $"Новий заголовок: {newTitle}",
                parseMode: ParseMode.Html,
                cancellationToken: cancellationToken);

            _logger.LogInformation(
                "News {NewsId} title updated by admin {AdminId}",
                newsId,
                userId);
        }
        else
        {
            await botClient.SendTextMessageAsync(
                chatId: userId,
                text: $"❌ Помилка при оновленні: {result.Error}",
                parseMode: ParseMode.Html,
                cancellationToken: cancellationToken);
        }

        // Очищуємо стан
        await stateManager.ClearStateAsync(userId, cancellationToken);
        await stateManager.ClearAllDataAsync(userId, cancellationToken);
    }

    private async Task HandleEditNewsContent(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
    {
        var userId = message.From!.Id;
        var newContent = message.Text?.Trim();

        if (string.IsNullOrWhiteSpace(newContent))
        {
            await botClient.SendTextMessageAsync(
                chatId: userId,
                text: "❌ Контент не може бути порожнім. Спробуйте ще раз:",
                parseMode: ParseMode.Html,
                cancellationToken: cancellationToken);
            return;
        }

        if (newContent.Length < 50)
        {
            await botClient.SendTextMessageAsync(
                chatId: userId,
                text: "❌ Контент має бути не менше 50 символів. Спробуйте ще раз:",
                parseMode: ParseMode.Html,
                cancellationToken: cancellationToken);
            return;
        }

        using var scope = _scopeFactory.CreateScope();
        var stateManager = scope.ServiceProvider.GetRequiredService<IUserStateManager>();

        // Отримуємо ID новини зі збереженого стану
        var newsIdStr = await stateManager.GetDataAsync<string>(userId, "editing_news_id", cancellationToken);
        
        if (string.IsNullOrEmpty(newsIdStr) || !int.TryParse(newsIdStr, out var newsId))
        {
            await botClient.SendTextMessageAsync(
                chatId: userId,
                text: "❌ Помилка: новина не знайдена. Спробуйте знову з початку.",
                parseMode: ParseMode.Html,
                cancellationToken: cancellationToken);
            
            await stateManager.ClearStateAsync(userId, cancellationToken);
            return;
        }

        // Оновлюємо контент через UpdateNewsCommand
        var updateCommand = new Application.News.Commands.UpdateNews.UpdateNewsCommand
        {
            NewsId = newsId,
            Content = newContent,
            EditorId = userId
        };

        var result = await _mediator.Send(updateCommand, cancellationToken);

        if (result.IsSuccess)
        {
            await botClient.SendTextMessageAsync(
                chatId: userId,
                text: $"✅ <b>Контент успішно оновлено!</b>\n\n" +
                      $"Перші 100 символів:\n{newContent.Substring(0, Math.Min(100, newContent.Length))}...",
                parseMode: ParseMode.Html,
                cancellationToken: cancellationToken);

            _logger.LogInformation(
                "News {NewsId} content updated by admin {AdminId}",
                newsId,
                userId);
        }
        else
        {
            await botClient.SendTextMessageAsync(
                chatId: userId,
                text: $"❌ Помилка при оновленні: {result.Error}",
                parseMode: ParseMode.Html,
                cancellationToken: cancellationToken);
        }

        // Очищуємо стан
        await stateManager.ClearStateAsync(userId, cancellationToken);
        await stateManager.ClearAllDataAsync(userId, cancellationToken);
    }

    #endregion
}