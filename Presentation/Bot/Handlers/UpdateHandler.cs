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
using StudentUnionBot.Application.Users.Commands.ChangeLanguage;
using StudentUnionBot.Presentation.Bot.Keyboards;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace StudentUnionBot.Presentation.Bot.Handlers;

/// <summary>
/// ╨ô╨╛╨╗╨╛╨▓╨╜╨╕╨╣ ╨╛╨▒╤Ç╨╛╨▒╨╜╨╕╨║ ╨╛╨╜╨╛╨▓╨╗╨╡╨╜╤î ╨▓╤û╨┤ Telegram
/// </summary>
public class UpdateHandler : IBotUpdateHandler
{
    private readonly ILogger<UpdateHandler> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IUserStateManager _stateManager;
    private readonly ILocalizationService _localizationService;

    public UpdateHandler(
        ILogger<UpdateHandler> logger,
        IServiceScopeFactory scopeFactory,
        IUserStateManager stateManager,
        ILocalizationService localizationService)
    {
        _logger = logger;
        _scopeFactory = scopeFactory;
        _stateManager = stateManager;
        _localizationService = localizationService;
    }

    // Helper methods for keyboards
    private InlineKeyboardMarkup GetMainMenu(bool isAdmin = false)
    {
        return KeyboardFactory.GetMainMenuKeyboard(_localizationService, Language.Ukrainian, isAdmin);
    }

    private InlineKeyboardMarkup GetBackToMainMenu()
    {
        return KeyboardFactory.GetBackToMainMenuKeyboard(_localizationService, Language.Ukrainian);
    }

    private InlineKeyboardMarkup GetAppealCategories()
    {
        return KeyboardFactory.GetAppealCategoriesKeyboard(_localizationService, Language.Ukrainian);
    }

    private InlineKeyboardMarkup GetAdminPanel()
    {
        return KeyboardFactory.GetAdminPanelKeyboard(_localizationService, Language.Ukrainian);
    }

    private InlineKeyboardMarkup GetAdminAppealActions(int appealId, bool isAssignedToMe, bool isClosed)
    {
        return KeyboardFactory.GetAdminAppealActionsKeyboard(_localizationService, Language.Ukrainian, appealId, isAssignedToMe, isClosed);
    }

    private InlineKeyboardMarkup GetPrioritySelection(int appealId)
    {
        return KeyboardFactory.GetPrioritySelectionKeyboard(_localizationService, Language.Ukrainian, appealId);
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
            "╨₧╤é╤Ç╨╕╨╝╨░╨╜╨╛ ╨┐╨╛╨▓╤û╨┤╨╛╨╝╨╗╨╡╨╜╨╜╤Å ╨▓╤û╨┤ {UserId} ╨▓ ╤ç╨░╤é╤û {ChatId}: {Text}",
            userId,
            chatId,
            messageText);

        // ╨á╨╡╤ö╤ü╤é╤Ç╤â╤ö╨╝╨╛/╨╛╨╜╨╛╨▓╨╗╤Ä╤ö╨╝╨╛ ╨║╨╛╤Ç╨╕╤ü╤é╤â╨▓╨░╤ç╨░ ╨┐╤Ç╨╕ ╨║╨╛╨╢╨╜╤û╨╣ ╨▓╨╖╨░╤ö╨╝╨╛╨┤╤û╤ù
        await RegisterOrUpdateUserAsync(message.From, cancellationToken);

        // ╨₧╨▒╤Ç╨╛╨▒╨║╨░ ╨║╨╛╨╝╨░╨╜╨┤
        if (messageText.StartsWith('/'))
        {
            await HandleCommandAsync(botClient, message, cancellationToken);
            return;
        }

        // ╨₧╨▒╤Ç╨╛╨▒╨║╨░ ╨╖╨▓╨╕╤ç╨░╨╣╨╜╨╕╤à ╨┐╨╛╨▓╤û╨┤╨╛╨╝╨╗╨╡╨╜╤î
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
                Language = (user.LanguageCode?.ToLower() == "en") ? Language.English : Language.Ukrainian
            };

            await mediator.Send(command, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "╨ƒ╨╛╨╝╨╕╨╗╨║╨░ ╨┐╤Ç╨╕ ╤Ç╨╡╤ö╤ü╤é╤Ç╨░╤å╤û╤ù ╨║╨╛╤Ç╨╕╤ü╤é╤â╨▓╨░╤ç╨░ {UserId}", user.Id);
        }
    }

    private async Task HandleCommandAsync(
        ITelegramBotClient botClient,
        Message message,
        CancellationToken cancellationToken)
    {
        var command = message.Text!.Split(' ')[0].ToLower();
        var userId = message.From!.Id;

        _logger.LogInformation("╨₧╨▒╤Ç╨╛╨▒╨║╨░ ╨║╨╛╨╝╨░╨╜╨┤╨╕: {Command}", command);

        // ╨Ü╨╛╨╝╨░╨╜╨┤╨╕ /start ╤é╨░ /appeal ╤ü╨║╨░╤ü╨╛╨▓╤â╤Ä╤é╤î ╨┐╨╛╤é╨╛╤ç╨╜╨╕╨╣ ╨┐╤Ç╨╛╤å╨╡╤ü
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
            "╨Ü╨╛╤Ç╨╕╤ü╤é╤â╨▓╨░╤ç {TelegramId} ╨╝╨░╤ö ╤Ç╨╛╨╗╤î {Role}, isAdmin={IsAdmin}",
            message.From.Id,
            userForMenu?.Role,
            isAdmin);

        var (responseText, keyboard) = command switch
        {
            "/start" => (
                "≡ƒÄô <b>╨Æ╤û╤é╨░╤ö╨╝╨╛ ╨▓ ╨▒╨╛╤é╤û ╨í╤é╤â╨┤╨╡╨╜╤é╤ü╤î╨║╨╛╨│╨╛ ╨ƒ╤Ç╨╛╤ä╨║╨╛╨╝╤â ╨Æ╨¥╨£╨ú!</b>\n\n" +
                "╨» ╨┤╨╛╨┐╨╛╨╝╨╛╨╢╤â ╨▓╨░╨╝:\n" +
                "≡ƒô¥ ╨í╤é╨▓╨╛╤Ç╨╕╤é╨╕ ╨╖╨▓╨╡╤Ç╨╜╨╡╨╜╨╜╤Å ╨┤╨╛ ╨┐╤Ç╨╛╤ä╨║╨╛╨╝╤â\n" +
                "≡ƒôï ╨Æ╤û╨┤╤ü╤é╨╡╨╢╤â╨▓╨░╤é╨╕ ╤ü╤é╨░╤é╤â╤ü ╨▓╨░╤ê╨╕╤à ╨╖╨▓╨╡╤Ç╨╜╨╡╨╜╤î\n" +
                "≡ƒô░ ╨ö╤û╨╖╨╜╨░╨▓╨░╤é╨╕╤ü╤Å ╨╛╤ü╤é╨░╨╜╨╜╤û ╨╜╨╛╨▓╨╕╨╜╨╕\n" +
                "≡ƒÄë ╨æ╤â╤é╨╕ ╨▓ ╨║╤â╤Ç╤ü╤û ╨╝╨░╨╣╨▒╤â╤é╨╜╤û╤à ╨┐╨╛╨┤╤û╨╣\n" +
                "≡ƒñ¥ ╨₧╤é╤Ç╨╕╨╝╤â╨▓╨░╤é╨╕ ╨╖╨╜╨╕╨╢╨║╨╕ ╨▓╤û╨┤ ╨┐╨░╤Ç╤é╨╜╨╡╤Ç╤û╨▓\n\n" +
                "╨₧╨▒╨╡╤Ç╤û╤é╤î ╨┤╤û╤Ä ╨╖ ╨╝╨╡╨╜╤Ä ╨╜╨╕╨╢╤ç╨╡:",
                GetMainMenu(isAdmin) as IReplyMarkup),
            
            "/help" => (
                "≡ƒôî <b>╨ö╨╛╨▓╤û╨┤╨║╨░ ╨┐╨╛ ╨▒╨╛╤é╤â</b>\n\n" +
                "<b>╨₧╤ü╨╜╨╛╨▓╨╜╤û ╨║╨╛╨╝╨░╨╜╨┤╨╕:</b>\n" +
                "/start - ╨ô╨╛╨╗╨╛╨▓╨╜╨╡ ╨╝╨╡╨╜╤Ä\n" +
                "/appeal - ╨í╤é╨▓╨╛╤Ç╨╕╤é╨╕ ╨╖╨▓╨╡╤Ç╨╜╨╡╨╜╨╜╤Å\n" +
                "/myappeals - ╨£╨╛╤ù ╨╖╨▓╨╡╤Ç╨╜╨╡╨╜╨╜╤Å\n" +
                "/news - ╨¥╨╛╨▓╨╕╨╜╨╕\n" +
                "/events - ╨ù╨░╤à╨╛╨┤╨╕\n" +
                "/profile - ╨£╤û╨╣ ╨┐╤Ç╨╛╤ä╤û╨╗╤î\n" +
                "/contacts - ╨Ü╨╛╨╜╤é╨░╨║╤é╨╕\n\n" +
                "<b>╨»╨║ ╤ü╤é╨▓╨╛╤Ç╨╕╤é╨╕ ╨╖╨▓╨╡╤Ç╨╜╨╡╨╜╨╜╤Å?</b>\n" +
                "1. ╨¥╨░╤é╨╕╤ü╨╜╤û╤é╤î '≡ƒô¥ ╨í╤é╨▓╨╛╤Ç╨╕╤é╨╕ ╨╖╨▓╨╡╤Ç╨╜╨╡╨╜╨╜╤Å'\n" +
                "2. ╨₧╨▒╨╡╤Ç╤û╤é╤î ╨║╨░╤é╨╡╨│╨╛╤Ç╤û╤Ä\n" +
                "3. ╨₧╨┐╨╕╤ê╤û╤é╤î ╨▓╨░╤ê╤â ╨┐╤Ç╨╛╨▒╨╗╨╡╨╝╤â\n" +
                "4. ╨₧╤ç╤û╨║╤â╨╣╤é╨╡ ╨╜╨░ ╨▓╤û╨┤╨┐╨╛╨▓╤û╨┤╤î ╨▓╤û╨┤ ╨░╨┤╨╝╤û╨╜╤û╤ü╤é╤Ç╨░╤å╤û╤ù\n\n" +
                "╨ù╨░ ╨┐╨╕╤é╨░╨╜╨╜╤Å╨╝╨╕ ╨╖╨▓╨╡╤Ç╤é╨░╨╣╤é╨╡╤ü╤î: @vnmu_profkom",
                GetBackToMainMenu() as IReplyMarkup),
            
            "/appeal" => (
                "≡ƒô¥ <b>╨í╤é╨▓╨╛╤Ç╨╡╨╜╨╜╤Å ╨╖╨▓╨╡╤Ç╨╜╨╡╨╜╨╜╤Å</b>\n\n" +
                "╨₧╨▒╨╡╤Ç╤û╤é╤î ╨║╨░╤é╨╡╨│╨╛╤Ç╤û╤Ä ╨▓╨░╤ê╨╛╨│╨╛ ╨╖╨▓╨╡╤Ç╨╜╨╡╨╜╨╜╤Å:",
                GetAppealCategories() as IReplyMarkup),
            
            "/myappeals" => (
                "≡ƒôï ╨ù╨░╨▓╨░╨╜╤é╨░╨╢╨╡╨╜╨╜╤Å ╨▓╨░╤ê╨╕╤à ╨╖╨▓╨╡╤Ç╨╜╨╡╨╜╤î...",
                null as IReplyMarkup),
            
            "/news" => (
                "≡ƒô░ ╨ù╨░╨▓╨░╨╜╤é╨░╨╢╨╡╨╜╨╜╤Å ╨╜╨╛╨▓╨╕╨╜...",
                null as IReplyMarkup),
            
            "/events" => (
                "≡ƒÄë ╨ù╨░╨▓╨░╨╜╤é╨░╨╢╨╡╨╜╨╜╤Å ╨┐╨╛╨┤╤û╨╣...",
                null as IReplyMarkup),
            
            "/profile" => (
                "≡ƒæñ ╨ù╨░╨▓╨░╨╜╤é╨░╨╢╨╡╨╜╨╜╤Å ╨┐╤Ç╨╛╤ä╤û╨╗╤Ä...",
                null as IReplyMarkup),
            
            "/contacts" => (
                "≡ƒô₧ <b>╨Ü╨╛╨╜╤é╨░╨║╤é╨╜╨░ ╤û╨╜╤ä╨╛╤Ç╨╝╨░╤å╤û╤Å</b>\n\n" +
                "≡ƒÅ¢ <b>╨í╤é╤â╨┤╨╡╨╜╤é╤ü╤î╨║╨╕╨╣ ╨┐╤Ç╨╛╤ä╤ü╨┐╤û╨╗╨║╨╛╨▓╨╕╨╣ ╨║╨╛╨╝╤û╤é╨╡╤é ╨Æ╨¥╨£╨ú</b>\n\n" +
                "≡ƒôº Email: profkom@vnmu.edu.ua\n" +
                "≡ƒô▒ Telegram: @vnmu_profkom\n" +
                "≡ƒôì ╨É╨┤╤Ç╨╡╤ü╨░: ╨▓╤â╨╗. ╨ƒ╨╕╤Ç╨╛╨│╨╛╨▓╨░, 56, ╨Æ╤û╨╜╨╜╨╕╤å╤Å\n" +
                "≡ƒòÉ ╨ô╨╛╨┤╨╕╨╜╨╕ ╤Ç╨╛╨▒╨╛╤é╨╕: ╨ƒ╨¥-╨ƒ╨ó 9:00-17:00\n\n" +
                "╨£╨╕ ╨╖╨░╨▓╨╢╨┤╨╕ ╤Ç╨░╨┤╤û ╨▓╨░╨╝ ╨┤╨╛╨┐╨╛╨╝╨╛╨│╤é╨╕! ≡ƒñ¥",
                GetBackToMainMenu() as IReplyMarkup),
            
            _ => (
                "Γ¥ô ╨¥╨╡╨▓╤û╨┤╨╛╨╝╨░ ╨║╨╛╨╝╨░╨╜╨┤╨░. ╨Æ╨╕╨║╨╛╤Ç╨╕╤ü╤é╨╛╨▓╤â╨╣╤é╨╡ /help ╨┤╨╗╤Å ╨┐╨╡╤Ç╨╡╨│╨╗╤Å╨┤╤â ╤ü╨┐╨╕╤ü╨║╤â ╨║╨╛╨╝╨░╨╜╨┤.",
                GetBackToMainMenu() as IReplyMarkup)
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

        // ╨₧╨▒╤Ç╨╛╨▒╨║╨░ ╨┐╨╛╨▓╤û╨┤╨╛╨╝╨╗╨╡╨╜╤î ╨╜╨░ ╨╛╤ü╨╜╨╛╨▓╤û ╨┐╨╛╤é╨╛╤ç╨╜╨╛╨│╨╛ ╤ü╤é╨░╨╜╤â
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

            case UserConversationState.WaitingCloseReason:
                await HandleCloseReasonInputAsync(botClient, message, cancellationToken);
                break;

            default:
                // ╨í╤é╨░╨╜╨┤╨░╤Ç╤é╨╜╨░ ╨▓╤û╨┤╨┐╨╛╨▓╤û╨┤╤î ╨┤╨╗╤Å Idle ╤ü╤é╨░╨╜╤â
                await botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: "╨» ╨╛╤é╤Ç╨╕╨╝╨░╨▓ ╨▓╨░╤ê╨╡ ╨┐╨╛╨▓╤û╨┤╨╛╨╝╨╗╨╡╨╜╨╜╤Å. ╨Æ╨╕╨║╨╛╤Ç╨╕╤ü╤é╨╛╨▓╤â╨╣╤é╨╡ /help ╨┤╨╗╤Å ╨┐╨╡╤Ç╨╡╨│╨╗╤Å╨┤╤â ╨┤╨╛╤ü╤é╤â╨┐╨╜╨╕╤à ╨║╨╛╨╝╨░╨╜╨┤.",
                    replyMarkup: GetBackToMainMenu(),
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
                text: "Γ¥î ╨ó╨╡╨╝╨░ ╨╖╨▓╨╡╤Ç╨╜╨╡╨╜╨╜╤Å ╨╖╨░╨╜╨░╨┤╤é╨╛ ╨║╨╛╤Ç╨╛╤é╨║╨░. ╨æ╤â╨┤╤î ╨╗╨░╤ü╨║╨░, ╨▓╨▓╨╡╨┤╤û╤é╤î ╤ë╨╛╨╜╨░╨╣╨╝╨╡╨╜╤ê╨╡ 5 ╤ü╨╕╨╝╨▓╨╛╨╗╤û╨▓.",
                cancellationToken: cancellationToken);
            return;
        }

        if (subject.Length > 200)
        {
            await botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: "Γ¥î ╨ó╨╡╨╝╨░ ╨╖╨▓╨╡╤Ç╨╜╨╡╨╜╨╜╤Å ╨╖╨░╨╜╨░╨┤╤é╨╛ ╨┤╨╛╨▓╨│╨░. ╨£╨░╨║╤ü╨╕╨╝╤â╨╝ 200 ╤ü╨╕╨╝╨▓╨╛╨╗╤û╨▓.",
                cancellationToken: cancellationToken);
            return;
        }

        // ╨ù╨▒╨╡╤Ç╤û╨│╨░╤ö╨╝╨╛ ╤é╨╡╨╝╤â
        await _stateManager.SetDataAsync(userId, "appeal_subject", subject, cancellationToken);

        // ╨ƒ╨╡╤Ç╨╡╤à╨╛╨┤╨╕╨╝╨╛ ╨┤╨╛ ╨╜╨░╤ü╤é╤â╨┐╨╜╨╛╨│╨╛ ╨║╤Ç╨╛╨║╤â
        await _stateManager.SetStateAsync(userId, UserConversationState.WaitingAppealMessage, cancellationToken);

        await botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: $"Γ£à ╨ó╨╡╨╝╨░ ╨╖╨▒╨╡╤Ç╨╡╨╢╨╡╨╜╨░: <b>{subject}</b>\n\n" +
                  "≡ƒô¥ ╨ó╨╡╨┐╨╡╤Ç ╨╛╨┐╨╕╤ê╤û╤é╤î ╨▓╨░╤ê╤â ╨┐╤Ç╨╛╨▒╨╗╨╡╨╝╤â ╨┤╨╡╤é╨░╨╗╤î╨╜╨╛.\n\n" +
                  "<i>╨£╤û╨╜╤û╨╝╤â╨╝ 10 ╤ü╨╕╨╝╨▓╨╛╨╗╤û╨▓, ╨╝╨░╨║╤ü╨╕╨╝╤â╨╝ 2000 ╤ü╨╕╨╝╨▓╨╛╨╗╤û╨▓.</i>",
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
                text: "Γ¥î ╨ƒ╨╛╨▓╤û╨┤╨╛╨╝╨╗╨╡╨╜╨╜╤Å ╨╖╨░╨╜╨░╨┤╤é╨╛ ╨║╨╛╤Ç╨╛╤é╨║╨╡. ╨æ╤â╨┤╤î ╨╗╨░╤ü╨║╨░, ╨╛╨┐╨╕╤ê╤û╤é╤î ╨┐╤Ç╨╛╨▒╨╗╨╡╨╝╤â ╨┤╨╡╤é╨░╨╗╤î╨╜╤û╤ê╨╡ (╨╝╤û╨╜╤û╨╝╤â╨╝ 10 ╤ü╨╕╨╝╨▓╨╛╨╗╤û╨▓).",
                cancellationToken: cancellationToken);
            return;
        }

        if (messageText.Length > 2000)
        {
            await botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: "Γ¥î ╨ƒ╨╛╨▓╤û╨┤╨╛╨╝╨╗╨╡╨╜╨╜╤Å ╨╖╨░╨╜╨░╨┤╤é╨╛ ╨┤╨╛╨▓╨│╨╡. ╨£╨░╨║╤ü╨╕╨╝╤â╨╝ 2000 ╤ü╨╕╨╝╨▓╨╛╨╗╤û╨▓.",
                cancellationToken: cancellationToken);
            return;
        }

        // ╨₧╤é╤Ç╨╕╨╝╤â╤ö╨╝╨╛ ╨╖╨▒╨╡╤Ç╨╡╨╢╨╡╨╜╤û ╨┤╨░╨╜╤û
        var category = await _stateManager.GetDataAsync<AppealCategory>(userId, "appeal_category", cancellationToken);
        var subject = await _stateManager.GetDataAsync<string>(userId, "appeal_subject", cancellationToken);

        // AppealCategory ╤ö enum, ╤é╨╛╨╝╤â ╨┐╨╡╤Ç╨╡╨▓╤û╤Ç╤Å╤ö╨╝╨╛ subject
        if (string.IsNullOrEmpty(subject))
        {
            await botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: "Γ¥î ╨ƒ╨╛╨╝╨╕╨╗╨║╨░: ╨┤╨░╨╜╤û ╨╖╨▓╨╡╤Ç╨╜╨╡╨╜╨╜╤Å ╨▓╤é╤Ç╨░╤ç╨╡╨╜╨╛. ╨ƒ╨╛╤ç╨╜╤û╤é╤î ╤ü╨┐╨╛╤ç╨░╤é╨║╤â /appeal",
                replyMarkup: GetBackToMainMenu(),
                cancellationToken: cancellationToken);

            await _stateManager.ClearStateAsync(userId, cancellationToken);
            await _stateManager.ClearAllDataAsync(userId, cancellationToken);
            return;
        }

        // ╨í╤é╨▓╨╛╤Ç╤Ä╤ö╨╝╨╛ ╨╖╨▓╨╡╤Ç╨╜╨╡╨╜╨╜╤Å ╤ç╨╡╤Ç╨╡╨╖ MediatR
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
                    text: $"Γ£à <b>╨ù╨▓╨╡╤Ç╨╜╨╡╨╜╨╜╤Å ╤â╤ü╨┐╤û╤ê╨╜╨╛ ╤ü╤é╨▓╨╛╤Ç╨╡╨╜╨╛!</b>\n\n" +
                          $"≡ƒôï ╨¥╨╛╨╝╨╡╤Ç: #{result.Value!.Id}\n" +
                          $"≡ƒôé ╨Ü╨░╤é╨╡╨│╨╛╤Ç╤û╤Å: {result.Value.Category}\n" +
                          $"≡ƒôî ╨ó╨╡╨╝╨░: {result.Value.Subject}\n" +
                          $"≡ƒôè ╨í╤é╨░╤é╤â╤ü: {result.Value.Status}\n\n" +
                          $"╨£╨╕ ╤Ç╨╛╨╖╨│╨╗╤Å╨╜╨╡╨╝╨╛ ╨▓╨░╤ê╨╡ ╨╖╨▓╨╡╤Ç╨╜╨╡╨╜╨╜╤Å ╨╜╨░╨╣╨▒╨╗╨╕╨╢╤ç╨╕╨╝ ╤ç╨░╤ü╨╛╨╝.\n" +
                          $"╨Æ╨╕ ╨╛╤é╤Ç╨╕╨╝╨░╤ö╤é╨╡ ╤ü╨┐╨╛╨▓╤û╤ë╨╡╨╜╨╜╤Å ╨┐╤Ç╨╛ ╨╖╨╝╤û╨╜╤â ╤ü╤é╨░╤é╤â╤ü╤â.",
                    parseMode: ParseMode.Html,
                    replyMarkup: GetBackToMainMenu(),
                    cancellationToken: cancellationToken);

                // ╨₧╤ç╨╕╤ë╨░╤ö╨╝╨╛ ╤ü╤é╨░╨╜
                await _stateManager.ClearStateAsync(userId, cancellationToken);
                await _stateManager.ClearAllDataAsync(userId, cancellationToken);
            }
            else
            {
                await botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: $"Γ¥î ╨ƒ╨╛╨╝╨╕╨╗╨║╨░ ╨┐╤Ç╨╕ ╤ü╤é╨▓╨╛╤Ç╨╡╨╜╨╜╤û ╨╖╨▓╨╡╤Ç╨╜╨╡╨╜╨╜╤Å:\n{result.Error}\n\n╨í╨┐╤Ç╨╛╨▒╤â╨╣╤é╨╡ ╤ë╨╡ ╤Ç╨░╨╖ /appeal",
                    parseMode: ParseMode.Html,
                    replyMarkup: GetBackToMainMenu(),
                    cancellationToken: cancellationToken);

                await _stateManager.ClearStateAsync(userId, cancellationToken);
                await _stateManager.ClearAllDataAsync(userId, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "╨ƒ╨╛╨╝╨╕╨╗╨║╨░ ╨┐╤Ç╨╕ ╤ü╤é╨▓╨╛╤Ç╨╡╨╜╨╜╤û ╨╖╨▓╨╡╤Ç╨╜╨╡╨╜╨╜╤Å ╨┤╨╗╤Å ╨║╨╛╤Ç╨╕╤ü╤é╤â╨▓╨░╤ç╨░ {UserId}", userId);

            await botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: "Γ¥î ╨Æ╨╕╨╜╨╕╨║╨╗╨░ ╤é╨╡╤à╨╜╤û╤ç╨╜╨░ ╨┐╨╛╨╝╨╕╨╗╨║╨░. ╨í╨┐╤Ç╨╛╨▒╤â╨╣╤é╨╡ ╨┐╤û╨╖╨╜╤û╤ê╨╡ ╨░╨▒╨╛ ╨╖╨▓╨╡╤Ç╨╜╤û╤é╤î╤ü╤Å ╨┤╨╛ ╨░╨┤╨╝╤û╨╜╤û╤ü╤é╤Ç╨░╤é╨╛╤Ç╨░.",
                replyMarkup: GetBackToMainMenu(),
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
                text: "Γ¥î ╨¥╨╡╨▓╤û╤Ç╨╜╨╕╨╣ ╤ä╨╛╤Ç╨╝╨░╤é email. ╨æ╤â╨┤╤î ╨╗╨░╤ü╨║╨░, ╨▓╨▓╨╡╨┤╤û╤é╤î ╨║╨╛╤Ç╨╡╨║╤é╨╜╤â ╨░╨┤╤Ç╨╡╤ü╤â.\n\n" +
                      "<i>╨¥╨░╨┐╤Ç╨╕╨║╨╗╨░╨┤: student@vnmu.edu.ua</i>",
                parseMode: ParseMode.Html,
                cancellationToken: cancellationToken);
            return;
        }

        if (email.Length > 100)
        {
            await botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: "Γ¥î Email ╨╖╨░╨╜╨░╨┤╤é╨╛ ╨┤╨╛╨▓╨│╨╕╨╣. ╨£╨░╨║╤ü╨╕╨╝╤â╨╝ 100 ╤ü╨╕╨╝╨▓╨╛╨╗╤û╨▓.",
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
                    text: $"Γ£à <b>╨Ü╨╛╨┤ ╨▓╨╡╤Ç╨╕╤ä╤û╨║╨░╤å╤û╤ù ╨▓╤û╨┤╨┐╤Ç╨░╨▓╨╗╨╡╨╜╨╛!</b>\n\n" +
                          $"≡ƒôº ╨¥╨░ ╨░╨┤╤Ç╨╡╤ü╤â <code>{email}</code> ╨╜╨░╨┤╤û╤ü╨╗╨░╨╜╨╛ ╨╗╨╕╤ü╤é ╨╖ 6-╨╖╨╜╨░╤ç╨╜╨╕╨╝ ╨║╨╛╨┤╨╛╨╝.\n\n" +
                          $"ΓÅ░ ╨Ü╨╛╨┤ ╨┤╤û╨╣╤ü╨╜╨╕╨╣ ╨┐╤Ç╨╛╤é╤Å╨│╨╛╨╝ 15 ╤à╨▓╨╕╨╗╨╕╨╜.\n\n" +
                          $"╨Æ╨▓╨╡╨┤╤û╤é╤î ╨║╨╛╨┤ ╨╖ ╨╗╨╕╤ü╤é╨░:",
                    parseMode: ParseMode.Html,
                    replyMarkup: new InlineKeyboardMarkup(InlineKeyboardButton.WithCallbackData("Γ¥î ╨í╨║╨░╤ü╤â╨▓╨░╤é╨╕", "profile_view")),
                    cancellationToken: cancellationToken);
            }
            else
            {
                await botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: $"Γ¥î {result.Error}\n\n╨í╨┐╤Ç╨╛╨▒╤â╨╣╤é╨╡ ╤ë╨╡ ╤Ç╨░╨╖ ╨░╨▒╨╛ ╤ü╨║╨░╤ü╤â╨╣╤é╨╡ ╨╛╨┐╨╡╤Ç╨░╤å╤û╤Ä /cancel",
                    replyMarkup: new InlineKeyboardMarkup(InlineKeyboardButton.WithCallbackData("Γ¥î ╨í╨║╨░╤ü╤â╨▓╨░╤é╨╕", "profile_view")),
                    cancellationToken: cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "╨ƒ╨╛╨╝╨╕╨╗╨║╨░ ╨┐╤Ç╨╕ ╨▓╤û╨┤╨┐╤Ç╨░╨▓╤å╤û verification email ╨┤╨╗╤Å ╨║╨╛╤Ç╨╕╤ü╤é╤â╨▓╨░╤ç╨░ {UserId}", userId);

            await botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: "Γ¥î ╨Æ╨╕╨╜╨╕╨║╨╗╨░ ╤é╨╡╤à╨╜╤û╤ç╨╜╨░ ╨┐╨╛╨╝╨╕╨╗╨║╨░ ╨┐╤Ç╨╕ ╨▓╤û╨┤╨┐╤Ç╨░╨▓╤å╤û ╨║╨╛╨┤╤â. ╨í╨┐╤Ç╨╛╨▒╤â╨╣╤é╨╡ ╨┐╤û╨╖╨╜╤û╤ê╨╡.",
                replyMarkup: GetBackToMainMenu(),
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
                text: "Γ¥î ╨¥╨╡╨▓╤û╤Ç╨╜╨╕╨╣ ╤ä╨╛╤Ç╨╝╨░╤é ╨║╨╛╨┤╤â. ╨æ╤â╨┤╤î ╨╗╨░╤ü╨║╨░, ╨▓╨▓╨╡╨┤╤û╤é╤î 6 ╤å╨╕╤ä╤Ç.\n\n" +
                      "<i>╨ƒ╨╡╤Ç╨╡╨▓╤û╤Ç╤é╨╡ ╨║╨╛╨┤ ╤â ╨╗╨╕╤ü╤é╤û ╤é╨░ ╤ü╨┐╤Ç╨╛╨▒╤â╨╣╤é╨╡ ╤ë╨╡ ╤Ç╨░╨╖.</i>",
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
                    text: "Γ£à <b>Email ╤â╤ü╨┐╤û╤ê╨╜╨╛ ╨┐╤û╨┤╤é╨▓╨╡╤Ç╨┤╨╢╨╡╨╜╨╛!</b>\n\n" +
                          "≡ƒÄë ╨ó╨╡╨┐╨╡╤Ç ╨▓╨╕ ╨╝╨╛╨╢╨╡╤é╨╡ ╨╛╤é╤Ç╨╕╨╝╤â╨▓╨░╤é╨╕ ╤ü╨┐╨╛╨▓╤û╤ë╨╡╨╜╨╜╤Å ╨╜╨░ ╨▓╨░╤ê╤â ╨╡╨╗╨╡╨║╤é╤Ç╨╛╨╜╨╜╤â ╨┐╨╛╤ê╤é╤â.\n\n" +
                          "╨ö╤Å╨║╤â╤ö╨╝╨╛ ╨╖╨░ ╨▓╨╡╤Ç╨╕╤ä╤û╨║╨░╤å╤û╤Ä!",
                    parseMode: ParseMode.Html,
                    replyMarkup: GetBackToMainMenu(),
                    cancellationToken: cancellationToken);

                // Clear state
                await _stateManager.ClearStateAsync(userId, cancellationToken);
            }
            else
            {
                await botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: $"Γ¥î {result.Error}\n\n" +
                          "╨ƒ╨╡╤Ç╨╡╨▓╤û╤Ç╤é╨╡ ╨║╨╛╨┤ ╤é╨░ ╤ü╨┐╤Ç╨╛╨▒╤â╨╣╤é╨╡ ╤ë╨╡ ╤Ç╨░╨╖, ╨░╨▒╨╛ ╨╖╨░╨┐╨╕╤é╨░╨╣╤é╨╡ ╨╜╨╛╨▓╨╕╨╣ ╨║╨╛╨┤ ╨┐╨╛╨▓╤é╨╛╤Ç╨╜╨╛.",
                    replyMarkup: new InlineKeyboardMarkup(InlineKeyboardButton.WithCallbackData("Γ¥î ╨í╨║╨░╤ü╤â╨▓╨░╤é╨╕", "profile_view")),
                    cancellationToken: cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "╨ƒ╨╛╨╝╨╕╨╗╨║╨░ ╨┐╤Ç╨╕ ╨▓╨╡╤Ç╨╕╤ä╤û╨║╨░╤å╤û╤ù email ╨┤╨╗╤Å ╨║╨╛╤Ç╨╕╤ü╤é╤â╨▓╨░╤ç╨░ {UserId}", userId);

            await botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: "Γ¥î ╨Æ╨╕╨╜╨╕╨║╨╗╨░ ╤é╨╡╤à╨╜╤û╤ç╨╜╨░ ╨┐╨╛╨╝╨╕╨╗╨║╨░ ╨┐╤Ç╨╕ ╨┐╨╡╤Ç╨╡╨▓╤û╤Ç╤å╤û ╨║╨╛╨┤╤â. ╨í╨┐╤Ç╨╛╨▒╤â╨╣╤é╨╡ ╨┐╤û╨╖╨╜╤û╤ê╨╡.",
                replyMarkup: GetBackToMainMenu(),
                cancellationToken: cancellationToken);

            await _stateManager.ClearStateAsync(userId, cancellationToken);
        }
    }

    private async Task HandleCloseReasonInputAsync(
        ITelegramBotClient botClient,
        Message message,
        CancellationToken cancellationToken)
    {
        var userId = message.From!.Id;
        var reason = message.Text?.Trim();

        // Validate reason length
        if (string.IsNullOrWhiteSpace(reason) || reason.Length < 5)
        {
            await botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: "❌ Причина занадто коротка. Будь ласка, введіть мінімум 5 символів.",
                cancellationToken: cancellationToken);
            return;
        }

        if (reason.Length > 500)
        {
            await botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: "❌ Причина занадто довга. Максимум 500 символів.",
                cancellationToken: cancellationToken);
            return;
        }

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

            // Retrieve stored appeal ID
            var appealId = await _stateManager.GetDataAsync<int>(userId, "close_appeal_id", cancellationToken);

            if (appealId == 0)
            {
                await botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: "❌ Помилка: ID звернення не знайдено. Спробуйте ще раз через адмін-панель.",
                    replyMarkup: GetBackToMainMenu(),
                    cancellationToken: cancellationToken);

                await _stateManager.ClearStateAsync(userId, cancellationToken);
                await _stateManager.ClearAllDataAsync(userId, cancellationToken);
                return;
            }

            // Close appeal with provided reason
            var result = await mediator.Send(new CloseAppealCommand
            {
                AppealId = appealId,
                AdminId = userId,
                Reason = reason
            }, cancellationToken);

            if (result.IsSuccess)
            {
                await botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: $"✅ <b>Звернення #{appealId} успішно закрито</b>\n\n" +
                          $"📝 Причина: {reason}",
                    parseMode: ParseMode.Html,
                    replyMarkup: GetBackToMainMenu(),
                    cancellationToken: cancellationToken);

                // Clear state and data
                await _stateManager.ClearStateAsync(userId, cancellationToken);
                await _stateManager.RemoveDataAsync(userId, "close_appeal_id", cancellationToken);
            }
            else
            {
                await botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: $"❌ Помилка при закритті звернення: {result.Error}\n\n" +
                          "Спробуйте ще раз.",
                    cancellationToken: cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Помилка при обробці причини закриття звернення для користувача {UserId}", userId);

            await botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: "❌ Виникла технічна помилка при закритті звернення. Спробуйте пізніше.",
                replyMarkup: GetBackToMainMenu(),
                cancellationToken: cancellationToken);

            await _stateManager.ClearStateAsync(userId, cancellationToken);
            await _stateManager.ClearAllDataAsync(userId, cancellationToken);
        }
    }

    private async Task HandleCallbackQueryAsync(
        ITelegramBotClient botClient,
        CallbackQuery callbackQuery,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "╨₧╤é╤Ç╨╕╨╝╨░╨╜╨╛ callback ╨▓╤û╨┤ {UserId}: {Data}",
            callbackQuery.From.Id,
            callbackQuery.Data);

        if (callbackQuery.Data == null || callbackQuery.Message == null)
            return;

        var data = callbackQuery.Data;

        // ╨₧╨▒╤Ç╨╛╨▒╨║╨░ ╤Ç╤û╨╖╨╜╨╕╤à callback'╤û╨▓
        try
        {
            if (data == "back_to_main")
            {
                // ╨í╨║╨░╤ü╨╛╨▓╤â╤ö╨╝╨╛ ╨▒╤â╨┤╤î-╤Å╨║╨╕╨╣ ╨░╨║╤é╨╕╨▓╨╜╨╕╨╣ ╨┐╤Ç╨╛╤å╨╡╤ü
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
                    text: "≡ƒÄô <b>╨ô╨╛╨╗╨╛╨▓╨╜╨╡ ╨╝╨╡╨╜╤Ä</b>\n\n╨₧╨▒╨╡╤Ç╤û╤é╤î ╨┤╤û╤Ä:",
                    parseMode: ParseMode.Html,
                    replyMarkup: GetMainMenu(isAdminBack),
                    cancellationToken: cancellationToken);
            }
            else if (data == "appeal_create")
            {
                await botClient.EditMessageTextAsync(
                    chatId: callbackQuery.Message.Chat.Id,
                    messageId: callbackQuery.Message.MessageId,
                    text: "≡ƒô¥ <b>╨í╤é╨▓╨╛╤Ç╨╡╨╜╨╜╤Å ╨╖╨▓╨╡╤Ç╨╜╨╡╨╜╨╜╤Å</b>\n\n╨₧╨▒╨╡╤Ç╤û╤é╤î ╨║╨░╤é╨╡╨│╨╛╤Ç╤û╤Ä:",
                    parseMode: ParseMode.Html,
                    replyMarkup: GetAppealCategories(),
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
            else if (data == "profile_change_language")
            {
                await HandleProfileChangeLanguageCallback(botClient, callbackQuery, cancellationToken);
            }
            else if (data.StartsWith("set_lang_"))
            {
                await HandleSetLanguageCallback(botClient, callbackQuery, cancellationToken);
            }
            else if (data == "help")
            {
                await botClient.EditMessageTextAsync(
                    chatId: callbackQuery.Message.Chat.Id,
                    messageId: callbackQuery.Message.MessageId,
                    text: "Γä╣∩╕Å <b>╨ö╨╛╨┐╨╛╨╝╨╛╨│╨░</b>\n\n" +
                          "╨Æ╨╕╨║╨╛╤Ç╨╕╤ü╤é╨╛╨▓╤â╨╣╤é╨╡ ╨╝╨╡╨╜╤Ä ╨┤╨╗╤Å ╨╜╨░╨▓╤û╨│╨░╤å╤û╤ù.\n" +
                          "╨Ü╨╛╨╝╨░╨╜╨┤╨╕: /start, /help, /appeal, /contacts",
                    parseMode: ParseMode.Html,
                    replyMarkup: GetBackToMainMenu(),
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
            _logger.LogError(ex, "╨ƒ╨╛╨╝╨╕╨╗╨║╨░ ╨┐╤Ç╨╕ ╨╛╨▒╤Ç╨╛╨▒╤å╤û callback: {Data}", data);
            await botClient.AnswerCallbackQueryAsync(
                callbackQueryId: callbackQuery.Id,
                text: "Γ¥î ╨Æ╨╕╨╜╨╕╨║╨╗╨░ ╨┐╨╛╨╝╨╕╨╗╨║╨░. ╨í╨┐╤Ç╨╛╨▒╤â╨╣╤é╨╡ ╤ë╨╡ ╤Ç╨░╨╖.",
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
            { 1, "≡ƒÆ░ ╨í╤é╨╕╨┐╨╡╨╜╨┤╤û╤Å" },
            { 2, "≡ƒÅá ╨ô╤â╤Ç╤é╨╛╨╢╨╕╤é╨╛╨║" },
            { 3, "≡ƒÄë ╨ù╨░╤à╨╛╨┤╨╕" },
            { 4, "≡ƒÆí ╨ƒ╤Ç╨╛╨┐╨╛╨╖╨╕╤å╤û╤Å" },
            { 5, "ΓÜá∩╕Å ╨í╨║╨░╤Ç╨│╨░" },
            { 6, "≡ƒô¥ ╨å╨╜╤ê╨╡" }
        };

        var categoryName = categoryNames.GetValueOrDefault(categoryId, "╨¥╨╡╨▓╤û╨┤╨╛╨╝╨░ ╨║╨░╤é╨╡╨│╨╛╤Ç╤û╤Å");
        var category = (AppealCategory)categoryId;

        // ╨ù╨▒╨╡╤Ç╤û╨│╨░╤ö╨╝╨╛ ╨╛╨▒╤Ç╨░╨╜╤â ╨║╨░╤é╨╡╨│╨╛╤Ç╤û╤Ä
        await _stateManager.SetDataAsync(userId, "appeal_category", category, cancellationToken);

        // ╨Æ╤ü╤é╨░╨╜╨╛╨▓╨╗╤Ä╤ö╨╝╨╛ ╤ü╤é╨░╨╜ ╨╛╤ç╤û╨║╤â╨▓╨░╨╜╨╜╤Å ╤é╨╡╨╝╨╕
        await _stateManager.SetStateAsync(userId, UserConversationState.WaitingAppealSubject, cancellationToken);

        await botClient.EditMessageTextAsync(
            chatId: callbackQuery.Message!.Chat.Id,
            messageId: callbackQuery.Message.MessageId,
            text: $"≡ƒô¥ <b>╨í╤é╨▓╨╛╤Ç╨╡╨╜╨╜╤Å ╨╖╨▓╨╡╤Ç╨╜╨╡╨╜╨╜╤Å</b>\n\n" +
                  $"Γ£à ╨Ü╨░╤é╨╡╨│╨╛╤Ç╤û╤Å: {categoryName}\n\n" +
                  $"╨¥╨░╨┐╨╕╤ê╤û╤é╤î <b>╤é╨╡╨╝╤â</b> ╨▓╨░╤ê╨╛╨│╨╛ ╨╖╨▓╨╡╤Ç╨╜╨╡╨╜╨╜╤Å:\n\n" +
                  $"<i>╨£╤û╨╜╤û╨╝╤â╨╝ 5 ╤ü╨╕╨╝╨▓╨╛╨╗╤û╨▓, ╨╝╨░╨║╤ü╨╕╨╝╤â╨╝ 200 ╤ü╨╕╨╝╨▓╨╛╨╗╤û╨▓</i>",
            parseMode: ParseMode.Html,
            replyMarkup: GetBackToMainMenu(),
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

            // ╨₧╤é╤Ç╨╕╨╝╤â╤ö╨╝╨╛ ╨╖╨▓╨╡╤Ç╨╜╨╡╨╜╨╜╤Å ╨║╨╛╤Ç╨╕╤ü╤é╤â╨▓╨░╤ç╨░
            var query = new GetUserAppealsQuery
            {
                UserId = userId,
                PageSize = 5,
                PageNumber = 1,
                OnlyActive = false // ╨ƒ╨╛╨║╨░╨╖╤â╤ö╨╝╨╛ ╨▓╤ü╤û ╨╖╨▓╨╡╤Ç╨╜╨╡╨╜╨╜╤Å
            };

            var result = await mediator.Send(query, cancellationToken);

            if (!result.IsSuccess)
            {
                await botClient.EditMessageTextAsync(
                    chatId: callbackQuery.Message!.Chat.Id,
                    messageId: callbackQuery.Message.MessageId,
                    text: $"Γ¥î ╨ƒ╨╛╨╝╨╕╨╗╨║╨░: {result.Error}",
                    parseMode: ParseMode.Html,
                    replyMarkup: GetBackToMainMenu(),
                    cancellationToken: cancellationToken);
                return;
            }

            var appeals = result.Value ?? new List<StudentUnionBot.Application.Appeals.DTOs.AppealDto>();

            if (appeals.Count == 0)
            {
                await botClient.EditMessageTextAsync(
                    chatId: callbackQuery.Message!.Chat.Id,
                    messageId: callbackQuery.Message.MessageId,
                    text: "≡ƒôï <b>╨£╨╛╤ù ╨╖╨▓╨╡╤Ç╨╜╨╡╨╜╨╜╤Å</b>\n\n" +
                          "╨ú ╨▓╨░╤ü ╨┐╨╛╨║╨╕ ╨╜╨╡╨╝╨░╤ö ╨╖╨▓╨╡╤Ç╨╜╨╡╨╜╤î.\n\n" +
                          "╨¥╨░╤é╨╕╤ü╨╜╤û╤é╤î \"≡ƒô¥ ╨í╤é╨▓╨╛╤Ç╨╕╤é╨╕ ╨╖╨▓╨╡╤Ç╨╜╨╡╨╜╨╜╤Å\" ╤ë╨╛╨▒ ╨┐╨╛╨┤╨░╤é╨╕ ╨╜╨╛╨▓╨╡ ╨╖╨▓╨╡╤Ç╨╜╨╡╨╜╨╜╤Å.",
                    parseMode: ParseMode.Html,
                    replyMarkup: GetBackToMainMenu(),
                    cancellationToken: cancellationToken);
                return;
            }

            // ╨ñ╨╛╤Ç╨╝╤â╤ö╨╝╨╛ ╤é╨╡╨║╤ü╤é ╨╖╤û ╤ü╨┐╨╕╤ü╨║╨╛╨╝ ╨╖╨▓╨╡╤Ç╨╜╨╡╨╜╤î
            var text = "≡ƒôï <b>╨£╨╛╤ù ╨╖╨▓╨╡╤Ç╨╜╨╡╨╜╨╜╤Å</b>\n\n";

            foreach (var appeal in appeals)
            {
                var statusEmoji = appeal.Status.GetEmoji();
                var categoryEmoji = appeal.Category.GetEmoji();
                var daysAgo = (DateTime.UtcNow - appeal.CreatedAt).Days;
                var timeAgo = daysAgo == 0 ? "╤ü╤î╨╛╨│╨╛╨┤╨╜╤û" : 
                              daysAgo == 1 ? "╨▓╤ç╨╛╤Ç╨░" : 
                              $"{daysAgo} ╨┤╨╜. ╤é╨╛╨╝╤â";

                text += $"ΓöüΓöüΓöüΓöüΓöüΓöüΓöüΓöüΓöüΓöüΓöüΓöüΓöüΓöü\n";
                text += $"<b>#{appeal.Id}</b> {categoryEmoji} {appeal.CategoryName}\n";
                text += $"≡ƒôî <b>{appeal.Subject}</b>\n";
                text += $"{statusEmoji} {appeal.StatusName}\n";
                text += $"≡ƒôà {appeal.CreatedAt:dd.MM.yyyy} ({timeAgo})\n";

                if (appeal.MessageCount > 0)
                {
                    text += $"≡ƒÆ¼ ╨ƒ╨╛╨▓╤û╨┤╨╛╨╝╨╗╨╡╨╜╤î: {appeal.MessageCount}\n";
                }

                if (appeal.ClosedAt.HasValue)
                {
                    text += $"Γ£à ╨ù╨░╨║╤Ç╨╕╤é╨╛: {appeal.ClosedAt.Value:dd.MM.yyyy HH:mm}\n";
                }

                text += "\n";
            }

            // ╨í╤é╨▓╨╛╤Ç╤Ä╤ö╨╝╨╛ ╨║╨╗╨░╨▓╤û╨░╤é╤â╤Ç╤â ╨╖ ╨║╨╜╨╛╨┐╨║╨░╨╝╨╕ ╨┤╨╗╤Å ╨║╨╛╨╢╨╜╨╛╨│╨╛ ╨╖╨▓╨╡╤Ç╨╜╨╡╨╜╨╜╤Å
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

            // ╨Ü╨╜╨╛╨┐╨║╨░ "╨¥╨░╨╖╨░╨┤"
            buttons.Add(new List<InlineKeyboardButton>
            {
                InlineKeyboardButton.WithCallbackData("≡ƒöÖ ╨¥╨░╨╖╨░╨┤ ╨┤╨╛ ╨╝╨╡╨╜╤Ä", "back_to_main")
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
            _logger.LogError(ex, "╨ƒ╨╛╨╝╨╕╨╗╨║╨░ ╨┐╤Ç╨╕ ╨╛╤é╤Ç╨╕╨╝╨░╨╜╨╜╤û ╨╖╨▓╨╡╤Ç╨╜╨╡╨╜╤î ╨║╨╛╤Ç╨╕╤ü╤é╤â╨▓╨░╤ç╨░ {UserId}", userId);

            await botClient.EditMessageTextAsync(
                chatId: callbackQuery.Message!.Chat.Id,
                messageId: callbackQuery.Message.MessageId,
                text: "Γ¥î ╨Æ╨╕╨╜╨╕╨║╨╗╨░ ╨┐╨╛╨╝╨╕╨╗╨║╨░ ╨┐╤Ç╨╕ ╨╖╨░╨▓╨░╨╜╤é╨░╨╢╨╡╨╜╨╜╤û ╨╖╨▓╨╡╤Ç╨╜╨╡╨╜╤î. ╨í╨┐╤Ç╨╛╨▒╤â╨╣╤é╨╡ ╨┐╤û╨╖╨╜╤û╤ê╨╡.",
                parseMode: ParseMode.Html,
                replyMarkup: GetBackToMainMenu(),
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
        
        // ╨ƒ╨░╤Ç╤ü╨╕╨╝╨╛ ID ╨╖╨▓╨╡╤Ç╨╜╨╡╨╜╨╜╤Å ╨╖ callback data (appeal_view_123)
        var appealIdStr = callbackData.Replace("appeal_view_", "");
        if (!int.TryParse(appealIdStr, out var appealId))
        {
            await botClient.AnswerCallbackQueryAsync(
                callbackQuery.Id,
                "Γ¥î ╨¥╨╡╨▓╤û╤Ç╨╜╨╕╨╣ ╤ä╨╛╤Ç╨╝╨░╤é ╨╖╨▓╨╡╤Ç╨╜╨╡╨╜╨╜╤Å",
                showAlert: true,
                cancellationToken: cancellationToken);
            return;
        }

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

            // ╨₧╤é╤Ç╨╕╨╝╤â╤ö╨╝╨╛ ╨┤╨╡╤é╨░╨╗╤û ╨╖╨▓╨╡╤Ç╨╜╨╡╨╜╨╜╤Å
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
                    text: $"Γ¥î {result.Error}",
                    parseMode: ParseMode.Html,
                    replyMarkup: new InlineKeyboardMarkup(new[]
                    {
                        InlineKeyboardButton.WithCallbackData("≡ƒöÖ ╨ö╨╛ ╤ü╨┐╨╕╤ü╨║╤â ╨╖╨▓╨╡╤Ç╨╜╨╡╨╜╤î", "appeal_list")
                    }),
                    cancellationToken: cancellationToken);
                return;
            }

            var appeal = result.Value!;

            // ╨ñ╨╛╤Ç╨╝╤â╤ö╨╝╨╛ ╤é╨╡╨║╤ü╤é ╨╖ ╨┤╨╡╤é╨░╨╗╤Å╨╝╨╕ ╨╖╨▓╨╡╤Ç╨╜╨╡╨╜╨╜╤Å
            var statusEmoji = appeal.Status.GetEmoji();
            var categoryEmoji = appeal.Category.GetEmoji();
            
            var text = $"≡ƒôï <b>╨ù╨▓╨╡╤Ç╨╜╨╡╨╜╨╜╤Å #{appeal.Id}</b>\n\n";
            text += $"{categoryEmoji} <b>╨Ü╨░╤é╨╡╨│╨╛╤Ç╤û╤Å:</b> {appeal.CategoryName}\n";
            text += $"{statusEmoji} <b>╨í╤é╨░╤é╤â╤ü:</b> {appeal.StatusName}\n";
            text += $"≡ƒôè <b>╨ƒ╤Ç╤û╨╛╤Ç╨╕╤é╨╡╤é:</b> {appeal.PriorityName}\n";
            text += $"≡ƒôà <b>╨í╤é╨▓╨╛╤Ç╨╡╨╜╨╛:</b> {appeal.CreatedAt:dd.MM.yyyy HH:mm}\n";
            
            if (appeal.FirstResponseAt.HasValue)
            {
                text += $"ΓÅ▒ <b>╨ƒ╨╡╤Ç╤ê╨░ ╨▓╤û╨┤╨┐╨╛╨▓╤û╨┤╤î:</b> {appeal.FirstResponseAt.Value:dd.MM.yyyy HH:mm}\n";
            }
            
            if (appeal.ClosedAt.HasValue)
            {
                text += $"Γ£à <b>╨ù╨░╨║╤Ç╨╕╤é╨╛:</b> {appeal.ClosedAt.Value:dd.MM.yyyy HH:mm}\n";
                if (!string.IsNullOrEmpty(appeal.ClosedReason))
                {
                    text += $"<b>╨ƒ╤Ç╨╕╤ç╨╕╨╜╨░ ╨╖╨░╨║╤Ç╨╕╤é╤é╤Å:</b> {appeal.ClosedReason}\n";
                }
            }
            
            text += $"\n<b>╨ó╨╡╨╝╨░:</b>\n{appeal.Subject}\n\n";
            text += $"<b>╨ƒ╨╛╨▓╤û╨┤╨╛╨╝╨╗╨╡╨╜╨╜╤Å:</b>\n{appeal.Message}\n";

            // ╨ö╨╛╨┤╨░╤ö╨╝╨╛ ╤û╤ü╤é╨╛╤Ç╤û╤Ä ╨┐╨╛╨▓╤û╨┤╨╛╨╝╨╗╨╡╨╜╤î, ╤Å╨║╤ë╨╛ ╨▓╨╛╨╜╨╕ ╤ö
            if (appeal.Messages.Count > 0)
            {
                text += "\nΓöüΓöüΓöüΓöüΓöüΓöüΓöüΓöüΓöüΓöüΓöüΓöüΓöüΓöü\n";
                text += "<b>≡ƒô¼ ╨å╤ü╤é╨╛╤Ç╤û╤Å ╨┐╨╛╨▓╤û╨┤╨╛╨╝╨╗╨╡╨╜╤î:</b>\n\n";

                foreach (var msg in appeal.Messages)
                {
                    var senderIcon = msg.IsFromAdmin ? "≡ƒæ¿ΓÇì≡ƒÆ╝" : "≡ƒæñ";
                    var senderLabel = msg.IsFromAdmin ? "╨É╨┤╨╝╤û╨╜╤û╤ü╤é╤Ç╨░╤é╨╛╤Ç" : "╨Æ╨╕";
                    
                    text += $"{senderIcon} <b>{senderLabel}</b> ({msg.SentAt:dd.MM HH:mm})\n";
                    text += $"{msg.Text}\n\n";
                }
            }

            // ╨í╤é╨▓╨╛╤Ç╤Ä╤ö╨╝╨╛ ╨║╨╗╨░╨▓╤û╨░╤é╤â╤Ç╤â
            var buttons = new List<List<InlineKeyboardButton>>();

            // ╨»╨║╤ë╨╛ ╨╖╨▓╨╡╤Ç╨╜╨╡╨╜╨╜╤Å ╨░╨║╤é╨╕╨▓╨╜╨╡ - ╨╝╨╛╨╢╨╜╨░ ╨┤╨╛╨┤╨░╤é╨╕ ╨┐╨╛╨▓╤û╨┤╨╛╨╝╨╗╨╡╨╜╨╜╤Å
            if (appeal.Status != AppealStatus.Closed)
            {
                buttons.Add(new List<InlineKeyboardButton>
                {
                    InlineKeyboardButton.WithCallbackData(
                        "≡ƒÆ¼ ╨ö╨╛╨┤╨░╤é╨╕ ╨┐╨╛╨▓╤û╨┤╨╛╨╝╨╗╨╡╨╜╨╜╤Å",
                        $"appeal_add_msg_{appeal.Id}"
                    )
                });
            }

            // ╨Ü╨╜╨╛╨┐╨║╨░ ╨╜╨░╨╖╨░╨┤
            buttons.Add(new List<InlineKeyboardButton>
            {
                InlineKeyboardButton.WithCallbackData("≡ƒöÖ ╨ö╨╛ ╤ü╨┐╨╕╤ü╨║╤â", "appeal_list")
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
            _logger.LogError(ex, "╨ƒ╨╛╨╝╨╕╨╗╨║╨░ ╨┐╤Ç╨╕ ╨┐╨╡╤Ç╨╡╨│╨╗╤Å╨┤╤û ╨╖╨▓╨╡╤Ç╨╜╨╡╨╜╨╜╤Å {AppealId}", appealId);

            await botClient.EditMessageTextAsync(
                chatId: callbackQuery.Message!.Chat.Id,
                messageId: callbackQuery.Message.MessageId,
                text: "Γ¥î ╨Æ╨╕╨╜╨╕╨║╨╗╨░ ╨┐╨╛╨╝╨╕╨╗╨║╨░ ╨┐╤Ç╨╕ ╨╖╨░╨▓╨░╨╜╤é╨░╨╢╨╡╨╜╨╜╤û ╨╖╨▓╨╡╤Ç╨╜╨╡╨╜╨╜╤Å",
                parseMode: ParseMode.Html,
                replyMarkup: new InlineKeyboardMarkup(new[]
                {
                    InlineKeyboardButton.WithCallbackData("≡ƒöÖ ╨ö╨╛ ╤ü╨┐╨╕╤ü╨║╤â", "appeal_list")
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
                "Γ¥î ╨ƒ╨╛╨╝╨╕╨╗╨║╨░: ╨║╨╛╤Ç╨╕╤ü╤é╤â╨▓╨░╤ç ╨╜╨╡ ╨╖╨╜╨░╨╣╨┤╨╡╨╜╨╕╨╣",
                showAlert: true,
                cancellationToken: cancellationToken);
            return;
        }

        var username = user.Username ?? "╨╜╨╡╨╝╨░╤ö";
        var fullName = user.FullName ?? $"{callbackQuery.From.FirstName} {callbackQuery.From.LastName}".Trim();
        var email = user.Email ?? "<i>╨╜╨╡ ╨▓╨║╨░╨╖╨░╨╜╨╛</i>";
        var emailStatus = user.IsEmailVerified ? "Γ£à ╨ƒ╤û╨┤╤é╨▓╨╡╤Ç╨┤╨╢╨╡╨╜╨╛" : "Γ¥î ╨¥╨╡ ╨┐╤û╨┤╤é╨▓╨╡╤Ç╨┤╨╢╨╡╨╜╨╛";
        var languageDisplay = user.Language == Domain.Enums.Language.Ukrainian ? "≡ƒç║≡ƒç¼ ╨ú╨║╤Ç╨░╤ù╨╜╤ü╤î╨║╨░" : "≡ƒç¼≡ƒç¿ English";

        var profileText = "≡ƒæñ <b>╨£╤û╨╣ ╨┐╤Ç╨╛╤ä╤û╨╗╤î</b>\n\n" +
                         $"<b>╨å╨╝'╤Å:</b> {fullName}\n" +
                         $"<b>Username:</b> @{username}\n" +
                         $"<b>Email:</b> {email}\n" +
                         $"<b>╨í╤é╨░╤é╤â╤ü email:</b> {emailStatus}\n" +
                         $"<b>╨£╨╛╨▓╨░:</b> {languageDisplay}\n" +
                         $"<b>ID:</b> <code>{userId}</code>";

        // ╨Ü╨╜╨╛╨┐╨║╨╕ ╨┤╨╗╤Å ╤Ç╨╡╨┤╨░╨│╤â╨▓╨░╨╜╨╜╤Å
        var keyboard = new InlineKeyboardMarkup(new[]
        {
            new[]
            {
                InlineKeyboardButton.WithCallbackData("≡ƒôº ╨ù╨╝╤û╨╜╨╕╤é╨╕ email", "profile_edit_email")
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData("≡ƒîÅ ╨ù╨╝╤û╨╜╨╕╤é╨╕ ╨╝╨╛╨▓╤â", "profile_change_language")
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData("≡ƒöÖ ╨ô╨╛╨╗╨╛╨▓╨╜╨╡ ╨╝╨╡╨╜╤Ä", "back_to_main")
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
            text: "≡ƒôº <b>╨ù╨╝╤û╨╜╨░ email</b>\n\n" +
                  "╨Æ╨▓╨╡╨┤╤û╤é╤î ╨▓╨░╤ê╤â ╨║╨╛╤Ç╨┐╨╛╤Ç╨░╤é╨╕╨▓╨╜╤â email ╨░╨┤╤Ç╨╡╤ü╤â (╨▒╨░╨╢╨░╨╜╨╛ ╤â╨╜╤û╨▓╨╡╤Ç╤ü╨╕╤é╨╡╤é╤ü╤î╨║╤â):\n\n" +
                  "<i>╨¥╨░╨┐╤Ç╨╕╨║╨╗╨░╨┤: student@vnmu.edu.ua</i>\n\n" +
                  "╨¥╨░╨┤╤û╤ê╨╗╤û╤é╤î /cancel ╤ë╨╛╨▒ ╤ü╨║╨░╤ü╤â╨▓╨░╤é╨╕",
            parseMode: ParseMode.Html,
            replyMarkup: new InlineKeyboardMarkup(InlineKeyboardButton.WithCallbackData("Γ¥î ╨í╨║╨░╤ü╤â╨▓╨░╤é╨╕", "profile_view")),
            cancellationToken: cancellationToken);
    }

    private async Task HandleProfileChangeLanguageCallback(
        ITelegramBotClient botClient,
        CallbackQuery callbackQuery,
        CancellationToken cancellationToken)
    {
        var keyboard = new InlineKeyboardMarkup(new[]
        {
            new[]
            {
                InlineKeyboardButton.WithCallbackData("≡ƒç║≡ƒç¼ ╨ú╨║╤Ç╨░╤ù╨╜╤ü╤î╨║╨░", "set_lang_uk"),
                InlineKeyboardButton.WithCallbackData("≡ƒç¼≡ƒç¿ English", "set_lang_en")
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData("≡ƒöÖ ╨¥╨░╨╖╨░╨┤", "profile_view")
            }
        });

        await botClient.EditMessageTextAsync(
            chatId: callbackQuery.Message!.Chat.Id,
            messageId: callbackQuery.Message.MessageId,
            text: "≡ƒîÅ <b>╨Æ╨╕╨▒╤û╤Ç ╨╝╨╛╨▓╨╕</b>\n\n" +
                  "╨₧╨▒╨╡╤Ç╤û╤é╤î ╨╝╨╛╨▓╤â ╤û╨╜╤é╨╡╤Ç╤ä╨╡╨╣╤ü╤â:\n\n" +
                  "≡ƒç║≡ƒç¼ <b>╨ú╨║╤Ç╨░╤ù╨╜╤ü╤î╨║╨░</b> - ╨╝╨╛╨▓╨░ ╨╖╨░ ╨╖╨░╨╝╨╛╨▓╤ç╤â╨▓╨░╨╜╨╜╤Å╨╝\n" +
                  "≡ƒç¼≡ƒç¿ <b>English</b> - ╨░╨╜╨│╨╗╤û╨╣╤ü╤î╨║╨░ ╨╝╨╛╨▓╨░",
            parseMode: ParseMode.Html,
            replyMarkup: keyboard,
            cancellationToken: cancellationToken);
    }

    private async Task HandleSetLanguageCallback(
        ITelegramBotClient botClient,
        CallbackQuery callbackQuery,
        CancellationToken cancellationToken)
    {
        var userId = callbackQuery.From.Id;
        var data = callbackQuery.Data!;
        
        var language = data == "set_lang_uk" ? Domain.Enums.Language.Ukrainian : Domain.Enums.Language.English;
        var languageName = data == "set_lang_uk" ? "≡ƒç║≡ƒç¼ ╨ú╨║╤Ç╨░╤ù╨╜╤ü╤î╨║╨░" : "≡ƒç¼≡ƒç¿ English";

        try
        {
            using var scope = _scopeFactory.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

            var command = new ChangeLanguageCommand
            {
                TelegramId = userId,
                Language = language
            };

            var result = await mediator.Send(command, cancellationToken);

            if (result.IsSuccess)
            {
                await botClient.AnswerCallbackQueryAsync(
                    callbackQuery.Id,
                    $"Γ£à ╨£╨╛╨▓╨░ ╨╖╨╝╤û╨╜╨╡╨╜╨░ ╨╜╨░ {languageName}",
                    cancellationToken: cancellationToken);

                // ╨ƒ╨╛╨▓╨╡╤Ç╨╜╤â╤é╨╕╤ü╤î ╨┤╨╛ ╨┐╤Ç╨╛╤ä╤û╨╗╤Ä
                await HandleProfileViewCallback(botClient, callbackQuery, cancellationToken);
            }
            else
            {
                await botClient.AnswerCallbackQueryAsync(
                    callbackQuery.Id,
                    $"Γ¥î {result.Error}",
                    showAlert: true,
                    cancellationToken: cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "╨ƒ╨╛╨╝╨╕╨╗╨║╨░ ╨┐╤Ç╨╕ ╨╖╨╝╤û╨╜╤û ╨╝╨╛╨▓╨╕ ╨┤╨╗╤Å ╨║╨╛╤Ç╨╕╤ü╤é╤â╨▓╨░╤ç╨░ {UserId}", userId);
            await botClient.AnswerCallbackQueryAsync(
                callbackQuery.Id,
                "Γ¥î ╨Æ╨╕╨╜╨╕╨║╨╗╨░ ╨┐╨╛╨╝╨╕╨╗╨║╨░. ╨í╨┐╤Ç╨╛╨▒╤â╨╣╤é╨╡ ╨┐╤û╨╖╨╜╤û╤ê╨╡.",
                showAlert: true,
                cancellationToken: cancellationToken);
        }
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
            
            // ╨₧╤é╤Ç╨╕╨╝╤â╤ö╨╝╨╛ ╨╜╨╛╨▓╨╕╨╜╨╕ ╤ç╨╡╤Ç╨╡╨╖ MediatR
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
                    text: "≡ƒô░ <b>╨¥╨╛╨▓╨╕╨╜╨╕</b>\n\nΓ¥î ╨¥╨╡ ╨▓╨┤╨░╨╗╨╛╤ü╤Å ╨╖╨░╨▓╨░╨╜╤é╨░╨╢╨╕╤é╨╕ ╨╜╨╛╨▓╨╕╨╜╨╕. ╨í╨┐╤Ç╨╛╨▒╤â╨╣╤é╨╡ ╨┐╤û╨╖╨╜╤û╤ê╨╡.",
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
                    text: "≡ƒô░ <b>╨¥╨╛╨▓╨╕╨╜╨╕</b>\n\n≡ƒô¡ ╨ƒ╨╛╨║╨╕ ╤ë╨╛ ╨╜╨╡╨╝╨░╤ö ╨╛╨┐╤â╨▒╨╗╤û╨║╨╛╨▓╨░╨╜╨╕╤à ╨╜╨╛╨▓╨╕╨╜.",
                    parseMode: ParseMode.Html,
                    replyMarkup: GetBackToMainMenu(),
                    cancellationToken: cancellationToken);
                return;
            }

            // ╨ñ╨╛╤Ç╨╝╤â╤ö╨╝╨╛ ╤é╨╡╨║╤ü╤é ╨╖ ╨╜╨╛╨▓╨╕╨╜╨░╨╝╨╕
            var newsText = "≡ƒô░ <b>╨₧╤ü╤é╨░╨╜╨╜╤û ╨╜╨╛╨▓╨╕╨╜╨╕</b>\n\n";
            
            foreach (var news in newsList.Items.Take(5))
            {
                var pinnedMark = news.IsPinned ? "≡ƒôî " : "";
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
                
                newsText += $"≡ƒôà {news.CreatedAt:dd.MM.yyyy HH:mm}\n\n";
            }

            if (newsList.TotalCount > 5)
            {
                newsText += $"<i>╨ƒ╨╛╨║╨░╨╖╨░╨╜╨╛ {newsList.Items.Count} ╨╖ {newsList.TotalCount} ╨╜╨╛╨▓╨╕╨╜</i>";
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
            _logger.LogError(ex, "╨ƒ╨╛╨╝╨╕╨╗╨║╨░ ╨┐╤Ç╨╕ ╨╛╤é╤Ç╨╕╨╝╨░╨╜╨╜╤û ╨╜╨╛╨▓╨╕╨╜ ╨┤╨╗╤Å ╨║╨╛╤Ç╨╕╤ü╤é╤â╨▓╨░╤ç╨░ {UserId}", callbackQuery.From.Id);
            
            await botClient.EditMessageTextAsync(
                chatId: callbackQuery.Message!.Chat.Id,
                messageId: callbackQuery.Message.MessageId,
                text: "≡ƒô░ <b>╨¥╨╛╨▓╨╕╨╜╨╕</b>\n\nΓ¥î ╨Æ╨╕╨╜╨╕╨║╨╗╨░ ╨┐╨╛╨╝╨╕╨╗╨║╨░ ╨┐╤Ç╨╕ ╨╖╨░╨▓╨░╨╜╤é╨░╨╢╨╡╨╜╨╜╤û ╨╜╨╛╨▓╨╕╨╜.",
                parseMode: ParseMode.Html,
                replyMarkup: GetBackToMainMenu(),
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
            
            // ╨₧╤é╤Ç╨╕╨╝╤â╤ö╨╝╨╛ ╨╝╨░╨╣╨▒╤â╤é╨╜╤û ╨┐╨╛╨┤╤û╤ù ╤ç╨╡╤Ç╨╡╨╖ MediatR
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
                    text: "≡ƒÄë <b>╨ù╨░╤à╨╛╨┤╨╕</b>\n\nΓ¥î ╨¥╨╡ ╨▓╨┤╨░╨╗╨╛╤ü╤Å ╨╖╨░╨▓╨░╨╜╤é╨░╨╢╨╕╤é╨╕ ╨┐╨╛╨┤╤û╤ù. ╨í╨┐╤Ç╨╛╨▒╤â╨╣╤é╨╡ ╨┐╤û╨╖╨╜╤û╤ê╨╡.",
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
                    text: "≡ƒÄë <b>╨ù╨░╤à╨╛╨┤╨╕</b>\n\n≡ƒô¡ ╨¥╨░╤Ç╨░╨╖╤û ╨╜╨╡╨╝╨░╤ö ╨╖╨░╨┐╨╗╨░╨╜╨╛╨▓╨░╨╜╨╕╤à ╨┐╨╛╨┤╤û╨╣.\n\n" +
                          "<i>╨í╨╗╤û╨┤╨║╤â╨╣╤é╨╡ ╨╖╨░ ╨╛╨╜╨╛╨▓╨╗╨╡╨╜╨╜╤Å╨╝╨╕!</i>",
                    parseMode: ParseMode.Html,
                    replyMarkup: GetBackToMainMenu(),
                    cancellationToken: cancellationToken);
                return;
            }

            // ╨ñ╨╛╤Ç╨╝╤â╤ö╨╝╨╛ ╤é╨╡╨║╤ü╤é ╨╖ ╨┐╨╛╨┤╤û╤Å╨╝╨╕
            var eventsText = "≡ƒÄë <b>╨£╨░╨╣╨▒╤â╤é╨╜╤û ╨╖╨░╤à╨╛╨┤╨╕</b>\n\n";
            
            foreach (var ev in eventsList.Items.Take(5))
            {
                var featuredMark = ev.IsFeatured ? "Γ¡É " : "";
                eventsText += $"{featuredMark}{ev.TypeEmoji} <b>{ev.Title}</b>\n";
                
                eventsText += $"≡ƒôà {ev.StartDate:dd.MM.yyyy HH:mm}";
                if (ev.EndDate.HasValue)
                {
                    eventsText += $" - {ev.EndDate.Value:HH:mm}";
                }
                eventsText += "\n";
                
                if (!string.IsNullOrEmpty(ev.Location))
                {
                    eventsText += $"≡ƒôì {ev.Location}\n";
                }
                
                if (ev.RequiresRegistration)
                {
                    var spotsLeft = ev.MaxParticipants.HasValue 
                        ? $"{ev.MaxParticipants.Value - ev.CurrentParticipants}" 
                        : "Γê₧";
                    eventsText += $"≡ƒæÑ ╨á╨╡╤ö╤ü╤é╤Ç╨░╤å╤û╤Å: {ev.CurrentParticipants}/{(ev.MaxParticipants?.ToString() ?? "Γê₧")} (╨▓╤û╨╗╤î╨╜╨╛: {spotsLeft})\n";
                    
                    if (ev.RegistrationDeadline.HasValue)
                    {
                        eventsText += $"ΓÅ░ ╨ö╨╡╨┤╨╗╨░╨╣╨╜: {ev.RegistrationDeadline.Value:dd.MM.yyyy HH:mm}\n";
                    }
                }
                
                eventsText += "\n";
            }

            if (eventsList.TotalCount > 5)
            {
                eventsText += $"<i>╨ƒ╨╛╨║╨░╨╖╨░╨╜╨╛ {eventsList.Items.Count} ╨╖ {eventsList.TotalCount} ╨┐╨╛╨┤╤û╨╣</i>";
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
            _logger.LogError(ex, "╨ƒ╨╛╨╝╨╕╨╗╨║╨░ ╨┐╤Ç╨╕ ╨╛╤é╤Ç╨╕╨╝╨░╨╜╨╜╤û ╨┐╨╛╨┤╤û╨╣ ╨┤╨╗╤Å ╨║╨╛╤Ç╨╕╤ü╤é╤â╨▓╨░╤ç╨░ {UserId}", callbackQuery.From.Id);
            
            await botClient.EditMessageTextAsync(
                chatId: callbackQuery.Message!.Chat.Id,
                messageId: callbackQuery.Message.MessageId,
                text: "≡ƒÄë <b>╨ù╨░╤à╨╛╨┤╨╕</b>\n\nΓ¥î ╨Æ╨╕╨╜╨╕╨║╨╗╨░ ╨┐╨╛╨╝╨╕╨╗╨║╨░ ╨┐╤Ç╨╕ ╨╖╨░╨▓╨░╨╜╤é╨░╨╢╨╡╨╜╨╜╤û ╨┐╨╛╨┤╤û╨╣.",
                parseMode: ParseMode.Html,
                replyMarkup: GetBackToMainMenu(),
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
            
            // ╨₧╤é╤Ç╨╕╨╝╤â╤ö╨╝╨╛ ╨┐╨░╤Ç╤é╨╜╨╡╤Ç╤û╨▓ ╤ç╨╡╤Ç╨╡╨╖ MediatR
            var query = new GetActivePartnersQuery();
            var result = await mediator.Send(query, cancellationToken);

            if (!result.IsSuccess || result.Value == null)
            {
                await botClient.EditMessageTextAsync(
                    chatId: callbackQuery.Message!.Chat.Id,
                    messageId: callbackQuery.Message.MessageId,
                    text: "≡ƒñ¥ <b>╨ƒ╨░╤Ç╤é╨╜╨╡╤Ç╨╕</b>\n\nΓ¥î ╨¥╨╡ ╨▓╨┤╨░╨╗╨╛╤ü╤Å ╨╖╨░╨▓╨░╨╜╤é╨░╨╢╨╕╤é╨╕ ╨┐╨░╤Ç╤é╨╜╨╡╤Ç╤û╨▓. ╨í╨┐╤Ç╨╛╨▒╤â╨╣╤é╨╡ ╨┐╤û╨╖╨╜╤û╤ê╨╡.",
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
                    text: "≡ƒñ¥ <b>╨ƒ╨░╤Ç╤é╨╜╨╡╤Ç╨╕</b>\n\n≡ƒô¡ ╨¥╨░╤Ç╨░╨╖╤û ╨╜╨╡╨╝╨░╤ö ╨░╨║╤é╨╕╨▓╨╜╨╕╤à ╨┐╨░╤Ç╤é╨╜╨╡╤Ç╤û╨▓.\n\n" +
                          "<i>╨£╨╕ ╨┐╤Ç╨░╤å╤Ä╤ö╨╝╨╛ ╨╜╨░╨┤ ╨╜╨╛╨▓╨╕╨╝╨╕ ╨┐╨░╤Ç╤é╨╜╨╡╤Ç╤ü╤é╨▓╨░╨╝╨╕!</i>",
                    parseMode: ParseMode.Html,
                    replyMarkup: GetBackToMainMenu(),
                    cancellationToken: cancellationToken);
                return;
            }

            // ╨ñ╨╛╤Ç╨╝╤â╤ö╨╝╨╛ ╤é╨╡╨║╤ü╤é ╨╖ ╨┐╨░╤Ç╤é╨╜╨╡╤Ç╨░╨╝╨╕
            var partnersText = "≡ƒñ¥ <b>╨¥╨░╤ê╤û ╨┐╨░╤Ç╤é╨╜╨╡╤Ç╨╕</b>\n\n";
            partnersText += "<i>╨ƒ╤Ç╨╛╨┐╨╛╨╜╤â╤ö╨╝╨╛ ╨╖╨╜╨╕╨╢╨║╨╕ ╤é╨░ ╨┐╤Ç╨╕╨▓╤û╨╗╨╡╤ù ╨┤╨╗╤Å ╤ç╨╗╨╡╨╜╤û╨▓ ╨┐╤Ç╨╛╤ä╤ü╨┐╤û╨╗╨║╨╕:</i>\n\n";
            
            foreach (var partner in partnersList.Items)
            {
                var featuredMark = partner.IsFeatured ? "Γ¡É " : "";
                partnersText += $"{featuredMark}{partner.TypeEmoji} <b>{partner.Name}</b>\n";
                
                if (!string.IsNullOrEmpty(partner.Description))
                {
                    partnersText += $"{partner.Description}\n";
                }
                
                if (!string.IsNullOrEmpty(partner.DiscountInfo))
                {
                    partnersText += $"≡ƒÆ░ <b>╨ù╨╜╨╕╨╢╨║╨░:</b> {partner.DiscountInfo}\n";
                }
                
                if (!string.IsNullOrEmpty(partner.Address))
                {
                    partnersText += $"≡ƒôì {partner.Address}\n";
                }
                
                if (!string.IsNullOrEmpty(partner.PhoneNumber))
                {
                    partnersText += $"≡ƒô₧ {partner.PhoneNumber}\n";
                }
                
                partnersText += "\n";
            }

            partnersText += $"<i>╨Æ╤ü╤î╨╛╨│╨╛ ╨┐╨░╤Ç╤é╨╜╨╡╤Ç╤û╨▓: {partnersList.TotalCount}</i>";

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
            _logger.LogError(ex, "╨ƒ╨╛╨╝╨╕╨╗╨║╨░ ╨┐╤Ç╨╕ ╨╛╤é╤Ç╨╕╨╝╨░╨╜╨╜╤û ╨┐╨░╤Ç╤é╨╜╨╡╤Ç╤û╨▓ ╨┤╨╗╤Å ╨║╨╛╤Ç╨╕╤ü╤é╤â╨▓╨░╤ç╨░ {UserId}", callbackQuery.From.Id);
            
            await botClient.EditMessageTextAsync(
                chatId: callbackQuery.Message!.Chat.Id,
                messageId: callbackQuery.Message.MessageId,
                text: "≡ƒñ¥ <b>╨ƒ╨░╤Ç╤é╨╜╨╡╤Ç╨╕</b>\n\nΓ¥î ╨Æ╨╕╨╜╨╕╨║╨╗╨░ ╨┐╨╛╨╝╨╕╨╗╨║╨░ ╨┐╤Ç╨╕ ╨╖╨░╨▓╨░╨╜╤é╨░╨╢╨╡╨╜╨╜╤û ╨┐╨░╤Ç╤é╨╜╨╡╤Ç╤û╨▓.",
                parseMode: ParseMode.Html,
                replyMarkup: GetBackToMainMenu(),
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
            
            // ╨₧╤é╤Ç╨╕╨╝╤â╤ö╨╝╨╛ ╨║╨╛╨╜╤é╨░╨║╤é╨╕ ╤ç╨╡╤Ç╨╡╨╖ MediatR
            var query = new GetAllContactsQuery();
            var result = await mediator.Send(query, cancellationToken);

            if (!result.IsSuccess || result.Value == null)
            {
                await botClient.EditMessageTextAsync(
                    chatId: callbackQuery.Message!.Chat.Id,
                    messageId: callbackQuery.Message.MessageId,
                    text: "≡ƒô₧ <b>╨Ü╨╛╨╜╤é╨░╨║╤é╨╕</b>\n\nΓ¥î ╨¥╨╡ ╨▓╨┤╨░╨╗╨╛╤ü╤Å ╨╖╨░╨▓╨░╨╜╤é╨░╨╢╨╕╤é╨╕ ╨║╨╛╨╜╤é╨░╨║╤é╨╕. ╨í╨┐╤Ç╨╛╨▒╤â╨╣╤é╨╡ ╨┐╤û╨╖╨╜╤û╤ê╨╡.",
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
                    text: "≡ƒô₧ <b>╨Ü╨╛╨╜╤é╨░╨║╤é╨╜╨░ ╤û╨╜╤ä╨╛╤Ç╨╝╨░╤å╤û╤Å</b>\n\n" +
                          "≡ƒÅ¢ <b>╨í╤é╤â╨┤╨╡╨╜╤é╤ü╤î╨║╨╕╨╣ ╨┐╤Ç╨╛╤ä╤ü╨┐╤û╨╗╨║╨╛╨▓╨╕╨╣ ╨║╨╛╨╝╤û╤é╨╡╤é</b>\n\n" +
                          "≡ƒôº Email: profkom@vnmu.edu.ua\n" +
                          "≡ƒô▒ Telegram: @vnmu_profkom\n" +
                          "≡ƒôì ╨É╨┤╤Ç╨╡╤ü╨░: ╨▓╤â╨╗. ╨ƒ╨╕╤Ç╨╛╨│╨╛╨▓╨░, 56, ╨Æ╤û╨╜╨╜╨╕╤å╤Å\n" +
                          "≡ƒòÉ ╨ô╨╛╨┤╨╕╨╜╨╕ ╤Ç╨╛╨▒╨╛╤é╨╕: ╨ƒ╨¥-╨ƒ╨ó 9:00-17:00",
                    parseMode: ParseMode.Html,
                    replyMarkup: GetBackToMainMenu(),
                    cancellationToken: cancellationToken);
                return;
            }

            // ╨ñ╨╛╤Ç╨╝╤â╤ö╨╝╨╛ ╤é╨╡╨║╤ü╤é ╨╖ ╨║╨╛╨╜╤é╨░╨║╤é╨░╨╝╨╕
            var contactsText = "≡ƒô₧ <b>╨Ü╨╛╨╜╤é╨░╨║╤é╨╜╨░ ╤û╨╜╤ä╨╛╤Ç╨╝╨░╤å╤û╤Å</b>\n\n";
            
            foreach (var contact in contactsList.Items)
            {
                contactsText += $"≡ƒÅ¢ <b>{contact.Title}</b>\n";
                
                if (!string.IsNullOrEmpty(contact.Description))
                {
                    contactsText += $"<i>{contact.Description}</i>\n";
                }
                
                if (!string.IsNullOrEmpty(contact.PhoneNumber))
                {
                    contactsText += $"≡ƒô₧ {contact.PhoneNumber}\n";
                }
                
                if (!string.IsNullOrEmpty(contact.Email))
                {
                    contactsText += $"≡ƒôº {contact.Email}\n";
                }
                
                if (!string.IsNullOrEmpty(contact.TelegramUsername))
                {
                    contactsText += $"≡ƒô▒ @{contact.TelegramUsername}\n";
                }
                
                if (!string.IsNullOrEmpty(contact.Address))
                {
                    contactsText += $"≡ƒôì {contact.Address}\n";
                }
                
                if (!string.IsNullOrEmpty(contact.WorkingHours))
                {
                    contactsText += $"≡ƒòÉ {contact.WorkingHours}\n";
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
            _logger.LogError(ex, "╨ƒ╨╛╨╝╨╕╨╗╨║╨░ ╨┐╤Ç╨╕ ╨╛╤é╤Ç╨╕╨╝╨░╨╜╨╜╤û ╨║╨╛╨╜╤é╨░╨║╤é╤û╨▓ ╨┤╨╗╤Å ╨║╨╛╤Ç╨╕╤ü╤é╤â╨▓╨░╤ç╨░ {UserId}", callbackQuery.From.Id);
            
            await botClient.EditMessageTextAsync(
                chatId: callbackQuery.Message!.Chat.Id,
                messageId: callbackQuery.Message.MessageId,
                text: "≡ƒô₧ <b>╨Ü╨╛╨╜╤é╨░╨║╤é╨╕</b>\n\nΓ¥î ╨Æ╨╕╨╜╨╕╨║╨╗╨░ ╨┐╨╛╨╝╨╕╨╗╨║╨░ ╨┐╤Ç╨╕ ╨╖╨░╨▓╨░╨╜╤é╨░╨╢╨╡╨╜╨╜╤û ╨║╨╛╨╜╤é╨░╨║╤é╤û╨▓.",
                parseMode: ParseMode.Html,
                replyMarkup: GetBackToMainMenu(),
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
                "Γ¢ö ╨ú ╨▓╨░╤ü ╨╜╨╡╨╝╨░╤ö ╨┐╤Ç╨░╨▓ ╨░╨┤╨╝╤û╨╜╤û╤ü╤é╤Ç╨░╤é╨╛╤Ç╨░",
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

        var statsText = $"≡ƒæ¿ΓÇì≡ƒÆ╝ <b>╨É╨┤╨╝╤û╨╜ ╨┐╨░╨╜╨╡╨╗╤î</b>\n\n" +
                       $"≡ƒôè <b>╨í╤é╨░╤é╨╕╤ü╤é╨╕╨║╨░:</b>\n" +
                       $"≡ƒôï ╨Æ╤ü╤î╨╛╨│╨╛ ╨╖╨▓╨╡╤Ç╨╜╨╡╨╜╤î: {allAppealsResult.Value?.TotalCount ?? 0}\n" +
                       $"≡ƒåò ╨¥╨╛╨▓╨╕╤à: {newAppealsResult.Value?.TotalCount ?? 0}\n" +
                       $"≡ƒæñ ╨£╨╛╤ù╤à: {myAppealsResult.Value?.TotalCount ?? 0}\n" +
                       $"Γ¥ô ╨¥╨╡╨┐╤Ç╨╕╨╖╨╜╨░╤ç╨╡╨╜╨╕╤à: {unassignedResult.Value?.TotalCount ?? 0}\n\n" +
                       $"╨₧╨▒╨╡╤Ç╤û╤é╤î ╨┤╤û╤Ä:";

        await botClient.EditMessageTextAsync(
            chatId: callbackQuery.Message!.Chat.Id,
            messageId: callbackQuery.Message.MessageId,
            text: statsText,
            parseMode: ParseMode.Html,
            replyMarkup: GetAdminPanel(),
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
                "Γ¢ö ╨ú ╨▓╨░╤ü ╨╜╨╡╨╝╨░╤ö ╨┐╤Ç╨░╨▓ ╨░╨┤╨╝╤û╨╜╤û╤ü╤é╤Ç╨░╤é╨╛╤Ç╨░",
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
                "≡ƒô¡ ╨ù╨▓╨╡╤Ç╨╜╨╡╨╜╤î ╨╜╨╡ ╨╖╨╜╨░╨╣╨┤╨╡╨╜╨╛",
                cancellationToken: cancellationToken);
            return;
        }

        var appealsText = "≡ƒôï <b>╨ù╨▓╨╡╤Ç╨╜╨╡╨╜╨╜╤Å:</b>\n\n";
        foreach (var appeal in result.Value.Appeals)
        {
            var statusEmoji = appeal.Status switch
            {
                Domain.Enums.AppealStatus.New => "≡ƒåò",
                Domain.Enums.AppealStatus.InProgress => "ΓÅ│",
                Domain.Enums.AppealStatus.Closed => "Γ£à",
                _ => "Γ¥ô"
            };

            var priorityEmoji = appeal.Priority switch
            {
                Domain.Enums.AppealPriority.Low => "≡ƒƒó",
                Domain.Enums.AppealPriority.Normal => "≡ƒƒí",
                Domain.Enums.AppealPriority.High => "≡ƒƒá",
                Domain.Enums.AppealPriority.Urgent => "≡ƒö┤",
                _ => "ΓÜ¬"
            };

            var assignedText = appeal.AssignedToAdminId.HasValue ? "≡ƒæñ" : "Γ¥ô";

            appealsText += $"{statusEmoji} {priorityEmoji} #{appeal.Id} | {appeal.Category.GetDisplayName()}\n" +
                          $"<b>{appeal.Subject}</b>\n" +
                          $"{assignedText} ╨í╤é╨░╤é╤â╤ü: {appeal.Status.GetDisplayName()}\n\n";
        }

        appealsText += $"╨í╤é╨╛╤Ç╤û╨╜╨║╨░ 1 ╨╖ {Math.Ceiling((double)result.Value.TotalCount / 10)}";

        var keyboard = new InlineKeyboardMarkup(new[]
        {
            result.Value.Appeals.Select(a => 
                InlineKeyboardButton.WithCallbackData($"#{a.Id}", $"admin_view_{a.Id}")
            ).ToArray(),
            new[] { InlineKeyboardButton.WithCallbackData("┬½ ╨¥╨░╨╖╨░╨┤ ╨┤╨╛ ╨░╨┤╨╝╤û╨╜ ╨┐╨░╨╜╨╡╨╗╤û", "admin_panel") }
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
                "Γ¢ö ╨ú ╨▓╨░╤ü ╨╜╨╡╨╝╨░╤ö ╨┐╤Ç╨░╨▓ ╨░╨┤╨╝╤û╨╜╤û╤ü╤é╤Ç╨░╤é╨╛╤Ç╨░",
                showAlert: true,
                cancellationToken: cancellationToken);
            return;
        }

        var appealIdStr = callbackQuery.Data!.Replace("admin_view_", "");
        if (!int.TryParse(appealIdStr, out var appealId))
        {
            await botClient.AnswerCallbackQueryAsync(
                callbackQuery.Id,
                "Γ¥î ╨¥╨╡╨▓╤û╤Ç╨╜╨╕╨╣ ID ╨╖╨▓╨╡╤Ç╨╜╨╡╨╜╨╜╤Å",
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
                "Γ¥î ╨ù╨▓╨╡╤Ç╨╜╨╡╨╜╨╜╤Å ╨╜╨╡ ╨╖╨╜╨░╨╣╨┤╨╡╨╜╨╛",
                showAlert: true,
                cancellationToken: cancellationToken);
            return;
        }

        var appeal = result.Value;
        var isAssignedToMe = appeal.AssignedToAdminId == user.TelegramId;
        var isClosed = appeal.Status == Domain.Enums.AppealStatus.Closed;

        var appealText = $"≡ƒôï <b>╨ù╨▓╨╡╤Ç╨╜╨╡╨╜╨╜╤Å #{appeal.Id}</b>\n\n" +
                        $"≡ƒôé ╨Ü╨░╤é╨╡╨│╨╛╤Ç╤û╤Å: {appeal.CategoryName}\n" +
                        $"≡ƒôî ╨ó╨╡╨╝╨░: {appeal.Subject}\n" +
                        $"≡ƒô¥ ╨₧╨┐╨╕╤ü:\n{appeal.Message}\n\n" +
                        $"≡ƒôè ╨í╤é╨░╤é╤â╤ü: {appeal.StatusName}\n" +
                        $"≡ƒÄ» ╨ƒ╤Ç╤û╨╛╤Ç╨╕╤é╨╡╤é: {appeal.PriorityName}\n" +
                        $"≡ƒæñ ╨ƒ╤Ç╨╕╨╖╨╜╨░╤ç╨╡╨╜╨╛: {(appeal.AssignedToAdminId.HasValue ? $"╨É╨┤╨╝╤û╨╜ #{appeal.AssignedToAdminId}" : "╨¥╨╡╨┐╤Ç╨╕╨╖╨╜╨░╤ç╨╡╨╜╨╛")}\n" +
                        $"≡ƒôà ╨í╤é╨▓╨╛╤Ç╨╡╨╜╨╛: {appeal.CreatedAt:dd.MM.yyyy HH:mm}\n";

        if (appeal.FirstResponseAt.HasValue)
        {
            appealText += $"ΓÅ▒∩╕Å ╨ƒ╨╡╤Ç╤ê╨░ ╨▓╤û╨┤╨┐╨╛╨▓╤û╨┤╤î: {appeal.FirstResponseAt:dd.MM.yyyy HH:mm}\n";
        }

        if (appeal.ClosedAt.HasValue)
        {
            appealText += $"Γ£à ╨ù╨░╨║╤Ç╨╕╤é╨╛: {appeal.ClosedAt:dd.MM.yyyy HH:mm}\n";
            if (!string.IsNullOrEmpty(appeal.ClosedReason))
            {
                appealText += $"≡ƒô¥ ╨ƒ╤Ç╨╕╤ç╨╕╨╜╨░ ╨╖╨░╨║╤Ç╨╕╤é╤é╤Å: {appeal.ClosedReason}\n";
            }
        }

        if (appeal.Messages.Any())
        {
            appealText += $"\n≡ƒÆ¼ <b>╨å╤ü╤é╨╛╤Ç╤û╤Å ({appeal.Messages.Count}):</b>\n";
            foreach (var msg in appeal.Messages.OrderBy(m => m.SentAt).Take(3))
            {
                var senderType = msg.IsFromAdmin ? "≡ƒæ¿ΓÇì≡ƒÆ╝ ╨É╨┤╨╝╤û╨╜" : "≡ƒæñ ╨Ü╨╛╤Ç╨╕╤ü╤é╤â╨▓╨░╤ç";
                appealText += $"{senderType} ({msg.SentAt:dd.MM HH:mm}):\n{msg.Text}\n\n";
            }
            if (appeal.Messages.Count > 3)
            {
                appealText += $"... ╤é╨░ ╤ë╨╡ {appeal.Messages.Count - 3} ╨┐╨╛╨▓╤û╨┤╨╛╨╝╨╗╨╡╨╜╤î\n";
            }
        }

        await botClient.EditMessageTextAsync(
            chatId: callbackQuery.Message!.Chat.Id,
            messageId: callbackQuery.Message.MessageId,
            text: appealText,
            parseMode: ParseMode.Html,
            replyMarkup: GetAdminAppealActions(appealId, isAssignedToMe, isClosed),
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
                "Γ¢ö ╨ú ╨▓╨░╤ü ╨╜╨╡╨╝╨░╤ö ╨┐╤Ç╨░╨▓ ╨░╨┤╨╝╤û╨╜╤û╤ü╤é╤Ç╨░╤é╨╛╤Ç╨░",
                showAlert: true,
                cancellationToken: cancellationToken);
            return;
        }

        var appealIdStr = callbackQuery.Data!.Replace("admin_assign_me_", "");
        if (!int.TryParse(appealIdStr, out var appealId))
        {
            await botClient.AnswerCallbackQueryAsync(
                callbackQuery.Id,
                "Γ¥î ╨¥╨╡╨▓╤û╤Ç╨╜╨╕╨╣ ID ╨╖╨▓╨╡╤Ç╨╜╨╡╨╜╨╜╤Å",
                showAlert: true,
                cancellationToken: cancellationToken);
            return;
        }

        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var result = await mediator.Send(new AssignAppealCommand(
            appealId: appealId,
            adminId: user.TelegramId,
            assignedByUserId: user.TelegramId,
            reason: "Адмін призначив звернення собі"
        ), cancellationToken);

        if (result.IsSuccess)
        {
            await botClient.AnswerCallbackQueryAsync(
                callbackQuery.Id,
                "Γ£à ╨ù╨▓╨╡╤Ç╨╜╨╡╨╜╨╜╤Å ╨┐╤Ç╨╕╨╖╨╜╨░╤ç╨╡╨╜╨╛ ╨▓╨░╨╝",
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
                $"Γ¥î {result.Error}",
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
                "Γ¢ö ╨ú ╨▓╨░╤ü ╨╜╨╡╨╝╨░╤ö ╨┐╤Ç╨░╨▓ ╨░╨┤╨╝╤û╨╜╤û╤ü╤é╤Ç╨░╤é╨╛╤Ç╨░",
                showAlert: true,
                cancellationToken: cancellationToken);
            return;
        }

        var appealIdStr = callbackQuery.Data!.Replace("admin_unassign_", "");
        if (!int.TryParse(appealIdStr, out var appealId))
        {
            await botClient.AnswerCallbackQueryAsync(
                callbackQuery.Id,
                "Γ¥î ╨¥╨╡╨▓╤û╤Ç╨╜╨╕╨╣ ID ╨╖╨▓╨╡╤Ç╨╜╨╡╨╜╨╜╤Å",
                showAlert: true,
                cancellationToken: cancellationToken);
            return;
        }

        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var result = await mediator.Send(new AssignAppealCommand(
            appealId: appealId,
            assignedByUserId: user.TelegramId
        ), cancellationToken);

        if (result.IsSuccess)
        {
            await botClient.AnswerCallbackQueryAsync(
                callbackQuery.Id,
                "Γ£à ╨ƒ╤Ç╨╕╨╖╨╜╨░╤ç╨╡╨╜╨╜╤Å ╨╖╨╜╤Å╤é╨╛",
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
                $"Γ¥î {result.Error}",
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
                "Γ¢ö ╨ú ╨▓╨░╤ü ╨╜╨╡╨╝╨░╤ö ╨┐╤Ç╨░╨▓ ╨░╨┤╨╝╤û╨╜╤û╤ü╤é╤Ç╨░╤é╨╛╤Ç╨░",
                showAlert: true,
                cancellationToken: cancellationToken);
            return;
        }

        var appealIdStr = callbackQuery.Data!.Replace("admin_priority_", "");
        if (!int.TryParse(appealIdStr, out var appealId))
        {
            await botClient.AnswerCallbackQueryAsync(
                callbackQuery.Id,
                "Γ¥î ╨¥╨╡╨▓╤û╤Ç╨╜╨╕╨╣ ID ╨╖╨▓╨╡╤Ç╨╜╨╡╨╜╨╜╤Å",
                showAlert: true,
                cancellationToken: cancellationToken);
            return;
        }

        await botClient.EditMessageReplyMarkupAsync(
            chatId: callbackQuery.Message!.Chat.Id,
            messageId: callbackQuery.Message.MessageId,
            replyMarkup: GetPrioritySelection(appealId),
            cancellationToken: cancellationToken);

        await botClient.AnswerCallbackQueryAsync(
            callbackQuery.Id,
            "╨₧╨▒╨╡╤Ç╤û╤é╤î ╨┐╤Ç╤û╨╛╤Ç╨╕╤é╨╡╤é:",
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
                "Γ¢ö ╨ú ╨▓╨░╤ü ╨╜╨╡╨╝╨░╤ö ╨┐╤Ç╨░╨▓ ╨░╨┤╨╝╤û╨╜╤û╤ü╤é╤Ç╨░╤é╨╛╤Ç╨░",
                showAlert: true,
                cancellationToken: cancellationToken);
            return;
        }

        var parts = callbackQuery.Data!.Replace("admin_set_priority_", "").Split('_');
        if (parts.Length != 2 || !int.TryParse(parts[0], out var appealId) || !int.TryParse(parts[1], out var priorityValue))
        {
            await botClient.AnswerCallbackQueryAsync(
                callbackQuery.Id,
                "Γ¥î ╨¥╨╡╨▓╤û╤Ç╨╜╤û ╨┐╨░╤Ç╨░╨╝╨╡╤é╤Ç╨╕",
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
                "Γ£à ╨ƒ╤Ç╤û╨╛╤Ç╨╕╤é╨╡╤é ╨╛╨╜╨╛╨▓╨╗╨╡╨╜╨╛",
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
                $"Γ¥î {result.Error}",
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
                "Γ¢ö ╨ú ╨▓╨░╤ü ╨╜╨╡╨╝╨░╤ö ╨┐╤Ç╨░╨▓ ╨░╨┤╨╝╤û╨╜╤û╤ü╤é╤Ç╨░╤é╨╛╤Ç╨░",
                showAlert: true,
                cancellationToken: cancellationToken);
            return;
        }

        var appealIdStr = callbackQuery.Data!.Replace("admin_close_", "");
        if (!int.TryParse(appealIdStr, out var appealId))
        {
            await botClient.AnswerCallbackQueryAsync(
                callbackQuery.Id,
                "Γ¥î ╨¥╨╡╨▓╤û╤Ç╨╜╨╕╨╣ ID ╨╖╨▓╨╡╤Ç╨╜╨╡╨╜╨╜╤Å",
                showAlert: true,
                cancellationToken: cancellationToken);
            return;
        }

        // Store appeal ID and set state for close reason input
        await _stateManager.SetDataAsync(user.TelegramId, "close_appeal_id", appealId, cancellationToken);
        await _stateManager.SetStateAsync(user.TelegramId, UserConversationState.WaitingCloseReason, cancellationToken);

        await botClient.AnswerCallbackQueryAsync(
            callbackQuery.Id,
            cancellationToken: cancellationToken);

        await botClient.SendTextMessageAsync(
            chatId: callbackQuery.Message!.Chat.Id,
            text: "📝 Введіть причину закриття звернення:\n\n" +
                  "<i>Мінімум 5 символів, максимум 500 символів.</i>\n\n" +
                  "Або натисніть /cancel для відміни.",
            parseMode: ParseMode.Html,
            cancellationToken: cancellationToken);
    }

    #endregion

    private Task HandleEditedMessageAsync(
        ITelegramBotClient botClient,
        Message message,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("╨₧╤é╤Ç╨╕╨╝╨░╨╜╨╛ ╨▓╤û╨┤╤Ç╨╡╨┤╨░╨│╨╛╨▓╨░╨╜╨╡ ╨┐╨╛╨▓╤û╨┤╨╛╨╝╨╗╨╡╨╜╨╜╤Å ╨▓╤û╨┤ {UserId}", message.From?.Id);
        return Task.CompletedTask;
    }

    private Task HandleUnknownUpdateAsync(
        ITelegramBotClient botClient,
        Update update,
        CancellationToken cancellationToken)
    {
        _logger.LogWarning("╨¥╨╡╨▓╤û╨┤╨╛╨╝╨╕╨╣ ╤é╨╕╨┐ ╨╛╨╜╨╛╨▓╨╗╨╡╨╜╨╜╤Å: {UpdateType}", update.Type);
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

        _logger.LogError(exception, "╨ƒ╨╛╨╝╨╕╨╗╨║╨░ ╨┐╤Ç╨╕ ╨╛╨▒╤Ç╨╛╨▒╤å╤û ╨╛╨╜╨╛╨▓╨╗╨╡╨╜╨╜╤Å: {ErrorMessage}", errorMessage);
        return Task.CompletedTask;
    }
}
