using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StudentUnionBot.Application.Common.Interfaces;
using StudentUnionBot.Application.Users.Queries.GetUserByTelegramId;
using StudentUnionBot.Domain.Enums;
using StudentUnionBot.Presentation.Bot.Handlers.Common;
using StudentUnionBot.Presentation.Bot.Handlers.Interfaces;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace StudentUnionBot.Presentation.Bot.Handlers.Commands;

/// <summary>
/// Обробник команд бота (/start, /help, /appeal, /contacts тощо)
/// </summary>
public class CommandHandler : BaseHandler, ICommandHandler
{
    public CommandHandler(
        IServiceScopeFactory scopeFactory,
        ILogger<CommandHandler> logger,
        IMediator mediator)
        : base(scopeFactory, logger, mediator)
    {
    }

    /// <summary>
    /// Цей метод не використовується в CommandHandler, оскільки команди обробляються безпосередньо
    /// </summary>
    public override Task HandleTextMessageAsync(
        ITelegramBotClient botClient, 
        Message message, 
        UserConversationState state, 
        CancellationToken cancellationToken)
    {
        // CommandHandler обробляє команди безпосередньо через HandleCommandAsync
        throw new NotImplementedException("Використовуйте HandleCommandAsync для обробки команд");
    }

    /// <summary>
    /// Обробляє команди бота
    /// </summary>
    public async Task HandleCommandAsync(
        ITelegramBotClient botClient,
        Message message,
        CancellationToken cancellationToken)
    {
        try
        {
            var command = message.Text!.Split(' ')[0].ToLower();
            var userId = message.From!.Id;

            _logger.LogInformation("Обробка команди: {Command} від користувача {UserId}", command, userId);

            // Команди /start та /appeal скасовують поточний процес
            if (command == "/start" || command == "/appeal")
            {
                await ClearUserStateAsync(userId, cancellationToken);
            }

            // Перевіряємо, чи є користувач адміністратором
            var isAdmin = await IsUserAdminAsync(message.From.Id, cancellationToken);

            var (responseText, keyboard) = await GetCommandResponseAsync(command, isAdmin, userId, cancellationToken);

            await botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: responseText,
                parseMode: ParseMode.Html,
                replyMarkup: keyboard,
                cancellationToken: cancellationToken);

            _logger.LogInformation("Команда {Command} успішно оброблена", command);
        }
        catch (Exception ex)
        {
            await HandleErrorAsync(
                botClient, 
                message.Chat.Id, 
                ex, 
                $"HandleCommandAsync - команда: {message.Text}",
                cancellationToken);
        }
    }

    #region Private Methods

    /// <summary>
    /// Перевіряє, чи є користувач адміністратором
    /// </summary>
    private async Task<bool> IsUserAdminAsync(long telegramId, CancellationToken cancellationToken)
    {
        try
        {
            var getUserQuery = new GetUserByTelegramIdQuery { TelegramId = telegramId };
            var userResult = await _mediator.Send(getUserQuery, cancellationToken);
            var isAdmin = userResult.IsSuccess && userResult.Value?.Role == UserRole.Admin;

            _logger.LogInformation(
                "Користувач {TelegramId} має роль {Role}, isAdmin={IsAdmin}",
                telegramId,
                userResult.Value?.Role,
                isAdmin);

            return isAdmin;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Помилка при перевірці ролі користувача {TelegramId}", telegramId);
            return false;
        }
    }

    /// <summary>
    /// Отримує відповідь на команду
    /// </summary>
    private async Task<(string responseText, IReplyMarkup? keyboard)> GetCommandResponseAsync(string command, bool isAdmin, long userId, CancellationToken cancellationToken)
    {
        return command switch
        {
            "/start" => (
                await GetLocalizedTextForUserAsync(userId, "command.start_welcome", cancellationToken),
                await GetMainMenuAsync(userId, isAdmin, cancellationToken)),
            
            "/help" => (
                await GetLocalizedTextForUserAsync(userId, "command.help_text", cancellationToken),
                await GetBackToMainMenuAsync(userId, cancellationToken)),
            
            "/appeal" => (
                await GetLocalizedTextForUserAsync(userId, "command.appeal_create", cancellationToken),
                await GetAppealCategoriesAsync(userId, cancellationToken)),
            
            "/myappeals" => (
                await GetLocalizedTextForUserAsync(userId, "command.loading_appeals", cancellationToken),
                null),
            
            "/news" => (
                await GetLocalizedTextForUserAsync(userId, "command.loading_news", cancellationToken),
                null),
            
            "/events" => (
                await GetLocalizedTextForUserAsync(userId, "command.loading_events", cancellationToken),
                null),
            
            "/profile" => (
                await GetLocalizedTextForUserAsync(userId, "command.loading_profile", cancellationToken),
                null),
            
            "/contacts" => (
                await GetLocalizedTextForUserAsync(userId, "command.contacts_info", cancellationToken),
                await GetBackToMainMenuAsync(userId, cancellationToken)),
            
            _ => (
                await GetLocalizedTextForUserAsync(userId, "command.unknown_command", cancellationToken),
                await GetBackToMainMenuAsync(userId, cancellationToken))
        };
    }




    #endregion

    #region ICommandHandler Implementation

    /// <summary>
    /// Обробляє команду /start
    /// </summary>
    public async Task HandleStartCommandAsync(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
    {
        await HandleCommandAsync(botClient, message, cancellationToken);
    }

    /// <summary>
    /// Обробляє команду /help
    /// </summary>
    public async Task HandleHelpCommandAsync(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
    {
        await HandleCommandAsync(botClient, message, cancellationToken);
    }

    /// <summary>
    /// Обробляє команду /appeal
    /// </summary>
    public async Task HandleAppealCommandAsync(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
    {
        await HandleCommandAsync(botClient, message, cancellationToken);
    }

    /// <summary>
    /// Обробляє команду /contacts
    /// </summary>
    public async Task HandleContactsCommandAsync(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
    {
        await HandleCommandAsync(botClient, message, cancellationToken);
    }

    #endregion
}