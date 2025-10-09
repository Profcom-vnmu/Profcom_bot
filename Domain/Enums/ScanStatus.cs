namespace StudentUnionBot.Domain.Enums;

/// <summary>
/// Статус сканування файла на віруси
/// </summary>
public enum ScanStatus
{
    /// <summary>
    /// Очікує сканування
    /// </summary>
    Pending = 0,

    /// <summary>
    /// Безпечний файл
    /// </summary>
    Safe = 1,

    /// <summary>
    /// Виявлено загрозу
    /// </summary>
    Threat = 2,

    /// <summary>
    /// Помилка сканування
    /// </summary>
    Error = 3,

    /// <summary>
    /// Сканування пропущено (наприклад, файл занадто великий)
    /// </summary>
    Skipped = 4
}

/// <summary>
/// Extension methods для ScanStatus
/// </summary>
public static class ScanStatusExtensions
{
    /// <summary>
    /// Отримати іконку для статусу сканування
    /// </summary>
    public static string GetIcon(this ScanStatus status)
    {
        return status switch
        {
            ScanStatus.Pending => "⏳",
            ScanStatus.Safe => "✅",
            ScanStatus.Threat => "⚠️",
            ScanStatus.Error => "❌",
            ScanStatus.Skipped => "⏭️",
            _ => "❓"
        };
    }

    /// <summary>
    /// Отримати назву статусу
    /// </summary>
    public static string GetDisplayName(this ScanStatus status)
    {
        return status switch
        {
            ScanStatus.Pending => "Очікує перевірки",
            ScanStatus.Safe => "Безпечний",
            ScanStatus.Threat => "Виявлено загрозу",
            ScanStatus.Error => "Помилка перевірки",
            ScanStatus.Skipped => "Перевірка пропущена",
            _ => "Невідомий статус"
        };
    }

    /// <summary>
    /// Перевірити чи безпечний статус для завантаження
    /// </summary>
    public static bool IsSafeToDownload(this ScanStatus status)
    {
        return status == ScanStatus.Safe || status == ScanStatus.Skipped;
    }

    /// <summary>
    /// Перевірити чи завершено сканування
    /// </summary>
    public static bool IsCompleted(this ScanStatus status)
    {
        return status != ScanStatus.Pending;
    }
}