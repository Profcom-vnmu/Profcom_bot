using Telegram.Bot;
using Telegram.Bot.Types;

namespace StudentUnionBot.Presentation.Bot.Handlers.Interfaces;

/// <summary>
/// Базовий інтерфейс для адміністративних хендлерів
/// </summary>
public interface IAdminHandler : IBaseHandler
{
    /// <summary>
    /// Показує головну панель адміністратора
    /// </summary>
    Task HandleAdminPanelCallback(ITelegramBotClient botClient, CallbackQuery callbackQuery, CancellationToken cancellationToken);

    /// <summary>
    /// Показує статистику звернень
    /// </summary>
    Task HandleAdminStatisticsCallback(ITelegramBotClient botClient, CallbackQuery callbackQuery, CancellationToken cancellationToken);
    
    /// <summary>
    /// Показує меню бекапів
    /// </summary>
    Task HandleAdminBackupMenuCallback(ITelegramBotClient botClient, CallbackQuery callbackQuery, CancellationToken cancellationToken);
}

/// <summary>
/// Інтерфейс для управління зверненнями адміністратором
/// </summary>
public interface IAdminAppealHandler : IBaseHandler
{
    /// <summary>
    /// Показує список звернень для адміна
    /// </summary>
    Task HandleAdminAppealsListCallback(ITelegramBotClient botClient, CallbackQuery callbackQuery, CancellationToken cancellationToken);

    /// <summary>
    /// Показує деталі звернення для адміна
    /// </summary>
    Task HandleAdminAppealViewCallback(ITelegramBotClient botClient, CallbackQuery callbackQuery, CancellationToken cancellationToken);

    /// <summary>
    /// Призначає звернення собі
    /// </summary>
    Task HandleAdminAssignToMeCallback(ITelegramBotClient botClient, CallbackQuery callbackQuery, CancellationToken cancellationToken);

    /// <summary>
    /// Відміняє призначення звернення
    /// </summary>
    Task HandleAdminUnassignCallback(ITelegramBotClient botClient, CallbackQuery callbackQuery, CancellationToken cancellationToken);

    /// <summary>
    /// Показує меню зміни пріоритету
    /// </summary>
    Task HandleAdminPriorityMenuCallback(ITelegramBotClient botClient, CallbackQuery callbackQuery, CancellationToken cancellationToken);

    /// <summary>
    /// Встановлює пріоритет звернення
    /// </summary>
    Task HandleAdminSetPriorityCallback(ITelegramBotClient botClient, CallbackQuery callbackQuery, CancellationToken cancellationToken);

    /// <summary>
    /// Розпочинає процес відповіді на звернення
    /// </summary>
    Task HandleAdminReplyCallback(ITelegramBotClient botClient, CallbackQuery callbackQuery, CancellationToken cancellationToken);

    /// <summary>
    /// Розпочинає процес закриття звернення
    /// </summary>
    Task HandleAdminCloseAppealCallback(ITelegramBotClient botClient, CallbackQuery callbackQuery, CancellationToken cancellationToken);

    /// <summary>
    /// Обробляє введення тексту відповіді адміном
    /// </summary>
    Task HandleAdminReplyInputAsync(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken);

    /// <summary>
    /// Обробляє введення причини закриття звернення
    /// </summary>
    Task HandleCloseReasonInputAsync(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken);
}

/// <summary>
/// Інтерфейс для управління бекапами
/// </summary>
public interface IAdminBackupHandler : IBaseHandler
{
    /// <summary>
    /// Показує меню бекапів
    /// </summary>
    Task HandleAdminBackupMenuCallback(ITelegramBotClient botClient, CallbackQuery callbackQuery, CancellationToken cancellationToken);

    /// <summary>
    /// Створює новий бекап
    /// </summary>
    Task HandleAdminBackupCreateCallback(ITelegramBotClient botClient, CallbackQuery callbackQuery, CancellationToken cancellationToken);

    /// <summary>
    /// Показує список бекапів
    /// </summary>
    Task HandleAdminBackupListCallback(ITelegramBotClient botClient, CallbackQuery callbackQuery, CancellationToken cancellationToken);

    /// <summary>
    /// Відновлює бекап
    /// </summary>
    Task HandleAdminBackupRestoreCallback(ITelegramBotClient botClient, CallbackQuery callbackQuery, CancellationToken cancellationToken);

    /// <summary>
    /// Підтверджує відновлення резервної копії
    /// </summary>
    Task HandleAdminRestoreConfirmCallback(ITelegramBotClient botClient, CallbackQuery callbackQuery, CancellationToken cancellationToken);
}

/// <summary>
/// Інтерфейс для розсилок
/// </summary>

