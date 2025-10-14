using StudentUnionBot.Domain.Interfaces;
using StudentUnionBot.Domain.Enums;
using Telegram.Bot.Types.ReplyMarkups;

namespace StudentUnionBot.Presentation.Bot.Keyboards;

/// <summary>
/// Покращена фабрика клавіатур з Card-Based Layout та групуванням функцій
/// Відповідає рекомендаціям UI/UX аналізу від 11.10.2025
/// </summary>
public static class ImprovedKeyboardFactory
{
    /// <summary>
    /// Головне меню з Card-Based Layout та візуальним групуванням
    /// </summary>
    public static async Task<InlineKeyboardMarkup> GetImprovedMainMenuKeyboardAsync(
        ILocalizationService localization, 
        Language userLanguage, 
        bool isAdmin = false,
        int unreadAppealsCount = 0,
        int unreadNewsCount = 0,
        int upcomingEventsCount = 0,
        CancellationToken cancellationToken = default)
    {
        var buttons = new List<InlineKeyboardButton[]>();

        // ╔═══════════════════════════╗
        // ║  🚨 ШВИДКІ ДІЇ            ║
        // ╚═══════════════════════════╝
        buttons.Add(new[] 
        { 
            InlineKeyboardButton.WithCallbackData(
                "━━━━━ 🚨 ШВИДКІ ДІЇ ━━━━━", 
                "noop") 
        });

        var createAppealText = await localization.GetLocalizedStringAsync("button.create_appeal", userLanguage, cancellationToken);
        var myAppealsText = await localization.GetLocalizedStringAsync("button.my_appeals", userLanguage, cancellationToken);
        
        // Додаємо badge з кількістю непрочитаних
        if (unreadAppealsCount > 0)
        {
            myAppealsText += $" ({unreadAppealsCount})";
        }

        buttons.Add(new[]
        {
            InlineKeyboardButton.WithCallbackData($"➕ {createAppealText}", "appeal_create"),
            InlineKeyboardButton.WithCallbackData($"📋 {myAppealsText}", "appeal_list")
        });

        // ╔═══════════════════════════╗
        // ║  📚 ІНФОРМАЦІЯ            ║
        // ╚═══════════════════════════╝
        buttons.Add(new[] 
        { 
            InlineKeyboardButton.WithCallbackData(
                "━━━━━ 📚 ІНФОРМАЦІЯ ━━━━━", 
                "noop") 
        });

        var newsText = await localization.GetLocalizedStringAsync("button.news", userLanguage, cancellationToken);
        var eventsText = await localization.GetLocalizedStringAsync("button.events", userLanguage, cancellationToken);
        var partnersText = await localization.GetLocalizedStringAsync("button.partners", userLanguage, cancellationToken);

        if (unreadNewsCount > 0)
        {
            newsText += $" ({unreadNewsCount} 🆕)";
        }
        
        if (upcomingEventsCount > 0)
        {
            eventsText += $" ({upcomingEventsCount})";
        }

        buttons.Add(new[]
        {
            InlineKeyboardButton.WithCallbackData($"📰 {newsText}", "news_list"),
            InlineKeyboardButton.WithCallbackData($"📅 {eventsText}", "events_list")
        });

        buttons.Add(new[]
        {
            InlineKeyboardButton.WithCallbackData($"🤝 {partnersText}", "partners_list")
        });

        // ╔═══════════════════════════╗
        // ║  ⚙️ НАЛАШТУВАННЯ          ║
        // ╚═══════════════════════════╝
        buttons.Add(new[] 
        { 
            InlineKeyboardButton.WithCallbackData(
                "━━━ ⚙️ НАЛАШТУВАННЯ ━━━", 
                "noop") 
        });

        var profileText = await localization.GetLocalizedStringAsync("button.profile", userLanguage, cancellationToken);
        var contactsText = await localization.GetLocalizedStringAsync("button.contacts", userLanguage, cancellationToken);
        var helpText = await localization.GetLocalizedStringAsync("button.help", userLanguage, cancellationToken);

        buttons.Add(new[]
        {
            InlineKeyboardButton.WithCallbackData($"👤 {profileText}", "profile_view"),
            InlineKeyboardButton.WithCallbackData($"📞 {contactsText}", "contacts_list")
        });

        buttons.Add(new[]
        {
            InlineKeyboardButton.WithCallbackData($"❓ {helpText}", "help")
        });

        // Адмін панель для адміністраторів
        if (isAdmin)
        {
            var adminText = await localization.GetLocalizedStringAsync("button.admin_panel", userLanguage, cancellationToken);
            buttons.Add(new[]
            {
                InlineKeyboardButton.WithCallbackData($"🔧 {adminText}", "admin_panel")
            });
        }

        return new InlineKeyboardMarkup(buttons);
    }

    /// <summary>
    /// Категорії звернень з описом (2-етапний вибір)
    /// </summary>
    public static async Task<InlineKeyboardMarkup> GetImprovedAppealCategoriesKeyboardAsync(
        ILocalizationService localization, 
        Language userLanguage, 
        CancellationToken cancellationToken = default)
    {
        var buttons = new List<InlineKeyboardButton[]>
        {
            // Заголовок
            new[] 
            { 
                InlineKeyboardButton.WithCallbackData(
                    "📝 Оберіть тип звернення:", 
                    "noop") 
            },
            
            // Фінанси
            new[]
            {
                InlineKeyboardButton.WithCallbackData(
                    "💰 Фінанси та стипендії", 
                    "appeal_cat_1")
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData(
                    "   Стипендії, виплати, пільги", 
                    "appeal_cat_1_desc")
            },
            
            // Гуртожиток
            new[]
            {
                InlineKeyboardButton.WithCallbackData(
                    "🏠 Гуртожиток", 
                    "appeal_cat_2")
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData(
                    "   Поселення, ремонт, питання проживання", 
                    "appeal_cat_2_desc")
            },
            
            // Навчання
            new[]
            {
                InlineKeyboardButton.WithCallbackData(
                    "🎓 Навчання", 
                    "appeal_cat_3")
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData(
                    "   Розклад, екзамени, академічні питання", 
                    "appeal_cat_3_desc")
            },
            
            // Події
            new[]
            {
                InlineKeyboardButton.WithCallbackData(
                    "🎉 Події та дозвілля", 
                    "appeal_cat_4")
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData(
                    "   Заходи, культурна програма", 
                    "appeal_cat_4_desc")
            },
            
            // Пропозиції
            new[]
            {
                InlineKeyboardButton.WithCallbackData(
                    "💡 Пропозиції та скарги", 
                    "appeal_cat_5")
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData(
                    "   Покращення роботи профкому", 
                    "appeal_cat_5_desc")
            },
            
            // Інше
            new[]
            {
                InlineKeyboardButton.WithCallbackData(
                    "❓ Інше", 
                    "appeal_cat_6")
            }
        };

        var backText = await localization.GetLocalizedStringAsync("button.back", userLanguage, cancellationToken);
        buttons.Add(new[]
        {
            InlineKeyboardButton.WithCallbackData($"⬅️ {backText}", "back_to_main")
        });

        return new InlineKeyboardMarkup(buttons);
    }

    /// <summary>
    /// Покращена пагінація з preview кількості результатів
    /// </summary>
    public static async Task<InlineKeyboardMarkup> GetImprovedPaginationKeyboardAsync(
        ILocalizationService localization,
        Language userLanguage,
        string dataPrefix,
        int currentPage,
        int totalPages,
        int totalItems,
        string? backCallback = null,
        bool hasActiveFilters = false,
        CancellationToken cancellationToken = default)
    {
        var buttons = new List<InlineKeyboardButton[]>();

        // Фільтри (якщо є)
        if (hasActiveFilters)
        {
            var filtersText = await localization.GetLocalizedStringAsync("button.filters", userLanguage, cancellationToken);
            buttons.Add(new[]
            {
                InlineKeyboardButton.WithCallbackData($"🔍 {filtersText} (активно ✅)", $"{dataPrefix}_filters_menu"),
                InlineKeyboardButton.WithCallbackData("❌ Скинути", $"{dataPrefix}_clear_filters")
            });
        }
        else
        {
            var filtersText = await localization.GetLocalizedStringAsync("button.filters", userLanguage, cancellationToken);
            buttons.Add(new[]
            {
                InlineKeyboardButton.WithCallbackData($"🔍 {filtersText}", $"{dataPrefix}_filters_menu")
            });
        }

        // Навігація
        var navButtons = new List<InlineKeyboardButton>();

        if (currentPage > 1)
        {
            navButtons.Add(InlineKeyboardButton.WithCallbackData("◀️", $"{dataPrefix}_page_{currentPage - 1}"));
        }

        navButtons.Add(InlineKeyboardButton.WithCallbackData(
            $"📄 {currentPage}/{totalPages} ({totalItems} всього)", 
            "noop"));

        if (currentPage < totalPages)
        {
            navButtons.Add(InlineKeyboardButton.WithCallbackData("▶️", $"{dataPrefix}_page_{currentPage + 1}"));
        }

        buttons.Add(navButtons.ToArray());

        // Назад
        if (!string.IsNullOrEmpty(backCallback))
        {
            var backText = await localization.GetLocalizedStringAsync("button.back", userLanguage, cancellationToken);
            buttons.Add(new[]
            {
                InlineKeyboardButton.WithCallbackData($"⬅️ {backText}", backCallback)
            });
        }

        return new InlineKeyboardMarkup(buttons);
    }

    /// <summary>
    /// Breadcrumb navigation з history stack
    /// </summary>
    public static async Task<InlineKeyboardMarkup> GetBreadcrumbNavigationKeyboardAsync(
        ILocalizationService localization,
        Language userLanguage,
        List<(string title, string callback)> breadcrumbPath,
        CancellationToken cancellationToken = default)
    {
        var buttons = new List<InlineKeyboardButton[]>();

        // Додаємо кожен рівень breadcrumb як кнопку
        if (breadcrumbPath.Count > 1)
        {
            var breadcrumbButtons = new List<InlineKeyboardButton>();
            
            foreach (var (title, callback) in breadcrumbPath.Take(breadcrumbPath.Count - 1))
            {
                breadcrumbButtons.Add(InlineKeyboardButton.WithCallbackData(title, callback));
            }

            // Розбиваємо на ряди по 2 кнопки
            for (int i = 0; i < breadcrumbButtons.Count; i += 2)
            {
                var row = breadcrumbButtons.Skip(i).Take(2).ToArray();
                buttons.Add(row);
            }
        }

        // Головне меню завжди доступне
        var mainMenuText = await localization.GetLocalizedStringAsync("button.main_menu", userLanguage, cancellationToken);
        buttons.Add(new[]
        {
            InlineKeyboardButton.WithCallbackData($"🏠 {mainMenuText}", "back_to_main")
        });

        return new InlineKeyboardMarkup(buttons);
    }

    /// <summary>
    /// Loading skeleton для показу під час завантаження
    /// </summary>
    public static string GetLoadingSkeleton(string title, int rowsCount = 3)
    {
        var skeleton = $"{title}\n\n";
        skeleton += "⏳ Завантаження...\n\n";
        
        for (int i = 0; i < rowsCount; i++)
        {
            skeleton += "⬜⬜⬜⬜⬜⬜⬜⬜\n";
            skeleton += "⬜⬜⬜⬜⬜⬜\n\n";
        }

        return skeleton;
    }

    /// <summary>
    /// Контекстна допомога для функцій
    /// </summary>
    public static async Task<InlineKeyboardMarkup> GetContextualHelpKeyboardAsync(
        ILocalizationService localization,
        Language userLanguage,
        string featureName,
        string actionCallback,
        CancellationToken cancellationToken = default)
    {
        var understandText = await localization.GetLocalizedStringAsync("button.understand", userLanguage, cancellationToken);
        var backText = await localization.GetLocalizedStringAsync("button.back", userLanguage, cancellationToken);

        return new InlineKeyboardMarkup(new[]
        {
            new[]
            {
                InlineKeyboardButton.WithCallbackData($"✅ {understandText}", actionCallback),
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData($"⬅️ {backText}", "back_to_main")
            }
        });
    }

    /// <summary>
    /// Tutorial steps для нових користувачів
    /// </summary>
    public static async Task<InlineKeyboardMarkup> GetTutorialKeyboardAsync(
        ILocalizationService localization,
        Language userLanguage,
        int currentStep,
        int totalSteps,
        CancellationToken cancellationToken = default)
    {
        var nextText = await localization.GetLocalizedStringAsync("button.next", userLanguage, cancellationToken);
        var skipText = await localization.GetLocalizedStringAsync("button.skip", userLanguage, cancellationToken);
        var finishText = await localization.GetLocalizedStringAsync("button.finish", userLanguage, cancellationToken);

        var buttons = new List<InlineKeyboardButton[]>();

        if (currentStep < totalSteps)
        {
            buttons.Add(new[]
            {
                InlineKeyboardButton.WithCallbackData($"➡️ {nextText}", $"tutorial_step_{currentStep + 1}"),
                InlineKeyboardButton.WithCallbackData($"⏭️ {skipText}", "tutorial_skip")
            });
        }
        else
        {
            buttons.Add(new[]
            {
                InlineKeyboardButton.WithCallbackData($"✅ {finishText}", "tutorial_finish")
            });
        }

        return new InlineKeyboardMarkup(buttons);
    }

    /// <summary>
    /// Синхронна версія покращеного головного меню
    /// </summary>
    public static InlineKeyboardMarkup GetImprovedMainMenuKeyboard(
        ILocalizationService localization, 
        Language userLanguage, 
        bool isAdmin = false,
        int unreadAppealsCount = 0,
        int unreadNewsCount = 0,
        int upcomingEventsCount = 0)
    {
        return GetImprovedMainMenuKeyboardAsync(
            localization, 
            userLanguage, 
            isAdmin, 
            unreadAppealsCount, 
            unreadNewsCount, 
            upcomingEventsCount).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Синхронна версія покращених категорій звернень
    /// </summary>
    public static InlineKeyboardMarkup GetImprovedAppealCategoriesKeyboard(
        ILocalizationService localization, 
        Language userLanguage)
    {
        return GetImprovedAppealCategoriesKeyboardAsync(localization, userLanguage).GetAwaiter().GetResult();
    }
}
