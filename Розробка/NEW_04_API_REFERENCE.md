# 📚 API Reference та довідник

## 🎯 Quick Reference

### NuGet Packages

```xml
<!-- Core -->
<PackageReference Include="Microsoft.EntityFrameworkCore" Version="8.0.0" />
<PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="8.0.0" />
<PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="8.0.0" />
<PackageReference Include="Microsoft.EntityFrameworkCore.Sqlite" Version="8.0.0" />
<PackageReference Include="Telegram.Bot" Version="19.0.0" />

<!-- CQRS & Validation -->
<PackageReference Include="MediatR" Version="12.2.0" />
<PackageReference Include="FluentValidation" Version="11.9.0" />
<PackageReference Include="FluentValidation.DependencyInjectionExtensions" Version="11.9.0" />
<PackageReference Include="AutoMapper" Version="12.0.1" />
<PackageReference Include="AutoMapper.Extensions.Microsoft.DependencyInjection" Version="12.0.1" />

<!-- Logging -->
<PackageReference Include="Serilog" Version="3.1.1" />
<PackageReference Include="Serilog.AspNetCore" Version="8.0.0" />
<PackageReference Include="Serilog.Sinks.Console" Version="5.0.1" />
<PackageReference Include="Serilog.Sinks.File" Version="5.0.0" />

<!-- Caching -->
<PackageReference Include="StackExchange.Redis" Version="2.7.10" />
<PackageReference Include="Microsoft.Extensions.Caching.StackExchangeRedis" Version="8.0.0" />

<!-- Monitoring -->
<PackageReference Include="prometheus-net.AspNetCore" Version="8.2.0" />

<!-- Testing -->
<PackageReference Include="xunit" Version="2.6.2" />
<PackageReference Include="Moq" Version="4.20.70" />
<PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" Version="8.0.0" />
<PackageReference Include="FluentAssertions" Version="6.12.0" />

<!-- Rate Limiting -->
<PackageReference Include="AspNetCoreRateLimit" Version="5.0.0" />

<!-- Email -->
<PackageReference Include="MailKit" Version="4.3.0" />
<PackageReference Include="MimeKit" Version="4.3.0" />

<!-- Utilities -->
<PackageReference Include="Polly" Version="8.2.0" />
<PackageReference Include="Scrutor" Version="4.2.2" />
```

---

## 🔧 Configuration (appsettings.json)

### Development
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=Data/studentunion_dev.db"
  },
  "BotConfiguration": {
    "BotToken": "YOUR_BOT_TOKEN_HERE",
    "UseWebhook": false,
    "WebhookUrl": ""
  },
  "Redis": {
    "ConnectionString": "localhost:6379",
    "InstanceName": "StudentBot:"
  },
  "Email": {
    "SmtpHost": "smtp.gmail.com",
    "SmtpPort": 587,
    "From": "noreply@studentunion.edu",
    "EnableSsl": true,
    "Username": "",
    "Password": ""
  },
  "RateLimit": {
    "CreateAppeal": {
      "Period": "10m",
      "Limit": 1
    },
    "SendMessage": {
      "Period": "1m",
      "Limit": 10
    },
    "Commands": {
      "Period": "1m",
      "Limit": 30
    }
  },
  "Serilog": {
    "MinimumLevel": {
      "Default": "Debug",
      "Override": {
        "Microsoft": "Warning",
        "System": "Warning"
      }
    },
    "WriteTo": [
      {
        "Name": "Console",
        "Args": {
          "outputTemplate": "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}"
        }
      },
      {
        "Name": "File",
        "Args": {
          "path": "logs/bot-.txt",
          "rollingInterval": "Day",
          "retainedFileCountLimit": 7,
          "outputTemplate": "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}"
        }
      }
    ]
  },
  "Localization": {
    "DefaultLanguage": "uk",
    "SupportedLanguages": ["uk", "en"]
  },
  "Features": {
    "EmailVerification": true,
    "EventRegistration": true,
    "NewsScheduling": true,
    "AdminLogging": true
  }
}
```

### Production
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=postgres.render.com;Database=studentbot;Username=user;Password=pass"
  },
  "BotConfiguration": {
    "BotToken": "${BOT_TOKEN}",
    "UseWebhook": true,
    "WebhookUrl": "https://your-app.onrender.com/api/webhook"
  },
  "Redis": {
    "ConnectionString": "${REDIS_URL}",
    "InstanceName": "StudentBot:"
  },
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "System": "Warning"
      }
    }
  }
}
```

---

## 📦 Domain Entities Reference

### Appeal

```csharp
public class Appeal
{
    // Properties
    public int Id { get; private set; }
    public long StudentId { get; private set; }
    public string StudentName { get; private set; }
    public AppealCategory Category { get; private set; }
    public string Subject { get; private set; }
    public string Message { get; private set; }
    public AppealStatus Status { get; private set; }
    public AppealPriority Priority { get; private set; }
    public long? AssignedToAdminId { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public DateTime? FirstResponseAt { get; private set; }
    public DateTime? ClosedAt { get; private set; }
    public long? ClosedBy { get; private set; }
    public string? ClosedReason { get; private set; }
    public int? Rating { get; private set; }
    public string? RatingComment { get; private set; }
    
    // Navigation Properties
    public BotUser Student { get; private set; }
    public ICollection<AppealMessage> Messages { get; private set; }
    
    // Factory Methods
    public static Appeal Create(
        long studentId,
        string studentName,
        AppealCategory category,
        string subject,
        string message
    );
    
    // Business Methods
    public void AssignTo(long adminId);
    public void MarkInProgress();
    public void MarkWaitingForStudent();
    public void MarkWaitingForAdmin();
    public void Escalate();
    public void Close(long closedBy, string reason);
    public void Rate(int rating, string? comment);
    public void AddMessage(AppealMessage message);
}
```

### BotUser

```csharp
public class BotUser
{
    // Properties
    public long TelegramId { get; private set; }
    public string? Username { get; private set; }
    public string? FirstName { get; private set; }
    public string? LastName { get; private set; }
    public string? FullName { get; private set; }
    public string? Faculty { get; private set; }
    public int? Course { get; private set; }
    public string? Group { get; private set; }
    public string? Email { get; private set; }
    public bool IsEmailVerified { get; private set; }
    public string? VerificationCode { get; private set; }
    public DateTime? VerificationCodeExpiry { get; private set; }
    public string Language { get; private set; }
    public string? TimeZone { get; private set; }
    public string? NotificationSettings { get; private set; }
    public DateTime JoinedAt { get; private set; }
    public bool IsActive { get; private set; }
    public bool IsBanned { get; private set; }
    public string? BanReason { get; private set; }
    public DateTime? ProfileUpdatedAt { get; private set; }
    public DateTime? LastActivityAt { get; private set; }
    public UserRole Role { get; private set; }
    
    // Navigation Properties
    public ICollection<Appeal> Appeals { get; private set; }
    public ICollection<EventParticipant> EventParticipations { get; private set; }
    
    // Factory Methods
    public static BotUser Create(long telegramId, string? username, string? firstName);
    
    // Business Methods
    public void UpdateProfile(string? faculty, int? course, string? group, string? email);
    public void GenerateVerificationCode();
    public bool VerifyEmail(string code);
    public void SetLanguage(string language);
    public void UpdateNotificationSettings(NotificationSettings settings);
    public void Ban(string reason);
    public void Unban();
    public void UpdateActivity();
    public void PromoteToRole(UserRole role);
}
```

### News

```csharp
public class News
{
    // Properties
    public int Id { get; private set; }
    public string Title { get; private set; }
    public string Content { get; private set; }
    public NewsCategory Category { get; private set; }
    public NewsPriority Priority { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? PublishAt { get; private set; }
    public bool IsPublished { get; private set; }
    public bool IsPinned { get; private set; }
    public string? PhotoFileIds { get; private set; }
    public string? VideoUrl { get; private set; }
    public string? TargetCourses { get; private set; }
    public string? TargetFaculties { get; private set; }
    public int ViewCount { get; private set; }
    public long CreatedBy { get; private set; }
    public DateTime? UpdatedAt { get; private set; }
    
    // Factory Methods
    public static News Create(
        string title,
        string content,
        NewsCategory category,
        NewsPriority priority,
        long createdBy
    );
    
    // Business Methods
    public void Schedule(DateTime publishAt);
    public void Publish();
    public void Pin();
    public void Unpin();
    public void SetTargetAudience(List<int>? courses, List<string>? faculties);
    public void AddPhoto(string fileId);
    public void IncrementViewCount();
}
```

### Event

```csharp
public class Event
{
    // Properties
    public int Id { get; private set; }
    public string Title { get; private set; }
    public string Description { get; private set; }
    public EventCategory Category { get; private set; }
    public DateTime StartDate { get; private set; }
    public DateTime? EndDate { get; private set; }
    public string? Location { get; private set; }
    public int? MaxParticipants { get; private set; }
    public string? PhotoFileId { get; private set; }
    public bool IsActive { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public long CreatedBy { get; private set; }
    
    // Navigation Properties
    public ICollection<EventParticipant> Participants { get; private set; }
    
    // Computed Properties
    public int ParticipantsCount => Participants.Count(p => p.Status != ParticipantStatus.Cancelled);
    public bool IsFull => MaxParticipants.HasValue && ParticipantsCount >= MaxParticipants.Value;
    public bool HasStarted => DateTime.UtcNow >= StartDate;
    public bool HasEnded => EndDate.HasValue && DateTime.UtcNow >= EndDate.Value;
    
    // Factory Methods
    public static Event Create(
        string title,
        string description,
        EventCategory category,
        DateTime startDate,
        long createdBy
    );
    
    // Business Methods
    public Result RegisterParticipant(long userId);
    public Result CancelRegistration(long userId);
    public Result ConfirmParticipant(long userId);
    public void MarkAttended(long userId);
    public void Cancel();
}
```

---

## 🎭 Enums Reference

### AppealCategory
```csharp
public enum AppealCategory
{
    Scholarship = 1,    // Стипендія
    Dormitory = 2,      // Гуртожиток
    Events = 3,         // Заходи
    Proposal = 4,       // Пропозиція
    Complaint = 5,      // Скарга
    Other = 6          // Інше
}

// Extension methods
public static class AppealCategoryExtensions
{
    public static string GetDisplayName(this AppealCategory category) =>
        category switch
        {
            AppealCategory.Scholarship => "📚 Стипендія",
            AppealCategory.Dormitory => "🏠 Гуртожиток",
            AppealCategory.Events => "🎉 Заходи",
            AppealCategory.Proposal => "💡 Пропозиція",
            AppealCategory.Complaint => "⚠️ Скарга",
            AppealCategory.Other => "❓ Інше",
            _ => "Unknown"
        };
        
    public static string GetIcon(this AppealCategory category) =>
        category switch
        {
            AppealCategory.Scholarship => "📚",
            AppealCategory.Dormitory => "🏠",
            AppealCategory.Events => "🎉",
            AppealCategory.Proposal => "💡",
            AppealCategory.Complaint => "⚠️",
            AppealCategory.Other => "❓",
            _ => "❓"
        };
}
```

### AppealStatus
```csharp
public enum AppealStatus
{
    New = 1,                // Нове
    InProgress = 2,         // В роботі
    WaitingForStudent = 3,  // Очікує студента
    WaitingForAdmin = 4,    // Очікує адміна
    Escalated = 5,          // Ескальовано
    Resolved = 6,           // Вирішено
    Closed = 7             // Закрито
}

public static class AppealStatusExtensions
{
    public static string GetDisplayName(this AppealStatus status) =>
        status switch
        {
            AppealStatus.New => "🆕 Нове",
            AppealStatus.InProgress => "⚙️ В роботі",
            AppealStatus.WaitingForStudent => "⏳ Очікує студента",
            AppealStatus.WaitingForAdmin => "⏰ Очікує адміна",
            AppealStatus.Escalated => "🔺 Ескальовано",
            AppealStatus.Resolved => "✅ Вирішено",
            AppealStatus.Closed => "🔒 Закрито",
            _ => "Unknown"
        };
}
```

### UserRole
```csharp
public enum UserRole
{
    Student = 1,        // Студент
    Moderator = 2,      // Модератор
    Admin = 3,          // Адміністратор
    SuperAdmin = 4     // Супер-адміністратор
}

public static class UserRoleExtensions
{
    public static bool CanManageAppeals(this UserRole role) =>
        role >= UserRole.Admin;
        
    public static bool CanPublishNews(this UserRole role) =>
        role >= UserRole.Admin;
        
    public static bool CanManageUsers(this UserRole role) =>
        role == UserRole.SuperAdmin;
        
    public static bool CanViewAdminLogs(this UserRole role) =>
        role == UserRole.SuperAdmin;
}
```

---

## 🔌 Services Interface Reference

### IAppealRepository
```csharp
public interface IAppealRepository
{
    // Read
    Task<Appeal?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<List<Appeal>> GetAllAsync(CancellationToken ct = default);
    Task<List<Appeal>> GetActiveAppealsAsync(
        AppealCategory? category = null,
        CancellationToken ct = default
    );
    Task<List<Appeal>> GetClosedAppealsAsync(
        AppealCategory? category = null,
        CancellationToken ct = default
    );
    Task<List<Appeal>> GetUserAppealsAsync(
        long userId,
        CancellationToken ct = default
    );
    Task<Appeal?> GetActiveAppealForStudentAsync(
        long studentId,
        CancellationToken ct = default
    );
    Task<bool> HasActiveAppealAsync(
        long studentId,
        CancellationToken ct = default
    );
    Task<PagedResult<Appeal>> SearchAppealsAsync(
        string searchTerm,
        int page,
        int pageSize,
        CancellationToken ct = default
    );
    
    // Write
    Task AddAsync(Appeal appeal, CancellationToken ct = default);
    void Update(Appeal appeal);
    void Delete(Appeal appeal);
    
    // Statistics
    Task<int> GetActiveCountAsync(CancellationToken ct = default);
    Task<int> GetClosedCountAsync(CancellationToken ct = default);
    Task<Dictionary<AppealCategory, int>> GetCountByCategoryAsync(
        CancellationToken ct = default
    );
    Task<double> GetAverageResponseTimeAsync(CancellationToken ct = default);
}
```

### IUserRepository
```csharp
public interface IUserRepository
{
    // Read
    Task<BotUser?> GetByIdAsync(long telegramId, CancellationToken ct = default);
    Task<BotUser?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<List<BotUser>> GetAllAsync(CancellationToken ct = default);
    Task<List<BotUser>> GetActiveUsersAsync(CancellationToken ct = default);
    Task<List<BotUser>> GetAdminsAsync(CancellationToken ct = default);
    Task<bool> ExistsAsync(long telegramId, CancellationToken ct = default);
    
    // Write
    Task AddAsync(BotUser user, CancellationToken ct = default);
    void Update(BotUser user);
    void Delete(BotUser user);
    
    // Statistics
    Task<int> GetTotalCountAsync(CancellationToken ct = default);
    Task<int> GetActiveCountAsync(CancellationToken ct = default);
    Task<Dictionary<string, int>> GetCountByFacultyAsync(CancellationToken ct = default);
    Task<Dictionary<int, int>> GetCountByCourseAsync(CancellationToken ct = default);
}
```

### IEmailService
```csharp
public interface IEmailService
{
    Task<Result> SendVerificationCodeAsync(
        string email,
        string code,
        CancellationToken ct = default
    );
    
    Task<Result> SendWelcomeEmailAsync(
        string email,
        string name,
        CancellationToken ct = default
    );
    
    Task<Result> SendAppealNotificationAsync(
        string email,
        Appeal appeal,
        CancellationToken ct = default
    );
    
    Task<Result> SendBulkEmailAsync(
        List<string> emails,
        string subject,
        string body,
        CancellationToken ct = default
    );
}
```

### ICacheService
```csharp
public interface ICacheService
{
    Task<T?> GetAsync<T>(string key, CancellationToken ct = default);
    
    Task SetAsync<T>(
        string key,
        T value,
        TimeSpan? expiry = null,
        CancellationToken ct = default
    );
    
    Task<bool> ExistsAsync(string key, CancellationToken ct = default);
    
    Task RemoveAsync(string key, CancellationToken ct = default);
    
    Task RemoveByPrefixAsync(string prefix, CancellationToken ct = default);
    
    Task<T> GetOrSetAsync<T>(
        string key,
        Func<Task<T>> factory,
        TimeSpan? expiry = null,
        CancellationToken ct = default
    );
}
```

### IRateLimiter
```csharp
public interface IRateLimiter
{
    Task<bool> AllowAsync(
        long userId,
        string action,
        CancellationToken ct = default
    );
    
    Task<RateLimitInfo> GetInfoAsync(
        long userId,
        string action,
        CancellationToken ct = default
    );
    
    Task ResetAsync(
        long userId,
        string action,
        CancellationToken ct = default
    );
}

public record RateLimitInfo(
    int Limit,
    int Remaining,
    DateTime ResetAt
);
```

### INotificationService
```csharp
public interface INotificationService
{
    Task NotifyAdminsAboutNewAppealAsync(
        Appeal appeal,
        CancellationToken ct = default
    );
    
    Task NotifyStudentAboutReplyAsync(
        Appeal appeal,
        AppealMessage message,
        CancellationToken ct = default
    );
    
    Task NotifyAboutEscalationAsync(
        Appeal appeal,
        CancellationToken ct = default
    );
    
    Task BroadcastNewsAsync(
        News news,
        List<long> userIds,
        CancellationToken ct = default
    );
    
    Task SendReminderAsync(
        long userId,
        string message,
        CancellationToken ct = default
    );
}
```

---

## 🎹 Keyboard Templates

### Main Menu Keyboard
```csharp
public static class MainMenuKeyboard
{
    public static InlineKeyboardMarkup Create(UserRole role)
    {
        var buttons = new List<List<InlineKeyboardButton>>
        {
            new()
            {
                InlineKeyboardButton.WithCallbackData("📝 Мої звернення", "my_appeals"),
                InlineKeyboardButton.WithCallbackData("➕ Нове звернення", "new_appeal")
            },
            new()
            {
                InlineKeyboardButton.WithCallbackData("📰 Новини", "news"),
                InlineKeyboardButton.WithCallbackData("📅 Заходи", "events")
            },
            new()
            {
                InlineKeyboardButton.WithCallbackData("ℹ️ Інформація", "info"),
                InlineKeyboardButton.WithCallbackData("⚙️ Налаштування", "settings")
            }
        };
        
        if (role >= UserRole.Admin)
        {
            buttons.Add(new List<InlineKeyboardButton>
            {
                InlineKeyboardButton.WithCallbackData("👨‍💼 Адмін-панель", "admin_panel")
            });
        }
        
        return new InlineKeyboardMarkup(buttons);
    }
}
```

### Pagination Keyboard
```csharp
public static class PaginationKeyboard
{
    public static InlineKeyboardMarkup Create(
        int currentPage,
        int totalPages,
        string callbackPrefix)
    {
        var buttons = new List<InlineKeyboardButton>();
        
        // Previous button
        if (currentPage > 1)
        {
            buttons.Add(InlineKeyboardButton.WithCallbackData(
                "◀️ Назад",
                $"{callbackPrefix}:page:{currentPage - 1}"
            ));
        }
        
        // Page indicator
        buttons.Add(InlineKeyboardButton.WithCallbackData(
            $"{currentPage}/{totalPages}",
            "noop"
        ));
        
        // Next button
        if (currentPage < totalPages)
        {
            buttons.Add(InlineKeyboardButton.WithCallbackData(
                "Вперед ▶️",
                $"{callbackPrefix}:page:{currentPage + 1}"
            ));
        }
        
        return new InlineKeyboardMarkup(new[] { buttons });
    }
}
```

---

## 📊 Metrics & Monitoring

### Prometheus Metrics
```csharp
public static class BotMetrics
{
    private static readonly Counter MessagesProcessed = Metrics
        .CreateCounter(
            "bot_messages_processed_total",
            "Total number of messages processed"
        );
    
    private static readonly Counter AppealsCreated = Metrics
        .CreateCounter(
            "bot_appeals_created_total",
            "Total number of appeals created"
        );
    
    private static readonly Histogram ResponseTime = Metrics
        .CreateHistogram(
            "bot_response_time_seconds",
            "Response time in seconds",
            new HistogramConfiguration
            {
                Buckets = Histogram.LinearBuckets(start: 0.1, width: 0.1, count: 10)
            }
        );
    
    private static readonly Gauge ActiveUsers = Metrics
        .CreateGauge(
            "bot_active_users_count",
            "Number of active users"
        );
    
    public static void RecordMessageProcessed() => MessagesProcessed.Inc();
    public static void RecordAppealCreated() => AppealsCreated.Inc();
    public static IDisposable MeasureResponseTime() => ResponseTime.NewTimer();
    public static void SetActiveUsers(int count) => ActiveUsers.Set(count);
}

// Usage
using (BotMetrics.MeasureResponseTime())
{
    await HandleMessageAsync(message);
}
BotMetrics.RecordMessageProcessed();
```

---

## 🔍 Useful Extensions

### String Extensions
```csharp
public static class StringExtensions
{
    public static string Truncate(this string value, int maxLength)
    {
        if (string.IsNullOrEmpty(value)) return value;
        return value.Length <= maxLength ? value : value[..maxLength] + "...";
    }
    
    public static string SanitizeHtml(this string value)
    {
        return value
            .Replace("<", "&lt;")
            .Replace(">", "&gt;");
    }
    
    public static bool ContainsAny(this string value, params string[] values)
    {
        return values.Any(v => value.Contains(v, StringComparison.OrdinalIgnoreCase));
    }
}
```

### DateTime Extensions
```csharp
public static class DateTimeExtensions
{
    public static string ToRelativeTime(this DateTime dateTime)
    {
        var timeSpan = DateTime.UtcNow - dateTime;
        
        return timeSpan switch
        {
            { TotalMinutes: < 1 } => "щойно",
            { TotalMinutes: < 60 } => $"{(int)timeSpan.TotalMinutes} хв тому",
            { TotalHours: < 24 } => $"{(int)timeSpan.TotalHours} год тому",
            { TotalDays: < 7 } => $"{(int)timeSpan.TotalDays} дн тому",
            _ => dateTime.ToString("dd.MM.yyyy HH:mm")
        };
    }
    
    public static bool IsRecent(this DateTime dateTime, TimeSpan threshold)
    {
        return DateTime.UtcNow - dateTime < threshold;
    }
}
```

### Enum Extensions
```csharp
public static class EnumExtensions
{
    public static string GetDisplayName<T>(this T enumValue) where T : Enum
    {
        var field = enumValue.GetType().GetField(enumValue.ToString());
        var attribute = field?.GetCustomAttribute<DisplayAttribute>();
        return attribute?.Name ?? enumValue.ToString();
    }
    
    public static List<T> GetValues<T>() where T : Enum
    {
        return Enum.GetValues(typeof(T)).Cast<T>().ToList();
    }
}
```

---

## 🐛 Debugging Tips

### Logging Best Practices
```csharp
// ✅ Structured logging
_logger.LogInformation(
    "Appeal {AppealId} created by user {UserId} in category {Category}",
    appeal.Id,
    userId,
    category
);

// ✅ Log with scope
using (_logger.BeginScope("Processing appeal {AppealId}", appealId))
{
    _logger.LogInformation("Validating appeal");
    // ... operations ...
    _logger.LogInformation("Appeal saved successfully");
}

// ✅ Log exceptions with context
try
{
    await ProcessAsync();
}
catch (Exception ex)
{
    _logger.LogError(
        ex,
        "Failed to process appeal {AppealId} for user {UserId}",
        appealId,
        userId
    );
    throw;
}
```

### Common Issues & Solutions

**Issue: EF Core tracking errors**
```csharp
// Solution: Use AsNoTracking for read-only queries
var appeals = await _context.Appeals
    .AsNoTracking()
    .ToListAsync();
```

**Issue: N+1 query problem**
```csharp
// Solution: Use Include for eager loading
var appeals = await _context.Appeals
    .Include(a => a.Messages)
    .Include(a => a.Student)
    .ToListAsync();
```

**Issue: Telegram API rate limits**
```csharp
// Solution: Add delay between messages
foreach (var userId in userIds)
{
    await _botClient.SendTextMessageAsync(userId, message);
    await Task.Delay(50); // 50ms delay
}
```

---

## 🔐 Authorization Service API

### IAuthorizationService Interface

```csharp
public interface IAuthorizationService
{
    /// <summary>
    /// Перевірити чи має користувач конкретний дозвіл
    /// </summary>
    /// <param name="userId">Telegram ID користувача</param>
    /// <param name="permission">Дозвіл для перевірки</param>
    /// <param name="cancellationToken">Токен скасування</param>
    /// <returns>true якщо користувач має дозвіл, false в іншому випадку</returns>
    Task<bool> HasPermissionAsync(
        long userId, 
        Permission permission, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Перевірити чи має користувач будь-який з переданих дозволів
    /// </summary>
    /// <param name="userId">Telegram ID користувача</param>
    /// <param name="cancellationToken">Токен скасування</param>
    /// <param name="permissions">Список дозволів для перевірки</param>
    /// <returns>true якщо користувач має хоча б один дозвіл</returns>
    Task<bool> HasAnyPermissionAsync(
        long userId, 
        CancellationToken cancellationToken = default, 
        params Permission[] permissions);
    
    /// <summary>
    /// Перевірити чи має користувач всі передані дозволи
    /// </summary>
    /// <param name="userId">Telegram ID користувача</param>
    /// <param name="cancellationToken">Токен скасування</param>
    /// <param name="permissions">Список дозволів для перевірки</param>
    /// <returns>true якщо користувач має всі дозволи</returns>
    Task<bool> HasAllPermissionsAsync(
        long userId, 
        CancellationToken cancellationToken = default, 
        params Permission[] permissions);
    
    /// <summary>
    /// Отримати всі дозволи користувача
    /// </summary>
    /// <param name="userId">Telegram ID користувача</param>
    /// <param name="cancellationToken">Токен скасування</param>
    /// <returns>Список всіх дозволів користувача</returns>
    Task<IReadOnlyList<Permission>> GetUserPermissionsAsync(
        long userId, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Отримати роль користувача
    /// </summary>
    /// <param name="userId">Telegram ID користувача</param>
    /// <param name="cancellationToken">Токен скасування</param>
    /// <returns>Роль користувача або null якщо користувач не знайдений</returns>
    Task<UserRole?> GetUserRoleAsync(
        long userId, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Перевірити чи є користувач адміністратором (Admin або SuperAdmin)
    /// </summary>
    /// <param name="userId">Telegram ID користувача</param>
    /// <param name="cancellationToken">Токен скасування</param>
    /// <returns>true якщо користувач є Admin або SuperAdmin</returns>
    Task<bool> IsAdminAsync(
        long userId, 
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Перевірити чи є користувач суперадміністратором
    /// </summary>
    /// <param name="userId">Telegram ID користувача</param>
    /// <param name="cancellationToken">Токен скасування</param>
    /// <returns>true якщо користувач є SuperAdmin</returns>
    Task<bool> IsSuperAdminAsync(
        long userId, 
        CancellationToken cancellationToken = default);
}
```

### Приклади використання

**Перевірка одного дозволу:**
```csharp
var canCreateNews = await _authorizationService.HasPermissionAsync(
    userId, 
    Permission.CreateNews, 
    cancellationToken);

if (!canCreateNews)
{
    return Result.Fail("Недостатньо прав для створення новин");
}
```

**Перевірка будь-якого з дозволів:**
```csharp
// Користувач може редагувати AБОВОСТИ створювати
var canModifyNews = await _authorizationService.HasAnyPermissionAsync(
    userId,
    cancellationToken,
    Permission.EditNews,
    Permission.CreateNews);
```

**Перевірка всіх дозволів:**
```csharp
// Користувач повинен мати обидва дозволи
var canDeleteAndManage = await _authorizationService.HasAllPermissionsAsync(
    userId,
    cancellationToken,
    Permission.DeleteNews,
    Permission.ManageUsers);
```

**Отримання списку всіх дозволів:**
```csharp
var permissions = await _authorizationService.GetUserPermissionsAsync(
    userId, 
    cancellationToken);

foreach (var permission in permissions)
{
    Console.WriteLine($"- {permission.GetDisplayName()}");
}
```

**Перевірка ролі:**
```csharp
var role = await _authorizationService.GetUserRoleAsync(userId);
if (role == UserRole.Admin || role == UserRole.SuperAdmin)
{
    // Показати адмін панель
}
```

**Перевірка адміністратора:**
```csharp
var isAdmin = await _authorizationService.IsAdminAsync(userId);
if (!isAdmin)
{
    await _botClient.SendTextMessageAsync(
        userId,
        "❌ Ця команда доступна тільки для адміністраторів");
    return;
}
```

### Authorization Attributes

**RequirePermissionAttribute:**
```csharp
// Один дозвіл
[RequirePermission(Permission.CreateNews)]
public class CreateNewsCommand : IRequest<Result<NewsDto>> { }

// Основний дозвіл + альтернативи
[RequirePermission(Permission.EditNews, Permission.CreateNews)]
public class UpdateNewsCommand : IRequest<Result<NewsDto>> { }
```

**RequireAllPermissionsAttribute:**
```csharp
// Всі дозволи обов'язкові
[RequireAllPermissions(Permission.DeleteNews, Permission.ManageUsers)]
public class DeleteAllNewsCommand : IRequest<Result<bool>> { }
```

**RequireAdminAttribute:**
```csharp
// Тільки Admin або SuperAdmin
[RequireAdmin]
public class ViewAdminPanelQuery : IRequest<Result<AdminPanelDto>> { }
```

**RequireSuperAdminAttribute:**
```csharp
// Тільки SuperAdmin
[RequireSuperAdmin]
public class ManageSystemCommand : IRequest<Result<bool>> { }
```

### Permission Extensions

**GetPermissions() - отримати дозволи ролі:**
```csharp
var adminPermissions = UserRole.Admin.GetPermissions();
// Повертає список всіх дозволів для ролі Admin
```

**HasPermission() - перевірка дозволу:**
```csharp
var canDelete = UserRole.Moderator.HasPermission(Permission.DeleteNews);
// false - Moderator не має цього дозволу
```

**HasAnyPermission() - перевірка будь-якого:**
```csharp
var canModify = UserRole.Moderator.HasAnyPermission(
    Permission.CreateNews,
    Permission.EditNews);
// true - Moderator має обидва ці дозволи
```

**HasAllPermissions() - перевірка всіх:**
```csharp
var hasAll = UserRole.Student.HasAllPermissions(
    Permission.ViewNews,
    Permission.CreateNews);
// false - Student не має CreateNews
```

**GetDisplayName() - українська назва:**
```csharp
var displayName = Permission.CreateNews.GetDisplayName();
// "Створення новин"
```

### Role Permissions Matrix

| Permission | Student | Moderator | Admin | SuperAdmin |
|------------|---------|-----------|-------|------------|
| ViewProfile | ✅ | ✅ | ✅ | ✅ |
| CreateAppeal | ✅ | ✅ | ✅ | ✅ |
| ViewNews | ✅ | ✅ | ✅ | ✅ |
| CreateNews | ❌ | ✅ | ✅ | ✅ |
| DeleteNews | ❌ | ❌ | ✅ | ✅ |
| AssignAppeal | ❌ | ❌ | ✅ | ✅ |
| ManageUsers | ❌ | ❌ | ✅ | ✅ |
| ManageSystem | ❌ | ❌ | ❌ | ✅ |

---

**Версія документа:** 2.1  
**Дата:** 11.10.2025  
**Автор:** AI Assistant  
**Призначення:** API довідник та швидкий reference guide
