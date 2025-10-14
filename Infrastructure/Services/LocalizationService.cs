using Microsoft.Extensions.Logging;
using StudentUnionBot.Application.Common.Models;
using StudentUnionBot.Domain.Enums;
using StudentUnionBot.Domain.Interfaces;
using System.Collections.Concurrent;

namespace StudentUnionBot.Infrastructure.Services;

public class LocalizationService : ILocalizationService
{
    private readonly ILogger<LocalizationService> _logger;
    private readonly ConcurrentDictionary<string, LocalizationResource> _resources;

    public LocalizationService(ILogger<LocalizationService> logger)
    {
        _logger = logger;
        _resources = new ConcurrentDictionary<string, LocalizationResource>();
        InitializeDefaultResources();
    }

    public Task<string> GetLocalizedStringAsync(string key, Language language, CancellationToken cancellationToken = default)
    {
        var resourceKey = GetResourceKey(key, language);
        
        if (_resources.TryGetValue(resourceKey, out var resource))
        {
            return Task.FromResult(resource.Value);
        }

        // Фолбек на українську мову
        if (language != Language.Ukrainian)
        {
            var fallbackKey = GetResourceKey(key, Language.Ukrainian);
            if (_resources.TryGetValue(fallbackKey, out var fallbackResource))
            {
                _logger.LogWarning("Локалізація не знайдена для ключа {Key} мовою {Language}, використано українську", key, language);
                return Task.FromResult(fallbackResource.Value);
            }
        }

        _logger.LogWarning("Локалізація не знайдена для ключа {Key}", key);
        return Task.FromResult($"[{key}]");
    }

    public async Task<string> GetLocalizedStringAsync(string key, Language language, object[] args, CancellationToken cancellationToken = default)
    {
        var template = await GetLocalizedStringAsync(key, language, cancellationToken);
        
        try
        {
            return string.Format(template, args);
        }
        catch (FormatException ex)
        {
            _logger.LogError(ex, "Помилка форматування локалізованого рядка для ключа {Key}", key);
            return template;
        }
    }

    public Task<bool> HasKeyAsync(string key, Language language, CancellationToken cancellationToken = default)
    {
        var resourceKey = GetResourceKey(key, language);
        return Task.FromResult(_resources.ContainsKey(resourceKey));
    }

    public Task<List<Language>> GetAvailableLanguagesAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new List<Language> { Language.Ukrainian, Language.English });
    }

    private static string GetResourceKey(string key, Language language)
    {
        return $"{key}_{language.GetCode()}";
    }

    private void InitializeDefaultResources()
    {
        // Основні команди та меню
        AddResource("menu.main.title", Language.Ukrainian, "🎓 <b>Головне меню</b>");
        AddResource("menu.main.title", Language.English, "🎓 <b>Main Menu</b>");
        
        AddResource("menu.main.description", Language.Ukrainian, "Оберіть дію:");
        AddResource("menu.main.description", Language.English, "Choose an action:");

        // Кнопки меню
        AddResource("button.create_appeal", Language.Ukrainian, "📝 Створити звернення");
        AddResource("button.create_appeal", Language.English, "📝 Create Appeal");
        
        AddResource("button.my_appeals", Language.Ukrainian, "📋 Мої звернення");
        AddResource("button.my_appeals", Language.English, "📋 My Appeals");
        
        AddResource("button.news", Language.Ukrainian, "📰 Новини");
        AddResource("button.news", Language.English, "📰 News");
        
        AddResource("button.events", Language.Ukrainian, "🎉 Події");
        AddResource("button.events", Language.English, "🎉 Events");
        
        AddResource("button.partners", Language.Ukrainian, "🤝 Партнери");
        AddResource("button.partners", Language.English, "🤝 Partners");
        
        AddResource("button.contacts", Language.Ukrainian, "📞 Контакти");
        AddResource("button.contacts", Language.English, "📞 Contacts");
        
        AddResource("button.profile", Language.Ukrainian, "👤 Профіль");
        AddResource("button.profile", Language.English, "👤 Profile");
        
        AddResource("button.language", Language.Ukrainian, "🌐 Мова");
        AddResource("button.language", Language.English, "🌐 Language");
        
        AddResource("button.help", Language.Ukrainian, "ℹ️ Допомога");
        AddResource("button.help", Language.English, "ℹ️ Help");
        
        AddResource("button.back", Language.Ukrainian, "🔙 Назад");
        AddResource("button.back", Language.English, "🔙 Back");
        
        AddResource("button.skip", Language.Ukrainian, "⏭️ Пропустити");
        AddResource("button.skip", Language.English, "⏭️ Skip");
        
        AddResource("button.finish", Language.Ukrainian, "✅ Завершити");
        AddResource("button.finish", Language.English, "✅ Finish");
        
        AddResource("button.cancel", Language.Ukrainian, "❌ Скасувати");
        AddResource("button.cancel", Language.English, "❌ Cancel");

        // Команда start
        AddResource("command.start.title", Language.Ukrainian, "🎓 <b>Вітаємо в боті Студентського Профкому ВНМУ!</b>");
        AddResource("command.start.title", Language.English, "🎓 <b>Welcome to VNMU Student Union Bot!</b>");
        
        AddResource("command.start_welcome", Language.Ukrainian, 
            "🎓 <b>Вітаємо в боті Студентського Профкому ВНМУ!</b>\n\n" +
            "Я допоможу вам:\n" +
            "📝 Створити звернення до профкому\n" +
            "📋 Відстежувати статус ваших звернень\n" +
            "📰 Дізнаватися останні новини\n" +
            "🎉 Бути в курсі майбутніх подій\n" +
            "🤝 Отримувати знижки від партнерів\n\n" +
            "Оберіть дію з меню нижче:");
            
        AddResource("command.start_welcome", Language.English, 
            "🎓 <b>Welcome to VNMU Student Union Bot!</b>\n\n" +
            "I will help you:\n" +
            "📝 Create appeals to the student union\n" +
            "📋 Track the status of your appeals\n" +
            "📰 Get the latest news\n" +
            "🎉 Stay informed about upcoming events\n" +
            "🤝 Get discounts from partners\n\n" +
            "Choose an action from the menu below:");
        
        AddResource("command.start.description", Language.Ukrainian, 
            "Я допоможу вам:\n" +
            "📝 Створити звернення до профкому\n" +
            "📋 Відстежувати статус ваших звернень\n" +
            "📰 Дізнаватися останні новини\n" +
            "🎉 Бути в курсі майбутніх подій\n" +
            "🤝 Отримувати знижки від партнерів\n\n" +
            "Оберіть дію з меню нижче:");
            
        AddResource("command.start.description", Language.English, 
            "I will help you:\n" +
            "📝 Create appeals to the student union\n" +
            "📋 Track the status of your appeals\n" +
            "📰 Get the latest news\n" +
            "🎉 Stay informed about upcoming events\n" +
            "🤝 Get discounts from partners\n\n" +
            "Choose an action from the menu below:");

        // Команда help
        AddResource("command.help.title", Language.Ukrainian, "ℹ️ <b>Допомога</b>");
        AddResource("command.help.title", Language.English, "ℹ️ <b>Help</b>");
        
        AddResource("command.help.description", Language.Ukrainian, 
            "Використовуйте меню для навігації.\nКоманди: /start, /help, /appeal, /contacts");
        AddResource("command.help.description", Language.English, 
            "Use the menu for navigation.\nCommands: /start, /help, /appeal, /contacts");

        // Контакти
        AddResource("contacts.title", Language.Ukrainian, "📞 <b>Контактна інформація</b>");
        AddResource("contacts.title", Language.English, "📞 <b>Contact Information</b>");
        
        AddResource("contacts.info", Language.Ukrainian, 
            "🏛 <b>Студентський профспілковий комітет ВНМУ</b>\n\n" +
            "📧 Email: profkom@vnmu.edu.ua\n" +
            "📱 Telegram: @vnmu_profkom\n" +
            "📍 Адреса: вул. Пирогова, 56, Вінниця\n" +
            "🕐 Години роботи: ПН-ПТ 9:00-17:00\n\n" +
            "Ми завжди раді вам допомогти! 🤝");
            
        AddResource("contacts.info", Language.English, 
            "🏛 <b>VNMU Student Union Committee</b>\n\n" +
            "📧 Email: profkom@vnmu.edu.ua\n" +
            "📱 Telegram: @vnmu_profkom\n" +
            "📍 Address: 56 Pirogova St., Vinnytsia\n" +
            "🕐 Working hours: Mon-Fri 9:00-17:00\n\n" +
            "We are always happy to help you! 🤝");

        // Профіль
        AddResource("profile.title", Language.Ukrainian, "👤 <b>Ваш профіль</b>");
        AddResource("profile.title", Language.English, "👤 <b>Your Profile</b>");
        
        AddResource("profile.info", Language.Ukrainian, 
            "👤 <b>Ім'я:</b> {0}\n" +
            "🏫 <b>Факультет:</b> {1}\n" +
            "📚 <b>Курс:</b> {2}\n" +
            "👥 <b>Група:</b> {3}\n" +
            "📧 <b>Email:</b> {4}\n" +
            "🌐 <b>Мова:</b> {5}\n" +
            "📅 <b>Зареєстровано:</b> {6}");
            
        AddResource("profile.info", Language.English, 
            "👤 <b>Name:</b> {0}\n" +
            "🏫 <b>Faculty:</b> {1}\n" +
            "📚 <b>Course:</b> {2}\n" +
            "👥 <b>Group:</b> {3}\n" +
            "📧 <b>Email:</b> {4}\n" +
            "🌐 <b>Language:</b> {5}\n" +
            "📅 <b>Registered:</b> {6}");

        AddResource("profile.not_specified", Language.Ukrainian, "<i>не вказано</i>");
        AddResource("profile.not_specified", Language.English, "<i>not specified</i>");
        
        AddResource("profile.language_changed", Language.Ukrainian, "✅ <b>Мову змінено!</b>\n\nТепер інтерфейс бота відображатиметься українською мовою.");
        AddResource("profile.language_changed", Language.English, "✅ <b>Language changed!</b>\n\nThe bot interface will now be displayed in English.");

        // Мова
        AddResource("language.select.title", Language.Ukrainian, "🌐 <b>Оберіть мову / Select Language</b>");
        AddResource("language.select.title", Language.English, "🌐 <b>Select Language / Оберіть мову</b>");
        
        AddResource("language.select.description", Language.Ukrainian, "Оберіть мову інтерфейсу:");
        AddResource("language.select.description", Language.English, "Select interface language:");
        
        AddResource("language.changed", Language.Ukrainian, "✅ Мову змінено на українську");
        AddResource("language.changed", Language.English, "✅ Language changed to English");

        // Загальні повідомлення
        AddResource("error.general", Language.Ukrainian, "❌ Виникла технічна помилка. Спробуйте пізніше.");
        AddResource("error.general", Language.English, "❌ A technical error occurred. Please try again later.");
        
        AddResource("error.access_denied", Language.Ukrainian, "❌ Недостатньо прав для виконання цієї дії");
        AddResource("error.access_denied", Language.English, "❌ Insufficient permissions to perform this action");
        
        AddResource("unknown_command", Language.Ukrainian, "❓ Невідома команда. Використовуйте /help для перегляду списку команд.");
        AddResource("unknown_command", Language.English, "❓ Unknown command. Use /help to view the list of commands.");

        // Файли та завантаження
        AddResource("file.photo.upload.success", Language.Ukrainian, "✅ Фото успішно завантажено!\n📎 Файл: {0}\n📏 Розмір: {1} KB");
        AddResource("file.photo.upload.success", Language.English, "✅ Photo uploaded successfully!\n📎 File: {0}\n📏 Size: {1} KB");
        
        AddResource("file.document.upload.success", Language.Ukrainian, "✅ Документ успішно завантажено!\n📎 Файл: {0}\n📏 Розмір: {1} KB\n📄 Тип: {2}");
        AddResource("file.document.upload.success", Language.English, "✅ Document uploaded successfully!\n📎 File: {0}\n📏 Size: {1} KB\n📄 Type: {2}");
        
        AddResource("file.upload.error.generic", Language.Ukrainian, "❌ Не вдалося завантажити файл. Спробуйте ще раз.");
        AddResource("file.upload.error.generic", Language.English, "❌ Failed to upload file. Please try again.");
        
        AddResource("file.upload.error.size", Language.Ukrainian, "❌ Файл занадто великий. Максимальний розмір: 20 MB");
        AddResource("file.upload.error.size", Language.English, "❌ File is too large. Maximum size: 20 MB");
        
        AddResource("file.upload.error.processing", Language.Ukrainian, "❌ Виникла помилка при обробці файлу. Спробуйте пізніше.");
        AddResource("file.upload.error.processing", Language.English, "❌ An error occurred while processing the file. Please try again later.");
        
        AddResource("file.upload.error.failed", Language.Ukrainian, "❌ Помилка завантаження: {0}");
        AddResource("file.upload.error.failed", Language.English, "❌ Upload error: {0}");

        // Створення апелів
        AddResource("appeal.create.title", Language.Ukrainian, "📝 <b>Створення звернення</b>\n\nОберіть категорію вашого звернення:");
        AddResource("appeal.create.title", Language.English, "📝 <b>Creating Appeal</b>\n\nSelect your appeal category:");
        
        AddResource("appeal.subject.prompt", Language.Ukrainian, "📝 Введіть тему вашого звернення:\n\n<i>Тема має бути короткою та зрозумілою (5-200 символів)</i>");
        AddResource("appeal.subject.prompt", Language.English, "📝 Enter the subject of your appeal:\n\n<i>The subject should be short and clear (5-200 characters)</i>");
        
        AddResource("appeal.subject.too_short", Language.Ukrainian, "❌ Тема звернення занадто коротка. Будь ласка, введіть щонайменше 5 символів.");
        AddResource("appeal.subject.too_short", Language.English, "❌ Appeal subject is too short. Please enter at least 5 characters.");
        
        AddResource("appeal.subject.too_long", Language.Ukrainian, "❌ Тема звернення занадто довга. Максимум 200 символів.");
        AddResource("appeal.subject.too_long", Language.English, "❌ Appeal subject is too long. Maximum 200 characters.");
        
        AddResource("appeal.subject.saved", Language.Ukrainian, "✅ Тема збережена: <b>{0}</b>\n\n📝 Тепер опишіть вашу проблему детально.\n\n<i>Ви також можете прикріпити фото або документи</i>");
        AddResource("appeal.subject.saved", Language.English, "✅ Subject saved: <b>{0}</b>\n\n📝 Now describe your problem in detail.\n\n<i>You can also attach photos or documents</i>");
        
        AddResource("appeal.message.prompt", Language.Ukrainian, "📝 Опишіть детально вашу ситуацію або проблему:\n\n<i>Мінімум 10 символів. Будьте конкретними, це допоможе швидше вирішити питання.</i>");
        AddResource("appeal.message.prompt", Language.English, "📝 Describe your situation or problem in detail:\n\n<i>Minimum 10 characters. Be specific, this will help resolve the issue faster.</i>");
        
        AddResource("appeal.message.too_short", Language.Ukrainian, "❌ Повідомлення занадто коротке. Будь ласка, опишіть проблему детальніше (мінімум 10 символів).");
        AddResource("appeal.message.too_short", Language.English, "❌ Message is too short. Please describe the problem in more detail (minimum 10 characters).");
        
        AddResource("appeal.message.too_long", Language.Ukrainian, "❌ Повідомлення занадто довге. Максимум 4000 символів.");
        AddResource("appeal.message.too_long", Language.English, "❌ Message is too long. Maximum 4000 characters.");

        // Події та новини
        AddResource("events.loading", Language.Ukrainian, "🎉 Завантаження подій...");
        AddResource("events.loading", Language.English, "🎉 Loading events...");
        
        AddResource("news.loading", Language.Ukrainian, "📰 Завантаження новин...");
        AddResource("news.loading", Language.English, "📰 Loading news...");

        // Кнопки клавіатур - додаткові кнопки та дії
        AddResource("button.admin_panel", Language.Ukrainian, "👨‍💼 Адмін панель");
        AddResource("button.admin_panel", Language.English, "👨‍💼 Admin Panel");
        
        AddResource("button.back", Language.Ukrainian, "⬅️ Назад");
        AddResource("button.back", Language.English, "⬅️ Back");
        
        AddResource("button.main_menu", Language.Ukrainian, "⬅️ Головне меню");
        AddResource("button.main_menu", Language.English, "⬅️ Main Menu");
        
        AddResource("button.yes", Language.Ukrainian, "✅ Так");
        AddResource("button.yes", Language.English, "✅ Yes");
        
        AddResource("button.no", Language.Ukrainian, "❌ Ні");
        AddResource("button.no", Language.English, "❌ No");
        
        AddResource("button.update", Language.Ukrainian, "🔄 Оновити");
        AddResource("button.update", Language.English, "🔄 Refresh");
        
        AddResource("button.close", Language.Ukrainian, "✅ Закрити");
        AddResource("button.close", Language.English, "✅ Close");
        
        AddResource("button.add_message", Language.Ukrainian, "💬 Додати повідомлення");
        AddResource("button.add_message", Language.English, "💬 Add Message");

        // Категорії звернень
        AddResource("button.category.scholarship", Language.Ukrainian, "💰 Стипендія");
        AddResource("button.category.scholarship", Language.English, "💰 Scholarship");
        
        AddResource("button.category.dormitory", Language.Ukrainian, "🏠 Гуртожиток");
        AddResource("button.category.dormitory", Language.English, "🏠 Dormitory");
        
        AddResource("button.category.events", Language.Ukrainian, "🎉 Заходи");
        AddResource("button.category.events", Language.English, "🎉 Events");
        
        AddResource("button.category.suggestion", Language.Ukrainian, "💡 Пропозиція");
        AddResource("button.category.suggestion", Language.English, "💡 Suggestion");

        // Управління новинами (Адміністратори)
        AddResource("admin.news_management_menu", Language.Ukrainian, "📰 <b>Управління новинами</b>\n\nОберіть дію:");
        AddResource("admin.news_management_menu", Language.English, "📰 <b>News Management</b>\n\nSelect an action:");

        AddResource("button.create_news", Language.Ukrainian, "➕ Створити новину");
        AddResource("button.create_news", Language.English, "➕ Create News");

        AddResource("button.all_news", Language.Ukrainian, "📋 Всі новини");
        AddResource("button.all_news", Language.English, "📋 All News");

        AddResource("button.draft_news", Language.Ukrainian, "📝 Чернетки");
        AddResource("button.draft_news", Language.English, "📝 Drafts");

        AddResource("button.published_news", Language.Ukrainian, "✅ Опубліковані");
        AddResource("button.published_news", Language.English, "✅ Published");

        AddResource("button.back_to_admin", Language.Ukrainian, "🔙 Адмін панель");
        AddResource("button.back_to_admin", Language.English, "🔙 Admin Panel");

        AddResource("button.back_to_news_menu", Language.Ukrainian, "🔙 Меню новин");
        AddResource("button.back_to_news_menu", Language.English, "🔙 News Menu");

        AddResource("admin.news_create_title_prompt", Language.Ukrainian, "📰 <b>Створення новини</b>\n\n📝 Введіть заголовок новини:\n\n<i>Заголовок має бути від 10 до 200 символів</i>");
        AddResource("admin.news_create_title_prompt", Language.English, "📰 <b>Creating News</b>\n\n📝 Enter news title:\n\n<i>Title should be 10-200 characters</i>");

        AddResource("admin.news_create_content_prompt", Language.Ukrainian, "✅ Заголовок збережено!\n\n📝 Тепер введіть зміст новини:\n\n<i>Мінімум 50 символів. Використовуйте HTML теги для форматування</i>");
        AddResource("admin.news_create_content_prompt", Language.English, "✅ Title saved!\n\n📝 Now enter news content:\n\n<i>Minimum 50 characters. Use HTML tags for formatting</i>");

        AddResource("admin.news_created_success", Language.Ukrainian, "✅ <b>Новину успішно створено!</b>\n\n📰 Заголовок: {0}\n📝 Статус: Чернетка\n\n<i>Ви можете опублікувати її пізніше через меню управління новинами</i>");
        AddResource("admin.news_created_success", Language.English, "✅ <b>News created successfully!</b>\n\n📰 Title: {0}\n📝 Status: Draft\n\n<i>You can publish it later through the news management menu</i>");

        AddResource("admin.news_create_error", Language.Ukrainian, "❌ Не вдалося створити новину. Спробуйте пізніше.");
        AddResource("admin.news_create_error", Language.English, "❌ Failed to create news. Please try again later.");

        AddResource("admin.news_load_error", Language.Ukrainian, "❌ Не вдалося завантажити список новин.");
        AddResource("admin.news_load_error", Language.English, "❌ Failed to load news list.");

        AddResource("admin.news_list_header", Language.Ukrainian, "📰 <b>Список новин</b>\n\n📊 Знайдено: {0}\n📄 Сторінка: {1} / {2}");
        AddResource("admin.news_list_header", Language.English, "📰 <b>News List</b>\n\n📊 Found: {0}\n📄 Page: {1} / {2}");

        AddResource("admin.news_list_empty", Language.Ukrainian, "📭 <i>Новин не знайдено</i>");
        AddResource("admin.news_list_empty", Language.English, "📭 <i>No news found</i>");

        AddResource("validation.news_title_empty", Language.Ukrainian, "❌ Заголовок новини не може бути порожнім");
        AddResource("validation.news_title_empty", Language.English, "❌ News title cannot be empty");

        AddResource("validation.news_title_length", Language.Ukrainian, "❌ Заголовок має бути від {0} до {1} символів");
        AddResource("validation.news_title_length", Language.English, "❌ Title should be {0}-{1} characters");

        AddResource("validation.news_content_empty", Language.Ukrainian, "❌ Вміст новини не може бути порожнім");
        AddResource("validation.news_content_empty", Language.English, "❌ News content cannot be empty");

        AddResource("validation.news_content_short", Language.Ukrainian, "❌ Вміст новини занадто короткий. Мінімум {0} символів");
        AddResource("validation.news_content_short", Language.English, "❌ News content is too short. Minimum {0} characters");

        // Управління подіями (Адміністратори)
        AddResource("admin.events_management_menu", Language.Ukrainian, "📅 <b>Управління подіями</b>\n\nОберіть дію:");
        AddResource("admin.events_management_menu", Language.English, "📅 <b>Events Management</b>\n\nSelect an action:");

        AddResource("button.create_event", Language.Ukrainian, "➕ Створити подію");
        AddResource("button.create_event", Language.English, "➕ Create Event");

        AddResource("button.all_events", Language.Ukrainian, "📋 Всі події");
        AddResource("button.all_events", Language.English, "📋 All Events");

        AddResource("button.draft_events", Language.Ukrainian, "📝 Чернетки");
        AddResource("button.draft_events", Language.English, "📝 Drafts");

        AddResource("button.planned_events", Language.Ukrainian, "📅 Заплановані");
        AddResource("button.planned_events", Language.English, "📅 Planned");

        AddResource("button.completed_events", Language.Ukrainian, "✅ Завершені");
        AddResource("button.completed_events", Language.English, "✅ Completed");

        AddResource("button.back_to_events_menu", Language.Ukrainian, "🔙 Меню подій");
        AddResource("button.back_to_events_menu", Language.English, "🔙 Events Menu");

        AddResource("admin.event_create_title_prompt", Language.Ukrainian, "📅 <b>Створення події</b>\n\n📝 Введіть назву події:\n\n<i>Назва має бути від 5 до 100 символів</i>");
        AddResource("admin.event_create_title_prompt", Language.English, "📅 <b>Creating Event</b>\n\n📝 Enter event title:\n\n<i>Title should be 5-100 characters</i>");

        AddResource("admin.event_create_description_prompt", Language.Ukrainian, "✅ Назву збережено!\n\n📝 Тепер введіть опис події:\n\n<i>Мінімум 20 символів. Опишіть деталі події</i>");
        AddResource("admin.event_create_description_prompt", Language.English, "✅ Title saved!\n\n📝 Now enter event description:\n\n<i>Minimum 20 characters. Describe event details</i>");

        AddResource("admin.event_create_location_prompt", Language.Ukrainian, "✅ Опис збережено!\n\n📍 Введіть місце проведення події:\n\n<i>Наприклад: Актова зала, Аудиторія 205, Онлайн</i>");
        AddResource("admin.event_create_location_prompt", Language.English, "✅ Description saved!\n\n📍 Enter event location:\n\n<i>For example: Main Hall, Room 205, Online</i>");

        AddResource("admin.event_create_datetime_prompt", Language.Ukrainian, "✅ Місце збережено!\n\n📅 Введіть дату та час події:\n\n<i>Формат: ДД.ММ.РРРР ГГ:ХХ або РРРР-ММ-ДД ГГ:ХХ</i>\n\n<b>Приклад:</b> 15.12.2024 14:30");
        AddResource("admin.event_create_datetime_prompt", Language.English, "✅ Location saved!\n\n📅 Enter event date and time:\n\n<i>Format: DD.MM.YYYY HH:MM or YYYY-MM-DD HH:MM</i>\n\n<b>Example:</b> 15.12.2024 14:30");

        AddResource("admin.event_created_success", Language.Ukrainian, "✅ <b>Подію успішно створено!</b>\n\n📅 Назва: {0}\n📅 Дата: {1}\n📝 Статус: Чернетка\n\n<i>Ви можете опублікувати її пізніше через меню управління подіями</i>");
        AddResource("admin.event_created_success", Language.English, "✅ <b>Event created successfully!</b>\n\n📅 Title: {0}\n📅 Date: {1}\n📝 Status: Draft\n\n<i>You can publish it later through the events management menu</i>");

        AddResource("admin.event_create_error", Language.Ukrainian, "❌ Не вдалося створити подію. Спробуйте пізніше.");
        AddResource("admin.event_create_error", Language.English, "❌ Failed to create event. Please try again later.");

        AddResource("admin.events_load_error", Language.Ukrainian, "❌ Не вдалося завантажити список подій.");
        AddResource("admin.events_load_error", Language.English, "❌ Failed to load events list.");

        AddResource("admin.events_list_header", Language.Ukrainian, "📅 <b>Список подій</b>\n\n📊 Знайдено: {0}\n📄 Сторінка: {1} / {2}");
        AddResource("admin.events_list_header", Language.English, "📅 <b>Events List</b>\n\n📊 Found: {0}\n📄 Page: {1} / {2}");

        AddResource("admin.events_list_empty", Language.Ukrainian, "📭 <i>Подій не знайдено</i>");
        AddResource("admin.events_list_empty", Language.English, "📭 <i>No events found</i>");

        AddResource("validation.event_title_empty", Language.Ukrainian, "❌ Назва події не може бути порожньою");
        AddResource("validation.event_title_empty", Language.English, "❌ Event title cannot be empty");

        AddResource("validation.event_title_length", Language.Ukrainian, "❌ Назва має бути від {0} до {1} символів");
        AddResource("validation.event_title_length", Language.English, "❌ Title should be {0}-{1} characters");

        AddResource("validation.event_description_empty", Language.Ukrainian, "❌ Опис події не може бути порожнім");
        AddResource("validation.event_description_empty", Language.English, "❌ Event description cannot be empty");

        AddResource("validation.event_description_short", Language.Ukrainian, "❌ Опис події занадто короткий. Мінімум {0} символів");
        AddResource("validation.event_description_short", Language.English, "❌ Event description is too short. Minimum {0} characters");

        AddResource("validation.event_location_empty", Language.Ukrainian, "❌ Місце події не може бути порожнім");
        AddResource("validation.event_location_empty", Language.English, "❌ Event location cannot be empty");

        AddResource("validation.event_datetime_empty", Language.Ukrainian, "❌ Дата та час події обов'язкові");
        AddResource("validation.event_datetime_empty", Language.English, "❌ Event date and time are required");

        AddResource("validation.event_datetime_invalid", Language.Ukrainian, "❌ Неправильний формат дати. Використовуйте: ДД.ММ.РРРР ГГ:ХХ");
        AddResource("validation.event_datetime_invalid", Language.English, "❌ Invalid date format. Use: DD.MM.YYYY HH:MM");

        AddResource("validation.event_datetime_past", Language.Ukrainian, "❌ Дата події не може бути в минулому");
        AddResource("validation.event_datetime_past", Language.English, "❌ Event date cannot be in the past");

        AddResource("error.session_expired", Language.Ukrainian, "❌ Сесія завершилася. Почніть спочатку");
        AddResource("error.session_expired", Language.English, "❌ Session expired. Please start over");

        AddResource("error.unknown_state", Language.Ukrainian, "❌ Невідомий стан. Поверніться в головне меню");
        AddResource("error.unknown_state", Language.English, "❌ Unknown state. Please return to main menu");

        AddResource("error.technical_error", Language.Ukrainian, "❌ Виникла технічна помилка. Спробуйте пізніше або зверніться до адміністратора");
        AddResource("error.technical_error", Language.English, "❌ A technical error occurred. Please try again later or contact administrator");
        
        AddResource("button.category.complaint", Language.Ukrainian, "⚠️ Скарга");
        AddResource("button.category.complaint", Language.English, "⚠️ Complaint");
        
        AddResource("button.category.other", Language.Ukrainian, "📝 Інше");
        AddResource("button.category.other", Language.English, "📝 Other");

        // Адмін кнопки
        AddResource("button.admin.all_appeals", Language.Ukrainian, "📋 Всі звернення");
        AddResource("button.admin.all_appeals", Language.English, "📋 All Appeals");
        
        AddResource("button.admin.new_appeals", Language.Ukrainian, "🆕 Нові");
        AddResource("button.admin.new_appeals", Language.English, "🆕 New");
        
        AddResource("button.admin.my_appeals", Language.Ukrainian, "👤 Мої звернення");
        AddResource("button.admin.my_appeals", Language.English, "👤 My Appeals");
        
        AddResource("button.admin.unassigned", Language.Ukrainian, "❓ Непризначені");
        AddResource("button.admin.unassigned", Language.English, "❓ Unassigned");
        
        AddResource("button.admin.search", Language.Ukrainian, "🔍 Пошук");
        AddResource("button.admin.search", Language.English, "🔍 Search");
        
        AddResource("button.admin.statistics", Language.Ukrainian, "📊 Статистика");
        AddResource("button.admin.statistics", Language.English, "📊 Statistics");
        
        AddResource("button.admin.reply", Language.Ukrainian, "💬 Відповісти");
        AddResource("button.admin.reply", Language.English, "💬 Reply");
        
        AddResource("button.admin.assign_me", Language.Ukrainian, "✋ Взяти в роботу");
        AddResource("button.admin.assign_me", Language.English, "✋ Assign to Me");
        
        AddResource("button.admin.unassign", Language.Ukrainian, "❌ Зняти призначення");
        AddResource("button.admin.unassign", Language.English, "❌ Unassign");
        
        AddResource("button.admin.priority", Language.Ukrainian, "⚡ Пріоритет");
        AddResource("button.admin.priority", Language.English, "⚡ Priority");
        
        AddResource("button.admin.panel", Language.Ukrainian, "⬅️ Адмін панель");
        AddResource("button.admin.panel", Language.English, "⬅️ Admin Panel");

        // Рівні пріоритету
        AddResource("button.priority.low", Language.Ukrainian, "🟢 Низький");
        AddResource("button.priority.low", Language.English, "🟢 Low");
        
        AddResource("button.priority.normal", Language.Ukrainian, "🟡 Нормальний");
        AddResource("button.priority.normal", Language.English, "🟡 Normal");
        
        AddResource("button.priority.high", Language.Ukrainian, "🟠 Високий");
        AddResource("button.priority.high", Language.English, "🟠 High");
        
        AddResource("button.priority.urgent", Language.Ukrainian, "🔴 Терміновий");
        AddResource("button.priority.urgent", Language.English, "🔴 Urgent");

        // Кнопки фільтрів
        AddResource("button.filter.all", Language.Ukrainian, "📋 Всі");
        AddResource("button.filter.all", Language.English, "📋 All");
        
        AddResource("button.filter.new", Language.Ukrainian, "🆕 Нові");
        AddResource("button.filter.new", Language.English, "🆕 New");
        
        AddResource("button.filter.inprogress", Language.Ukrainian, "⏳ В роботі");
        AddResource("button.filter.inprogress", Language.English, "⏳ In Progress");
        
        AddResource("button.filter.closed", Language.Ukrainian, "✅ Закриті");
        AddResource("button.filter.closed", Language.English, "✅ Closed");

        _logger.LogInformation("Ініціалізовано {Count} локалізаційних ресурсів", _resources.Count);
    }

    private void AddResource(string key, Language language, string value, string? description = null)
    {
        var resourceKey = GetResourceKey(key, language);
        _resources[resourceKey] = new LocalizationResource
        {
            Key = key,
            Language = language,
            Value = value,
            Description = description,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }
}