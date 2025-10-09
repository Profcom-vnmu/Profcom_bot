using StudentUnionBot.Domain.Enums;

namespace StudentUnionBot.Domain.Interfaces;

/// <summary>
/// Сервіс локалізації для підтримки багатомовності
/// </summary>
public interface ILocalizationService
{
    /// <summary>
    /// Отримати локалізований текст за ключем
    /// </summary>
    Task<string> GetLocalizedStringAsync(string key, Language language, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Отримати локалізований текст з параметрами
    /// </summary>
    Task<string> GetLocalizedStringAsync(string key, Language language, object[] args, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Перевірити, чи існує ключ локалізації
    /// </summary>
    Task<bool> HasKeyAsync(string key, Language language, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Отримати всі доступні мови
    /// </summary>
    Task<List<Language>> GetAvailableLanguagesAsync(CancellationToken cancellationToken = default);
}