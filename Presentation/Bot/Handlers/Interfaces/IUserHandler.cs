using Telegram.Bot;
using Telegram.Bot.Types;
using TelegramUser = Telegram.Bot.Types.User;

namespace StudentUnionBot.Presentation.Bot.Handlers.Interfaces;

/// <summary>
/// Інтерфейс для обробника користувацьких функцій
/// </summary>
public interface IUserHandler : IBaseHandler
{
    /// <summary>
    /// Реєструє або оновлює інформацію про користувача
    /// </summary>
    Task RegisterOrUpdateUserAsync(TelegramUser? user, CancellationToken cancellationToken);

    /// <summary>
    /// Обробляє введення email для верифікації
    /// </summary>
    Task HandleEmailInputAsync(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken);

    /// <summary>
    /// Обробляє введення коду верифікації email
    /// </summary>
    Task HandleVerificationCodeInputAsync(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken);

    /// <summary>
    /// Обробляє введення повного імені
    /// </summary>
    Task HandleFullNameInputAsync(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken);

    /// <summary>
    /// Обробляє введення факультету
    /// </summary>
    Task HandleFacultyInputAsync(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken);

    /// <summary>
    /// Обробляє введення курсу
    /// </summary>
    Task HandleCourseInputAsync(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken);

    /// <summary>
    /// Обробляє введення групи та завершує оновлення профілю
    /// </summary>
    Task HandleGroupInputAsync(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken);

    /// <summary>
    /// Показує профіль користувача
    /// </summary>
    Task HandleProfileViewCallback(ITelegramBotClient botClient, CallbackQuery callbackQuery, CancellationToken cancellationToken);

    /// <summary>
    /// Розпочинає редагування інформації профілю
    /// </summary>
    Task HandleProfileEditInfoCallback(ITelegramBotClient botClient, CallbackQuery callbackQuery, CancellationToken cancellationToken);

    /// <summary>
    /// Розпочинає редагування email адреси
    /// </summary>
    Task HandleProfileEditEmailCallback(ITelegramBotClient botClient, CallbackQuery callbackQuery, CancellationToken cancellationToken);

    /// <summary>
    /// Показує меню вибору мови
    /// </summary>
    Task HandleProfileChangeLanguageCallback(ITelegramBotClient botClient, CallbackQuery callbackQuery, CancellationToken cancellationToken);

    /// <summary>
    /// Встановлює мову користувача
    /// </summary>
    Task HandleSetLanguageCallback(ITelegramBotClient botClient, CallbackQuery callbackQuery, CancellationToken cancellationToken);
}