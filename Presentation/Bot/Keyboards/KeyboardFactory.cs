using StudentUnionBot.Domain.Enums;
using StudentUnionBot.Domain.Interfaces;
using Telegram.Bot.Types.ReplyMarkups;

namespace StudentUnionBot.Presentation.Bot.Keyboards;

/// <summary>
/// Фабрика для створення клавіатур бота
/// </summary>
public static class KeyboardFactory
{
    /// <summary>
    /// Головне меню
    /// </summary>
    public static async Task<InlineKeyboardMarkup> GetMainMenuKeyboardAsync(ILocalizationService localization, Language userLanguage, bool isAdmin = false, CancellationToken cancellationToken = default)
    {
        var buttons = new List<InlineKeyboardButton[]>
        {
            new[]
            {
                InlineKeyboardButton.WithCallbackData(await localization.GetLocalizedStringAsync("button.create_appeal", userLanguage, cancellationToken), "appeal_create"),
                InlineKeyboardButton.WithCallbackData(await localization.GetLocalizedStringAsync("button.my_appeals", userLanguage, cancellationToken), "appeal_list")
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData(await localization.GetLocalizedStringAsync("button.news", userLanguage, cancellationToken), "news_list"),
                InlineKeyboardButton.WithCallbackData(await localization.GetLocalizedStringAsync("button.events", userLanguage, cancellationToken), "events_list")
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData(await localization.GetLocalizedStringAsync("button.partners", userLanguage, cancellationToken), "partners_list"),
                InlineKeyboardButton.WithCallbackData(await localization.GetLocalizedStringAsync("button.contacts", userLanguage, cancellationToken), "contacts_list")
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData(await localization.GetLocalizedStringAsync("button.profile", userLanguage, cancellationToken), "profile_view"),
                InlineKeyboardButton.WithCallbackData(await localization.GetLocalizedStringAsync("button.help", userLanguage, cancellationToken), "help")
            }
        };

        // Додаємо Admin панель для адміністраторів
        if (isAdmin)
        {
            buttons.Add(new[]
            {
                InlineKeyboardButton.WithCallbackData(await localization.GetLocalizedStringAsync("button.admin_panel", userLanguage, cancellationToken), "admin_panel")
            });
        }

        return new InlineKeyboardMarkup(buttons);
    }

    /// <summary>
    /// Меню категорій звернень
    /// </summary>
    public static async Task<InlineKeyboardMarkup> GetAppealCategoriesKeyboardAsync(ILocalizationService localization, Language userLanguage, CancellationToken cancellationToken = default)
    {
        return new InlineKeyboardMarkup(new[]
        {
            new[]
            {
                InlineKeyboardButton.WithCallbackData(await localization.GetLocalizedStringAsync("button.category.scholarship", userLanguage, cancellationToken), "appeal_cat_1"),
                InlineKeyboardButton.WithCallbackData(await localization.GetLocalizedStringAsync("button.category.dormitory", userLanguage, cancellationToken), "appeal_cat_2")
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData(await localization.GetLocalizedStringAsync("button.category.events", userLanguage, cancellationToken), "appeal_cat_3"),
                InlineKeyboardButton.WithCallbackData(await localization.GetLocalizedStringAsync("button.category.suggestion", userLanguage, cancellationToken), "appeal_cat_4")
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData(await localization.GetLocalizedStringAsync("button.category.complaint", userLanguage, cancellationToken), "appeal_cat_5"),
                InlineKeyboardButton.WithCallbackData(await localization.GetLocalizedStringAsync("button.category.other", userLanguage, cancellationToken), "appeal_cat_6")
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData(await localization.GetLocalizedStringAsync("button.back", userLanguage, cancellationToken), "back_to_main")
            }
        });
    }

    /// <summary>
    /// Кнопка "Назад до головного меню"
    /// </summary>
    public static async Task<InlineKeyboardMarkup> GetBackToMainMenuKeyboardAsync(ILocalizationService localization, Language userLanguage, CancellationToken cancellationToken = default)
    {
        return new InlineKeyboardMarkup(new[]
        {
            new[]
            {
                InlineKeyboardButton.WithCallbackData(await localization.GetLocalizedStringAsync("button.main_menu", userLanguage, cancellationToken), "back_to_main")
            }
        });
    }

    /// <summary>
    /// Клавіатура для конкретного звернення
    /// </summary>
    public static async Task<InlineKeyboardMarkup> GetAppealDetailsKeyboardAsync(ILocalizationService localization, Language userLanguage, int appealId, bool isActive, CancellationToken cancellationToken = default)
    {
        var buttons = new List<InlineKeyboardButton[]>();

        if (isActive)
        {
            buttons.Add(new[]
            {
                InlineKeyboardButton.WithCallbackData(await localization.GetLocalizedStringAsync("button.add_message", userLanguage, cancellationToken), $"appeal_msg_{appealId}"),
                InlineKeyboardButton.WithCallbackData(await localization.GetLocalizedStringAsync("button.close", userLanguage, cancellationToken), $"appeal_close_{appealId}")
            });
        }

        buttons.Add(new[]
        {
            InlineKeyboardButton.WithCallbackData(await localization.GetLocalizedStringAsync("button.update", userLanguage, cancellationToken), $"appeal_view_{appealId}"),
            InlineKeyboardButton.WithCallbackData(await localization.GetLocalizedStringAsync("button.back", userLanguage, cancellationToken), "appeal_list")
        });

        return new InlineKeyboardMarkup(buttons);
    }

    /// <summary>
    /// Пагінація для списків
    /// </summary>
    public static async Task<InlineKeyboardMarkup> GetPaginationKeyboardAsync(
        ILocalizationService localization,
        Language userLanguage,
        string dataPrefix,
        int currentPage,
        int totalPages,
        string? backCallback = null,
        CancellationToken cancellationToken = default)
    {
        var buttons = new List<InlineKeyboardButton>();

        if (currentPage > 1)
        {
            buttons.Add(InlineKeyboardButton.WithCallbackData("◀️", $"{dataPrefix}_page_{currentPage - 1}"));
        }

        buttons.Add(InlineKeyboardButton.WithCallbackData($"{currentPage}/{totalPages}", "noop"));

        if (currentPage < totalPages)
        {
            buttons.Add(InlineKeyboardButton.WithCallbackData("▶️", $"{dataPrefix}_page_{currentPage + 1}"));
        }

        var rows = new List<InlineKeyboardButton[]> { buttons.ToArray() };

        if (!string.IsNullOrEmpty(backCallback))
        {
            rows.Add(new[]
            {
                InlineKeyboardButton.WithCallbackData(await localization.GetLocalizedStringAsync("button.back", userLanguage, cancellationToken), backCallback)
            });
        }

        return new InlineKeyboardMarkup(rows);
    }

    /// <summary>
    /// Підтвердження дії (Так/Ні)
    /// </summary>
    public static async Task<InlineKeyboardMarkup> GetConfirmationKeyboardAsync(ILocalizationService localization, Language userLanguage, string confirmCallback, string cancelCallback, CancellationToken cancellationToken = default)
    {
        return new InlineKeyboardMarkup(new[]
        {
            new[]
            {
                InlineKeyboardButton.WithCallbackData(await localization.GetLocalizedStringAsync("button.yes", userLanguage, cancellationToken), confirmCallback),
                InlineKeyboardButton.WithCallbackData(await localization.GetLocalizedStringAsync("button.no", userLanguage, cancellationToken), cancelCallback)
            }
        });
    }

    // ==================== ADMIN KEYBOARDS ====================

    /// <summary>
    /// Головне меню адмін панелі
    /// </summary>
    public static async Task<InlineKeyboardMarkup> GetAdminPanelKeyboardAsync(ILocalizationService localization, Language userLanguage, CancellationToken cancellationToken = default)
    {
        return new InlineKeyboardMarkup(new[]
        {
            new[]
            {
                InlineKeyboardButton.WithCallbackData(await localization.GetLocalizedStringAsync("button.admin.all_appeals", userLanguage, cancellationToken), "admin_appeals_all"),
                InlineKeyboardButton.WithCallbackData(await localization.GetLocalizedStringAsync("button.admin.new_appeals", userLanguage, cancellationToken), "admin_appeals_new")
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData(await localization.GetLocalizedStringAsync("button.admin.my_appeals", userLanguage, cancellationToken), "admin_appeals_my"),
                InlineKeyboardButton.WithCallbackData(await localization.GetLocalizedStringAsync("button.admin.unassigned", userLanguage, cancellationToken), "admin_appeals_unassigned")
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData(await localization.GetLocalizedStringAsync("button.admin.search", userLanguage, cancellationToken), "admin_appeals_search"),
                InlineKeyboardButton.WithCallbackData(await localization.GetLocalizedStringAsync("button.admin.statistics", userLanguage, cancellationToken), "admin_stats")
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData("💾 Backup", "admin_backup"),
                InlineKeyboardButton.WithCallbackData("📢 Розсилка", "admin_broadcast")
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData(await localization.GetLocalizedStringAsync("button.main_menu", userLanguage, cancellationToken), "back_to_main")
            }
        });
    }

    /// <summary>
    /// Клавіатура для керування зверненням (адмін)
    /// </summary>
    public static async Task<InlineKeyboardMarkup> GetAdminAppealActionsKeyboardAsync(ILocalizationService localization, Language userLanguage, int appealId, bool isAssignedToMe, bool isClosed, CancellationToken cancellationToken = default)
    {
        var buttons = new List<InlineKeyboardButton[]>();

        if (!isClosed)
        {
            // Відповісти
            buttons.Add(new[]
            {
                InlineKeyboardButton.WithCallbackData(await localization.GetLocalizedStringAsync("button.admin.reply", userLanguage, cancellationToken), $"admin_reply_{appealId}")
            });

            // Призначення
            if (isAssignedToMe)
            {
                buttons.Add(new[]
                {
                    InlineKeyboardButton.WithCallbackData(await localization.GetLocalizedStringAsync("button.admin.unassign", userLanguage, cancellationToken), $"admin_unassign_{appealId}")
                });
            }
            else
            {
                buttons.Add(new[]
                {
                    InlineKeyboardButton.WithCallbackData(await localization.GetLocalizedStringAsync("button.admin.assign_me", userLanguage, cancellationToken), $"admin_assign_me_{appealId}")
                });
            }

            // Пріоритет та закриття
            buttons.Add(new[]
            {
                InlineKeyboardButton.WithCallbackData(await localization.GetLocalizedStringAsync("button.admin.priority", userLanguage, cancellationToken), $"admin_priority_{appealId}"),
                InlineKeyboardButton.WithCallbackData(await localization.GetLocalizedStringAsync("button.close", userLanguage, cancellationToken), $"admin_close_{appealId}")
            });
        }

        // Навігація
        buttons.Add(new[]
        {
            InlineKeyboardButton.WithCallbackData(await localization.GetLocalizedStringAsync("button.update", userLanguage, cancellationToken), $"admin_view_{appealId}"),
            InlineKeyboardButton.WithCallbackData(await localization.GetLocalizedStringAsync("button.admin.panel", userLanguage, cancellationToken), "admin_panel")
        });

        return new InlineKeyboardMarkup(buttons);
    }

    /// <summary>
    /// Вибір пріоритету
    /// </summary>
    public static async Task<InlineKeyboardMarkup> GetPrioritySelectionKeyboardAsync(ILocalizationService localization, Language userLanguage, int appealId, CancellationToken cancellationToken = default)
    {
        return new InlineKeyboardMarkup(new[]
        {
            new[]
            {
                InlineKeyboardButton.WithCallbackData(await localization.GetLocalizedStringAsync("button.priority.low", userLanguage, cancellationToken), $"admin_set_priority_{appealId}_1"),
                InlineKeyboardButton.WithCallbackData(await localization.GetLocalizedStringAsync("button.priority.normal", userLanguage, cancellationToken), $"admin_set_priority_{appealId}_2")
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData(await localization.GetLocalizedStringAsync("button.priority.high", userLanguage, cancellationToken), $"admin_set_priority_{appealId}_3"),
                InlineKeyboardButton.WithCallbackData(await localization.GetLocalizedStringAsync("button.priority.urgent", userLanguage, cancellationToken), $"admin_set_priority_{appealId}_4")
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData(await localization.GetLocalizedStringAsync("button.back", userLanguage, cancellationToken), $"admin_view_{appealId}")
            }
        });
    }

    /// <summary>
    /// Фільтри для звернень
    /// </summary>
    public static async Task<InlineKeyboardMarkup> GetAdminAppealFiltersKeyboardAsync(ILocalizationService localization, Language userLanguage, string currentFilter = "all", CancellationToken cancellationToken = default)
    {
        return new InlineKeyboardMarkup(new[]
        {
            new[]
            {
                InlineKeyboardButton.WithCallbackData(await localization.GetLocalizedStringAsync("button.filter.all", userLanguage, cancellationToken), "admin_filter_all"),
                InlineKeyboardButton.WithCallbackData(await localization.GetLocalizedStringAsync("button.filter.new", userLanguage, cancellationToken), "admin_filter_new")
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData(await localization.GetLocalizedStringAsync("button.filter.inprogress", userLanguage, cancellationToken), "admin_filter_inprogress"),
                InlineKeyboardButton.WithCallbackData(await localization.GetLocalizedStringAsync("button.filter.closed", userLanguage, cancellationToken), "admin_filter_closed")
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData(await localization.GetLocalizedStringAsync("button.admin.panel", userLanguage, cancellationToken), "admin_panel")
            }
        });
    }

    // ==================== SYNCHRONOUS WRAPPERS (для сумісності) ====================

    /// <summary>
    /// Головне меню (синхронна версія)
    /// </summary>
    public static InlineKeyboardMarkup GetMainMenuKeyboard(ILocalizationService localization, Language userLanguage, bool isAdmin = false)
    {
        return GetMainMenuKeyboardAsync(localization, userLanguage, isAdmin).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Меню категорій звернень (синхронна версія)
    /// </summary>
    public static InlineKeyboardMarkup GetAppealCategoriesKeyboard(ILocalizationService localization, Language userLanguage)
    {
        return GetAppealCategoriesKeyboardAsync(localization, userLanguage).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Кнопка "Назад до головного меню" (синхронна версія)
    /// </summary>
    public static InlineKeyboardMarkup GetBackToMainMenuKeyboard(ILocalizationService localization, Language userLanguage)
    {
        return GetBackToMainMenuKeyboardAsync(localization, userLanguage).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Головне меню адмін панелі (синхронна версія)
    /// </summary>
    public static InlineKeyboardMarkup GetAdminPanelKeyboard(ILocalizationService localization, Language userLanguage)
    {
        return GetAdminPanelKeyboardAsync(localization, userLanguage).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Клавіатура для керування зверненням (адмін) (синхронна версія)
    /// </summary>
    public static InlineKeyboardMarkup GetAdminAppealActionsKeyboard(ILocalizationService localization, Language userLanguage, int appealId, bool isAssignedToMe, bool isClosed)
    {
        return GetAdminAppealActionsKeyboardAsync(localization, userLanguage, appealId, isAssignedToMe, isClosed).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Вибір пріоритету (синхронна версія)
    /// </summary>
    public static InlineKeyboardMarkup GetPrioritySelectionKeyboard(ILocalizationService localization, Language userLanguage, int appealId)
    {
        return GetPrioritySelectionKeyboardAsync(localization, userLanguage, appealId).GetAwaiter().GetResult();
    }
}
