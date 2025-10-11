using Telegram.Bot.Types.ReplyMarkups;
using StudentUnionBot.Domain.Enums;

namespace StudentUnionBot.Presentation.Bot.Helpers;

/// <summary>
/// Помічник для швидких шаблонів відповідей адміністраторів
/// </summary>
public static class AdminReplyTemplatesHelper
{
    /// <summary>
    /// Категорії шаблонів
    /// </summary>
    public enum TemplateCategory
    {
        Acknowledgment,   // Підтвердження отримання
        InProgress,       // В роботі
        NeedInfo,         // Потрібна додаткова інформація
        Resolved,         // Вирішено
        Rejection         // Відхилено
    }

    /// <summary>
    /// Шаблони відповідей за категоріями
    /// </summary>
    private static readonly Dictionary<TemplateCategory, List<string>> Templates = new()
    {
        [TemplateCategory.Acknowledgment] = new()
        {
            "✅ Ваше звернення прийнято в роботу. Очікуйте відповіді найближчим часом.",
            "✅ Дякуємо за звернення! Ми вже працюємо над вашим питанням.",
            "✅ Ваше питання прийнято. Відповідь буде надана протягом 1-2 робочих днів.",
            "✅ Отримали ваше звернення. Воно перебуває в обробці."
        },
        
        [TemplateCategory.InProgress] = new()
        {
            "⏳ Працюємо над вашим зверненням. Інформація буде надана найближчим часом.",
            "⏳ Ваше питання розглядається профільним спеціалістом.",
            "⏳ Збираємо необхідну інформацію для відповіді на ваше запитання.",
            "⏳ Звернення в роботі. Очікуйте оновлення."
        },
        
        [TemplateCategory.NeedInfo] = new()
        {
            "❓ Для вирішення вашого питання потрібна додаткова інформація. Будь ласка, надайте більше деталей.",
            "❓ Щоб допомогти вам, потрібно уточнити деякі моменти. Напишіть, будь ласка, детальніше.",
            "❓ Будь ласка, надайте більше інформації:\n- Факультет та курс\n- Дата виникнення питання\n- Інші важливі деталі",
            "❓ Для опрацювання звернення нам потрібні додаткові дані. Уточніть, будь ласка."
        },
        
        [TemplateCategory.Resolved] = new()
        {
            "✅ Ваше питання вирішено. Якщо виникнуть додаткові запитання - пишіть!",
            "✅ Проблему вирішено. Дякуємо за звернення до профкому!",
            "✅ Питання опрацьовано та вирішено. Приємного навчання!",
            "✅ Вирішено! Якщо потрібна додаткова допомога - звертайтесь."
        },
        
        [TemplateCategory.Rejection] = new()
        {
            "⚠️ На жаль, це питання не входить до компетенції профкому. Зверніться, будь ласка, до деканату.",
            "⚠️ Для вирішення цього питання потрібно звернутися безпосередньо до адміністрації університету.",
            "⚠️ Дане питання не можемо вирішити через відсутність необхідних повноважень.",
            "⚠️ На жаль, не можемо допомогти з цим питанням. Рекомендуємо звернутися до відповідального відділу."
        }
    };

    /// <summary>
    /// Спеціальні шаблони за категоріями звернень
    /// </summary>
    private static readonly Dictionary<AppealCategory, List<string>> CategorySpecificTemplates = new()
    {
        [AppealCategory.Scholarship] = new()
        {
            "💰 Щодо стипендії: документи передані до бухгалтерії. Очікуйте нарахування найближчим часом.",
            "💰 Ваше питання про стипендію опрацьовано. Перевірте статус в особистому кабінеті.",
            "💰 Для отримання стипендії потрібно надати довідку про успішність. Зверніться до деканату."
        },
        
        [AppealCategory.Dormitory] = new()
        {
            "🏠 Щодо гуртожитку: ваша заявка передана комендантові. Очікуйте відповіді.",
            "🏠 Питання по гуртожитку опрацьовано. Зверніться особисто до коменданта для уточнення деталей.",
            "🏠 Ваше звернення щодо поселення в гуртожиток прийнято в роботу."
        },
        
        [AppealCategory.Events] = new()
        {
            "🎉 Дякуємо за ідею заходу! Ми обговоримо її на найближчому засіданні профкому.",
            "🎉 Ваша пропозиція щодо заходу прийнята. Повідомимо про деталі найближчим часом.",
            "🎉 Чудова ідея! Працюємо над організацією, тримайте зв'язок."
        },
        
        [AppealCategory.Proposal] = new()
        {
            "💡 Дякуємо за пропозицію! Ми розглянемо її на найближчому засіданні.",
            "💡 Цікава ідея! Обговоримо можливості реалізації з адміністрацією.",
            "💡 Вашу пропозицію прийнято до розгляду. Повідомимо про рішення."
        },
        
        [AppealCategory.Complaint] = new()
        {
            "⚠️ Вашу скаргу прийнято. Проведемо розслідування та повідомимо про результати.",
            "⚠️ Дякуємо за інформацію. Питання буде розглянуте в першочерговому порядку.",
            "⚠️ Скаргу опрацьовано. Вживаємо необхідних заходів для вирішення ситуації."
        }
    };

    /// <summary>
    /// Отримує список шаблонів за категорією
    /// </summary>
    public static List<string> GetTemplates(TemplateCategory category)
    {
        return Templates.TryGetValue(category, out var templates) 
            ? new List<string>(templates) 
            : new List<string>();
    }

    /// <summary>
    /// Отримує спеціалізовані шаблони для категорії звернення
    /// </summary>
    public static List<string> GetCategoryTemplates(AppealCategory appealCategory)
    {
        return CategorySpecificTemplates.TryGetValue(appealCategory, out var templates)
            ? new List<string>(templates)
            : new List<string>();
    }

    /// <summary>
    /// Отримує всі шаблони для вибору
    /// </summary>
    public static Dictionary<string, string> GetAllTemplates(AppealCategory? appealCategory = null)
    {
        var result = new Dictionary<string, string>();
        int index = 1;

        // Додаємо категоризовані шаблони
        foreach (var category in Templates.Keys)
        {
            var categoryName = GetCategoryName(category);
            var templates = Templates[category];
            
            foreach (var template in templates)
            {
                result[$"template_{index}"] = $"[{categoryName}] {template}";
                index++;
            }
        }

        // Додаємо спеціальні шаблони для категорії звернення
        if (appealCategory.HasValue && CategorySpecificTemplates.ContainsKey(appealCategory.Value))
        {
            var specialTemplates = CategorySpecificTemplates[appealCategory.Value];
            foreach (var template in specialTemplates)
            {
                result[$"template_{index}"] = $"[Спеціальний] {template}";
                index++;
            }
        }

        return result;
    }

    /// <summary>
    /// Створює inline keyboard з шаблонами
    /// </summary>
    public static InlineKeyboardMarkup CreateTemplatesKeyboard(int appealId, AppealCategory? appealCategory = null)
    {
        var buttons = new List<List<InlineKeyboardButton>>();

        // Кнопки категорій шаблонів
        buttons.Add(new List<InlineKeyboardButton>
        {
            InlineKeyboardButton.WithCallbackData("✅ Підтвердження", $"admin_template_ack_{appealId}"),
            InlineKeyboardButton.WithCallbackData("⏳ В роботі", $"admin_template_progress_{appealId}")
        });

        buttons.Add(new List<InlineKeyboardButton>
        {
            InlineKeyboardButton.WithCallbackData("❓ Потрібна інфо", $"admin_template_needinfo_{appealId}"),
            InlineKeyboardButton.WithCallbackData("✅ Вирішено", $"admin_template_resolved_{appealId}")
        });

        // Якщо є категорія звернення - додаємо спеціальні шаблони
        if (appealCategory.HasValue && CategorySpecificTemplates.ContainsKey(appealCategory.Value))
        {
            buttons.Add(new List<InlineKeyboardButton>
            {
                InlineKeyboardButton.WithCallbackData($"⭐ {appealCategory.Value.GetEmoji()} Спеціальні", $"admin_template_special_{appealId}")
            });
        }

        // Кнопка "Назад"
        buttons.Add(new List<InlineKeyboardButton>
        {
            InlineKeyboardButton.WithCallbackData("🔙 Назад до звернення", $"admin_view_{appealId}")
        });

        return new InlineKeyboardMarkup(buttons);
    }

    /// <summary>
    /// Створює inline keyboard зі списком конкретних шаблонів категорії
    /// </summary>
    public static InlineKeyboardMarkup CreateCategoryTemplatesKeyboard(int appealId, TemplateCategory category)
    {
        var buttons = new List<List<InlineKeyboardButton>>();
        var templates = GetTemplates(category);

        for (int i = 0; i < templates.Count; i++)
        {
            var previewText = templates[i].Length > 50 
                ? templates[i].Substring(0, 47) + "..." 
                : templates[i];
            
            buttons.Add(new List<InlineKeyboardButton>
            {
                InlineKeyboardButton.WithCallbackData(previewText, $"admin_use_template_{appealId}_{(int)category}_{i}")
            });
        }

        // Кнопка "Назад"
        buttons.Add(new List<InlineKeyboardButton>
        {
            InlineKeyboardButton.WithCallbackData("🔙 Назад до категорій", $"admin_templates_{appealId}")
        });

        return new InlineKeyboardMarkup(buttons);
    }

    /// <summary>
    /// Створює inline keyboard зі спеціальними шаблонами для категорії звернення
    /// </summary>
    public static InlineKeyboardMarkup CreateSpecialTemplatesKeyboard(int appealId, AppealCategory appealCategory)
    {
        var buttons = new List<List<InlineKeyboardButton>>();
        var templates = GetCategoryTemplates(appealCategory);

        for (int i = 0; i < templates.Count; i++)
        {
            var previewText = templates[i].Length > 50 
                ? templates[i].Substring(0, 47) + "..." 
                : templates[i];
            
            buttons.Add(new List<InlineKeyboardButton>
            {
                InlineKeyboardButton.WithCallbackData(previewText, $"admin_use_special_template_{appealId}_{(int)appealCategory}_{i}")
            });
        }

        // Кнопка "Назад"
        buttons.Add(new List<InlineKeyboardButton>
        {
            InlineKeyboardButton.WithCallbackData("🔙 Назад до категорій", $"admin_templates_{appealId}")
        });

        return new InlineKeyboardMarkup(buttons);
    }

    /// <summary>
    /// Отримує назву категорії українською
    /// </summary>
    private static string GetCategoryName(TemplateCategory category)
    {
        return category switch
        {
            TemplateCategory.Acknowledgment => "Підтвердження",
            TemplateCategory.InProgress => "В роботі",
            TemplateCategory.NeedInfo => "Потрібна інфо",
            TemplateCategory.Resolved => "Вирішено",
            TemplateCategory.Rejection => "Відхилено",
            _ => "Інше"
        };
    }

    /// <summary>
    /// Отримує текст шаблону за індексами
    /// </summary>
    public static string? GetTemplateText(TemplateCategory category, int templateIndex)
    {
        var templates = GetTemplates(category);
        return templateIndex >= 0 && templateIndex < templates.Count 
            ? templates[templateIndex] 
            : null;
    }

    /// <summary>
    /// Отримує текст спеціального шаблону за індексами
    /// </summary>
    public static string? GetSpecialTemplateText(AppealCategory appealCategory, int templateIndex)
    {
        var templates = GetCategoryTemplates(appealCategory);
        return templateIndex >= 0 && templateIndex < templates.Count 
            ? templates[templateIndex] 
            : null;
    }
}
