namespace StudentUnionBot.Services;

public class UserState
{
    public DialogState DialogState { get; set; } = DialogState.None;
    public MenuState CurrentMenu { get; set; } = MenuState.Main;
    public int? LastViewedMessageId { get; set; } = null;
    public int? AppealHistoryMessageId { get; set; } = null; // ID повідомлення з історією звернення
    
    // Для адміністратора
    public int? SelectedAppealId { get; set; } = null; // ID обраного звернення для перегляду
    public string AdminAppealsFilter { get; set; } = "Active"; // "Active" або "Closed"
    
    // Пагінація для списків звернень
    public int CurrentPage { get; set; } = 0; // Поточна сторінка (починається з 0)
    public int PageSize { get; set; } = 5;     // Кількість звернень на сторінці
    
    // Для публікації новин
    public string? NewsTitle { get; set; }
    public string? NewsContent { get; set; }
    public string? NewsPhotoFileId { get; set; }
    public List<int> SelectedCourses { get; set; } = new();
    public List<string> SelectedFaculties { get; set; } = new();
    
    public Dictionary<string, object> Data { get; set; } = new();
}

public enum DialogState
{
    None,
    CreatingAppeal,
    ConfirmingAppeal,
    WritingToAppeal,
    ViewingAppeals,
    RespondingToAppeal,
    ViewingNews,
    CreatingNews,
    CreatingNewsWithPhoto,     // Очікування фото для новини
    ChoosingNewsAudience,      // Вибір аудиторії для розсилки (курси/факультети)
    EditingContactInfo,        // Редагування контактної інформації
    EditingPartnersInfo,       // Редагування інформації про партнерів
    EditingEventsInfo,         // Редагування інформації про заходи
    AdminReplyingToAppeal,     // Адмін пише відповідь на звернення
    ClosingAppeal,             // Введення причини закриття звернення
    AdminClosingAppeal         // Адмін вводит причину закриття
}

public enum MenuState
{
    Main,
    Appeals,
    Help,
    Info,
    Dormitory,             // Розділ гуртожитку
    Opportunities,         // Розділ можливостей
    AdminAppeals,          // Розділ звернень для адміна
    AdminPublishNews,      // Розділ публікації оголошень
    AdminStatistics,       // Розділ статистики
    AdminEditContacts      // Розділ редагування контактів
}