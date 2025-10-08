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
    public static InlineKeyboardMarkup GetMainMenuKeyboard(bool isAdmin = false)
    {
        var buttons = new List<InlineKeyboardButton[]>
        {
            new[]
            {
                InlineKeyboardButton.WithCallbackData("📝 Створити звернення", "appeal_create"),
                InlineKeyboardButton.WithCallbackData("📋 Мої звернення", "appeal_list")
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData("📰 Новини", "news_list"),
                InlineKeyboardButton.WithCallbackData("🎉 Заходи", "events_list")
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData("🤝 Партнери", "partners_list"),
                InlineKeyboardButton.WithCallbackData("📞 Контакти", "contacts_list")
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData("👤 Мій профіль", "profile_view"),
                InlineKeyboardButton.WithCallbackData("ℹ️ Допомога", "help")
            }
        };

        // Додаємо Admin панель для адміністраторів
        if (isAdmin)
        {
            buttons.Add(new[]
            {
                InlineKeyboardButton.WithCallbackData("👨‍💼 Адмін панель", "admin_panel")
            });
        }

        return new InlineKeyboardMarkup(buttons);
    }

    /// <summary>
    /// Меню категорій звернень
    /// </summary>
    public static InlineKeyboardMarkup GetAppealCategoriesKeyboard()
    {
        return new InlineKeyboardMarkup(new[]
        {
            new[]
            {
                InlineKeyboardButton.WithCallbackData("💰 Стипендія", "appeal_cat_1"),
                InlineKeyboardButton.WithCallbackData("🏠 Гуртожиток", "appeal_cat_2")
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData("🎉 Заходи", "appeal_cat_3"),
                InlineKeyboardButton.WithCallbackData("💡 Пропозиція", "appeal_cat_4")
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData("⚠️ Скарга", "appeal_cat_5"),
                InlineKeyboardButton.WithCallbackData("📝 Інше", "appeal_cat_6")
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData("⬅️ Назад", "back_to_main")
            }
        });
    }

    /// <summary>
    /// Кнопка "Назад до головного меню"
    /// </summary>
    public static InlineKeyboardMarkup GetBackToMainMenuKeyboard()
    {
        return new InlineKeyboardMarkup(new[]
        {
            new[]
            {
                InlineKeyboardButton.WithCallbackData("⬅️ Головне меню", "back_to_main")
            }
        });
    }

    /// <summary>
    /// Клавіатура для конкретного звернення
    /// </summary>
    public static InlineKeyboardMarkup GetAppealDetailsKeyboard(int appealId, bool isActive)
    {
        var buttons = new List<InlineKeyboardButton[]>();

        if (isActive)
        {
            buttons.Add(new[]
            {
                InlineKeyboardButton.WithCallbackData("💬 Додати повідомлення", $"appeal_msg_{appealId}"),
                InlineKeyboardButton.WithCallbackData("✅ Закрити", $"appeal_close_{appealId}")
            });
        }

        buttons.Add(new[]
        {
            InlineKeyboardButton.WithCallbackData("🔄 Оновити", $"appeal_view_{appealId}"),
            InlineKeyboardButton.WithCallbackData("⬅️ Назад", "appeal_list")
        });

        return new InlineKeyboardMarkup(buttons);
    }

    /// <summary>
    /// Пагінація для списків
    /// </summary>
    public static InlineKeyboardMarkup GetPaginationKeyboard(
        string dataPrefix,
        int currentPage,
        int totalPages,
        string? backCallback = null)
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
                InlineKeyboardButton.WithCallbackData("⬅️ Назад", backCallback)
            });
        }

        return new InlineKeyboardMarkup(rows);
    }

    /// <summary>
    /// Підтвердження дії (Так/Ні)
    /// </summary>
    public static InlineKeyboardMarkup GetConfirmationKeyboard(string confirmCallback, string cancelCallback)
    {
        return new InlineKeyboardMarkup(new[]
        {
            new[]
            {
                InlineKeyboardButton.WithCallbackData("✅ Так", confirmCallback),
                InlineKeyboardButton.WithCallbackData("❌ Ні", cancelCallback)
            }
        });
    }

    // ==================== ADMIN KEYBOARDS ====================

    /// <summary>
    /// Головне меню адмін панелі
    /// </summary>
    public static InlineKeyboardMarkup GetAdminPanelKeyboard()
    {
        return new InlineKeyboardMarkup(new[]
        {
            new[]
            {
                InlineKeyboardButton.WithCallbackData("📋 Всі звернення", "admin_appeals_all"),
                InlineKeyboardButton.WithCallbackData("🆕 Нові", "admin_appeals_new")
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData("👤 Мої звернення", "admin_appeals_my"),
                InlineKeyboardButton.WithCallbackData("❓ Непризначені", "admin_appeals_unassigned")
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData("🔍 Пошук", "admin_appeals_search"),
                InlineKeyboardButton.WithCallbackData("📊 Статистика", "admin_stats")
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData("⬅️ Головне меню", "back_to_main")
            }
        });
    }

    /// <summary>
    /// Клавіатура для керування зверненням (адмін)
    /// </summary>
    public static InlineKeyboardMarkup GetAdminAppealActionsKeyboard(int appealId, bool isAssignedToMe, bool isClosed)
    {
        var buttons = new List<InlineKeyboardButton[]>();

        if (!isClosed)
        {
            // Відповісти
            buttons.Add(new[]
            {
                InlineKeyboardButton.WithCallbackData("💬 Відповісти", $"admin_reply_{appealId}")
            });

            // Призначення
            if (isAssignedToMe)
            {
                buttons.Add(new[]
                {
                    InlineKeyboardButton.WithCallbackData("❌ Зняти призначення", $"admin_unassign_{appealId}")
                });
            }
            else
            {
                buttons.Add(new[]
                {
                    InlineKeyboardButton.WithCallbackData("✋ Взяти в роботу", $"admin_assign_me_{appealId}")
                });
            }

            // Пріоритет та закриття
            buttons.Add(new[]
            {
                InlineKeyboardButton.WithCallbackData("⚡ Пріоритет", $"admin_priority_{appealId}"),
                InlineKeyboardButton.WithCallbackData("✅ Закрити", $"admin_close_{appealId}")
            });
        }

        // Навігація
        buttons.Add(new[]
        {
            InlineKeyboardButton.WithCallbackData("🔄 Оновити", $"admin_view_{appealId}"),
            InlineKeyboardButton.WithCallbackData("⬅️ Назад", "admin_panel")
        });

        return new InlineKeyboardMarkup(buttons);
    }

    /// <summary>
    /// Вибір пріоритету
    /// </summary>
    public static InlineKeyboardMarkup GetPrioritySelectionKeyboard(int appealId)
    {
        return new InlineKeyboardMarkup(new[]
        {
            new[]
            {
                InlineKeyboardButton.WithCallbackData("🟢 Низький", $"admin_set_priority_{appealId}_1"),
                InlineKeyboardButton.WithCallbackData("🟡 Нормальний", $"admin_set_priority_{appealId}_2")
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData("🟠 Високий", $"admin_set_priority_{appealId}_3"),
                InlineKeyboardButton.WithCallbackData("🔴 Терміновий", $"admin_set_priority_{appealId}_4")
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData("⬅️ Назад", $"admin_view_{appealId}")
            }
        });
    }

    /// <summary>
    /// Фільтри для звернень
    /// </summary>
    public static InlineKeyboardMarkup GetAdminAppealFiltersKeyboard(string currentFilter = "all")
    {
        return new InlineKeyboardMarkup(new[]
        {
            new[]
            {
                InlineKeyboardButton.WithCallbackData("📋 Всі", "admin_filter_all"),
                InlineKeyboardButton.WithCallbackData("🆕 Нові", "admin_filter_new")
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData("⏳ В роботі", "admin_filter_inprogress"),
                InlineKeyboardButton.WithCallbackData("✅ Закриті", "admin_filter_closed")
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData("⬅️ Адмін панель", "admin_panel")
            }
        });
    }
}
