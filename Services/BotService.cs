using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using StudentUnionBot.Data;
using StudentUnionBot.Models;
using Microsoft.EntityFrameworkCore;

namespace StudentUnionBot.Services;

public class BotService
{
    private readonly ITelegramBotClient _botClient;
    private readonly BotDbContext _context;
    private readonly AppealService _appealService;
    private readonly NewsService _newsService;
    private readonly UserService _userService;
    private readonly Dictionary<long, UserState> _userStates;

    public BotService(string botToken, BotDbContext context)
    {
        _botClient = new TelegramBotClient(botToken);
        _context = context;
        _appealService = new AppealService(_context);
        _newsService = new NewsService(_context, _botClient);
        _userService = new UserService(_context);
        _userStates = new Dictionary<long, UserState>();
    }

    public async Task HandleUpdateAsync(Update update)
    {
        // Обробка Callback Query (inline-кнопки)
        if (update.CallbackQuery is { } callbackQuery)
        {
            await HandleCallbackQueryAsync(callbackQuery);
            return;
        }

        if (update.Message is not { } message)
            return;

        if (message.From is not { } user)
            return;

        var chatId = message.Chat.Id;
        var userId = user.Id;
        var userState = GetUserState(userId);

        await _userService.EnsureUserCreatedAsync(user);

        // Обробка непідтримуваних типів медіа
        if (message.Video != null || message.Audio != null || message.Voice != null || 
            message.VideoNote != null || message.Sticker != null || message.Animation != null)
        {
            await _botClient.SendTextMessageAsync(
                chatId: chatId,
                text: "⚠️ Даний тип файлу не підтримується\n\n" +
                      "📎 Підтримувані формати:\n" +
                      "• 📸 Фото/зображення\n" +
                      "• 📄 Документи (PDF, DOCX, XLSX, тощо)\n\n" +
                      "💡 Для інших типів файлів:\n" +
                      "Завантажте файл на хмарне сховище (Google Drive, Dropbox) та надішліть посилання для перегляду."
            );
            return;
        }

        // Отримуємо текст повідомлення (може бути caption для фото/документа)
        var messageText = message.Text ?? message.Caption ?? string.Empty;
        
        // Якщо є фото або документ без тексту - використовуємо дефолтний опис
        if (string.IsNullOrWhiteSpace(messageText) && (message.Photo != null || message.Document != null))
        {
            messageText = message.Photo != null ? "[Фото]" : $"[Файл: {message.Document?.FileName}]";
        }
        
        // Якщо немає ні тексту, ні медіа - ігноруємо
        if (string.IsNullOrWhiteSpace(messageText) && message.Photo == null && message.Document == null)
            return;

        // Перевіряємо чи це відповідь від адміністратора на повідомлення бота
        if (await _userService.IsAdminAsync(userId) && message.ReplyToMessage != null)
        {
            await HandleAdminReply(message);
            return;
        }

        // Перевіряємо чи це Reply студента на історію звернення
        if (!await _userService.IsAdminAsync(userId) && message.ReplyToMessage != null)
        {
            // Перевіряємо чи це Reply на повідомлення з історією
            if (userState.AppealHistoryMessageId.HasValue && 
                message.ReplyToMessage.MessageId == userState.AppealHistoryMessageId.Value)
            {
                // Це відповідь на історію звернення - обробляємо як нове повідомлення
                var activeAppeal = _appealService.GetActiveAppealForStudent(userId);
                if (activeAppeal != null && activeAppeal.Status != AppealStatus.ClosedByAdmin && activeAppeal.Status != AppealStatus.ClosedByStudent)
                {
                    // Отримуємо медіа-вкладення
                    string? photoFileId = message.Photo?.LastOrDefault()?.FileId;
                    string? documentFileId = message.Document?.FileId;
                    string? documentFileName = message.Document?.FileName;

                    var addedMessage = _appealService.AddMessage(
                        activeAppeal.Id, 
                        userId, 
                        $"{user.FirstName} {user.LastName}".Trim(), 
                        false, 
                        messageText,
                        photoFileId,
                        documentFileId,
                        documentFileName);

                    if (addedMessage != null)
                    {
                        string confirmText = $"✅ Ваше повідомлення додано до звернення #{activeAppeal.Id}";
                        if (photoFileId != null)
                            confirmText += "\n📸 Фото прикріплено";
                        if (documentFileId != null)
                            confirmText += $"\n📄 Документ прикріплено: {documentFileName}";

                        await _botClient.SendTextMessageAsync(
                            chatId: chatId,
                            text: confirmText
                        );

                        // Повідомити адміністраторів про нове повідомлення
                        await NotifyAdminsAboutNewMessage(activeAppeal, userId, 
                            $"{user.FirstName} {user.LastName}".Trim(), messageText, photoFileId, documentFileId, documentFileName);

                        // Оновлюємо історію - показуємо нову
                        await ShowUpdatedAppealHistory(message, activeAppeal.Id);
                    }
                    else
                    {
                        await _botClient.SendTextMessageAsync(
                            chatId: chatId,
                            text: "❌ Не вдалося додати повідомлення. Можливо звернення вже закрите."
                        );
                    }
                }
                return;
            }
        }

        // Якщо користувач в процесі написання повідомлення до звернення
        if (userState.DialogState == DialogState.WritingToAppeal)
        {
            if (messageText == "Скасувати" || messageText == "/cancel")
            {
                await HandleCancelCommand(message);
                return;
            }
            
            // Додаємо повідомлення до активного звернення
            if (userState.Data.TryGetValue("ActiveAppealId", out var appealIdObj) && appealIdObj is int appealId)
            {
                var activeAppeal = _appealService.GetAppealById(appealId);
                if (activeAppeal != null && activeAppeal.Status != AppealStatus.ClosedByAdmin && activeAppeal.Status != AppealStatus.ClosedByStudent)
                {
                    // Отримуємо медіа-вкладення
                    string? photoFileId = message.Photo?.LastOrDefault()?.FileId;
                    string? documentFileId = message.Document?.FileId;
                    string? documentFileName = message.Document?.FileName;

                    var addedMessage = _appealService.AddMessage(
                        activeAppeal.Id, 
                        userId, 
                        $"{user.FirstName} {user.LastName}".Trim(), 
                        false, 
                        messageText,
                        photoFileId,
                        documentFileId,
                        documentFileName);

                    if (addedMessage != null)
                    {
                        string confirmText = $"✅ Ваше повідомлення додано до звернення #{activeAppeal.Id}";
                        if (photoFileId != null)
                            confirmText += "\n📸 Фото прикріплено";
                        if (documentFileId != null)
                            confirmText += $"\n📄 Документ прикріплено: {documentFileName}";

                        await _botClient.SendTextMessageAsync(
                            chatId: chatId,
                            text: confirmText
                        );

                        // Повідомити адміністраторів про нове повідомлення
                        await NotifyAdminsAboutNewMessage(activeAppeal, userId, 
                            $"{user.FirstName} {user.LastName}".Trim(), messageText, photoFileId, documentFileId, documentFileName);

                        // Оновлюємо історію - показуємо нову
                        await ShowUpdatedAppealHistory(message, activeAppeal.Id);
                    }
                    else
                    {
                        await _botClient.SendTextMessageAsync(
                            chatId: chatId,
                            text: "❌ Не вдалося додати повідомлення. Можливо звернення вже закрите."
                        );
                    }
                }
                
                // Повертаємо користувача до меню звернень
                userState.DialogState = DialogState.None;
                userState.Data.Clear();
                return;
            }
        }

        // Якщо адміністратор пише відповідь на звернення
        if (userState.DialogState == DialogState.AdminReplyingToAppeal)
        {
            if (messageText == "Скасувати" || messageText == "/cancel")
            {
                userState.DialogState = DialogState.None;
                await _botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: "❌ Відповідь скасовано"
                );
                
                if (userState.SelectedAppealId.HasValue)
                {
                    await HandleAdminViewAppealCommand(message, userState.SelectedAppealId.Value);
                }
                return;
            }

            // Додаємо відповідь адміністратора до звернення
            if (userState.SelectedAppealId.HasValue)
            {
                var appeal = _appealService.GetAppealById(userState.SelectedAppealId.Value);
                if (appeal != null && appeal.Status != AppealStatus.ClosedByAdmin && appeal.Status != AppealStatus.ClosedByStudent)
                {
                    // Отримуємо медіа-вкладення
                    string? photoFileId = message.Photo?.LastOrDefault()?.FileId;
                    string? documentFileId = message.Document?.FileId;
                    string? documentFileName = message.Document?.FileName;

                    var addedMessage = _appealService.AddMessage(
                        appeal.Id, 
                        userId,
                        $"{user.FirstName} {user.LastName}".Trim(),
                        true, // isFromAdmin = true
                        messageText,
                        photoFileId,
                        documentFileId,
                        documentFileName
                    );

                    if (addedMessage != null)
                    {
                        string confirmText = $"✅ Відповідь додано до звернення #{appeal.Id}";
                        if (photoFileId != null)
                            confirmText += "\n📸 Фото прикріплено";
                        if (documentFileId != null)
                            confirmText += $"\n📄 Документ прикріплено: {documentFileName}";

                        await _botClient.SendTextMessageAsync(
                            chatId: chatId,
                            text: confirmText
                        );

                        // Повідомити студента про відповідь
                        try
                        {
                            string notificationText = $"💬 Нова відповідь на ваше звернення #{appeal.Id}\n\n" +
                                      $"Адміністратор:\n{messageText}";
                            
                            if (photoFileId != null)
                                notificationText += "\n📸 Прикріплено фото";
                            if (documentFileId != null)
                                notificationText += $"\n📄 Прикріплено: {documentFileName}";
                                
                            notificationText += "\n\nПерейдіть в розділ 'Звернення' щоб переглянути повну історію.";

                            await _botClient.SendTextMessageAsync(
                                chatId: appeal.StudentId,
                                text: notificationText
                            );
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Помилка при повідомленні студента: {ex.Message}");
                        }

                        // Показуємо оновлену історію адміністратору
                        userState.DialogState = DialogState.None;
                        await HandleAdminViewAppealCommand(message, appeal.Id);
                    }
                    else
                    {
                        await _botClient.SendTextMessageAsync(
                            chatId: chatId,
                            text: "❌ Не вдалося додати відповідь. Можливо звернення закрите."
                        );
                    }
                }
            }
            return;
        }

        // Якщо користувач в процесі створення звернення
        if (userState.DialogState == DialogState.CreatingAppeal)
        {
            if (messageText == "Скасувати" || messageText == "/cancel")
            {
                await HandleCancelCommand(message);
                return;
            }
            // Зберігаємо текст звернення та переходимо до підтвердження
            userState.Data["AppealText"] = messageText;
            userState.DialogState = DialogState.ConfirmingAppeal;
            await ShowAppealConfirmation(message, messageText);
            return;
        }

        // Якщо користувач підтверджує звернення
        if (userState.DialogState == DialogState.ConfirmingAppeal)
        {
            if (messageText == "Підтвердити")
            {
                await HandleAppealCreation(message);
                return;
            }
            else if (messageText == "Скасувати" || messageText == "/cancel")
            {
                await HandleCancelCommand(message);
                return;
            }
            else if (messageText == "Редагувати")
            {
                userState.DialogState = DialogState.CreatingAppeal;
                await _botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: "Добре, введіть новий текст звернення:",
                    replyMarkup: new ReplyKeyboardMarkup(new[]
                    {
                        new[] { new KeyboardButton("Скасувати") }
                    })
                    {
                        ResizeKeyboard = true
                    }
                );
                return;
            }
        }

        // Якщо студент вводить причину закриття звернення
        if (userState.DialogState == DialogState.ClosingAppeal)
        {
            if (messageText == "Скасувати" || messageText == "/cancel")
            {
                userState.DialogState = DialogState.None;
                userState.Data.Clear();
                await HandleAppealsMenuCommand(message);
                return;
            }

            // Отримуємо ID звернення для закриття
            if (userState.Data.TryGetValue("AppealToClose", out var appealIdObj) && appealIdObj is int appealId)
            {
                var appeal = _appealService.CloseAppeal(appealId, userId, false, messageText);
                
                if (appeal != null)
                {
                    // Повідомляємо адміністраторів
                    await NotifyAdminsAboutClosedAppeal(appeal, user);

                    await _botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: $"✅ Ваше звернення #{appeal.Id} закрито.\n\n" +
                              $"Причина: {messageText}\n\n" +
                              "Якщо у вас виникнуть нові питання, ви можете створити нове звернення."
                    );
                }
                else
                {
                    await _botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: "❌ Не вдалося закрити звернення"
                    );
                }

                userState.DialogState = DialogState.None;
                userState.Data.Clear();
                await HandleAppealsMenuCommand(message);
            }
            return;
        }

        // Якщо адмін вводить причину закриття звернення
        if (userState.DialogState == DialogState.AdminClosingAppeal)
        {
            if (messageText == "Скасувати" || messageText == "/cancel")
            {
                userState.DialogState = DialogState.None;
                if (userState.SelectedAppealId.HasValue)
                {
                    await HandleAdminViewAppealCommand(message, userState.SelectedAppealId.Value);
                }
                return;
            }

            if (userState.SelectedAppealId.HasValue)
            {
                var appeal = _appealService.CloseAppeal(userState.SelectedAppealId.Value, userId, true, messageText);
                
                if (appeal != null)
                {
                    await _botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: $"✅ Звернення #{appeal.Id} закрито\n\n" +
                              $"Причина: {messageText}"
                    );

                    // Повідомити студента
                    try
                    {
                        await _botClient.SendTextMessageAsync(
                            chatId: appeal.StudentId,
                            text: $"🔒 Ваше звернення #{appeal.Id} закрито адміністратором\n\n" +
                                  $"Причина: {messageText}\n\n" +
                                  "Якщо у вас виникнуть інші питання, створіть нове звернення."
                        );
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Помилка при повідомленні студента: {ex.Message}");
                    }
                }

                userState.DialogState = DialogState.None;
                userState.SelectedAppealId = null;
                await HandleAdminAppealsMenuCommand(message);
            }
            return;
        }

        // Якщо адміністратор публікує оголошення
        if (userState.DialogState == DialogState.CreatingNews)
        {
            if (messageText == "Скасувати" || messageText == "/cancel" || messageText == "◀️ Назад")
            {
                userState.DialogState = DialogState.None;
                userState.NewsTitle = null;
                userState.NewsContent = null;
                userState.NewsPhotoFileId = null;
                await HandleStartCommand(message);
                return;
            }

            // Обробка тексту або фото
            string? photoFileId = null;
            string title;
            string content;

            // Якщо є фото з підписом
            if (message.Photo != null && message.Photo.Length > 0)
            {
                photoFileId = message.Photo.Last().FileId;
                
                if (string.IsNullOrWhiteSpace(message.Caption))
                {
                    await _botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: "❌ Додайте підпис до фото у форматі:\nЗаголовок | Текст оголошення"
                    );
                    return;
                }

                var parts = message.Caption.Split('|', 2);
                if (parts.Length != 2)
                {
                    await _botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: "❌ Неправильний формат підпису. Використовуйте:\nЗаголовок | Текст оголошення"
                    );
                    return;
                }

                title = parts[0].Trim();
                content = parts[1].Trim();
            }
            // Якщо тільки текст
            else if (!string.IsNullOrWhiteSpace(messageText))
            {
                var parts = messageText.Split('|', 2);
                if (parts.Length != 2)
                {
                    await _botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: "❌ Неправильний формат. Використовуйте:\nЗаголовок | Текст оголошення"
                    );
                    return;
                }

                title = parts[0].Trim();
                content = parts[1].Trim();
            }
            else
            {
                await _botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: "❌ Надішліть текст або фото з підписом"
                );
                return;
            }

            if (string.IsNullOrEmpty(title) || string.IsNullOrEmpty(content))
            {
                await _botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: "❌ Заголовок та текст не можуть бути порожніми"
                );
                return;
            }

            // Зберігаємо дані новини
            userState.NewsTitle = title;
            userState.NewsContent = content;
            userState.NewsPhotoFileId = photoFileId;

            // Показуємо передогляд
            var previewText = $"📢 Передогляд оголошення:\n\n" +
                             $"<b>{title}</b>\n\n" +
                             $"{content}\n\n" +
                             $"<i>Опубліковано: {DateTime.UtcNow:dd.MM.yyyy HH:mm}</i>";

            if (!string.IsNullOrEmpty(photoFileId))
            {
                await _botClient.SendPhotoAsync(
                    chatId: chatId,
                    photo: InputFile.FromFileId(photoFileId),
                    caption: previewText,
                    parseMode: ParseMode.Html
                );
            }
            else
            {
                await _botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: previewText,
                    parseMode: ParseMode.Html
                );
            }

            // Кнопки для вибору аудиторії
            var keyboard = new ReplyKeyboardMarkup(new[]
            {
                new[] { new KeyboardButton("📤 Надіслати всім") },
                new[] { new KeyboardButton("🎯 Надіслати обраним") },
                new[] { new KeyboardButton("◀️ Назад") }
            })
            {
                ResizeKeyboard = true
            };

            await _botClient.SendTextMessageAsync(
                chatId: chatId,
                text: "Оберіть отримувачів:",
                replyMarkup: keyboard
            );

            userState.DialogState = DialogState.ChoosingNewsAudience;
            return;
        }

        // Вибір аудиторії для розсилки новин
        if (userState.DialogState == DialogState.ChoosingNewsAudience)
        {
            if (messageText == "◀️ Назад")
            {
                userState.DialogState = DialogState.None;
                userState.NewsTitle = null;
                userState.NewsContent = null;
                userState.NewsPhotoFileId = null;
                userState.SelectedCourses.Clear();
                userState.SelectedFaculties.Clear();
                await HandleStartCommand(message);
                return;
            }

            if (messageText == "📤 Надіслати всім")
            {
                // Надсилаємо всім без фільтрів
                var news = await _newsService.CreateAndBroadcastNewsAsync(
                    userState.NewsTitle!,
                    userState.NewsContent!,
                    sendImmediately: true,
                    photoFileId: userState.NewsPhotoFileId
                );

                await _botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: $"✅ Оголошення надіслано всім студентам!"
                );

                // Очищаємо дані
                userState.DialogState = DialogState.None;
                userState.NewsTitle = null;
                userState.NewsContent = null;
                userState.NewsPhotoFileId = null;
                await HandleStartCommand(message);
                return;
            }

            if (messageText == "🎯 Надіслати обраним")
            {
                // Показуємо меню вибору курсів/факультетів
                var keyboard = new ReplyKeyboardMarkup(new[]
                {
                    new[] { new KeyboardButton("1 курс"), new KeyboardButton("2 курс"), new KeyboardButton("3 курс") },
                    new[] { new KeyboardButton("4 курс"), new KeyboardButton("5 курс"), new KeyboardButton("6 курс") },
                    new[] { new KeyboardButton("📋 Факультети") },
                    new[] { new KeyboardButton("✅ Завершити вибір"), new KeyboardButton("◀️ Назад") }
                })
                {
                    ResizeKeyboard = true
                };

                var selectedInfo = "🎯 Вибір аудиторії\n\n";
                if (userState.SelectedCourses.Any())
                    selectedInfo += $"Курси: {string.Join(", ", userState.SelectedCourses)}\n";
                if (userState.SelectedFaculties.Any())
                    selectedInfo += $"Факультети: {string.Join(", ", userState.SelectedFaculties)}\n";
                
                if (!userState.SelectedCourses.Any() && !userState.SelectedFaculties.Any())
                    selectedInfo += "Нічого не обрано\n";

                selectedInfo += "\nОберіть курси або факультети:";

                await _botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: selectedInfo,
                    replyMarkup: keyboard
                );
                return;
            }

            // Обробка вибору курсів
            if (messageText.EndsWith(" курс"))
            {
                var courseNum = int.Parse(messageText.Split(' ')[0]);
                if (userState.SelectedCourses.Contains(courseNum))
                {
                    userState.SelectedCourses.Remove(courseNum);
                    await _botClient.SendTextMessageAsync(chatId: chatId, text: $"❌ {courseNum} курс прибрано");
                }
                else
                {
                    userState.SelectedCourses.Add(courseNum);
                    await _botClient.SendTextMessageAsync(chatId: chatId, text: $"✅ {courseNum} курс додано");
                }
                return;
            }

            // Вибір факультетів
            if (messageText == "📋 Факультети")
            {
                var keyboard = new ReplyKeyboardMarkup(new[]
                {
                    new[] { new KeyboardButton("Медичний №1"), new KeyboardButton("Медичний №2") },
                    new[] { new KeyboardButton("Стоматологічний"), new KeyboardButton("Фармацевтичний") },
                    new[] { new KeyboardButton("Foreign students"), new KeyboardButton("ННІ Громадського здоров'я") },
                    new[] { new KeyboardButton("ННІ Медсестринства"), new KeyboardButton("ННІ Психології") },
                    new[] { new KeyboardButton("◀️ Назад до курсів") }
                })
                {
                    ResizeKeyboard = true
                };

                await _botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: "Оберіть факультети:",
                    replyMarkup: keyboard
                );
                return;
            }

            // Обробка конкретних факультетів
            if (messageText is "Медичний №1" or "Медичний №2" or "Стоматологічний" or "Фармацевтичний" or
                "Foreign students" or "ННІ Громадського здоров'я" or "ННІ Медсестринства" or "ННІ Психології")
            {
                if (userState.SelectedFaculties.Contains(messageText))
                {
                    userState.SelectedFaculties.Remove(messageText);
                    await _botClient.SendTextMessageAsync(chatId: chatId, text: $"❌ {messageText} прибрано");
                }
                else
                {
                    userState.SelectedFaculties.Add(messageText);
                    await _botClient.SendTextMessageAsync(chatId: chatId, text: $"✅ {messageText} додано");
                }
                return;
            }

            if (messageText == "◀️ Назад до курсів")
            {
                var keyboard = new ReplyKeyboardMarkup(new[]
                {
                    new[] { new KeyboardButton("1 курс"), new KeyboardButton("2 курс"), new KeyboardButton("3 курс") },
                    new[] { new KeyboardButton("4 курс"), new KeyboardButton("5 курс"), new KeyboardButton("6 курс") },
                    new[] { new KeyboardButton("📋 Факультети") },
                    new[] { new KeyboardButton("✅ Завершити вибір"), new KeyboardButton("◀️ Назад") }
                })
                {
                    ResizeKeyboard = true
                };

                await _botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: "Оберіть курси:",
                    replyMarkup: keyboard
                );
                return;
            }

            if (messageText == "✅ Завершити вибір")
            {
                if (!userState.SelectedCourses.Any() && !userState.SelectedFaculties.Any())
                {
                    await _botClient.SendTextMessageAsync(
                        chatId: chatId,
                        text: "❌ Оберіть хоча б один курс або факультет"
                    );
                    return;
                }

                // Надсилаємо з фільтрами
                var news = await _newsService.CreateAndBroadcastNewsAsync(
                    userState.NewsTitle!,
                    userState.NewsContent!,
                    sendImmediately: true,
                    photoFileId: userState.NewsPhotoFileId,
                    courses: userState.SelectedCourses.Any() ? userState.SelectedCourses : null,
                    faculties: userState.SelectedFaculties.Any() ? userState.SelectedFaculties : null
                );

                var summary = "✅ Оголошення надіслано!\n\n";
                if (userState.SelectedCourses.Any())
                    summary += $"Курси: {string.Join(", ", userState.SelectedCourses)}\n";
                if (userState.SelectedFaculties.Any())
                    summary += $"Факультети: {string.Join(", ", userState.SelectedFaculties)}";

                await _botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: summary
                );

                // Очищаємо дані
                userState.DialogState = DialogState.None;
                userState.NewsTitle = null;
                userState.NewsContent = null;
                userState.NewsPhotoFileId = null;
                userState.SelectedCourses.Clear();
                userState.SelectedFaculties.Clear();
                await HandleStartCommand(message);
                return;
            }
        }

        // Редагування контактної інформації
        if (userState.DialogState == DialogState.EditingContactInfo)
        {
            if (messageText == "◀️ Назад" || messageText == "/cancel")
            {
                userState.DialogState = DialogState.None;
                await HandleStartCommand(message);
                return;
            }

            // Зберігаємо нову контактну інформацію
            var contactInfo = await _context.ContactInfo.FirstOrDefaultAsync();
            
            if (contactInfo == null)
            {
                contactInfo = new ContactInfo
                {
                    Title = "Контактна інформація",
                    Content = messageText,
                    UpdatedAt = DateTime.UtcNow,
                    UpdatedBy = message.From!.Id
                };
                _context.ContactInfo.Add(contactInfo);
            }
            else
            {
                contactInfo.Content = messageText;
                contactInfo.UpdatedAt = DateTime.UtcNow;
                contactInfo.UpdatedBy = message.From!.Id;
                _context.ContactInfo.Update(contactInfo);
            }

            await _context.SaveChangesAsync();

            await _botClient.SendTextMessageAsync(
                chatId: chatId,
                text: "✅ Контактну інформацію успішно оновлено!"
            );

            userState.DialogState = DialogState.None;
            await HandleStartCommand(message);
            return;
        }

        // Редагування інформації про партнерів
        if (userState.DialogState == DialogState.EditingPartnersInfo)
        {
            if (messageText == "◀️ Назад" || messageText == "/cancel")
            {
                userState.DialogState = DialogState.None;
                await HandleStartCommand(message);
                return;
            }

            // Зберігаємо нову інформацію про партнерів
            var partnersInfo = await _context.PartnersInfo.FirstOrDefaultAsync();
            
            if (partnersInfo == null)
            {
                partnersInfo = new PartnersInfo
                {
                    Title = "Партнери",
                    Content = messageText,
                    UpdatedAt = DateTime.UtcNow,
                    UpdatedBy = message.From!.Id
                };
                _context.PartnersInfo.Add(partnersInfo);
            }
            else
            {
                partnersInfo.Content = messageText;
                partnersInfo.UpdatedAt = DateTime.UtcNow;
                partnersInfo.UpdatedBy = message.From!.Id;
                _context.PartnersInfo.Update(partnersInfo);
            }

            await _context.SaveChangesAsync();

            await _botClient.SendTextMessageAsync(
                chatId: chatId,
                text: "✅ Інформацію про партнерів успішно оновлено!"
            );

            userState.DialogState = DialogState.None;
            await HandleStartCommand(message);
            return;
        }

        // Редагування інформації про заходи
        if (userState.DialogState == DialogState.EditingEventsInfo)
        {
            if (messageText == "◀️ Назад" || messageText == "/cancel")
            {
                userState.DialogState = DialogState.None;
                await HandleStartCommand(message);
                return;
            }

            // Зберігаємо нову інформацію про заходи
            var eventsInfo = await _context.EventsInfo.FirstOrDefaultAsync();
            
            if (eventsInfo == null)
            {
                eventsInfo = new EventsInfo
                {
                    Title = "Заходи",
                    Content = messageText,
                    UpdatedAt = DateTime.UtcNow,
                    UpdatedBy = message.From!.Id
                };
                _context.EventsInfo.Add(eventsInfo);
            }
            else
            {
                eventsInfo.Content = messageText;
                eventsInfo.UpdatedAt = DateTime.UtcNow;
                eventsInfo.UpdatedBy = message.From!.Id;
                _context.EventsInfo.Update(eventsInfo);
            }

            await _context.SaveChangesAsync();

            await _botClient.SendTextMessageAsync(
                chatId: chatId,
                text: "✅ Інформацію про заходи успішно оновлено!"
            );

            userState.DialogState = DialogState.None;
            await HandleStartCommand(message);
            return;
        }

        if (messageText.StartsWith("/publish") && message.From is not null)
        {
            if (await _userService.IsAdminAsync(message.From.Id))
            {
                await HandlePublishNewsCommand(message);
                return;
            }
            else
            {
                await _botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: " У вас немає прав для публікації новин."
                );
                return;
            }
        }

        if (messageText.StartsWith("/close") && message.From is not null)
        {
            if (await _userService.IsAdminAsync(message.From.Id))
            {
                await HandleCloseAppealCommand(message);
                return;
            }
            else
            {
                await _botClient.SendTextMessageAsync(
                    chatId: chatId,
                    text: "⚠️ У вас немає прав для закриття звернень."
                );
                return;
            }
        }

        var command = messageText switch
        {
            "/start" => CommandType.Start,
            "/help" => CommandType.Help,
            "📩 Звернення" => CommandType.Appeal,
            "❓ Допомога" => CommandType.Help,
            "ℹ️ Інформація" => CommandType.Info,
            "🏠 Гуртожиток" => CommandType.Dormitory,
            "🌟 Можливості" => CommandType.Opportunities,
            "🤝 Партнери" => CommandType.Partners,
            "🎉 Заходи" => CommandType.Events,
            "💡 Запропонувати захід" => CommandType.SuggestEvent,
            "✍️ Написати адміністратору" => CommandType.WriteToAppeal,
            "✍️ Написати повідомлення" => CommandType.WriteToAppeal,
            "/cancelappeal" => CommandType.CancelAppeal,
            "Закрити звернення" => CommandType.CancelAppeal,
            "/cancel" => CommandType.Cancel,
            "Скасувати" => CommandType.Cancel,
            "◀️ Назад" => CommandType.Back,
            "Назад" => CommandType.Back,
            "Підтвердити" => CommandType.Unknown, // Обробляється вище
            "Редагувати" => CommandType.Unknown, // Обробляється вище
            
            // Команди адміністратора
            "📢 Опублікувати оголошення" => CommandType.AdminPublishNews,
            "📊 Статистика" => CommandType.AdminStatistics,
            "📝 Редагувати контакти" => CommandType.AdminEditContacts,
            "Редагувати контакти" => CommandType.AdminEditContacts,
            "🤝 Редагувати партнерів" => CommandType.AdminEditPartners,
            "🎉 Редагувати заходи" => CommandType.AdminEditEvents,
            "Експорт користувачів" => CommandType.AdminExportUsers,
            "✅ Активні звернення" => CommandType.AdminActiveAppeals,
            "❌ Закриті звернення" => CommandType.AdminClosedAppeals,
            "✍️ Відповісти" => CommandType.AdminReplyToAppeal,
            "👤 Дані студента" => CommandType.AdminUserInfo,
            "❌ Закрити звернення" => CommandType.AdminCloseAppeal,
            "⬅️ Попередня" => CommandType.PreviousPage,
            "Наступна ➡️" => CommandType.NextPage,
            
            _ when messageText.StartsWith("/appeal_") => CommandType.Appeal, // /appeal_123
            _ => CommandType.Unknown
        };

        var action = command switch
        {
            CommandType.Start => HandleStartCommand(message),
            CommandType.Help => HandleHelpCommand(message),
            CommandType.Info => HandleInfoCommand(message),
            CommandType.Dormitory => HandleDormitoryCommand(message),
            CommandType.Opportunities => HandleOpportunitiesCommand(message),
            CommandType.Partners => HandlePartnersCommand(message),
            CommandType.Events => HandleEventsCommand(message),
            CommandType.SuggestEvent => HandleSuggestEventCommand(message),
            CommandType.Appeal => HandleAppealCommandAsync(message),
            CommandType.Cancel => HandleCancelCommand(message),
            CommandType.CancelAppeal => HandleCancelAppealCommand(message),
            CommandType.WriteToAppeal => HandleWriteToAppealCommand(message),
            CommandType.Back => HandleBackCommand(message),
            
            // Адмін команди
            CommandType.AdminPublishNews => HandleAdminPublishNewsMenuCommand(message),
            CommandType.AdminStatistics => HandleAdminStatisticsCommand(message),
            CommandType.AdminEditContacts => HandleAdminEditContactsCommand(message),
            CommandType.AdminEditPartners => HandleAdminEditPartnersCommand(message),
            CommandType.AdminEditEvents => HandleAdminEditEventsCommand(message),
            CommandType.AdminExportUsers => HandleAdminExportUsersCommand(message),
            CommandType.AdminActiveAppeals => HandleAdminActiveAppealsCommand(message),
            CommandType.AdminClosedAppeals => HandleAdminClosedAppealsCommand(message),
            CommandType.AdminReplyToAppeal => HandleAdminReplyToAppealCommand(message),
            CommandType.AdminUserInfo => HandleAdminUserInfoCommand(message),
            CommandType.AdminCloseAppeal => HandleAdminCloseAppealCommand(message),
            CommandType.PreviousPage => HandlePreviousPageCommand(message),
            CommandType.NextPage => HandleNextPageCommand(message),
            
            _ => HandleUnknownCommand(message)
        };

        await action;
    }

    private async Task HandleAppealCreation(Message message)
    {
        if (message.From == null)
            return;

        var userState = GetUserState(message.From.Id);
        
        // Отримуємо текст звернення зі стану
        if (!userState.Data.TryGetValue("AppealText", out var appealTextObj) || appealTextObj is not string appealText)
            return;

        var appeal = _appealService.CreateAppeal(
            message.From.Id,
            $"{message.From.FirstName} {message.From.LastName}".Trim(),
            appealText
        );

        userState.DialogState = DialogState.None;
        userState.Data.Clear();

        await _botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: $"✅ Ваше звернення успішно створено!\n\n" +
                  $"Номер звернення: #{appeal.Id}\n" +
                  $"Статус: {GetStatusText(appeal.Status)}\n\n"
        );

        // Повідомити адміністраторів про нове звернення
        await NotifyAdminsAboutNewAppeal(appeal);

        // Повертаємося в меню звернень
        await HandleAppealsMenuCommand(message);
    }

    private async Task NotifyAdminsAboutNewAppeal(Appeal appeal)
    {
        var adminIds = await _userService.GetAdminIdsAsync();
        var userName = string.IsNullOrEmpty(appeal.StudentName) ? $"ID: {appeal.StudentId}" : appeal.StudentName;

        foreach (var adminId in adminIds)
        {
            try
            {
                await _botClient.SendTextMessageAsync(
                    chatId: adminId,
                    text: $"📩 Нове звернення #{appeal.Id}\n\n від: {userName}\n" +
                          $"Дата: {appeal.CreatedAt:dd.MM.yyyy HH:mm}\n\n" +
                          $"\n{appeal.Message}\n\n"
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Помилка при відправці повідомлення адміністратору {adminId}: {ex.Message}");
            }
        }
    }

    private async Task NotifyAdminsAboutNewMessage(Appeal appeal, long userId, string userName, string messageText,
        string? photoFileId = null, string? documentFileId = null, string? documentFileName = null)
    {
        var adminIds = await _userService.GetAdminIdsAsync();

        foreach (var adminId in adminIds)
        {
            try
            {
                string notificationText = $"💬 Нове повідомлення до звернення #{appeal.Id} від: {userName}\n\n" +
                        $"\n\n{messageText}";

                if (photoFileId != null)
                    notificationText += "\n📸 Прикріплено фото";
                if (documentFileId != null)
                    notificationText += $"\n📄 Прикріплено: {documentFileName}";

                await _botClient.SendTextMessageAsync(
                    chatId: adminId,
                    text: notificationText
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Помилка при відправці повідомлення адміністратору {adminId}: {ex.Message}");
            }
        }
    }

    private async Task HandleAdminReply(Message message)
    {
        if (message.ReplyToMessage?.Text == null || message.Text == null || message.From == null)
            return;

        // Витягуємо ID звернення з тексту повідомлення, на яке відповіли
        var replyText = message.ReplyToMessage.Text;
        var appealIdMatch = System.Text.RegularExpressions.Regex.Match(replyText, @"звернення #(\d+)");
        
        if (!appealIdMatch.Success)
        {
            await _botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: "⚠️ Не вдалося знайти номер звернення в повідомленні, на яке ви відповіли."
            );
            return;
        }

        var appealId = int.Parse(appealIdMatch.Groups[1].Value);
        var appeal = _appealService.GetAppealById(appealId);

        if (appeal == null)
        {
            await _botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: $"⚠️ Звернення #{appealId} не знайдено."
            );
            return;
        }

        if (appeal.ClosedAt != null)
        {
            await _botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: $"⚠️ Звернення #{appealId} вже закрито. Використайте команду для повторного відкриття або створіть нове."
            );
            return;
        }

        // Додаємо відповідь адміністратора до історії звернення
        var adminName = $"{message.From.FirstName} {message.From.LastName}".Trim();
        var addedMessage = _appealService.AddMessage(appealId, message.From.Id, adminName, true, message.Text);

        // Відправляємо повідомлення студенту одразу
        try
        {
            var sender = "👤 Адміністратор";
            var timeStamp = DateTime.UtcNow.ToString("dd.MM HH:mm");
            
            await _botClient.SendTextMessageAsync(
                chatId: appeal.StudentId,
                text: $"💬 Нова відповідь на звернення #{appealId}!\n\n" +
                      $"{sender} ({timeStamp}):\n{message.Text}\n\n"
            );
            

            await _botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: $"✅ Вашу відповідь надіслано студенту."
            );
        }
        catch (Exception ex)
        {
            await _botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: $"❌ Помилка при відправці повідомлення студенту: {ex.Message}"
            );
        }
    }

    private async Task HandleCloseAppealCommand(Message message)
    {
        if (message.Text == null)
            return;

        // Якщо команда використовується через Reply
        if (message.ReplyToMessage?.Text != null)
        {
            var replyText = message.ReplyToMessage.Text;
            var appealIdMatch = System.Text.RegularExpressions.Regex.Match(replyText, @"звернення #(\d+)");
            
            if (!appealIdMatch.Success)
            {
                await _botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: "⚠️ Не вдалося знайти номер звернення в повідомленні, на яке ви відповіли.\n" +
                          "Використайте формат: /close <номер>"
                );
                return;
            }

            var appealId = int.Parse(appealIdMatch.Groups[1].Value);
            await CloseAppealById(message, appealId);
        }
        // Якщо вказано номер в команді: /close 123
        else
        {
            var parts = message.Text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2 || !int.TryParse(parts[1], out var appealId))
            {
                await _botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: "⚠️ Використайте формат: /close <номер_звернення>\n" +
                          "Або зробіть Reply на повідомлення зі зверненням і напишіть /close"
                );
                return;
            }

            await CloseAppealById(message, appealId);
        }
    }

    private async Task CloseAppealById(Message message, int appealId)
    {
        if (message.From == null)
            return;

        var appeal = _appealService.CloseAppeal(appealId, message.From.Id, true, "Закрито через команду /close");

        if (appeal == null)
        {
            await _botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: $"⚠️ Звернення #{appealId} не знайдено."
            );
            return;
        }

        // Повідомляємо студента про закриття звернення
        try
        {
            await _botClient.SendTextMessageAsync(
                chatId: appeal.StudentId,
                text: $"✅ Ваше звернення #{appealId} було закрито адміністратором.\n\n" +
                      $"Причина: {appeal.ClosedReason}\n\n" +
                      "Якщо у вас виникнуть нові питання, ви можете створити нове звернення."
            );
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Помилка при повідомленні студента про закриття звернення: {ex.Message}");
        }

        await _botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: $"✅ Звернення #{appealId} успішно закрито."
        );
    }

    private async Task ShowAppealConfirmation(Message message, string appealText)
    {
        var keyboard = new ReplyKeyboardMarkup(new[]
        {
            new[] { new KeyboardButton("Підтвердити"), new KeyboardButton("Редагувати") },
            new[] { new KeyboardButton("Скасувати") }
        })
        {
            ResizeKeyboard = true
        };

        await _botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: $"Ваше звернення:\n\n{appealText}\n\n" +
                  "Перевірте текст звернення. Що бажаєте зробити?",
            replyMarkup: keyboard
        );
    }

    private async Task HandleStartCommand(Message message)
    {
        if (message.From == null)
            return;

        var userState = GetUserState(message.From.Id);
        userState.CurrentMenu = MenuState.Main;
        userState.DialogState = DialogState.None;

        await ShowMainMenuInline(message.Chat.Id, message.From.Id);
    }

    private async Task HandleAppealsMenuCommand(Message message)
    {
        if (message.From == null)
            return;

        var userState = GetUserState(message.From.Id);
        userState.CurrentMenu = MenuState.Appeals;

        // Перевіряємо чи користувач заблокований
        if (await _userService.IsBannedAsync(message.From.Id))
        {
            var keyboardBanned = new ReplyKeyboardMarkup(new[]
            {
                new[] { new KeyboardButton("◀️ Назад") }
            })
            {
                ResizeKeyboard = true
            };

            await _botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: "🚫 <b>Доступ до звернень заборонено</b>\n\n" +
                      "На жаль, вам заборонено створювати звернення через зловживання сервісом.\n\n" +
                      "Якщо ви вважаєте, що це помилка, зв'яжіться з адміністрацією профспілки іншими способами.",
                parseMode: ParseMode.Html,
                replyMarkup: keyboardBanned
            );
            return;
        }

        var activeAppeal = _appealService.GetActiveAppealForStudent(message.From.Id);

        if (activeAppeal == null)
        {
            // Немає активного звернення - показуємо кнопку написати адміністратору
            var keyboard = new ReplyKeyboardMarkup(new[]
            {
                new[] { new KeyboardButton("✍️ Написати адміністратору") },
                new[] { new KeyboardButton("◀️ Назад") }
            })
            {
                ResizeKeyboard = true
            };

            await _botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: "📩 Розділ: Звернення\n\n" +
                      "У вас немає активного звернення.\n\n" +
                      "Ви можете написати адміністратору, щоб створити нове звернення." +
                      "\n Увага! Звернення що стосуються проживання в гуртожитку просимо надсилати на пошту: \nhostel@vnmu.edu.ua",
                replyMarkup: keyboard
            );
            return;
        }

        // Є активне звернення - формуємо історію в одне повідомлення
        var historyText = $"📩 Звернення #{activeAppeal.Id}\n" +
                         $"Статус: {GetStatusText(activeAppeal.Status)}\n" +
                         $"Створено: {activeAppeal.CreatedAt:dd.MM.yyyy HH:mm}\n\n" +
                         $"━━━━━━━━━━━━━━━━━━━━\n\n";

        // Отримуємо всі повідомлення звернення
        var messages = _appealService.GetAllAppealMessages(activeAppeal.Id).ToList();

        // Додаємо кожне повідомлення до історії
        foreach (var msg in messages)
        {
            var sender = msg.IsFromAdmin ? "👤 Адміністратор" : "👨‍🎓 Ви";
            var timeStamp = msg.SentAt.ToString("dd.MM HH:mm");
            
            historyText += $"{sender} ({timeStamp}):\n{msg.Text}\n\n";
        }

        historyText += "━━━━━━━━━━━━━━━━━━━━\n\n" +
                      "💡 Щоб відповісти, зробіть Reply на це повідомлення\n" +
                      "або натисніть кнопку 'Написати повідомлення'";

        // Відправляємо історію одним повідомленням з клавіатурою
        var keyboardWithAppeal = new ReplyKeyboardMarkup(new[]
        {
            new[] { new KeyboardButton("✍️ Написати повідомлення") },
            new[] { new KeyboardButton("Закрити звернення"), new KeyboardButton("◀️ Назад") }
        })
        {
            ResizeKeyboard = true
        };

        var sentMessage = await _botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: historyText,
            replyMarkup: keyboardWithAppeal
        );

        // Зберігаємо ID повідомлення з історією для відстеження Reply
        userState.AppealHistoryMessageId = sentMessage.MessageId;
    }

    private async Task HandleHelpCommand(Message message)
    {
        if (message.From == null)
            return;

        var userState = GetUserState(message.From.Id);
        userState.CurrentMenu = MenuState.Help;

        var keyboard = new ReplyKeyboardMarkup(new[]
        {
            new[] { new KeyboardButton("◀️ Назад") }
        })
        {
            ResizeKeyboard = true
        };

        var helpText = "❓ <b>Розділ: Допомога</b>\n\n" +
                      "🤖 <b>Про бота:</b>\n" +
                      "Це офіційний бот Профкому студентів, який допомагає студентам звертатися до профкому та отримувати актуальні оголошення.\n\n" +
                      
                      "📋 <b>Розділи бота:</b>\n\n" +
                      
                      "📩 <b>Звернення</b>\n" +
                      "Створюйте звернення до профкому, отримуйте відповіді та відстежуйте їх статус.\n" +
                      "• Написати адміністратору - створити нове звернення\n" +
                      "• Переглянути відповіді - переглянути активні звернення\n" +
                      "• Історія звернень - архів закритих звернень\n\n" +
                      
                      "❓ <b>Допомога</b>\n" +
                      "Інформація про роботу з ботом та його можливості.\n\n" +
                      
                      "ℹ️ <b>Інформація</b>\n" +
                      "Контактна інформація профкому студентів.\n\n" +
                      
                      "📖 <b>Як створити звернення:</b>\n" +
                      "1️⃣ Перейдіть в розділ 'Звернення'\n" +
                      "2️⃣ Натисніть 'Написати адміністратору'\n" +
                      "3️⃣ Опишіть ваше питання або проблему\n" +
                      "4️⃣ Можете додати фото або документ\n" +
                      "5️⃣ Підтвердіть відправку\n\n" +
                      
                      "📬 <b>Як отримати відповідь:</b>\n" +
                      "1️⃣ Натисніть 'Переглянути відповіді'\n" +
                      "2️⃣ Виберіть звернення зі списку\n" +
                      "3️⃣ Переглянете всю історію спілкування\n" +
                      "4️⃣ Можете написати додаткове повідомлення\n\n" +
                      
                      "✅ <b>Закриття звернення:</b>\n" +
                      "Коли питання вирішено, натисніть 'Закрити звернення' та вкажіть причину закриття.\n\n" +
                      
                      "💡 <b>Корисні команди:</b>\n" +
                      "• /start - Головне меню\n" +
                      "• /cancel - Скасувати поточну дію";

        if (await _userService.IsAdminAsync(message.From.Id))
        {
            helpText += "\n\n� <b>Адміністративні функції:</b>\n\n" +
                       "📩 <b>Звернення</b>\n" +
                       "• Перегляд активних та закритих звернень\n" +
                       "• Відповіді студентам з можливістю медіа\n" +
                       "• Перегляд даних студента\n" +
                       "• Закриття звернень з вказанням причини\n\n" +
                       
                       "📢 <b>Опублікувати оголошення</b>\n" +
                       "• Створення оголошень з фото або текстом\n" +
                       "• Вибіркова розсилка за курсами/факультетами\n" +
                       "• Передогляд перед публікацією\n\n" +
                       
                       "📊 <b>Статистика</b>\n" +
                       "• Загальна статистика користувачів\n" +
                       "• Розподіл по курсах та факультетах\n" +
                       "• Статистика звернень\n" +
                       "• Експорт користувачів у CSV";
        }

        await _botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: helpText,
            parseMode: ParseMode.Html,
            replyMarkup: keyboard
        );
    }

    private async Task HandleInfoCommand(Message message)
    {
        if (message.From == null)
            return;

        var userState = GetUserState(message.From.Id);
        userState.CurrentMenu = MenuState.Info;

        var keyboard = new ReplyKeyboardMarkup(new[]
        {
            new[] { new KeyboardButton("◀️ Назад") }
        })
        {
            ResizeKeyboard = true
        };

        // Отримуємо контактну інформацію з БД
        var contactInfo = await _context.ContactInfo.FirstOrDefaultAsync();
        
        string infoText;
        if (contactInfo != null && !string.IsNullOrWhiteSpace(contactInfo.Content))
        {
            infoText = $"ℹ️ <b>{contactInfo.Title}</b>\n\n{contactInfo.Content}";
            
            if (contactInfo.UpdatedAt != default)
            {
                infoText += $"\n\n<i>Оновлено: {contactInfo.UpdatedAt:dd.MM.yyyy HH:mm}</i>";
            }
        }
        else
        {
            infoText = "ℹ️ <b>Контактна інформація</b>\n\n" +
                      "🏛️ Профспілка студентів\n\n" +
                      "Контактна інформація ще не налаштована.\n" +
                      "Зверніться до адміністратора.";
        }

        await _botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: infoText,
            parseMode: ParseMode.Html,
            replyMarkup: keyboard
        );
    }

    private async Task HandleDormitoryCommand(Message message)
    {
        if (message.From == null)
            return;

        var userState = GetUserState(message.From.Id);
        userState.CurrentMenu = MenuState.Dormitory;

        var keyboard = new ReplyKeyboardMarkup(new[]
        {
            new[] { new KeyboardButton("◀️ Назад") }
        })
        {
            ResizeKeyboard = true
        };

        var dormitoryText = "🏠 <b>Розділ: Гуртожиток</b>\n\n" +
                           "З питань щодо проживання та поселення в гуртожитки ви можете звернутись за допомогою електронної пошти:\n\n" +
                           "📧 hostel@vnmu.edu.ua";

        await _botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: dormitoryText,
            parseMode: ParseMode.Html,
            replyMarkup: keyboard
        );
    }

    private async Task HandleOpportunitiesCommand(Message message)
    {
        if (message.From == null)
            return;

        var userState = GetUserState(message.From.Id);
        userState.CurrentMenu = MenuState.Opportunities;

        var keyboard = new ReplyKeyboardMarkup(new[]
        {
            new[] { new KeyboardButton("🤝 Партнери") },
            new[] { new KeyboardButton("🎉 Заходи") },
            new[] { new KeyboardButton("💡 Запропонувати захід") },
            new[] { new KeyboardButton("◀️ Назад") }
        })
        {
            ResizeKeyboard = true
        };

        await _botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: "🌟 <b>Розділ: Можливості</b>\n\n" +
                  "Оберіть підрозділ:",
            parseMode: ParseMode.Html,
            replyMarkup: keyboard
        );
    }

    private async Task HandlePartnersCommand(Message message)
    {
        if (message.From == null)
            return;

        var userState = GetUserState(message.From.Id);
        userState.CurrentMenu = MenuState.Opportunities;

        var keyboard = new ReplyKeyboardMarkup(new[]
        {
            new[] { new KeyboardButton("◀️ Назад") }
        })
        {
            ResizeKeyboard = true
        };

        // Отримуємо інформацію про партнерів з БД
        var partnersInfo = await _context.PartnersInfo.FirstOrDefaultAsync();
        
        string partnersText;
        if (partnersInfo != null && !string.IsNullOrWhiteSpace(partnersInfo.Content))
        {
            partnersText = $"🤝 <b>{partnersInfo.Title}</b>\n\n{partnersInfo.Content}";
            
            if (partnersInfo.UpdatedAt != default)
            {
                partnersText += $"\n\n<i>Оновлено: {partnersInfo.UpdatedAt:dd.MM.yyyy HH:mm}</i>";
            }
        }
        else
        {
            partnersText = "🤝 <b>Партнери</b>\n\n" +
                          "Профспілка студентів співпрацює з різними організаціями та компаніями, " +
                          "які надають знижки та спеціальні пропозиції для студентів.\n\n" +
                          "📋 Список партнерів та доступних знижок:\n\n" +
                          "Інформація оновлюється. Слідкуйте за оголошеннями!";
        }

        await _botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: partnersText,
            parseMode: ParseMode.Html,
            replyMarkup: keyboard
        );
    }

    private async Task HandleEventsCommand(Message message)
    {
        if (message.From == null)
            return;

        var userState = GetUserState(message.From.Id);
        userState.CurrentMenu = MenuState.Opportunities;

        var keyboard = new ReplyKeyboardMarkup(new[]
        {
            new[] { new KeyboardButton("◀️ Назад") }
        })
        {
            ResizeKeyboard = true
        };

        // Отримуємо інформацію про заходи з БД
        var eventsInfo = await _context.EventsInfo.FirstOrDefaultAsync();
        
        string eventsText;
        if (eventsInfo != null && !string.IsNullOrWhiteSpace(eventsInfo.Content))
        {
            eventsText = $"🎉 <b>{eventsInfo.Title}</b>\n\n{eventsInfo.Content}";
            
            if (eventsInfo.UpdatedAt != default)
            {
                eventsText += $"\n\n<i>Оновлено: {eventsInfo.UpdatedAt:dd.MM.yyyy HH:mm}</i>";
            }
        }
        else
        {
            eventsText = "🎉 <b>Заходи</b>\n\n" +
                        "Тут ви можете переглянути інформацію про майбутні та поточні заходи, " +
                        "організовані профспілкою студентів.\n\n" +
                        "📅 Актуальні заходи:\n\n" +
                        "Інформація про заходи публікується через оголошення. " +
                        "Слідкуйте за новинами від бота!";
        }

        await _botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: eventsText,
            parseMode: ParseMode.Html,
            replyMarkup: keyboard
        );
    }

    private async Task HandleSuggestEventCommand(Message message)
    {
        if (message.From == null)
            return;

        var userState = GetUserState(message.From.Id);
        userState.CurrentMenu = MenuState.Opportunities;

        var keyboard = new ReplyKeyboardMarkup(new[]
        {
            new[] { new KeyboardButton("◀️ Назад") }
        })
        {
            ResizeKeyboard = true
        };

        var suggestText = "💡 <b>Запропонувати захід</b>\n\n" +
                         "Маєте ідею для цікавого заходу? Хочете організувати щось разом з профспілкою?\n\n" +
                         "📝 Заповніть форму для подачі пропозиції заходу:\n" +
                         "🔗 https://forms.gle/14ZGAxv15zgyhUHg7\n\n" +
                         "Ми розглянемо вашу пропозицію та зв'яжемося з вами!\n\n" +
                         "💡 У формі вкажіть:\n" +
                         "• Назву заходу\n" +
                         "• Опис ідеї\n" +
                         "• Бажаний формат та термін проведення\n" +
                         "• Ваші контактні дані для зв'язку";

        await _botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: suggestText,
            parseMode: ParseMode.Html,
            replyMarkup: keyboard
        );
    }

    private async Task HandleBackCommand(Message message)
    {
        if (message.From == null)
            return;

        var userState = GetUserState(message.From.Id);
        
        // Скидаємо стан діалогу при поверненні назад
        userState.DialogState = DialogState.None;
        userState.Data.Clear();
        
        // Визначаємо куди повертатись в залежності від поточного меню
        switch (userState.CurrentMenu)
        {
            case MenuState.Opportunities:
                // З підменю можливостей повертаємось в головне меню
                await HandleStartCommand(message);
                break;
                
            case MenuState.AdminAppeals:
            case MenuState.AdminPublishNews:
            case MenuState.AdminStatistics:
            case MenuState.AdminEditContacts:
                // З розділів адміна повертаємось в головне меню адміна
                await HandleStartCommand(message);
                break;
                
            case MenuState.Appeals:
            case MenuState.Help:
            case MenuState.Info:
            case MenuState.Dormitory:
                // З розділів студента повертаємось в головне меню студента
                await HandleStartCommand(message);
                break;
                
            default:
                // За замовчуванням - головне меню
                await HandleStartCommand(message);
                break;
        }
    }

    private async Task ShowUpdatedAppealHistory(Message message, int appealId)
    {
        if (message.From == null)
            return;

        var userState = GetUserState(message.From.Id);
        var appeal = _appealService.GetAppealById(appealId);

        if (appeal == null)
            return;

        // Формуємо історію звернення
        var historyText = $"📩 Звернення #{appeal.Id}\n" +
                         $"Статус: {GetStatusText(appeal.Status)}\n" +
                         $"Створено: {appeal.CreatedAt:dd.MM.yyyy HH:mm}\n\n" +
                         $"━━━━━━━━━━━━━━━━━━━━\n\n";

        // Отримуємо всі повідомлення звернення
        var messages = _appealService.GetAllAppealMessages(appeal.Id).ToList();
        Message? lastSentMessage = null;

        // Спочатку відправляємо всі медіа-повідомлення окремо
        var mediaMessages = messages.Where(m => !string.IsNullOrEmpty(m.PhotoFileId) || !string.IsNullOrEmpty(m.DocumentFileId)).ToList();
        
        foreach (var msg in mediaMessages)
        {
            var sender = msg.IsFromAdmin ? "👤 Адміністратор" : "👨‍🎓 Ви";
            var timeStamp = msg.SentAt.ToString("dd.MM HH:mm");
            var messageHeader = $"{sender} ({timeStamp}):";
            
            try
            {
                // Якщо є фото
                if (!string.IsNullOrEmpty(msg.PhotoFileId))
                {
                    var caption = msg.Text == "[Фото]" 
                        ? messageHeader 
                        : $"{messageHeader}\n{msg.Text}";
                        
                    lastSentMessage = await _botClient.SendPhotoAsync(
                        chatId: message.Chat.Id,
                        photo: InputFile.FromFileId(msg.PhotoFileId),
                        caption: caption
                    );
                }
                // Якщо є документ
                else if (!string.IsNullOrEmpty(msg.DocumentFileId))
                {
                    var caption = msg.Text.StartsWith("[Файл:") 
                        ? messageHeader 
                        : $"{messageHeader}\n{msg.Text}";
                        
                    lastSentMessage = await _botClient.SendDocumentAsync(
                        chatId: message.Chat.Id,
                        document: InputFile.FromFileId(msg.DocumentFileId),
                        caption: caption
                    );
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Помилка при відправці медіа: {ex.Message}");
                // Якщо не вдалося відправити медіа, додаємо як текст
                historyText += $"📎 {messageHeader}\n{msg.Text}\n\n";
            }
        }

        // Тепер додаємо всі текстові повідомлення до однієї історії
        var textMessages = messages.Where(m => string.IsNullOrEmpty(m.PhotoFileId) && string.IsNullOrEmpty(m.DocumentFileId)).ToList();
        
        foreach (var msg in textMessages)
        {
            var sender = msg.IsFromAdmin ? "👤 Адміністратор" : "👨‍🎓 Ви";
            var timeStamp = msg.SentAt.ToString("dd.MM HH:mm");
            
            historyText += $"{sender} ({timeStamp}):\n{msg.Text}\n\n";
        }

        historyText += "━━━━━━━━━━━━━━━━━━━━\n\n" ;

        // Перевіряємо довжину повідомлення (ліміт Telegram - 4096 символів)
        if (historyText.Length > 4096)
        {
            // Якщо повідомлення занадто довге, відправляємо частинами
            var chunks = SplitMessage(historyText, 4000); // Залишаємо запас
            foreach (var chunk in chunks)
            {
                try
                {
                    lastSentMessage = await _botClient.SendTextMessageAsync(
                        chatId: message.Chat.Id,
                        text: chunk
                    );
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Помилка при відправці історії: {ex.Message}");
                }
            }
        }
        else
        {
            // Відправляємо всю історію одним повідомленням
            try
            {
                lastSentMessage = await _botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: historyText
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Помилка при відправці історії: {ex.Message}");
            }
        }

        // Оновлюємо ID останнього повідомлення з історією для Reply
        if (lastSentMessage != null)
            userState.AppealHistoryMessageId = lastSentMessage.MessageId;
    }

    private string GetStatusText(AppealStatus status)
    {
        return status switch
        {
            AppealStatus.New => "🆕 Нове",
            AppealStatus.AdminReplied => "💬 Є відповідь адміністратора",
            AppealStatus.StudentReplied => "� Є відповідь студента",
            AppealStatus.ClosedByAdmin => "🔒 Закрито адміністратором",
            AppealStatus.ClosedByStudent => "🔒 Закрито студентом",
            _ => "❓ Невідомо"
        };
    }

    private List<string> SplitMessage(string text, int maxLength)
    {
        var result = new List<string>();
        
        if (text.Length <= maxLength)
        {
            result.Add(text);
            return result;
        }

        var lines = text.Split('\n');
        var currentChunk = new System.Text.StringBuilder();

        foreach (var line in lines)
        {
            // Якщо одна лінія більше ліміту - розбиваємо її
            if (line.Length > maxLength)
            {
                if (currentChunk.Length > 0)
                {
                    result.Add(currentChunk.ToString());
                    currentChunk.Clear();
                }

                // Розбиваємо довгу лінію на частини
                for (int i = 0; i < line.Length; i += maxLength)
                {
                    int length = Math.Min(maxLength, line.Length - i);
                    result.Add(line.Substring(i, length));
                }
            }
            else
            {
                // Якщо додавання лінії перевищить ліміт - зберігаємо поточний chunk
                if (currentChunk.Length + line.Length + 1 > maxLength)
                {
                    result.Add(currentChunk.ToString());
                    currentChunk.Clear();
                }

                if (currentChunk.Length > 0)
                    currentChunk.Append('\n');
                currentChunk.Append(line);
            }
        }

        if (currentChunk.Length > 0)
            result.Add(currentChunk.ToString());

        return result;
    }

    private async Task ShowMainMenu(Message message)
    {
        await HandleStartCommand(message);
    }

    private async Task HandleAppealCommand(Message message)
    {
        if (message.From == null)
            return;

        var userId = message.From.Id;
        var userState = GetUserState(userId);
        
        // Перевіряємо чи користувач заблокований
        if (await _userService.IsBannedAsync(userId))
        {
            await _botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: "🚫 <b>Доступ до створення звернень заборонено</b>\n\n" +
                      "На жаль, вам заборонено створювати звернення через зловживання сервісом.\n\n" +
                      "Якщо ви вважаєте, що це помилка, зв'яжіться з адміністрацією профспілки іншими способами.",
                parseMode: ParseMode.Html
            );
            return;
        }
        
        // Перевіряємо чи немає активного звернення
        var activeAppeal = _appealService.GetActiveAppealForStudent(userId);
        if (activeAppeal != null)
        {
            await _botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: $"⚠️ У вас вже є активне звернення #{activeAppeal.Id}\n" +
                      $"Статус: {GetStatusText(activeAppeal.Status)}\n\n" +
                      $"Спочатку закрийте поточне звернення, щоб створити нове."
            );
            return;
        }
        
        if (userState.DialogState == DialogState.CreatingAppeal)
        {
            await _botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: "Ви вже в процесі створення звернення. Будь ласка, опишіть ваше питання або натисніть 'Скасувати'"
            );
            return;
        }
        
        userState.DialogState = DialogState.CreatingAppeal;
        
        var keyboard = new ReplyKeyboardMarkup(new[]
        {
            new[] { new KeyboardButton("Скасувати") }
        })
        {
            ResizeKeyboard = true
        };

        await _botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: "📝 Створення нового звернення\n\n" +
                  "Будь ласка, опишіть ваше звернення. Намагайтеся детально викласти суть проблеми або питання.",
            replyMarkup: keyboard
        );
    }

    private async Task HandleCancelCommand(Message message)
    {
        if (message.From == null)
            return;

        var userId = message.From.Id;
        var userState = GetUserState(userId);
        
        // Скидаємо стан і повертаємо до головного меню
        userState.DialogState = DialogState.None;
        userState.Data.Clear();
        userState.SelectedAppealId = null;
        userState.AppealHistoryMessageId = null;
        
        await HandleStartCommand(message);
    }

    private async Task HandlePublishNewsCommand(Message message)
    {
        if (message.Text == null)
            return;

        var text = message.Text;
        if (text.Length <= 9)
        {
            await _botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: " Використання: /publish Заголовок | Текст новини"
            );
            return;
        }

        var parts = text[9..].Split('|', 2);
        if (parts.Length != 2)
        {
            await _botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: " Неправильний формат. Використовуйте: /publish Заголовок | Текст новини"
            );
            return;
        }

        var title = parts[0].Trim();
        var content = parts[1].Trim();

        if (string.IsNullOrEmpty(title) || string.IsNullOrEmpty(content))
        {
            await _botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: " Заголовок та текст новини не можуть бути порожніми"
            );
            return;
        }

        var news = await _newsService.CreateAndBroadcastNewsAsync(title, content);
        
        await _botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: $" Новину успішно опубліковано!\n\n" +
                  $" <b>{news.Title}</b>\n\n" +
                  $"{news.Content}",
            parseMode: ParseMode.Html
        );
    }

    private async Task HandleCancelAppealCommand(Message message)
    {
        if (message.From == null)
            return;

        var userState = GetUserState(message.From.Id);
        var activeAppeal = _appealService.GetActiveAppealForStudent(message.From.Id);
        
        if (activeAppeal == null)
        {
            await _botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: "❌ У вас немає активного звернення."
            );
            return;
        }

        // Переходимо в режим введення причини закриття
        userState.DialogState = DialogState.ClosingAppeal;
        userState.Data["AppealToClose"] = activeAppeal.Id;

        var keyboard = new ReplyKeyboardMarkup(new[]
        {
            new[] { new KeyboardButton("Скасувати") }
        })
        {
            ResizeKeyboard = true
        };

        await _botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: $"🔒 Закриття звернення #{activeAppeal.Id}\n\n" +
                  "Будь ласка, вкажіть причину закриття звернення:\n" +
                  "(наприклад: 'Проблему вирішено', 'Знайшов відповідь сам', тощо)",
            replyMarkup: keyboard
        );
    }

    private async Task HandleWriteToAppealCommand(Message message)
    {
        if (message.From == null)
            return;

        // Перевіряємо чи користувач заблокований
        if (await _userService.IsBannedAsync(message.From.Id))
        {
            await _botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: "🚫 <b>Доступ до створення звернень заборонено</b>\n\n" +
                      "На жаль, вам заборонено створювати звернення через зловживання сервісом.\n\n" +
                      "Якщо ви вважаєте, що це помилка, зв'яжіться з адміністрацією профспілки іншими способами.",
                parseMode: ParseMode.Html
            );
            return;
        }

        var activeAppeal = _appealService.GetActiveAppealForStudent(message.From.Id);
        
        if (activeAppeal == null)
        {
            // Немає активного звернення - створюємо нове
            await HandleAppealCommand(message);
            return;
        }

        // Є активне звернення - переходимо в режим написання повідомлення
        var userState = GetUserState(message.From.Id);
        userState.DialogState = DialogState.WritingToAppeal;
        userState.Data["ActiveAppealId"] = activeAppeal.Id;

        var keyboard = new ReplyKeyboardMarkup(new[]
        {
            new[] { new KeyboardButton("Скасувати") }
        })
        {
            ResizeKeyboard = true
        };

        await _botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: $"📝 Звернення #{activeAppeal.Id}\n\n" +
                  "Напишіть ваше повідомлення для адміністратора:",
            replyMarkup: keyboard
        );
    }

    private async Task NotifyAdminsAboutClosedAppeal(Appeal appeal, Telegram.Bot.Types.User user)
    {
        var adminIds = await _userService.GetAdminIdsAsync();
        var userName = $"{user.FirstName} {user.LastName}".Trim();

        foreach (var adminId in adminIds)
        {
            try
            {
                await _botClient.SendTextMessageAsync(
                    chatId: adminId,
                    text: $"🔒 Звернення #{appeal.Id} закрито студентом\n\n" +
                          $"Студент: {userName}\n" +
                          $"Причина: {appeal.ClosedReason}\n" +
                          $"Закрито: {DateTime.UtcNow:dd.MM.yyyy HH:mm}"
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Помилка при повідомленні адміністратора {adminId}: {ex.Message}");
            }
        }
    }

    private UserState GetUserState(long userId)
    {
        if (!_userStates.ContainsKey(userId))
            _userStates[userId] = new UserState();
        return _userStates[userId];
    }

    private string GetAppealStatusText(AppealStatus status)
    {
        return status switch
        {
            AppealStatus.New => "🆕 Нове",
            AppealStatus.AdminReplied => "💬 Є відповідь адміністратора",
            AppealStatus.StudentReplied => "📝 Очікує відповіді",
            AppealStatus.ClosedByAdmin => "🔒 Закрито адміністратором",
            AppealStatus.ClosedByStudent => "🔒 Закрито",
            _ => "❓ Невідомо"
        };
    }

    /// <summary>
    /// Відправляє або редагує повідомлення залежно від контексту callback query.
    /// Це дозволяє не засмічувати чат - замість нових повідомлень редагуються існуючі.
    /// </summary>
    private async Task<int> SendOrEditMessageAsync(
        long chatId, 
        long userId, 
        string text, 
        InlineKeyboardMarkup? replyMarkup = null, 
        ParseMode parseMode = ParseMode.Html,
        int? messageId = null)
    {
        try
        {
            if (messageId.HasValue)
            {
                // Намагаємося відредагувати існуюче повідомлення
                await _botClient.EditMessageTextAsync(
                    chatId: chatId,
                    messageId: messageId.Value,
                    text: text,
                    parseMode: parseMode,
                    replyMarkup: replyMarkup
                );
                return messageId.Value;
            }
        }
        catch (Exception)
        {
            // Якщо не вдалося відредагувати (наприклад, повідомлення видалене) - відправимо нове
        }

        // Відправляємо нове повідомлення
        var message = await _botClient.SendTextMessageAsync(
            chatId: chatId,
            text: text,
            parseMode: parseMode,
            replyMarkup: replyMarkup
        );

        // Зберігаємо ID повідомлення для майбутніх редагувань
        var userState = GetUserState(userId);
        userState.LastViewedMessageId = message.MessageId;

        return message.MessageId;
    }

    private async Task HandleUnknownCommand(Message message)
    {
        if (message.From == null || message.Text == null)
            return;

        var userState = GetUserState(message.From.Id);

        // Якщо студент знаходиться в розділі Звернення і має активне звернення
        if (userState.CurrentMenu == MenuState.Appeals && !await _userService.IsAdminAsync(message.From.Id))
        {
            var activeAppeal = _appealService.GetActiveAppealForStudent(message.From.Id);
            
            if (activeAppeal != null && activeAppeal.Status != AppealStatus.ClosedByAdmin && activeAppeal.Status != AppealStatus.ClosedByStudent)
            {
                // Додаємо повідомлення до звернення
                var addedMessage = _appealService.AddMessage(
                    activeAppeal.Id, 
                    message.From.Id,
                    $"{message.From.FirstName} {message.From.LastName}".Trim(),
                    false, 
                    message.Text
                );

                if (addedMessage != null)
                {
                    await _botClient.SendTextMessageAsync(
                        chatId: message.Chat.Id,
                        text: $"✅ Ваше повідомлення додано до звернення #{activeAppeal.Id}"
                    );

                    // Повідомити адміністраторів про нове повідомлення
                    await NotifyAdminsAboutNewMessage(activeAppeal, message.From.Id, 
                        $"{message.From.FirstName} {message.From.LastName}".Trim(), message.Text);

                    // Оновлюємо історію - показуємо нову
                    await ShowUpdatedAppealHistory(message, activeAppeal.Id);
                }
                else
                {
                    await _botClient.SendTextMessageAsync(
                        chatId: message.Chat.Id,
                        text: "❌ Не вдалося додати повідомлення. Можливо звернення вже закрите."
                    );
                }
                return;
            }
        }

        // Для всіх інших випадків - показуємо стандартне повідомлення
        await _botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: "Вибачте, я не розумію цю команду. Використайте /help для перегляду доступних команд."
        );
    }

    // ========== МЕТОДИ ДЛЯ АДМІНІСТРАТОРА ==========

    /// <summary>
    /// Універсальний обробник команди "📩 Звернення" для студента і адміна
    /// </summary>
    private async Task HandleAppealCommandAsync(Message message)
    {
        if (message.From == null)
            return;

        // Якщо це команда /appeal_123 - адмін обирає звернення
        if (message.Text != null && message.Text.StartsWith("/appeal_"))
        {
            if (await _userService.IsAdminAsync(message.From.Id))
            {
                if (int.TryParse(message.Text.Substring(8), out int appealId))
                {
                    await HandleAdminViewAppealCommand(message, appealId);
                    return;
                }
            }
        }

        // Якщо адміністратор - показуємо адмінське меню звернень
        if (await _userService.IsAdminAsync(message.From.Id))
        {
            await HandleAdminAppealsMenuCommand(message);
        }
        else
        {
            // Студент - показуємо звичайне меню
            await HandleAppealsMenuCommand(message);
        }
    }

    /// <summary>
    /// Розділ "Звернення" для адміністратора
    /// </summary>
    private async Task HandleAdminAppealsMenuCommand(Message message)
    {
        if (message.From == null)
            return;

        var userState = GetUserState(message.From.Id);
        userState.CurrentMenu = MenuState.AdminAppeals;
        userState.DialogState = DialogState.None;

        var keyboard = new ReplyKeyboardMarkup(new[]
        {
            new[] { new KeyboardButton("✅ Активні звернення") },
            new[] { new KeyboardButton("❌ Закриті звернення") },
            new[] { new KeyboardButton("◀️ Назад") }
        })
        {
            ResizeKeyboard = true
        };

        await _botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: "📩 Розділ: Звернення\n\n" +
                  "Оберіть категорію звернень:",
            replyMarkup: keyboard
        );
    }

    /// <summary>
    /// Показати активні звернення
    /// </summary>
    private async Task HandleAdminActiveAppealsCommand(Message message)
    {
        if (message.From == null)
            return;

        var userState = GetUserState(message.From.Id);
        userState.AdminAppealsFilter = "Active";
        userState.CurrentPage = 0; // Скидаємо на першу сторінку

        await ShowAppealsPage(message, userState, isActive: true);
    }

    /// <summary>
    /// Показати сторінку зі зверненнями з пагінацією
    /// </summary>
    private async Task ShowAppealsPage(Message message, UserState userState, bool isActive)
    {
        var allAppeals = isActive 
            ? _appealService.GetActiveAppeals().ToList()
            : _appealService.GetClosedAppeals().ToList();

        if (!allAppeals.Any())
        {
            await _botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: isActive 
                    ? "ℹ️ Немає активних звернень"
                    : "ℹ️ Немає закритих звернень"
            );
            return;
        }

        // Розрахунок пагінації
        int totalPages = (int)Math.Ceiling(allAppeals.Count / (double)userState.PageSize);
        int currentPage = Math.Max(0, Math.Min(userState.CurrentPage, totalPages - 1));
        userState.CurrentPage = currentPage;

        var appealsOnPage = allAppeals
            .Skip(currentPage * userState.PageSize)
            .Take(userState.PageSize)
            .ToList();

        // Формуємо список звернень
        var appealsList = isActive 
            ? $"✅ Активні звернення (сторінка {currentPage + 1}/{totalPages}):\n\n"
            : $"🔒 Закриті звернення (сторінка {currentPage + 1}/{totalPages}):\n\n";
        
        foreach (var appeal in appealsOnPage)
        {
            if (isActive)
            {
                var hasUnread = _appealService.HasUnreadMessages(appeal.Id);
                var unreadCount = _appealService.GetUnreadMessagesCount(appeal.Id);
                var unreadIndicator = hasUnread ? $" 🔔 ({unreadCount} нових)" : "";
                
                appealsList += $"/appeal_{appeal.Id} - {appeal.StudentName}{unreadIndicator}\n" +
                              $"   Статус: {GetStatusText(appeal.Status)}\n" +
                              $"   Створено: {appeal.CreatedAt:dd.MM HH:mm}\n\n";
            }
            else
            {
                appealsList += $"/appeal_{appeal.Id} - {appeal.StudentName}\n" +
                              $"   Статус: {GetStatusText(appeal.Status)}\n" +
                              $"   Закрито: {appeal.ClosedAt:dd.MM HH:mm}\n\n";
            }
        }

        appealsList += $"💡 Натисніть на /appeal_XXX щоб переглянути звернення\n";
        appealsList += $"📊 Показано {appealsOnPage.Count} з {allAppeals.Count} звернень";

        await _botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: appealsList
        );

        // Кнопки навігації
        var buttons = new List<KeyboardButton[]>();
        
        // Кнопки пагінації
        var paginationButtons = new List<KeyboardButton>();
        
        if (currentPage > 0)
            paginationButtons.Add(new KeyboardButton("⬅️ Попередня"));
            
        if (currentPage < totalPages - 1)
            paginationButtons.Add(new KeyboardButton("Наступна ➡️"));
        
        if (paginationButtons.Count > 0)
            buttons.Add(paginationButtons.ToArray());

        // Кнопка назад
        buttons.Add(new[] { new KeyboardButton("◀️ Назад") });

        var keyboard = new ReplyKeyboardMarkup(buttons)
        {
            ResizeKeyboard = true
        };

        await _botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: "Навігація:",
            replyMarkup: keyboard
        );
    }

    /// <summary>
    /// Показати закриті звернення
    /// </summary>
    private async Task HandleAdminClosedAppealsCommand(Message message)
    {
        if (message.From == null)
            return;

        var userState = GetUserState(message.From.Id);
        userState.AdminAppealsFilter = "Closed";
        userState.CurrentPage = 0; // Скинути на першу сторінку

        await ShowAppealsPage(message, userState, isActive: false);
    }

    /// <summary>
    /// Попередня сторінка звернень
    /// </summary>
    private async Task HandlePreviousPageCommand(Message message)
    {
        if (message.From == null)
            return;

        var userState = GetUserState(message.From.Id);
        
        // Зменшуємо номер сторінки
        if (userState.CurrentPage > 0)
        {
            userState.CurrentPage--;
        }

        // Відображаємо звернення на новій сторінці
        bool isActive = userState.AdminAppealsFilter == "Active";
        await ShowAppealsPage(message, userState, isActive);
    }

    /// <summary>
    /// Наступна сторінка звернень
    /// </summary>
    private async Task HandleNextPageCommand(Message message)
    {
        if (message.From == null)
            return;

        var userState = GetUserState(message.From.Id);
        
        // Збільшуємо номер сторінки
        userState.CurrentPage++;

        // Відображаємо звернення на новій сторінці
        bool isActive = userState.AdminAppealsFilter == "Active";
        await ShowAppealsPage(message, userState, isActive);
    }

    /// <summary>
    /// Переглянути конкретне звернення (для адміна)
    /// </summary>
    private async Task HandleAdminViewAppealCommand(Message message, int appealId)
    {
        if (message.From == null)
            return;

        var userState = GetUserState(message.From.Id);
        userState.SelectedAppealId = appealId;

        var appeal = _appealService.GetAppealById(appealId);
        
        if (appeal == null)
        {
            await _botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: "❌ Звернення не знайдено"
            );
            return;
        }

        // Позначаємо всі повідомлення як прочитані
        _appealService.MarkMessagesAsReadByAdmin(appealId);

        // Формуємо історію звернення
        var historyText = $"📩 Звернення #{appeal.Id}\n" +
                         $"Студент: {appeal.StudentName}\n" +
                         $"Статус: {GetStatusText(appeal.Status)}\n" +
                         $"Створено: {appeal.CreatedAt:dd.MM.yyyy HH:mm}\n\n" +
                         $"━━━━━━━━━━━━━━━━━━━━\n\n";

        // Отримуємо всі повідомлення звернення
        var messages = _appealService.GetAllAppealMessages(appealId).ToList();
        Message? lastSentMessage = null;

        // Спочатку відправляємо всі медіа-повідомлення окремо
        var mediaMessages = messages.Where(m => !string.IsNullOrEmpty(m.PhotoFileId) || !string.IsNullOrEmpty(m.DocumentFileId)).ToList();
        
        foreach (var msg in mediaMessages)
        {
            var sender = msg.IsFromAdmin ? "👤 Адміністратор" : "👨‍🎓 Студент";
            var timeStamp = msg.SentAt.ToString("dd.MM HH:mm");
            var messageHeader = $"{sender} ({timeStamp}):";
            
            try
            {
                // Якщо є фото
                if (!string.IsNullOrEmpty(msg.PhotoFileId))
                {
                    var caption = msg.Text == "[Фото]" 
                        ? messageHeader 
                        : $"{messageHeader}\n{msg.Text}";
                        
                    lastSentMessage = await _botClient.SendPhotoAsync(
                        chatId: message.Chat.Id,
                        photo: InputFile.FromFileId(msg.PhotoFileId),
                        caption: caption
                    );
                }
                // Якщо є документ
                else if (!string.IsNullOrEmpty(msg.DocumentFileId))
                {
                    var caption = msg.Text.StartsWith("[Файл:") 
                        ? messageHeader 
                        : $"{messageHeader}\n{msg.Text}";
                        
                    lastSentMessage = await _botClient.SendDocumentAsync(
                        chatId: message.Chat.Id,
                        document: InputFile.FromFileId(msg.DocumentFileId),
                        caption: caption
                    );
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Помилка при відправці медіа: {ex.Message}");
                // Якщо не вдалося відправити медіа, додаємо як текст
                historyText += $"📎 {messageHeader}\n{msg.Text}\n\n";
            }
        }

        // Тепер додаємо всі текстові повідомлення до однієї історії
        var textMessages = messages.Where(m => string.IsNullOrEmpty(m.PhotoFileId) && string.IsNullOrEmpty(m.DocumentFileId)).ToList();
        
        foreach (var msg in textMessages)
        {
            var sender = msg.IsFromAdmin ? "👤 Адміністратор" : "👨‍🎓 Студент";
            var timeStamp = msg.SentAt.ToString("dd.MM HH:mm");
            
            historyText += $"{sender} ({timeStamp}):\n{msg.Text}\n\n";
        }

        historyText += "━━━━━━━━━━━━━━━━━━━━\n\n" +
                      "💡 Щоб відповісти, зробіть Reply на це повідомлення\nабо натисніть кнопку 'Відповісти'";

        // Перевіряємо довжину повідомлення (ліміт Telegram - 4096 символів)
        if (historyText.Length > 4096)
        {
            // Якщо повідомлення занадто довге, відправляємо частинами
            var chunks = SplitMessage(historyText, 4000);
            foreach (var chunk in chunks)
            {
                try
                {
                    lastSentMessage = await _botClient.SendTextMessageAsync(
                        chatId: message.Chat.Id,
                        text: chunk
                    );
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Помилка при відправці історії: {ex.Message}");
                }
            }
        }
        else
        {
            // Відправляємо всю історію одним повідомленням
            try
            {
                lastSentMessage = await _botClient.SendTextMessageAsync(
                    chatId: message.Chat.Id,
                    text: historyText
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Помилка при відправці історії: {ex.Message}");
            }
        }

        // Зберігаємо ID останнього повідомлення для Reply
        if (lastSentMessage != null)
            userState.AppealHistoryMessageId = lastSentMessage.MessageId;

        // Показуємо кнопки дій
        var keyboard = (appeal.Status == AppealStatus.ClosedByAdmin || appeal.Status == AppealStatus.ClosedByStudent) 
            ? new ReplyKeyboardMarkup(new[]
            {
                new[] { new KeyboardButton("👤 Дані студента") },
                new[] { new KeyboardButton("◀️ Назад") }
            })
            : new ReplyKeyboardMarkup(new[]
            {
                new[] { new KeyboardButton("✍️ Відповісти") },
                new[] { new KeyboardButton("👤 Дані студента"), new KeyboardButton("❌ Закрити звернення") },
                new[] { new KeyboardButton("◀️ Назад") }
            });

        keyboard.ResizeKeyboard = true;

        await _botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: "Оберіть дію:",
            replyMarkup: keyboard
        );
    }

    /// <summary>
    /// Відповісти на звернення (адмін натискає кнопку)
    /// </summary>
    private async Task HandleAdminReplyToAppealCommand(Message message)
    {
        if (message.From == null)
            return;

        var userState = GetUserState(message.From.Id);

        if (!userState.SelectedAppealId.HasValue)
        {
            await _botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: "❌ Спочатку оберіть звернення"
            );
            return;
        }

        userState.DialogState = DialogState.AdminReplyingToAppeal;

        var keyboard = new ReplyKeyboardMarkup(new[]
        {
            new[] { new KeyboardButton("Скасувати") }
        })
        {
            ResizeKeyboard = true
        };

        await _botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: $"✍️ Звернення #{userState.SelectedAppealId}\n\n" +
                  "Напишіть вашу відповідь студенту:",
            replyMarkup: keyboard
        );
    }

    /// <summary>
    /// Показати дані студента
    /// </summary>
    private async Task HandleAdminUserInfoCommand(Message message)
    {
        if (message.From == null)
            return;

        var userState = GetUserState(message.From.Id);

        if (!userState.SelectedAppealId.HasValue)
        {
            await _botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: "❌ Спочатку оберіть звернення"
            );
            return;
        }

        var appeal = _appealService.GetAppealById(userState.SelectedAppealId.Value);
        
        if (appeal == null)
        {
            await _botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: "❌ Звернення не знайдено"
            );
            return;
        }

        // Отримуємо повну інформацію про користувача
        var user = await _userService.GetUserByIdAsync(appeal.StudentId);
        
        // Формуємо правильне посилання на профіль
        string telegramLink;
        if (!string.IsNullOrEmpty(user?.Username))
        {
            telegramLink = $"@{user.Username}";
        }
        else
        {
            // Використовуємо mention link для користувачів без username
            telegramLink = $"<a href=\"tg://user?id={appeal.StudentId}\">{appeal.StudentName}</a>";
        }

        var userInfo = $"👤 <b>Дані студента</b>\n\n" +
                      $"<b>Telegram інформація:</b>\n" +
                      $"• ID: <code>{appeal.StudentId}</code>\n" +
                      $"• Ім'я: {appeal.StudentName}\n" +
                      $"• Username: {(string.IsNullOrEmpty(user?.Username) ? "не встановлено" : "@" + user.Username)}\n" +
                      $"• Профіль: {telegramLink}\n\n" +
                      $"<b>Особисті дані:</b>\n" +
                      $"• ПІБ: {user?.FullName ?? "не встановлено"}\n" +
                      $"• Факультет: {user?.Faculty ?? "не встановлено"}\n" +
                      $"• Курс: {(user?.Course.HasValue == true ? user.Course.ToString() : "не встановлено")}\n" +
                      $"• Група: {user?.Group ?? "не встановлено"}\n" +
                      $"• Ел.пошта: {user?.Email ?? "не встановлено"}\n" +
                      $"• Оновлено: {(user?.ProfileUpdatedAt.HasValue == true ? user.ProfileUpdatedAt.Value.ToString("dd.MM.yyyy HH:mm") : "не встановлено")}\n\n" +
                      $"<b>Статистика звернення:</b>\n" +
                      $"• Номер: #{appeal.Id}\n" +
                      $"• Статус: {GetStatusText(appeal.Status)}\n" +
                      $"• Повідомлень: {appeal.Messages?.Count ?? 0}";

        await _botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: userInfo,
            parseMode: Telegram.Bot.Types.Enums.ParseMode.Html
        );
    }

    /// <summary>
    /// Закрити звернення (адмін)
    /// </summary>
    private async Task HandleAdminCloseAppealCommand(Message message)
    {
        if (message.From == null)
            return;

        var userState = GetUserState(message.From.Id);

        if (!userState.SelectedAppealId.HasValue)
        {
            await _botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: "❌ Спочатку оберіть звернення"
            );
            return;
        }

        // Запитуємо причину закриття
        userState.DialogState = DialogState.AdminClosingAppeal;
        
        var keyboard = new ReplyKeyboardMarkup(new[]
        {
            new KeyboardButton[] { "Скасувати" }
        })
        {
            ResizeKeyboard = true
        };

        await _botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: $"🔒 Закриття звернення #{userState.SelectedAppealId.Value}\n\n" +
                  "Будь ласка, вкажіть причину закриття звернення:",
            replyMarkup: keyboard
        );
    }

    /// <summary>
    /// Розділ публікації оголошень
    /// </summary>
    private async Task HandleAdminPublishNewsMenuCommand(Message message)
    {
        if (message.From == null)
            return;

        var userState = GetUserState(message.From.Id);
        userState.CurrentMenu = MenuState.AdminPublishNews;
        userState.DialogState = DialogState.CreatingNews;

        var keyboard = new ReplyKeyboardMarkup(new[]
        {
            new[] { new KeyboardButton("◀️ Назад") }
        })
        {
            ResizeKeyboard = true
        };

        await _botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: "📢 Публікація оголошення\n\n" +
                  "Надішліть оголошення у форматі:\n" +
                  "Заголовок | Текст оголошення\n\n" +
                  "Можете додати фото або надіслати тільки текст.\n\n" +
                  "Приклад:\n" +
                  "Важливо! | Завтра о 10:00 загальні збори студентів",
            replyMarkup: keyboard
        );
    }

    /// <summary>
    /// Розділ статистики
    /// </summary>
    private async Task HandleAdminStatisticsCommand(Message message)
    {
        if (message.From == null)
            return;

        var userState = GetUserState(message.From.Id);
        userState.CurrentMenu = MenuState.AdminStatistics;

        // Отримуємо статистику
        var stats = await _userService.GetUserStatisticsAsync();
        var appealStats = await GetAppealStatisticsAsync();

        var statsText = "📊 <b>Статистика системи</b>\n\n" +
                       "👥 <b>Користувачі:</b>\n" +
                       $"├ Всього: {stats["total"]}\n" +
                       $"├ Активних: {stats["active"]}\n" +
                       $"└ З профілем: {stats["withProfile"]}\n\n" +
                       
                       "📚 <b>По курсах:</b>\n" +
                       $"├ 1 курс: {stats.GetValueOrDefault("course1", 0)}\n" +
                       $"├ 2 курс: {stats.GetValueOrDefault("course2", 0)}\n" +
                       $"├ 3 курс: {stats.GetValueOrDefault("course3", 0)}\n" +
                       $"├ 4 курс: {stats.GetValueOrDefault("course4", 0)}\n" +
                       $"├ 5 курс: {stats.GetValueOrDefault("course5", 0)}\n" +
                       $"└ 6 курс: {stats.GetValueOrDefault("course6", 0)}\n\n";

        // Додаємо факультети якщо є
        var facultyStats = stats.Where(s => s.Key.StartsWith("faculty_")).ToList();
        if (facultyStats.Any())
        {
            statsText += "🏛 <b>По факультетах:</b>\n";
            for (int i = 0; i < facultyStats.Count; i++)
            {
                var faculty = facultyStats[i];
                var facultyName = faculty.Key.Replace("faculty_", "");
                var prefix = i == facultyStats.Count - 1 ? "└" : "├";
                statsText += $"{prefix} {facultyName}: {faculty.Value}\n";
            }
            statsText += "\n";
        }

        statsText += "📩 <b>Звернення:</b>\n" +
                    $"├ Всього: {appealStats["total"]}\n" +
                    $"├ Активних: {appealStats["active"]}\n" +
                    $"├ Закритих: {appealStats["closed"]}\n" +
                    $"└ Повідомлень: {appealStats["messages"]}\n\n" +
                    
                    $"📅 <b>Дата формування:</b> {DateTime.Now:dd.MM.yyyy HH:mm}";

        var keyboard = new ReplyKeyboardMarkup(new[]
        {
            new[] { new KeyboardButton("Експорт користувачів") },
            new[] { new KeyboardButton("◀️ Назад") }
        })
        {
            ResizeKeyboard = true
        };

        await _botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: statsText,
            parseMode: ParseMode.Html,
            replyMarkup: keyboard
        );
    }

    /// <summary>
    /// Експорт користувачів у CSV
    /// </summary>
    private async Task HandleAdminExportUsersCommand(Message message)
    {
        if (message.From == null)
            return;

        await _botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: "⏳ Формування CSV файлу..."
        );

        try
        {
            var csv = await _userService.ExportUsersToCsvAsync();
            var fileName = $"users_export_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
            
            // Конвертуємо в байти
            var bytes = System.Text.Encoding.UTF8.GetBytes(csv);
            
            using (var stream = new MemoryStream(bytes))
            {
                await _botClient.SendDocumentAsync(
                    chatId: message.Chat.Id,
                    document: InputFile.FromStream(stream, fileName),
                    caption: $"Експорт користувачів\n\n" +
                            $"Всього записів: {csv.Split('\n').Length - 2}\n" +
                            $"Дата: {DateTime.Now:dd.MM.yyyy HH:mm}"
                );
            }

            await _botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: "✅ Файл успішно сформовано!"
            );
        }
        catch (Exception ex)
        {
            await _botClient.SendTextMessageAsync(
                chatId: message.Chat.Id,
                text: $"❌ Помилка при формуванні файлу: {ex.Message}"
            );
        }
    }

    /// <summary>
    /// Отримати статистику звернень
    /// </summary>
    private async Task<Dictionary<string, int>> GetAppealStatisticsAsync()
    {
        var totalAppeals = await _appealService.GetAllAppeals().CountAsync();
        var activeAppeals = _appealService.GetActiveAppeals().Count();
        var closedAppeals = _appealService.GetClosedAppeals().Count();
        
        // Підрахунок повідомлень
        var totalMessages = 0;
        var allAppeals = await _appealService.GetAllAppeals().ToListAsync();
        foreach (var appeal in allAppeals)
        {
            totalMessages += _appealService.GetAllAppealMessages(appeal.Id).Count();
        }

        return new Dictionary<string, int>
        {
            ["total"] = totalAppeals,
            ["active"] = activeAppeals,
            ["closed"] = closedAppeals,
            ["messages"] = totalMessages
        };
    }

    /// <summary>
    /// Редагування контактної інформації
    /// </summary>
    private async Task HandleAdminEditContactsCommand(Message message)
    {
        if (message.From == null)
            return;

        var userState = GetUserState(message.From.Id);
        userState.CurrentMenu = MenuState.AdminEditContacts;
        userState.DialogState = DialogState.EditingContactInfo;

        // Отримуємо поточну контактну інформацію
        var contactInfo = await _context.ContactInfo.FirstOrDefaultAsync();

        var currentInfo = "";
        if (contactInfo != null && !string.IsNullOrWhiteSpace(contactInfo.Content))
        {
            currentInfo = $"\n\n<b>Поточна інформація:</b>\n{contactInfo.Content}\n";
            if (contactInfo.UpdatedAt != default)
            {
                currentInfo += $"\n<i>Оновлено: {contactInfo.UpdatedAt:dd.MM.yyyy HH:mm}</i>";
            }
        }

        var keyboard = new ReplyKeyboardMarkup(new[]
        {
            new[] { new KeyboardButton("◀️ Назад") }
        })
        {
            ResizeKeyboard = true
        };

        await _botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: "📝 <b>Редагування контактної інформації</b>\n\n" +
                  "Надішліть текст, який буде відображатись студентам у розділі \"ℹ️ Інформація\".\n\n" +
                  "Ви можете використовувати HTML форматування:\n" +
                  "• <code>&lt;b&gt;жирний&lt;/b&gt;</code>\n" +
                  "• <code>&lt;i&gt;курсив&lt;/i&gt;</code>\n" +
                  "• <code>&lt;u&gt;підкреслений&lt;/u&gt;</code>\n" +
                  "• <code>&lt;a href=\"url\"&gt;посилання&lt;/a&gt;</code>\n\n" +
                  "Приклад:\n" +
                  "<code>🏛️ Профспілка студентів\n\n" +
                  "📞 Телефон: +38 (XXX) XXX-XX-XX\n" +
                  "📧 Email: student@university.edu\n" +
                  "📍 Адреса: вул. Університетська, 1, к. 101\n\n" +
                  "⏰ Графік роботи:\n" +
                  "Пн-Пт: 9:00-18:00\n" +
                  "Сб-Нд: вихідний</code>" +
                  currentInfo,
            parseMode: ParseMode.Html,
            replyMarkup: keyboard
        );
    }

    private async Task HandleAdminEditPartnersCommand(Message message)
    {
        if (message.From == null)
            return;

        var userState = GetUserState(message.From.Id);
        userState.CurrentMenu = MenuState.Main;
        userState.DialogState = DialogState.EditingPartnersInfo;

        // Отримуємо поточну інформацію про партнерів
        var partnersInfo = await _context.PartnersInfo.FirstOrDefaultAsync();

        var currentInfo = "";
        if (partnersInfo != null && !string.IsNullOrWhiteSpace(partnersInfo.Content))
        {
            currentInfo = $"\n\n<b>Поточна інформація:</b>\n{partnersInfo.Content}\n";
            if (partnersInfo.UpdatedAt != default)
            {
                currentInfo += $"\n<i>Оновлено: {partnersInfo.UpdatedAt:dd.MM.yyyy HH:mm}</i>";
            }
        }

        var keyboard = new ReplyKeyboardMarkup(new[]
        {
            new[] { new KeyboardButton("◀️ Назад") }
        })
        {
            ResizeKeyboard = true
        };

        await _botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: "🤝 <b>Редагування інформації про партнерів</b>\n\n" +
                  "Надішліть текст, який буде відображатись студентам у розділі \"Партнери\".\n\n" +
                  "Ви можете використовувати HTML форматування:\n" +
                  "• <code>&lt;b&gt;жирний&lt;/b&gt;</code>\n" +
                  "• <code>&lt;i&gt;курсив&lt;/i&gt;</code>\n" +
                  "• <code>&lt;u&gt;підкреслений&lt;/u&gt;</code>\n" +
                  "• <code>&lt;a href=\"url\"&gt;посилання&lt;/a&gt;</code>\n\n" +
                  "Приклад:\n" +
                  "<code>🤝 Наші партнери:\n\n" +
                  "🏪 Магазин \"Книгарня\" - знижка 10%\n" +
                  "🍕 Піцерія \"Смачно\" - знижка 15%\n" +
                  "🏋️ Спортзал \"Енергія\" - знижка 20%\n\n" +
                  "Для отримання знижки пред'явіть студентський квиток!</code>" +
                  currentInfo,
            parseMode: ParseMode.Html,
            replyMarkup: keyboard
        );
    }

    private async Task HandleAdminEditEventsCommand(Message message)
    {
        if (message.From == null)
            return;

        var userState = GetUserState(message.From.Id);
        userState.CurrentMenu = MenuState.Main;
        userState.DialogState = DialogState.EditingEventsInfo;

        // Отримуємо поточну інформацію про заходи
        var eventsInfo = await _context.EventsInfo.FirstOrDefaultAsync();

        var currentInfo = "";
        if (eventsInfo != null && !string.IsNullOrWhiteSpace(eventsInfo.Content))
        {
            currentInfo = $"\n\n<b>Поточна інформація:</b>\n{eventsInfo.Content}\n";
            if (eventsInfo.UpdatedAt != default)
            {
                currentInfo += $"\n<i>Оновлено: {eventsInfo.UpdatedAt:dd.MM.yyyy HH:mm}</i>";
            }
        }

        var keyboard = new ReplyKeyboardMarkup(new[]
        {
            new[] { new KeyboardButton("◀️ Назад") }
        })
        {
            ResizeKeyboard = true
        };

        await _botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: "🎉 <b>Редагування інформації про заходи</b>\n\n" +
                  "Надішліть текст, який буде відображатись студентам у розділі \"Заходи\".\n\n" +
                  "Ви можете використовувати HTML форматування:\n" +
                  "• <code>&lt;b&gt;жирний&lt;/b&gt;</code>\n" +
                  "• <code>&lt;i&gt;курсив&lt;/i&gt;</code>\n" +
                  "• <code>&lt;u&gt;підкреслений&lt;/u&gt;</code>\n" +
                  "• <code>&lt;a href=\"url\"&gt;посилання&lt;/a&gt;</code>\n\n" +
                  "Приклад:\n" +
                  "<code>🎉 Найближчі заходи:\n\n" +
                  "📅 15.10.2025 - День студента\n" +
                  "🎓 20.10.2025 - Церемонія посвяти першокурсників\n" +
                  "🎪 25.10.2025 - Студентський фестиваль\n\n" +
                  "Детальна інформація буде в оголошеннях!</code>" +
                  currentInfo,
            parseMode: ParseMode.Html,
            replyMarkup: keyboard
        );
    }

    // ============================================
    // ОБРОБКА INLINE-КНОПОК (CALLBACK QUERY)
    // ============================================

    private async Task HandleCallbackQueryAsync(CallbackQuery callbackQuery)
    {
        if (callbackQuery.Message == null || callbackQuery.From == null)
            return;

        var chatId = callbackQuery.Message.Chat.Id;
        var userId = callbackQuery.From.Id;
        var messageId = callbackQuery.Message.MessageId;
        var data = callbackQuery.Data ?? string.Empty;

        try
        {
            // Відповідаємо на callback query щоб прибрати годинник очікування
            await _botClient.AnswerCallbackQueryAsync(callbackQuery.Id);

            // Парсимо команду з callback data
            var parts = data.Split(':');
            var command = parts[0];

            switch (command)
            {
                case "menu_main":
                    await ShowMainMenuInline(chatId, userId, messageId);
                    break;

                case "menu_appeals":
                    await HandleAppealsMenuInline(chatId, userId, messageId);
                    break;

                case "menu_help":
                    await HandleHelpCommandInline(chatId, userId, messageId);
                    break;

                case "menu_info":
                    await HandleInfoCommandInline(chatId, userId, messageId);
                    break;

                case "menu_dormitory":
                    await HandleDormitoryCommandInline(chatId, userId, messageId);
                    break;

                case "menu_opportunities":
                    await HandleOpportunitiesCommandInline(chatId, userId, messageId);
                    break;

                case "menu_partners":
                    await HandlePartnersCommandInline(chatId, userId, messageId);
                    break;

                case "menu_events":
                    await HandleEventsCommandInline(chatId, userId, messageId);
                    break;

                case "menu_suggest_event":
                    await HandleSuggestEventCommandInline(chatId, userId, messageId);
                    break;

                // Адмін меню
                case "admin_appeals":
                    await HandleAdminAppealsMenuInline(chatId, userId, messageId);
                    break;

                case "admin_publish_news":
                    await HandleAdminPublishNewsMenuInline(chatId, userId);
                    break;

                case "admin_statistics":
                    await HandleAdminStatisticsInline(chatId, userId);
                    break;

                case "admin_edit_contacts":
                    await HandleAdminEditContactsInline(chatId, userId);
                    break;

                case "admin_edit_partners":
                    await HandleAdminEditPartnersInline(chatId, userId);
                    break;

                case "admin_edit_events":
                    await HandleAdminEditEventsInline(chatId, userId);
                    break;

                // Обробка звернень (студенти)
                case var s when s.StartsWith("appeal_view_"):
                    var viewAppealId = int.Parse(data.Replace("appeal_view_", ""));
                    await HandleAppealViewInline(chatId, userId, viewAppealId);
                    break;

                case var s when s.StartsWith("appeal_write_"):
                    var writeAppealId = int.Parse(data.Replace("appeal_write_", ""));
                    await HandleAppealWriteInline(chatId, userId, writeAppealId);
                    break;

                case var s when s.StartsWith("appeal_close_"):
                    var closeAppealId = int.Parse(data.Replace("appeal_close_", ""));
                    await HandleAppealCloseInline(chatId, userId, closeAppealId);
                    break;

                case "appeal_create":
                    await HandleAppealCreateInline(chatId, userId);
                    break;

                // Обробка звернень (адміністратори)
                case "admin_appeals_active":
                    await HandleAdminActiveAppealsInline(chatId, userId);
                    break;

                case "admin_appeals_closed":
                    await HandleAdminClosedAppealsInline(chatId, userId);
                    break;

                default:
                    Console.WriteLine($"Unknown callback command: {data}");
                    break;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error handling callback query: {ex}");
            await _botClient.SendTextMessageAsync(chatId, "❌ Сталася помилка. Спробуйте ще раз або напишіть /start");
        }
    }

    private async Task ShowMainMenuInline(long chatId, long userId, int? messageId = null)
    {
        // Перевіряємо чи це адміністратор
        if (await _userService.IsAdminAsync(userId))
        {
            var adminKeyboard = new InlineKeyboardMarkup(new[]
            {
                new[] { InlineKeyboardButton.WithCallbackData("📩 Звернення", "admin_appeals") },
                new[] { InlineKeyboardButton.WithCallbackData("📢 Опублікувати оголошення", "admin_publish_news") },
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("📊 Статистика", "admin_statistics"),
                    InlineKeyboardButton.WithCallbackData("📝 Контакти", "admin_edit_contacts")
                },
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("🤝 Партнери", "admin_edit_partners"),
                    InlineKeyboardButton.WithCallbackData("🎉 Заходи", "admin_edit_events")
                }
            });

            await SendOrEditMessageAsync(
                chatId: chatId,
                userId: userId,
                text: "👤 <b>Адміністративна панель</b>\n\n" +
                      "Виберіть розділ для продовження:",
                replyMarkup: adminKeyboard,
                parseMode: ParseMode.Html,
                messageId: messageId
            );
            return;
        }

        // Меню студента
        var keyboard = new InlineKeyboardMarkup(new[]
        {
            new[] { InlineKeyboardButton.WithCallbackData("📩 Звернення", "menu_appeals") },
            new[] { InlineKeyboardButton.WithCallbackData("🏠 Гуртожиток", "menu_dormitory") },
            new[] { InlineKeyboardButton.WithCallbackData("🌟 Можливості", "menu_opportunities") },
            new[]
            {
                InlineKeyboardButton.WithCallbackData("❓ Допомога", "menu_help"),
                InlineKeyboardButton.WithCallbackData("ℹ️ Інформація", "menu_info")
            }
        });

        await SendOrEditMessageAsync(
            chatId: chatId,
            userId: userId,
            text: "📚 <b>Головне меню</b>\n\n" +
                  "Виберіть розділ для продовження:",
            replyMarkup: keyboard,
            parseMode: ParseMode.Html,
            messageId: messageId
        );
    }

    // Заглушки для нових методів inline - реалізуємо поступово
    private async Task HandleAppealsMenuInline(long chatId, long userId, int? messageId = null)
    {
        var activeAppeal = _appealService.GetActiveAppealForStudent(userId);
        
        if (activeAppeal != null)
        {
            // У користувача є активне звернення
            var keyboard = new InlineKeyboardMarkup(new[]
            {
                new[] { InlineKeyboardButton.WithCallbackData("💬 Переглянути звернення", $"appeal_view_{activeAppeal.Id}") },
                new[] { InlineKeyboardButton.WithCallbackData("✍️ Написати повідомлення", $"appeal_write_{activeAppeal.Id}") },
                new[] { InlineKeyboardButton.WithCallbackData("🔒 Закрити звернення", $"appeal_close_{activeAppeal.Id}") },
                new[] { InlineKeyboardButton.WithCallbackData("◀️ Назад до меню", "menu_main") }
            });

            await _botClient.SendTextMessageAsync(
                chatId: chatId,
                text: $"� <b>Ваше активне звернення #{activeAppeal.Id}</b>\n\n" +
                      $"📅 Створено: {activeAppeal.CreatedAt:dd.MM.yyyy HH:mm}\n" +
                      $"📊 Статус: {GetAppealStatusText(activeAppeal.Status)}\n\n" +
                      $"Оберіть дію:",
                parseMode: ParseMode.Html,
                replyMarkup: keyboard
            );
        }
        else
        {
            // Немає активного звернення
            var keyboard = new InlineKeyboardMarkup(new[]
            {
                new[] { InlineKeyboardButton.WithCallbackData("✍️ Написати адміністратору", "appeal_create") },
                new[] { InlineKeyboardButton.WithCallbackData("◀️ Назад до меню", "menu_main") }
            });

            await _botClient.SendTextMessageAsync(
                chatId: chatId,
                text: "📩 <b>Розділ: Звернення</b>\n\n" +
                      "У вас немає активних звернень.\n" +
                      "Ви можете створити нове звернення до адміністратора.",
                parseMode: ParseMode.Html,
                replyMarkup: keyboard
            );
        }
    }

    private async Task HandleHelpCommandInline(long chatId, long userId, int? messageId = null)
    {
        var helpText = "❓ <b>Розділ: Допомога</b>\n\n" +
                      "🤖 <b>Про бота:</b>\n" +
                      "Це офіційний бот Профкому студентів, який допомагає студентам звертатися до профкому та отримувати актуальні оголошення.\n\n" +
                      
                      "📋 <b>Розділи бота:</b>\n\n" +
                      
                      "📩 <b>Звернення</b>\n" +
                      "Створюйте звернення до профкому, отримуйте відповіді та відстежуйте їх статус.\n\n" +
                      
                      "❓ <b>Допомога</b>\n" +
                      "Інформація про роботу з ботом та його можливості.\n\n" +
                      
                      "ℹ️ <b>Інформація</b>\n" +
                      "Контактна інформація профкому студентів.\n\n" +
                      
                      "📖 <b>Як створити звернення:</b>\n" +
                      "1️⃣ Перейдіть в розділ 'Звернення'\n" +
                      "2️⃣ Натисніть 'Написати адміністратору'\n" +
                      "3️⃣ Опишіть ваше питання або проблему\n" +
                      "4️⃣ Можете додати фото або документ\n" +
                      "5️⃣ Підтвердіть відправку\n\n" +
                      
                      "📬 <b>Як отримати відповідь:</b>\n" +
                      "Переглянете всю історію спілкування та можете написати додаткове повідомлення\n\n" +
                      
                      "✅ <b>Закриття звернення:</b>\n" +
                      "Коли питання вирішено, натисніть 'Закрити звернення' та вкажіть причину закриття.\n\n" +
                      
                      "💡 <b>Корисні команди:</b>\n" +
                      "• /start - Головне меню\n" +
                      "• /cancel - Скасувати поточну дію";

        if (await _userService.IsAdminAsync(userId))
        {
            helpText += "\n\n� <b>Адміністративні функції:</b>\n\n" +
                       "📩 <b>Звернення</b>\n" +
                       "• Перегляд активних та закритих звернень\n" +
                       "• Відповіді студентам з можливістю медіа\n" +
                       "• Перегляд даних студента\n" +
                       "• Закриття звернень з вказанням причини\n\n" +
                       
                       "📢 <b>Опублікувати оголошення</b>\n" +
                       "• Створення оголошень з фото або текстом\n" +
                       "• Вибіркова розсилка за курсами/факультетами\n" +
                       "• Передогляд перед публікацією\n\n" +
                       
                       "📊 <b>Статистика</b>\n" +
                       "• Загальна статистика користувачів\n" +
                       "• Розподіл по курсах та факультетах\n" +
                       "• Статистика звернень\n" +
                       "• Експорт користувачів у CSV";
        }

        var keyboard = new InlineKeyboardMarkup(new[]
        {
            new[] { InlineKeyboardButton.WithCallbackData("◀️ Назад до меню", "menu_main") }
        });

        await SendOrEditMessageAsync(chatId, userId, helpText, keyboard, ParseMode.Html, messageId);
    }

    private async Task HandleInfoCommandInline(long chatId, long userId, int? messageId = null)
    {
        // Отримуємо контактну інформацію з БД
        var contactInfo = await _context.ContactInfo.FirstOrDefaultAsync();
        
        string infoText;
        if (contactInfo != null && !string.IsNullOrWhiteSpace(contactInfo.Content))
        {
            infoText = $"ℹ️ <b>{contactInfo.Title}</b>\n\n{contactInfo.Content}";
            
            if (contactInfo.UpdatedAt != default)
            {
                infoText += $"\n\n<i>Оновлено: {contactInfo.UpdatedAt:dd.MM.yyyy HH:mm}</i>";
            }
        }
        else
        {
            infoText = "ℹ️ <b>Контактна інформація</b>\n\n" +
                      "🏛️ Профспілка студентів\n\n" +
                      "Контактна інформація ще не налаштована.\n" +
                      "Зверніться до адміністратора.";
        }

        var keyboard = new InlineKeyboardMarkup(new[]
        {
            new[] { InlineKeyboardButton.WithCallbackData("◀️ Назад до меню", "menu_main") }
        });

        await SendOrEditMessageAsync(chatId, userId, infoText, keyboard, ParseMode.Html, messageId);
    }

    private async Task HandleDormitoryCommandInline(long chatId, long userId, int? messageId = null)
    {
        var dormitoryText = "🏠 <b>Розділ: Гуртожиток</b>\n\n" +
                           "З питань щодо проживання та поселення в гуртожитки ви можете звернутись за допомогою електронної пошти:\n\n" +
                           "📧 hostel@vnmu.edu.ua";

        var keyboard = new InlineKeyboardMarkup(new[]
        {
            new[] { InlineKeyboardButton.WithCallbackData("◀️ Назад до меню", "menu_main") }
        });

        await SendOrEditMessageAsync(chatId, userId, dormitoryText, keyboard, ParseMode.Html, messageId);
    }

    private async Task HandleOpportunitiesCommandInline(long chatId, long userId, int? messageId = null)
    {
        var keyboard = new InlineKeyboardMarkup(new[]
        {
            new[] { InlineKeyboardButton.WithCallbackData("🤝 Партнери", "menu_partners") },
            new[] { InlineKeyboardButton.WithCallbackData("🎉 Заходи", "menu_events") },
            new[] { InlineKeyboardButton.WithCallbackData("💡 Запропонувати захід", "menu_suggest_event") },
            new[] { InlineKeyboardButton.WithCallbackData("◀️ Назад до меню", "menu_main") }
        });

        await _botClient.SendTextMessageAsync(
            chatId: chatId,
            text: "🌟 <b>Розділ: Можливості</b>\n\n" +
                  "Оберіть підрозділ:",
            parseMode: ParseMode.Html,
            replyMarkup: keyboard
        );
    }

    private async Task HandlePartnersCommandInline(long chatId, long userId, int? messageId = null)
    {
        // Отримуємо інформацію про партнерів з БД
        var partnersInfo = await _context.PartnersInfo.FirstOrDefaultAsync();
        
        string partnersText;
        if (partnersInfo != null && !string.IsNullOrWhiteSpace(partnersInfo.Content))
        {
            partnersText = $"🤝 <b>{partnersInfo.Title}</b>\n\n{partnersInfo.Content}";
            
            if (partnersInfo.UpdatedAt != default)
            {
                partnersText += $"\n\n<i>Оновлено: {partnersInfo.UpdatedAt:dd.MM.yyyy HH:mm}</i>";
            }
        }
        else
        {
            partnersText = "🤝 <b>Партнери</b>\n\n" +
                          "Профспілка студентів співпрацює з різними організаціями та компаніями, " +
                          "які надають знижки та спеціальні пропозиції для студентів.\n\n" +
                          "📋 Список партнерів та доступних знижок:\n\n" +
                          "Інформація оновлюється. Слідкуйте за оголошеннями!";
        }

        var keyboard = new InlineKeyboardMarkup(new[]
        {
            new[] { InlineKeyboardButton.WithCallbackData("◀️ Назад", "menu_opportunities") }
        });

        await SendOrEditMessageAsync(chatId, userId, partnersText, keyboard, ParseMode.Html, messageId);
    }

    private async Task HandleEventsCommandInline(long chatId, long userId, int? messageId = null)
    {
        // Отримуємо інформацію про заходи з БД
        var eventsInfo = await _context.EventsInfo.FirstOrDefaultAsync();
        
        string eventsText;
        if (eventsInfo != null && !string.IsNullOrWhiteSpace(eventsInfo.Content))
        {
            eventsText = $"🎉 <b>{eventsInfo.Title}</b>\n\n{eventsInfo.Content}";
            
            if (eventsInfo.UpdatedAt != default)
            {
                eventsText += $"\n\n<i>Оновлено: {eventsInfo.UpdatedAt:dd.MM.yyyy HH:mm}</i>";
            }
        }
        else
        {
            eventsText = "🎉 <b>Заходи</b>\n\n" +
                        "Тут ви можете переглянути інформацію про майбутні та поточні заходи, " +
                        "організовані профспілкою студентів.\n\n" +
                        "📅 Актуальні заходи:\n\n" +
                        "Інформація про заходи публікується через оголошення. " +
                        "Слідкуйте за новинами від бота!";
        }

        var keyboard = new InlineKeyboardMarkup(new[]
        {
            new[] { InlineKeyboardButton.WithCallbackData("◀️ Назад", "menu_opportunities") }
        });

        await SendOrEditMessageAsync(chatId, userId, eventsText, keyboard, ParseMode.Html, messageId);
    }

    private async Task HandleSuggestEventCommandInline(long chatId, long userId, int? messageId = null)
    {
        var suggestText = "💡 <b>Запропонувати захід</b>\n\n" +
                         "Маєте ідею для цікавого заходу? Хочете організувати щось разом з профспілкою?\n\n" +
                         "📝 Заповніть форму для подачі пропозиції заходу:\n" +
                         "🔗 https://forms.gle/14ZGAxv15zgyhUHg7\n\n" +
                         "Ми розглянемо вашу пропозицію та зв'яжемося з вами!\n\n" +
                         "💡 У формі вкажіть:\n" +
                         "• Назву заходу\n" +
                         "• Опис ідеї\n" +
                         "• Бажаний формат та термін проведення\n" +
                         "• Ваші контактні дані для зв'язку";

        var keyboard = new InlineKeyboardMarkup(new[]
        {
            new[] { InlineKeyboardButton.WithUrl("📝 Відкрити форму", "https://forms.gle/14ZGAxv15zgyhUHg7") },
            new[] { InlineKeyboardButton.WithCallbackData("◀️ Назад", "menu_opportunities") }
        });

        await SendOrEditMessageAsync(chatId, userId, suggestText, keyboard, ParseMode.Html, messageId);
    }

    private async Task HandleAdminAppealsMenuInline(long chatId, long userId, int? messageId = null)
    {
        var keyboard = new InlineKeyboardMarkup(new[]
        {
            new[] { InlineKeyboardButton.WithCallbackData("📬 Активні звернення", "admin_appeals_active") },
            new[] { InlineKeyboardButton.WithCallbackData("📁 Закриті звернення", "admin_appeals_closed") },
            new[] { InlineKeyboardButton.WithCallbackData("◀️ Назад до меню", "menu_main") }
        });

        await _botClient.SendTextMessageAsync(
            chatId: chatId,
            text: "� <b>Адміністрування звернень</b>\n\n" +
                  "Оберіть категорію звернень для перегляду:",
            parseMode: ParseMode.Html,
            replyMarkup: keyboard
        );
    }

    private async Task HandleAdminPublishNewsMenuInline(long chatId, long userId, int? messageId = null)
    {
        var keyboard = new InlineKeyboardMarkup(new[]
        {
            new[] { InlineKeyboardButton.WithCallbackData("◀️ Назад до меню", "menu_main") }
        });

        await _botClient.SendTextMessageAsync(
            chatId: chatId,
            text: "� <b>Опублікувати оголошення</b>\n\n" +
                  "Для публікації оголошення використовуйте команду:\n\n" +
                  "<code>/publish Заголовок | Текст оголошення</code>\n\n" +
                  "<b>Приклад:</b>\n" +
                  "<code>/publish Нове оголошення | Текст вашого оголошення</code>\n\n" +
                  "Оголошення буде розіслане всім активним користувачам бота.",
            parseMode: ParseMode.Html,
            replyMarkup: keyboard
        );
    }

    private async Task HandleAdminStatisticsInline(long chatId, long userId, int? messageId = null)
    {
        // Загальна статистика користувачів
        var totalUsers = await _context.Users.CountAsync();
        var totalActiveAppeals = await _context.Appeals.CountAsync(a => a.ClosedAt == null);
        var totalClosedAppeals = await _context.Appeals.CountAsync(a => a.ClosedAt != null);
        
        // Розподіл по курсах
        var usersByCourse = await _context.Users
            .Where(u => u.Course.HasValue)
            .GroupBy(u => u.Course)
            .Select(g => new { Course = g.Key, Count = g.Count() })
            .OrderBy(x => x.Course)
            .ToListAsync();

        // Розподіл по факультетах
        var usersByFaculty = await _context.Users
            .Where(u => !string.IsNullOrEmpty(u.Faculty))
            .GroupBy(u => u.Faculty)
            .Select(g => new { Faculty = g.Key, Count = g.Count() })
            .OrderByDescending(x => x.Count)
            .Take(5)
            .ToListAsync();

        var statsText = "📊 <b>Статистика бота</b>\n\n" +
                       $"👥 <b>Користувачі:</b> {totalUsers}\n" +
                       $"📬 <b>Активні звернення:</b> {totalActiveAppeals}\n" +
                       $"📁 <b>Закриті звернення:</b> {totalClosedAppeals}\n\n";

        if (usersByCourse.Any())
        {
            statsText += "📚 <b>Розподіл по курсах:</b>\n";
            foreach (var item in usersByCourse)
            {
                statsText += $"• {item.Course} курс: {item.Count}\n";
            }
            statsText += "\n";
        }

        if (usersByFaculty.Any())
        {
            statsText += "🏛 <b>Топ-5 факультетів:</b>\n";
            foreach (var item in usersByFaculty)
            {
                statsText += $"• {item.Faculty}: {item.Count}\n";
            }
        }

        var keyboard = new InlineKeyboardMarkup(new[]
        {
            new[] { InlineKeyboardButton.WithCallbackData("🔄 Оновити", "admin_stats") },
            new[] { InlineKeyboardButton.WithCallbackData("◀️ Назад до меню", "menu_main") }
        });

        await SendOrEditMessageAsync(chatId, userId, statsText, keyboard, ParseMode.Html, messageId);
    }

    private async Task HandleAdminEditContactsInline(long chatId, long userId, int? messageId = null)
    {
        var contactInfo = await _context.ContactInfo.FirstOrDefaultAsync();
        
        var infoText = "ℹ️ <b>Редагування контактної інформації</b>\n\n";
        
        if (contactInfo != null)
        {
            infoText += $"📝 <b>Поточний заголовок:</b>\n{contactInfo.Title}\n\n";
            infoText += $"📄 <b>Поточний текст:</b>\n{contactInfo.Content}\n\n";
            infoText += $"🕐 <b>Оновлено:</b> {contactInfo.UpdatedAt:dd.MM.yyyy HH:mm}\n\n";
        }
        else
        {
            infoText += "Контактна інформація ще не налаштована.\n\n";
        }

        infoText += "Для редагування використовуйте команду:\n" +
                   "<code>/setcontact Заголовок | Текст інформації</code>\n\n" +
                   "<b>Приклад:</b>\n" +
                   "<code>/setcontact Контакти | 📧 Email: example@vnmu.edu.ua</code>";

        var keyboard = new InlineKeyboardMarkup(new[]
        {
            new[] { InlineKeyboardButton.WithCallbackData("◀️ Назад до меню", "menu_main") }
        });

        await SendOrEditMessageAsync(chatId, userId, infoText, keyboard, ParseMode.Html, messageId);
    }

    private async Task HandleAdminEditPartnersInline(long chatId, long userId, int? messageId = null)
    {
        var partnersInfo = await _context.PartnersInfo.FirstOrDefaultAsync();
        
        var infoText = "🤝 <b>Редагування інформації про партнерів</b>\n\n";
        
        if (partnersInfo != null)
        {
            infoText += $"📝 <b>Поточний заголовок:</b>\n{partnersInfo.Title}\n\n";
            infoText += $"📄 <b>Поточний текст:</b>\n{partnersInfo.Content}\n\n";
            infoText += $"🕐 <b>Оновлено:</b> {partnersInfo.UpdatedAt:dd.MM.yyyy HH:mm}\n\n";
        }
        else
        {
            infoText += "Інформація про партнерів ще не налаштована.\n\n";
        }

        infoText += "Для редагування використовуйте команду:\n" +
                   "<code>/setpartners Заголовок | Текст інформації</code>\n\n" +
                   "<b>Приклад:</b>\n" +
                   "<code>/setpartners Наші партнери | 🏪 Магазин XYZ - знижка 10%</code>";

        var keyboard = new InlineKeyboardMarkup(new[]
        {
            new[] { InlineKeyboardButton.WithCallbackData("◀️ Назад до меню", "menu_main") }
        });

        await SendOrEditMessageAsync(chatId, userId, infoText, keyboard, ParseMode.Html, messageId);
    }

    private async Task HandleAdminEditEventsInline(long chatId, long userId, int? messageId = null)
    {
        var eventsInfo = await _context.EventsInfo.FirstOrDefaultAsync();
        
        var infoText = "🎉 <b>Редагування інформації про заходи</b>\n\n";
        
        if (eventsInfo != null)
        {
            infoText += $"📝 <b>Поточний заголовок:</b>\n{eventsInfo.Title}\n\n";
            infoText += $"📄 <b>Поточний текст:</b>\n{eventsInfo.Content}\n\n";
            infoText += $"🕐 <b>Оновлено:</b> {eventsInfo.UpdatedAt:dd.MM.yyyy HH:mm}\n\n";
        }
        else
        {
            infoText += "Інформація про заходи ще не налаштована.\n\n";
        }

        infoText += "Для редагування використовуйте команду:\n" +
                   "<code>/setevents Заголовок | Текст інформації</code>\n\n" +
                   "<b>Приклад:</b>\n" +
                   "<code>/setevents Майбутні заходи | 🎭 Концерт 15.03 о 18:00</code>";

        var keyboard = new InlineKeyboardMarkup(new[]
        {
            new[] { InlineKeyboardButton.WithCallbackData("◀️ Назад до меню", "menu_main") }
        });

        await SendOrEditMessageAsync(chatId, userId, infoText, keyboard, ParseMode.Html, messageId);
    }

    // Обробники звернень для inline-кнопок
    private async Task HandleAppealViewInline(long chatId, long userId, int appealId, int? messageId = null)
    {
        var appeal = await _context.Appeals
            .Include(a => a.Messages)
            .FirstOrDefaultAsync(a => a.Id == appealId && a.StudentId == userId);

        if (appeal == null)
        {
            await _botClient.SendTextMessageAsync(chatId, "❌ Звернення не знайдено або у вас немає до нього доступу.");
            return;
        }

        var messageText = $"📩 <b>Звернення #{appeal.Id}</b>\n" +
                         $"📅 Створено: {appeal.CreatedAt:dd.MM.yyyy HH:mm}\n" +
                         $"📊 Статус: {GetAppealStatusText(appeal.Status)}\n\n" +
                         $"💬 <b>Історія спілкування:</b>\n\n";

        foreach (var msg in appeal.Messages.OrderBy(m => m.SentAt))
        {
            var senderName = msg.IsFromAdmin ? "Адміністратор" : "Ви";
            var emoji = msg.IsFromAdmin ? "👨‍�" : "�";
            messageText += $"{emoji} <b>{senderName}</b> ({msg.SentAt:dd.MM HH:mm}):\n{msg.Text}\n\n";
        }

        var keyboard = new InlineKeyboardMarkup(new[]
        {
            new[] { InlineKeyboardButton.WithCallbackData("✍️ Написати повідомлення", $"appeal_write_{appealId}") },
            new[] { InlineKeyboardButton.WithCallbackData("🔒 Закрити звернення", $"appeal_close_{appealId}") },
            new[] { InlineKeyboardButton.WithCallbackData("◀️ Назад", "menu_appeals") }
        });

        await SendOrEditMessageAsync(chatId, userId, messageText, keyboard, ParseMode.Html, messageId);
    }

    private async Task HandleAppealWriteInline(long chatId, long userId, int appealId, int? messageId = null)
    {
        var appeal = await _context.Appeals.FirstOrDefaultAsync(a => a.Id == appealId && a.StudentId == userId);

        if (appeal == null)
        {
            await _botClient.SendTextMessageAsync(chatId, "❌ Звернення не знайдено або у вас немає до нього доступу.");
            return;
        }

        // Переводимо користувача в режим написання повідомлення
        var userState = GetUserState(userId);
        userState.DialogState = DialogState.WritingToAppeal;
        userState.Data["AppealId"] = appealId;

        await _botClient.SendTextMessageAsync(
            chatId: chatId,
            text: "✍️ <b>Додати повідомлення до звернення</b>\n\n" +
                  "Напишіть ваше повідомлення або надішліть фото/документ.\n\n" +
                  "Щоб скасувати, натисніть /cancel",
            parseMode: ParseMode.Html
        );
    }

    private async Task HandleAppealCloseInline(long chatId, long userId, int appealId, int? messageId = null)
    {
        var appeal = await _context.Appeals.FirstOrDefaultAsync(a => a.Id == appealId && a.StudentId == userId);

        if (appeal == null)
        {
            await _botClient.SendTextMessageAsync(chatId, "❌ Звернення не знайдено або у вас немає до нього доступу.");
            return;
        }

        // Переводимо користувача в режим закриття звернення
        var userState = GetUserState(userId);
        userState.DialogState = DialogState.ClosingAppeal;
        userState.Data["AppealToClose"] = appealId;

        await _botClient.SendTextMessageAsync(
            chatId: chatId,
            text: $"🔒 <b>Закриття звернення #{appealId}</b>\n\n" +
                  "Будь ласка, вкажіть причину закриття звернення:\n" +
                  "(наприклад: 'Проблему вирішено', 'Знайшов відповідь сам', тощо)\n\n" +
                  "Щоб скасувати, натисніть /cancel",
            parseMode: ParseMode.Html
        );
    }

    private async Task HandleAppealCreateInline(long chatId, long userId, int? messageId = null)
    {
        // Перевіряємо чи користувач заблокований
        if (await _userService.IsBannedAsync(userId))
        {
            await _botClient.SendTextMessageAsync(
                chatId: chatId,
                text: "🚫 <b>Доступ до створення звернень заборонено</b>\n\n" +
                      "На жаль, вам заборонено створювати звернення через зловживання сервісом.\n\n" +
                      "Якщо ви вважаєте, що це помилка, зв'яжіться з адміністрацією профспілки іншими способами.",
                parseMode: ParseMode.Html
            );
            return;
        }

        var activeAppeal = _appealService.GetActiveAppealForStudent(userId);
        if (activeAppeal != null)
        {
            await _botClient.SendTextMessageAsync(
                chatId: chatId,
                text: $"❌ У вас вже є активне звернення #{activeAppeal.Id}.\n\n" +
                      "Спочатку закрийте його або дочекайтеся відповіді адміністратора.",
                parseMode: ParseMode.Html
            );
            return;
        }

        // Переводимо в режим створення звернення
        var userState = GetUserState(userId);
        userState.DialogState = DialogState.CreatingAppeal;

        await _botClient.SendTextMessageAsync(
            chatId: chatId,
            text: "✍️ <b>Створення звернення</b>\n\n" +
                  "Опишіть ваше питання або проблему.\n" +
                  "Ви також можете надіслати фото або документ.\n\n" +
                  "Щоб скасувати, натисніть /cancel",
            parseMode: ParseMode.Html
        );
    }

    private async Task HandleAdminActiveAppealsInline(long chatId, long userId, int? messageId = null)
    {
        var activeAppeals = await _context.Appeals
            .Where(a => a.ClosedAt == null)
            .OrderByDescending(a => a.CreatedAt)
            .Take(10)
            .ToListAsync();

        if (!activeAppeals.Any())
        {
            var keyboard = new InlineKeyboardMarkup(new[]
            {
                new[] { InlineKeyboardButton.WithCallbackData("◀️ Назад", "admin_appeals") }
            });

            await _botClient.SendTextMessageAsync(
                chatId: chatId,
                text: "📬 <b>Активні звернення</b>\n\n" +
                      "Немає активних звернень.",
                parseMode: ParseMode.Html,
                replyMarkup: keyboard
            );
            return;
        }

        var messageText = "📬 <b>Активні звернення</b>\n\n";
        var buttons = new List<InlineKeyboardButton[]>();

        foreach (var appeal in activeAppeals)
        {
            var statusEmoji = appeal.Status == AppealStatus.New ? "🆕" : "✅";
            var studentName = appeal.StudentName;
            messageText += $"{statusEmoji} <b>#{appeal.Id}</b> - {studentName}\n" +
                          $"📅 {appeal.CreatedAt:dd.MM.yyyy HH:mm}\n\n";
            
            buttons.Add(new[] 
            { 
                InlineKeyboardButton.WithCallbackData($"#{appeal.Id} - {studentName}", $"admin_appeal_view_{appeal.Id}") 
            });
        }

        buttons.Add(new[] { InlineKeyboardButton.WithCallbackData("◀️ Назад", "admin_appeals") });

        await _botClient.SendTextMessageAsync(
            chatId: chatId,
            text: messageText,
            parseMode: ParseMode.Html,
            replyMarkup: new InlineKeyboardMarkup(buttons)
        );
    }

    private async Task HandleAdminClosedAppealsInline(long chatId, long userId, int? messageId = null)
    {
        var closedAppeals = await _context.Appeals
            .Where(a => a.ClosedAt != null)
            .OrderByDescending(a => a.ClosedAt)
            .Take(10)
            .ToListAsync();

        if (!closedAppeals.Any())
        {
            var keyboard = new InlineKeyboardMarkup(new[]
            {
                new[] { InlineKeyboardButton.WithCallbackData("◀️ Назад", "admin_appeals") }
            });

            await _botClient.SendTextMessageAsync(
                chatId: chatId,
                text: "📁 <b>Закриті звернення</b>\n\n" +
                      "Немає закритих звернень.",
                parseMode: ParseMode.Html,
                replyMarkup: keyboard
            );
            return;
        }

        var messageText = "📁 <b>Закриті звернення (останні 10)</b>\n\n";
        var buttons = new List<InlineKeyboardButton[]>();

        foreach (var appeal in closedAppeals)
        {
            var studentName = appeal.StudentName;
            messageText += $"✅ <b>#{appeal.Id}</b> - {studentName}\n" +
                          $"🔒 {appeal.ClosedAt:dd.MM.yyyy HH:mm}\n\n";
            
            buttons.Add(new[] 
            { 
                InlineKeyboardButton.WithCallbackData($"#{appeal.Id} - {studentName}", $"admin_appeal_view_{appeal.Id}") 
            });
        }

        buttons.Add(new[] { InlineKeyboardButton.WithCallbackData("◀️ Назад", "admin_appeals") });

        await _botClient.SendTextMessageAsync(
            chatId: chatId,
            text: messageText,
            parseMode: ParseMode.Html,
            replyMarkup: new InlineKeyboardMarkup(buttons)
        );
    }
}




