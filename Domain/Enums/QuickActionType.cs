namespace StudentUnionBot.Domain.Enums;

/// <summary>
/// Типи швидких дій для користувача
/// </summary>
public enum QuickActionType
{
    /// <summary>
    /// Створити звернення
    /// </summary>
    CreateAppeal = 1,
    
    /// <summary>
    /// Переглянути звернення
    /// </summary>
    ViewAppeals = 2,
    
    /// <summary>
    /// Переглянути новини
    /// </summary>
    ViewNews = 3,
    
    /// <summary>
    /// Переглянути події
    /// </summary>
    ViewEvents = 4,
    
    /// <summary>
    /// Мої зареєстровані події
    /// </summary>
    MyEvents = 5,
    
    /// <summary>
    /// Профіль
    /// </summary>
    Profile = 6,
    
    /// <summary>
    /// Навчальний туторіал
    /// </summary>
    Tutorial = 7,
    
    /// <summary>
    /// Переглянути конкретне звернення з новою відповіддю
    /// </summary>
    ViewAppealWithReply = 8,
    
    /// <summary>
    /// Переглянути подію, на яку зареєстрований
    /// </summary>
    ViewRegisteredEvent = 9
}
