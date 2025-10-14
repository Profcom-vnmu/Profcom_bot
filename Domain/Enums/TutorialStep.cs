namespace StudentUnionBot.Domain.Enums;

/// <summary>
/// Кроки туторіалу для нових користувачів
/// </summary>
public enum TutorialStep
{
    /// <summary>
    /// Не розпочато
    /// </summary>
    NotStarted = 0,
    
    /// <summary>
    /// Крок 1: Вітання та огляд можливостей
    /// </summary>
    Welcome = 1,
    
    /// <summary>
    /// Крок 2: Як створити звернення
    /// </summary>
    Appeals = 2,
    
    /// <summary>
    /// Крок 3: Події та реєстрація
    /// </summary>
    Events = 3,
    
    /// <summary>
    /// Крок 4: Новини та оновлення
    /// </summary>
    News = 4,
    
    /// <summary>
    /// Крок 5: Профіль користувача
    /// </summary>
    Profile = 5,
    
    /// <summary>
    /// Крок 6: Завершення туторіалу
    /// </summary>
    Completed = 6
}
