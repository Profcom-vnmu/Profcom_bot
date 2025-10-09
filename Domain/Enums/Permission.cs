namespace StudentUnionBot.Domain.Enums;

/// <summary>
/// Дозволи в системі для авторизації дій
/// </summary>
public enum Permission
{
    // User permissions
    ViewProfile = 1,
    EditProfile = 2,
    CreateAppeal = 3,
    ViewOwnAppeals = 4,
    
    // News permissions
    ViewNews = 10,
    CreateNews = 11,
    EditNews = 12,
    DeleteNews = 13,
    PublishNews = 14,
    PinNews = 15,
    
    // Events permissions
    ViewEvents = 20,
    CreateEvent = 21,
    EditEvent = 22,
    DeleteEvent = 23,
    PublishEvent = 24,
    RegisterForEvent = 25,
    UnregisterFromEvent = 26,
    ViewEventRegistrations = 27,
    ManageEventRegistrations = 28,
    
    // Appeals permissions
    ViewAppeals = 30,
    ViewAllAppeals = 31,
    AssignAppeal = 32,
    ReplyToAppeal = 33,
    CloseAppeal = 34,
    ReopenAppeal = 35,
    EditAppealPriority = 36,
    DeleteAppeal = 37,
    
    // Admin permissions
    ViewAdminPanel = 40,
    ManageUsers = 41,
    PromoteUsers = 42,
    BanUsers = 43,
    ViewUserActivity = 44,
    ViewStatistics = 45,
    CreateBackup = 46,
    RestoreBackup = 47,
    
    // File permissions
    UploadFile = 50,
    DownloadFile = 51,
    DeleteFile = 52,
    ViewFileInfo = 53,
    
    // System permissions
    ViewLogs = 60,
    ManageSystem = 61,
    ManageConfiguration = 62,
    ViewHealthChecks = 63,
    
    // Contact permissions
    ViewContacts = 70,
    EditContacts = 71,
    
    // Partner permissions
    ViewPartners = 80,
    ManagePartners = 81,
    
    // Notification permissions
    SendNotifications = 90,
    ViewNotificationHistory = 91
}

/// <summary>
/// Методи розширення для Permission
/// </summary>
public static class PermissionExtensions
{
    /// <summary>
    /// Отримати список дозволів для ролі
    /// </summary>
    public static IReadOnlyList<Permission> GetPermissions(this UserRole role)
    {
        return role switch
        {
            UserRole.Student => new List<Permission>
            {
                Permission.ViewProfile,
                Permission.EditProfile,
                Permission.CreateAppeal,
                Permission.ViewOwnAppeals,
                Permission.ViewNews,
                Permission.ViewEvents,
                Permission.RegisterForEvent,
                Permission.UnregisterFromEvent,
                Permission.ViewContacts,
                Permission.ViewPartners,
                Permission.UploadFile,
                Permission.DownloadFile,
                Permission.ViewFileInfo
            },
            
            UserRole.Moderator => new List<Permission>
            {
                // All Student permissions
                Permission.ViewProfile,
                Permission.EditProfile,
                Permission.CreateAppeal,
                Permission.ViewOwnAppeals,
                Permission.ViewNews,
                Permission.ViewEvents,
                Permission.RegisterForEvent,
                Permission.UnregisterFromEvent,
                Permission.ViewContacts,
                Permission.ViewPartners,
                Permission.UploadFile,
                Permission.DownloadFile,
                Permission.ViewFileInfo,
                
                // Plus Moderator-specific
                Permission.ViewAppeals,
                Permission.ReplyToAppeal,
                Permission.ViewEventRegistrations,
                Permission.CreateNews,
                Permission.EditNews,
                Permission.CreateEvent,
                Permission.EditEvent,
                Permission.ViewUserActivity
            },
            
            UserRole.Admin => new List<Permission>
            {
                // All Moderator permissions
                Permission.ViewProfile,
                Permission.EditProfile,
                Permission.CreateAppeal,
                Permission.ViewOwnAppeals,
                Permission.ViewNews,
                Permission.ViewEvents,
                Permission.RegisterForEvent,
                Permission.UnregisterFromEvent,
                Permission.ViewContacts,
                Permission.ViewPartners,
                Permission.UploadFile,
                Permission.DownloadFile,
                Permission.ViewFileInfo,
                Permission.ViewAppeals,
                Permission.ReplyToAppeal,
                Permission.ViewEventRegistrations,
                Permission.CreateNews,
                Permission.EditNews,
                Permission.CreateEvent,
                Permission.EditEvent,
                Permission.ViewUserActivity,
                
                // Plus Admin-specific
                Permission.ViewAllAppeals,
                Permission.AssignAppeal,
                Permission.CloseAppeal,
                Permission.ReopenAppeal,
                Permission.EditAppealPriority,
                Permission.DeleteAppeal,
                Permission.DeleteNews,
                Permission.PublishNews,
                Permission.PinNews,
                Permission.DeleteEvent,
                Permission.PublishEvent,
                Permission.ManageEventRegistrations,
                Permission.ViewAdminPanel,
                Permission.ManageUsers,
                Permission.BanUsers,
                Permission.ViewStatistics,
                Permission.CreateBackup,
                Permission.EditContacts,
                Permission.ManagePartners,
                Permission.SendNotifications,
                Permission.ViewNotificationHistory,
                Permission.DeleteFile
            },
            
            UserRole.SuperAdmin => Enum.GetValues<Permission>().ToList(),
            
            _ => new List<Permission>()
        };
    }

    /// <summary>
    /// Перевірити чи має роль конкретний дозвіл
    /// </summary>
    public static bool HasPermission(this UserRole role, Permission permission)
    {
        return role.GetPermissions().Contains(permission);
    }
    
    /// <summary>
    /// Перевірити чи має роль будь-який з переданих дозволів
    /// </summary>
    public static bool HasAnyPermission(this UserRole role, params Permission[] permissions)
    {
        var rolePermissions = role.GetPermissions();
        return permissions.Any(p => rolePermissions.Contains(p));
    }
    
    /// <summary>
    /// Перевірити чи має роль всі передані дозволи
    /// </summary>
    public static bool HasAllPermissions(this UserRole role, params Permission[] permissions)
    {
        var rolePermissions = role.GetPermissions();
        return permissions.All(p => rolePermissions.Contains(p));
    }

    /// <summary>
    /// Отримати назву дозволу
    /// </summary>
    public static string GetDisplayName(this Permission permission)
    {
        return permission switch
        {
            Permission.ViewProfile => "Перегляд профілю",
            Permission.EditProfile => "Редагування профілю",
            Permission.CreateAppeal => "Створення звернень",
            Permission.ViewOwnAppeals => "Перегляд власних звернень",
            
            Permission.ViewNews => "Перегляд новин",
            Permission.CreateNews => "Створення новин",
            Permission.EditNews => "Редагування новин",
            Permission.DeleteNews => "Видалення новин",
            Permission.PublishNews => "Публікація новин",
            Permission.PinNews => "Закріплення новин",
            
            Permission.ViewEvents => "Перегляд подій",
            Permission.CreateEvent => "Створення подій",
            Permission.EditEvent => "Редагування подій",
            Permission.DeleteEvent => "Видалення подій",
            Permission.PublishEvent => "Публікація подій",
            Permission.RegisterForEvent => "Реєстрація на події",
            Permission.UnregisterFromEvent => "Скасування реєстрації",
            Permission.ViewEventRegistrations => "Перегляд реєстрацій",
            Permission.ManageEventRegistrations => "Управління реєстраціями",
            
            Permission.ViewAppeals => "Перегляд звернень",
            Permission.ViewAllAppeals => "Перегляд всіх звернень",
            Permission.AssignAppeal => "Призначення звернень",
            Permission.ReplyToAppeal => "Відповідь на звернення",
            Permission.CloseAppeal => "Закриття звернень",
            Permission.ReopenAppeal => "Відкриття звернень",
            Permission.EditAppealPriority => "Зміна пріоритету звернень",
            Permission.DeleteAppeal => "Видалення звернень",
            
            Permission.ViewAdminPanel => "Адмін панель",
            Permission.ManageUsers => "Управління користувачами",
            Permission.PromoteUsers => "Надання ролей",
            Permission.BanUsers => "Блокування користувачів",
            Permission.ViewUserActivity => "Перегляд активності користувачів",
            Permission.ViewStatistics => "Перегляд статистики",
            Permission.CreateBackup => "Створення резервних копій",
            Permission.RestoreBackup => "Відновлення резервних копій",
            
            Permission.UploadFile => "Завантаження файлів",
            Permission.DownloadFile => "Скачування файлів",
            Permission.DeleteFile => "Видалення файлів",
            Permission.ViewFileInfo => "Перегляд інформації про файли",
            
            Permission.ViewLogs => "Перегляд логів",
            Permission.ManageSystem => "Управління системою",
            Permission.ManageConfiguration => "Управління конфігурацією",
            Permission.ViewHealthChecks => "Перегляд стану системи",
            
            Permission.ViewContacts => "Перегляд контактів",
            Permission.EditContacts => "Редагування контактів",
            
            Permission.ViewPartners => "Перегляд партнерів",
            Permission.ManagePartners => "Управління партнерами",
            
            Permission.SendNotifications => "Відправка повідомлень",
            Permission.ViewNotificationHistory => "Історія повідомлень",
            
            _ => permission.ToString()
        };
    }
}