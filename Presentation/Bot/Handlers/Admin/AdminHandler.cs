using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StudentUnionBot.Application.Admin.Queries.GetAppealStatistics;
using StudentUnionBot.Application.Appeals.Queries.GetAdminAppeals;
using StudentUnionBot.Application.Common.Interfaces;
using StudentUnionBot.Application.Users.Queries.GetUserByTelegramId;
using StudentUnionBot.Presentation.Bot.Handlers.Common;
using StudentUnionBot.Presentation.Bot.Handlers.Interfaces;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace StudentUnionBot.Presentation.Bot.Handlers.Admin;

/// <summary>
/// Обробник основної адміністративної панелі та статистики
/// </summary>
public class AdminHandler : BaseHandler, IAdminHandler
{
    public AdminHandler(
        IServiceScopeFactory scopeFactory,
        ILogger<AdminHandler> logger,
        IMediator mediator)
        : base(scopeFactory, logger, mediator)
    {
    }

    /// <summary>
    /// Показує головну панель адміністратора
    /// </summary>
    public async Task HandleAdminPanelCallback(
        ITelegramBotClient botClient,
        CallbackQuery callbackQuery,
        CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        // Перевіряємо права користувача через MediatR
        var getUserQuery = new StudentUnionBot.Application.Users.Queries.GetUserByTelegramId.GetUserByTelegramIdQuery 
        { 
            TelegramId = callbackQuery.From.Id 
        };
        
        var userResult = await mediator.Send(getUserQuery, cancellationToken);
        if (!userResult.IsSuccess || userResult.Value?.Role != Domain.Enums.UserRole.Admin)
        {
            await botClient.AnswerCallbackQueryAsync(
                callbackQuery.Id,
                "⛔ У вас немає прав адміністратора",
                showAlert: true,
                cancellationToken: cancellationToken);
            return;
        }

        var user = userResult.Value!;
        
        // Отримуємо статистику
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

        var statsText = $"🔧⚙️ <b>Адмін панель</b>\n\n" +
                       $"📊 <b>Статистика:</b>\n" +
                       $"📋 Всього звернень: {allAppealsResult.Value?.TotalCount ?? 0}\n" +
                       $"🆕 Нових: {newAppealsResult.Value?.TotalCount ?? 0}\n" +
                       $"👤 Моїх: {myAppealsResult.Value?.TotalCount ?? 0}\n" +
                       $"❌ Непризначених: {unassignedResult.Value?.TotalCount ?? 0}\n\n" +
                       $"Оберіть дію:";

        await botClient.EditMessageTextAsync(
            chatId: callbackQuery.Message!.Chat.Id,
            messageId: callbackQuery.Message.MessageId,
            text: statsText,
            parseMode: ParseMode.Html,
            replyMarkup: GetAdminPanelKeyboard(),
            cancellationToken: cancellationToken);

        await botClient.AnswerCallbackQueryAsync(
            callbackQueryId: callbackQuery.Id,
            cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Показує детальну статистику звернень
    /// </summary>
    public async Task HandleAdminStatisticsCallback(
        ITelegramBotClient botClient,
        CallbackQuery callbackQuery,
        CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        // Перевіряємо права користувача через MediatR
        var getUserQuery = new StudentUnionBot.Application.Users.Queries.GetUserByTelegramId.GetUserByTelegramIdQuery 
        { 
            TelegramId = callbackQuery.From.Id 
        };
        
        var userResult = await mediator.Send(getUserQuery, cancellationToken);
        if (!userResult.IsSuccess || userResult.Value?.Role != Domain.Enums.UserRole.Admin)
        {
            await botClient.AnswerCallbackQueryAsync(
                callbackQuery.Id,
                "⛔ У вас немає прав адміністратора",
                showAlert: true,
                cancellationToken: cancellationToken);
            return;
        }

        var user = userResult.Value!;

        try
        {
            
            var query = new GetAppealStatisticsQuery
            {
                AdminId = user.TelegramId,
                Days = 30
            };

            var result = await mediator.Send(query, cancellationToken);

            if (!result.IsSuccess || result.Value == null)
            {
                await botClient.AnswerCallbackQueryAsync(
                    callbackQuery.Id,
                    $"❌ {result.Error}",
                    showAlert: true,
                    cancellationToken: cancellationToken);
                return;
            }

            var stats = result.Value;
            
            // Формуємо детальне повідомлення зі статистикою
            var statsText = $"📊 <b>Статистика звернень</b>\n" +
                           $"📅 Період: {stats.FromDate:dd.MM.yyyy} - {stats.ToDate:dd.MM.yyyy}\n\n" +
                           
                           $"📋 <b>Загальна статистика:</b>\n" +
                           $"• Всього звернень: {stats.TotalAppeals}\n" +
                           $"• 🟢 Відкрито: {stats.OpenAppeals}\n" +
                           $"• ⚙️ В роботі: {stats.InProgressAppeals}\n" +
                           $"• ✅ Закрито: {stats.ClosedAppeals}\n" +
                           $"• ⏱ Середній час вирішення: {stats.FormattedAverageResolutionTime}\n\n";

            // Додаємо розбивку за категоріями
            if (stats.CategoryBreakdown.Any())
            {
                statsText += "📂 <b>За категоріями:</b>\n";
                foreach (var category in stats.CategoryBreakdown.OrderByDescending(c => c.Count).Take(5))
                {
                    var progressBar = CreateProgressBar(category.Percentage);
                    statsText += $"{category.Icon} {category.Category}: {category.Count} ({category.Percentage:0.0}%)\n";
                    statsText += $"{progressBar}\n";
                }
                statsText += "\n";
            }

            // Додаємо розбивку за пріоритетами
            if (stats.PriorityBreakdown.Any())
            {
                statsText += "🎯 <b>За пріоритетами:</b>\n";
                foreach (var priority in stats.PriorityBreakdown.OrderByDescending(p => p.Count))
                {
                    var progressBar = CreateProgressBar(priority.Percentage);
                    statsText += $"{priority.Icon} {priority.Priority}: {priority.Count} ({priority.Percentage:0.0}%)\n";
                    statsText += $"{progressBar}\n";
                }
                statsText += "\n";
            }

            // Додаємо тренд за останні дні (топ 7)
            if (stats.DailyStats.Any())
            {
                statsText += "📈 <b>Тренд за останні 7 днів:</b>\n";
                foreach (var day in stats.DailyStats.OrderByDescending(d => d.Date).Take(7))
                {
                    var trend = day.Created > day.Closed ? "📈" : day.Created < day.Closed ? "📉" : "➡️";
                    statsText += $"{trend} {day.FormattedDate}: +{day.Created} / -{day.Closed}\n";
                }
            }

            var keyboard = new InlineKeyboardMarkup(new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("🔙 Адмін панель", "admin_panel")
                }
            });

            await botClient.EditMessageTextAsync(
                chatId: callbackQuery.Message!.Chat.Id,
                messageId: callbackQuery.Message.MessageId,
                text: statsText,
                parseMode: ParseMode.Html,
                replyMarkup: keyboard,
                cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Помилка при отриманні статистики для адміна {AdminId}", user?.TelegramId);
            await botClient.AnswerCallbackQueryAsync(
                callbackQuery.Id,
                "❌ Виникла помилка при отриманні статистики",
                showAlert: true,
                cancellationToken: cancellationToken);
        }

        await botClient.AnswerCallbackQueryAsync(
            callbackQueryId: callbackQuery.Id,
            cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Створює прогрес-бар для відсотків
    /// </summary>
    private string CreateProgressBar(double percentage, int length = 10)
    {
        var filled = (int)Math.Round(percentage / 100 * length);
        var empty = length - filled;
        return new string('▓', filled) + new string('░', empty);
    }

    /// <summary>
    /// Створює клавіатуру адміністративної панелі
    /// </summary>
    private InlineKeyboardMarkup GetAdminPanelKeyboard()
    {
        return new InlineKeyboardMarkup(new[]
        {
            new[]
            {
                InlineKeyboardButton.WithCallbackData("📊 Статистика", "admin_stats"),
                InlineKeyboardButton.WithCallbackData("📋 Звернення", "admin_appeals")
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData("🆕 Нові", "admin_appeals_new"),
                InlineKeyboardButton.WithCallbackData("👤 Мої", "admin_appeals_my")
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData("❌ Непризначені", "admin_appeals_unassigned")
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData("💾 Бекапи", "admin_backup"),
                InlineKeyboardButton.WithCallbackData("📢 Розсилки", "admin_broadcast")
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData("📰 Новини", "news_management"),
                InlineKeyboardButton.WithCallbackData("📅 Події", "events_management")
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData("🔙 Головне меню", "back_to_main")
            }
        });
    }

    /// <summary>
    /// Обробляє текстові повідомлення (не використовується для цього хендлера)
    /// </summary>
    public override async Task HandleTextMessageAsync(
        ITelegramBotClient botClient,
        Message message,
        Domain.Enums.UserConversationState state,
        CancellationToken cancellationToken)
    {
        // AdminHandler не обробляє текстові повідомлення
        await Task.CompletedTask;
    }



    /// <summary>
    /// Обробка callback для списку звернень (тимчасово пусто)
    /// </summary>
    public async Task HandleAdminAppealsListCallbackAsync(
        ITelegramBotClient botClient,
        CallbackQuery callbackQuery,
        CancellationToken cancellationToken)
    {
        // TODO: Реалізувати список звернень
        await botClient.AnswerCallbackQueryAsync(
            callbackQuery.Id,
            "🚧 Функція в розробці",
            showAlert: true,
            cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Обробка callback для розсилок (тимчасово пусто)
    /// </summary>
    public async Task HandleBroadcastCallbackAsync(
        ITelegramBotClient botClient,
        CallbackQuery callbackQuery,
        CancellationToken cancellationToken)
    {
        // TODO: Реалізувати розсилки
        await botClient.AnswerCallbackQueryAsync(
            callbackQuery.Id,
            "🚧 Функція в розробці",
            showAlert: true,
            cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Показує меню бекапів
    /// </summary>
    public async Task HandleAdminBackupMenuCallback(ITelegramBotClient botClient, CallbackQuery callbackQuery, CancellationToken cancellationToken)
    {
        try
        {
            var backupMenu = new InlineKeyboardMarkup(new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("➕ Створити Backup", "admin_backup_create")
                },
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("📋 Список Backups", "admin_backup_list")
                },
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("🔙 Адмін панель", "admin_panel")
                }
            });

            await botClient.EditMessageTextAsync(
                chatId: callbackQuery.Message!.Chat.Id,
                messageId: callbackQuery.Message.MessageId,
                text: "💾 <b>Управління бекапами</b>\n\n" +
                      "Оберіть дію:",
                parseMode: ParseMode.Html,
                replyMarkup: backupMenu,
                cancellationToken: cancellationToken);

            await botClient.AnswerCallbackQueryAsync(
                callbackQueryId: callbackQuery.Id,
                cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Помилка в HandleAdminBackupMenuCallback");
            
            await botClient.AnswerCallbackQueryAsync(
                callbackQueryId: callbackQuery.Id,
                text: "❌ Виникла помилка. Спробуйте пізніше.",
                showAlert: true,
                cancellationToken: cancellationToken);
        }
    }
}