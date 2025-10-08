namespace StudentUnionBot.Models;

public enum CommandType
{
    Unknown,
    Start,
    Help,
    Info,
    Appeal,
    Cancel,
    CancelAppeal,
    WriteToAppeal,
    Back,
    Dormitory,             // Розділ гуртожитку
    Opportunities,         // Розділ можливостей
    Partners,              // Партнери
    Events,                // Заходи
    SuggestEvent,          // Запропонувати захід
    
    // Команди для адміністратора
    AdminAppeals,           // Розділ "Звернення" для адміна
    AdminActiveAppeals,     // Активні звернення
    AdminClosedAppeals,     // Закриті звернення
    AdminPublishNews,       // Опублікувати оголошення
    AdminReplyToAppeal,     // Відповісти на звернення
    AdminUserInfo,          // Отримати дані користувача
    AdminCloseAppeal,       // Закрити звернення
    AdminStatistics,        // Статистика
    AdminExportUsers,       // Експорт користувачів у CSV
    AdminEditContacts,      // Редагувати контактну інформацію
    AdminEditPartners,      // Редагувати інформацію про партнерів
    AdminEditEvents,        // Редагувати інформацію про заходи
    PreviousPage,           // Попередня сторінка (пагінація)
    NextPage                // Наступна сторінка (пагінація)
}