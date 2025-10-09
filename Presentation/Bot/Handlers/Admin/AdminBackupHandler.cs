using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StudentUnionBot.Application.Admin.Commands.CreateBackup;
using StudentUnionBot.Application.Users.Queries.GetUserByTelegramId;
using StudentUnionBot.Core.Results;
using StudentUnionBot.Domain.Enums;
using StudentUnionBot.Domain.Interfaces;
using StudentUnionBot.Presentation.Bot.Handlers.Common;
using StudentUnionBot.Presentation.Bot.Handlers.Interfaces;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace StudentUnionBot.Presentation.Bot.Handlers.Admin;

/// <summary>
/// Обробник адміністративних функцій бекапів
/// </summary>
public class AdminBackupHandler : BaseHandler, IAdminBackupHandler
{
    public AdminBackupHandler(
        IServiceScopeFactory scopeFactory,
        ILogger<AdminBackupHandler> logger,
        IMediator mediator)
        : base(scopeFactory, logger, mediator)
    {
    }

    /// <summary>
    /// Обробляє текстові повідомлення (AdminBackupHandler не обробляє текстові повідомлення)
    /// </summary>
    public override async Task HandleTextMessageAsync(ITelegramBotClient botClient, Message message, UserConversationState state, CancellationToken cancellationToken)
    {
        // AdminBackupHandler не обробляє текстові повідомлення
        await Task.CompletedTask;
    }

    /// <summary>
    /// Показує меню бекапів
    /// </summary>
    public async Task HandleAdminBackupMenuCallback(ITelegramBotClient botClient, CallbackQuery callbackQuery, CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        // Перевіряємо права користувача через MediatR
        var getUserQuery = new GetUserByTelegramIdQuery { TelegramId = callbackQuery.From.Id };
        var userResult = await mediator.Send(getUserQuery, cancellationToken);
        
        if (!userResult.IsSuccess || userResult.Value?.Role != UserRole.Admin && userResult.Value?.Role != UserRole.SuperAdmin)
        {
            await botClient.AnswerCallbackQueryAsync(
                callbackQuery.Id,
                "⛔ У вас немає прав адміністратора",
                showAlert: true,
                cancellationToken: cancellationToken);
            return;
        }

        var menuText = "💾 <b>Управління резервними копіями</b>\n\n" +
                      "Виберіть дію для роботи з резервними копіями бази даних:\n\n" +
                      "🔹 <b>Створити</b> - створити нову резервну копію\n" +
                      "🔹 <b>Список</b> - переглянути доступні резервні копії\n" +
                      "🔹 <b>Відновити</b> - відновити базу даних з резервної копії";

        var keyboard = new InlineKeyboardMarkup(new[]
        {
            new[]
            {
                InlineKeyboardButton.WithCallbackData("📁 Створити резервну копію", "admin_backup_create"),
                InlineKeyboardButton.WithCallbackData("📋 Список резервних копій", "admin_backup_list")
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData("🔙 Назад до адмін панелі", "admin_panel")
            }
        });

        await botClient.EditMessageTextAsync(
            chatId: callbackQuery.Message!.Chat.Id,
            messageId: callbackQuery.Message.MessageId,
            text: menuText,
            parseMode: ParseMode.Html,
            replyMarkup: keyboard,
            cancellationToken: cancellationToken);

        await botClient.AnswerCallbackQueryAsync(
            callbackQuery.Id,
            cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Підтверджує відновлення резервної копії
    /// </summary>
    public async Task HandleAdminRestoreConfirmCallback(ITelegramBotClient botClient, CallbackQuery callbackQuery, CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        // Перевіряємо права користувача через MediatR
        var getUserQuery = new GetUserByTelegramIdQuery { TelegramId = callbackQuery.From.Id };
        var userResult = await mediator.Send(getUserQuery, cancellationToken);
        
        if (!userResult.IsSuccess || userResult.Value?.Role != UserRole.Admin && userResult.Value?.Role != UserRole.SuperAdmin)
        {
            await botClient.AnswerCallbackQueryAsync(
                callbackQuery.Id,
                "⛔ У вас немає прав адміністратора",
                showAlert: true,
                cancellationToken: cancellationToken);
            return;
        }

        var user = userResult.Value!;
        var backupName = callbackQuery.Data!.Replace("admin_restore_confirm_", "");
        var backupDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Backups");
        var backupFilePath = Path.Combine(backupDirectory, $"{backupName}.db");

        if (!System.IO.File.Exists(backupFilePath))
        {
            await botClient.AnswerCallbackQueryAsync(
                callbackQuery.Id,
                "❌ Файл резервної копії не знайдено",
                showAlert: true,
                cancellationToken: cancellationToken);
            return;
        }

        await botClient.AnswerCallbackQueryAsync(
            callbackQuery.Id,
            "⏳ Відновлення бази даних...",
            cancellationToken: cancellationToken);

        await botClient.EditMessageTextAsync(
            chatId: callbackQuery.Message!.Chat.Id,
            messageId: callbackQuery.Message.MessageId,
            text: "⏳ <b>Відновлення бази даних</b>\n\n" +
                  "Зачекайте, йде процес відновлення бази даних з резервної копії...\n\n" +
                  "❗ <b>НЕ ВИМИКАЙТЕ БОТ!</b>",
            parseMode: ParseMode.Html,
            cancellationToken: cancellationToken);

        try
        {
            var backupService = scope.ServiceProvider.GetRequiredService<IBackupService>();
            var result = await backupService.RestoreBackupAsync(backupFilePath, cancellationToken);

            if (result.IsSuccess)
            {
                var successText = "✅ <b>База даних відновлена успішно!</b>\n\n" +
                                $"📁 <b>З файлу:</b> {backupName}.db\n" +
                                $"📅 <b>Відновлено:</b> {DateTime.Now:dd.MM.yyyy HH:mm:ss}\n\n" +
                                "✅ Поточна БД була збережена як резервна копія\n" +
                                "✅ Всі дані успішно відновлені\n\n" +
                                "⚠️ <b>Рекомендується перезапустити бот для повного застосування змін</b>";

                var keyboard = new InlineKeyboardMarkup(new[]
                {
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData("📋 Список копій", "admin_backup_list"),
                        InlineKeyboardButton.WithCallbackData("🏠 Адмін панель", "admin_panel")
                    }
                });

                await botClient.EditMessageTextAsync(
                    chatId: callbackQuery.Message.Chat.Id,
                    messageId: callbackQuery.Message.MessageId,
                    text: successText,
                    parseMode: ParseMode.Html,
                    replyMarkup: keyboard,
                    cancellationToken: cancellationToken);
            }
            else
            {
                var errorText = "❌ <b>Помилка відновлення бази даних</b>\n\n" +
                              $"Помилка: {result.Error}\n\n" +
                              "База даних залишилась без змін.\n" +
                              "Спробуйте ще раз або зверніться до розробника.";

                var keyboard = new InlineKeyboardMarkup(new[]
                {
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData("🔄 Спробувати ще раз", $"admin_backup_restore_{backupName}"),
                        InlineKeyboardButton.WithCallbackData("📋 Список копій", "admin_backup_list")
                    },
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData("🔙 Назад", "admin_backup")
                    }
                });

                await botClient.EditMessageTextAsync(
                    chatId: callbackQuery.Message.Chat.Id,
                    messageId: callbackQuery.Message.MessageId,
                    text: errorText,
                    parseMode: ParseMode.Html,
                    replyMarkup: keyboard,
                    cancellationToken: cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Помилка при відновленні резервної копії {BackupName} для користувача {UserId}", backupName, user.TelegramId);

            var errorText = "❌ <b>Критична помилка відновлення</b>\n\n" +
                          "Виникла критична помилка при відновленні бази даних.\n" +
                          "База даних може бути пошкоджена!\n\n" +
                          "⚠️ <b>ТЕРМІНОВІ ДІЇ:</b>\n" +
                          "1. Зупиніть бот\n" +
                          "2. Зверніться до розробника\n" +
                          "3. НЕ виконуйте інші операції з БД";

            var keyboard = new InlineKeyboardMarkup(new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("📋 Список копій", "admin_backup_list"),
                    InlineKeyboardButton.WithCallbackData("🏠 Головна", "back_to_main")
                }
            });

            await botClient.EditMessageTextAsync(
                chatId: callbackQuery.Message!.Chat.Id,
                messageId: callbackQuery.Message.MessageId,
                text: errorText,
                parseMode: ParseMode.Html,
                replyMarkup: keyboard,
                cancellationToken: cancellationToken);
        }
    }

    /// <summary>
    /// Форматує розмір файлу у зручному для читання вигляді
    /// </summary>
    private static string FormatBytes(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB" };
        double len = bytes;
        int order = 0;
        
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }

        return $"{len:0.##} {sizes[order]}";
    }

    /// <summary>
    /// Створює новий бекап
    /// </summary>
    public async Task HandleAdminBackupCreateCallback(ITelegramBotClient botClient, CallbackQuery callbackQuery, CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        // Перевіряємо права користувача через MediatR
        var getUserQuery = new GetUserByTelegramIdQuery { TelegramId = callbackQuery.From.Id };
        var userResult = await mediator.Send(getUserQuery, cancellationToken);
        
        if (!userResult.IsSuccess || userResult.Value?.Role != UserRole.Admin && userResult.Value?.Role != UserRole.SuperAdmin)
        {
            await botClient.AnswerCallbackQueryAsync(
                callbackQuery.Id,
                "⛔ У вас немає прав адміністратора",
                showAlert: true,
                cancellationToken: cancellationToken);
            return;
        }

        var user = userResult.Value!;

        await botClient.AnswerCallbackQueryAsync(
            callbackQuery.Id,
            "⏳ Створення резервної копії...",
            cancellationToken: cancellationToken);

        // Показуємо повідомлення про створення
        await botClient.EditMessageTextAsync(
            chatId: callbackQuery.Message!.Chat.Id,
            messageId: callbackQuery.Message.MessageId,
            text: "⏳ <b>Створення резервної копії</b>\n\nЗачекайте, йде процес створення резервної копії бази даних...",
            parseMode: ParseMode.Html,
            cancellationToken: cancellationToken);

        try
        {
            var command = new CreateBackupCommand { AdminId = user.TelegramId };
            var result = await mediator.Send(command, cancellationToken);

            if (result.IsSuccess && result.Value != null)
            {
                var backup = result.Value;
                var successText = "✅ <b>Резервну копію створено успішно!</b>\n\n" +
                                $"📁 <b>Назва файлу:</b> {backup.BackupFileName}\n" +
                                $"📏 <b>Розмір:</b> {FormatBytes(backup.FileSizeBytes)}\n" +
                                $"📅 <b>Створено:</b> {backup.CreatedAt:dd.MM.yyyy HH:mm:ss}\n" +
                                $"💾 <b>Розмір БД:</b> {backup.DatabaseSize}";

                var keyboard = new InlineKeyboardMarkup(new[]
                {
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData("📋 Список резервних копій", "admin_backup_list"),
                        InlineKeyboardButton.WithCallbackData("🔙 Назад", "admin_backup")
                    }
                });

                await botClient.EditMessageTextAsync(
                    chatId: callbackQuery.Message.Chat.Id,
                    messageId: callbackQuery.Message.MessageId,
                    text: successText,
                    parseMode: ParseMode.Html,
                    replyMarkup: keyboard,
                    cancellationToken: cancellationToken);
            }
            else
            {
                var errorText = "❌ <b>Помилка створення резервної копії</b>\n\n" +
                              $"Помилка: {result.Error}\n\n" +
                              "Спробуйте пізніше або зверніться до розробника.";

                var keyboard = new InlineKeyboardMarkup(new[]
                {
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData("🔄 Спробувати ще раз", "admin_backup_create"),
                        InlineKeyboardButton.WithCallbackData("🔙 Назад", "admin_backup")
                    }
                });

                await botClient.EditMessageTextAsync(
                    chatId: callbackQuery.Message.Chat.Id,
                    messageId: callbackQuery.Message.MessageId,
                    text: errorText,
                    parseMode: ParseMode.Html,
                    replyMarkup: keyboard,
                    cancellationToken: cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Помилка при створенні резервної копії для користувача {UserId}", user.TelegramId);

            var errorText = "❌ <b>Непередбачена помилка</b>\n\n" +
                          "Виникла помилка при створенні резервної копії.\n" +
                          "Спробуйте пізніше або зверніться до розробника.";

            var keyboard = new InlineKeyboardMarkup(new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("🔄 Спробувати ще раз", "admin_backup_create"),
                    InlineKeyboardButton.WithCallbackData("🔙 Назад", "admin_backup")
                }
            });

            await botClient.EditMessageTextAsync(
                chatId: callbackQuery.Message!.Chat.Id,
                messageId: callbackQuery.Message.MessageId,
                text: errorText,
                parseMode: ParseMode.Html,
                replyMarkup: keyboard,
                cancellationToken: cancellationToken);
        }
    }

    /// <summary>
    /// Показує список бекапів
    /// </summary>
    public async Task HandleAdminBackupListCallback(ITelegramBotClient botClient, CallbackQuery callbackQuery, CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        // Перевіряємо права користувача через MediatR
        var getUserQuery = new GetUserByTelegramIdQuery { TelegramId = callbackQuery.From.Id };
        var userResult = await mediator.Send(getUserQuery, cancellationToken);
        
        if (!userResult.IsSuccess || userResult.Value?.Role != UserRole.Admin && userResult.Value?.Role != UserRole.SuperAdmin)
        {
            await botClient.AnswerCallbackQueryAsync(
                callbackQuery.Id,
                "⛔ У вас немає прав адміністратора",
                showAlert: true,
                cancellationToken: cancellationToken);
            return;
        }

        var user = userResult.Value!;

        await botClient.AnswerCallbackQueryAsync(
            callbackQuery.Id,
            "📋 Завантаження списку...",
            cancellationToken: cancellationToken);

        try
        {
            var backupService = scope.ServiceProvider.GetRequiredService<IBackupService>();
            var result = await backupService.GetAvailableBackupsAsync(cancellationToken);

            if (result.IsSuccess && result.Value != null)
            {
                var backups = result.Value;
                
                if (backups.Count == 0)
                {
                    var noBackupsText = "📋 <b>Список резервних копій</b>\n\n" +
                                       "❌ Резервні копії відсутні.\n\n" +
                                       "Створіть першу резервну копію для початку роботи.";

                    var noBackupsKeyboard = new InlineKeyboardMarkup(new[]
                    {
                        new[]
                        {
                            InlineKeyboardButton.WithCallbackData("📁 Створити резервну копію", "admin_backup_create")
                        },
                        new[]
                        {
                            InlineKeyboardButton.WithCallbackData("🔙 Назад", "admin_backup")
                        }
                    });

                    await botClient.EditMessageTextAsync(
                        chatId: callbackQuery.Message!.Chat.Id,
                        messageId: callbackQuery.Message.MessageId,
                        text: noBackupsText,
                        parseMode: ParseMode.Html,
                        replyMarkup: noBackupsKeyboard,
                        cancellationToken: cancellationToken);
                    return;
                }

                var listText = "📋 <b>Список резервних копій</b>\n\n";
                listText += $"Знайдено {backups.Count} резервних копій:\n\n";

                var buttons = new List<List<InlineKeyboardButton>>();

                for (int i = 0; i < Math.Min(backups.Count, 10); i++) // Показуємо тільки перші 10
                {
                    var backup = backups[i];
                    listText += $"📁 <b>{backup.FileName}</b>\n";
                    listText += $"   📏 Розмір: {backup.FormattedSize}\n";
                    listText += $"   📅 Створено: {backup.FormattedDate}\n\n";

                    // Додаємо кнопку для відновлення
                    var restoreCallbackData = $"admin_backup_restore_{Path.GetFileNameWithoutExtension(backup.FileName)}";
                    buttons.Add(new List<InlineKeyboardButton>
                    {
                        InlineKeyboardButton.WithCallbackData($"🔄 Відновити {backup.FileName}", restoreCallbackData)
                    });
                }

                if (backups.Count > 10)
                {
                    listText += $"... та ще {backups.Count - 10} файлів\n\n";
                }

                listText += "⚠️ <b>УВАГА:</b> Відновлення замінить поточну базу даних!";

                // Додаємо кнопки навігації
                buttons.Add(new List<InlineKeyboardButton>
                {
                    InlineKeyboardButton.WithCallbackData("📁 Створити нову копію", "admin_backup_create"),
                    InlineKeyboardButton.WithCallbackData("🔄 Оновити список", "admin_backup_list")
                });

                buttons.Add(new List<InlineKeyboardButton>
                {
                    InlineKeyboardButton.WithCallbackData("🔙 Назад", "admin_backup")
                });

                var keyboard = new InlineKeyboardMarkup(buttons);

                await botClient.EditMessageTextAsync(
                    chatId: callbackQuery.Message!.Chat.Id,
                    messageId: callbackQuery.Message.MessageId,
                    text: listText,
                    parseMode: ParseMode.Html,
                    replyMarkup: keyboard,
                    cancellationToken: cancellationToken);
            }
            else
            {
                var errorText = "❌ <b>Помилка завантаження списку</b>\n\n" +
                              $"Помилка: {result.Error}\n\n" +
                              "Спробуйте пізніше або зверніться до розробника.";

                var keyboard = new InlineKeyboardMarkup(new[]
                {
                    new[]
                    {
                        InlineKeyboardButton.WithCallbackData("🔄 Спробувати ще раз", "admin_backup_list"),
                        InlineKeyboardButton.WithCallbackData("🔙 Назад", "admin_backup")
                    }
                });

                await botClient.EditMessageTextAsync(
                    chatId: callbackQuery.Message!.Chat.Id,
                    messageId: callbackQuery.Message.MessageId,
                    text: errorText,
                    parseMode: ParseMode.Html,
                    replyMarkup: keyboard,
                    cancellationToken: cancellationToken);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Помилка при завантаженні списку резервних копій для користувача {UserId}", user.TelegramId);

            var errorText = "❌ <b>Непередбачена помилка</b>\n\n" +
                          "Виникла помилка при завантаженні списку резервних копій.\n" +
                          "Спробуйте пізніше або зверніться до розробника.";

            var keyboard = new InlineKeyboardMarkup(new[]
            {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("🔄 Спробувати ще раз", "admin_backup_list"),
                    InlineKeyboardButton.WithCallbackData("🔙 Назад", "admin_backup")
                }
            });

            await botClient.EditMessageTextAsync(
                chatId: callbackQuery.Message!.Chat.Id,
                messageId: callbackQuery.Message.MessageId,
                text: errorText,
                parseMode: ParseMode.Html,
                replyMarkup: keyboard,
                cancellationToken: cancellationToken);
        }
    }

    /// <summary>
    /// Відновлює бекап
    /// </summary>
    public async Task HandleAdminBackupRestoreCallback(ITelegramBotClient botClient, CallbackQuery callbackQuery, CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        // Перевіряємо права користувача через MediatR
        var getUserQuery = new GetUserByTelegramIdQuery { TelegramId = callbackQuery.From.Id };
        var userResult = await mediator.Send(getUserQuery, cancellationToken);
        
        if (!userResult.IsSuccess || userResult.Value?.Role != UserRole.Admin && userResult.Value?.Role != UserRole.SuperAdmin)
        {
            await botClient.AnswerCallbackQueryAsync(
                callbackQuery.Id,
                "⛔ У вас немає прав адміністратора",
                showAlert: true,
                cancellationToken: cancellationToken);
            return;
        }

        var user = userResult.Value!;
        var backupName = callbackQuery.Data!.Replace("admin_backup_restore_", "");
        var backupFileName = $"{backupName}.db";

        // Підтвердження відновлення
        var confirmText = "⚠️ <b>ПІДТВЕРДЖЕННЯ ВІДНОВЛЕННЯ</b>\n\n" +
                         $"Ви дійсно хочете відновити базу даних з резервної копії:\n" +
                         $"<code>{backupFileName}</code>\n\n" +
                         "❗ <b>УВАГА:</b>\n" +
                         "• Поточна база даних буде замінена\n" +
                         "• Всі зміни після створення копії будуть втрачені\n" +
                         "• Перед відновленням буде створена резервна копія поточної БД\n" +
                         "• Процес незворотний!\n\n" +
                         "Продовжити?";

        var keyboard = new InlineKeyboardMarkup(new[]
        {
            new[]
            {
                InlineKeyboardButton.WithCallbackData("✅ ТАК, відновити", $"admin_restore_confirm_{backupName}"),
                InlineKeyboardButton.WithCallbackData("❌ НІ, скасувати", "admin_backup_list")
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData("🔙 Назад до списку", "admin_backup_list")
            }
        });

        await botClient.EditMessageTextAsync(
            chatId: callbackQuery.Message!.Chat.Id,
            messageId: callbackQuery.Message.MessageId,
            text: confirmText,
            parseMode: ParseMode.Html,
            replyMarkup: keyboard,
            cancellationToken: cancellationToken);

        await botClient.AnswerCallbackQueryAsync(
            callbackQuery.Id,
            "Функція у розробці",
            cancellationToken: cancellationToken);
    }
}