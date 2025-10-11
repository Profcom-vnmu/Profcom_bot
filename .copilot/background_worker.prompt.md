# Background Worker Development Prompt

**Мета:** Інструкції для створення фонових сервісів (Background Services) у StudentUnionBot для планових задач, моніторингу, очищення даних.

---

## Коли використовувати Background Services?

✅ **Використовуй для:**
- Планові задачі (щоденні нагадування, резервні копії)
- Моніторинг стану системи (health checks)
- Очищення старих даних (логи, файли)
- Відправка відкладених повідомлень
- Перевірка deadlines (терміни звернень)

❌ **НЕ використовуй для:**
- Обробки запитів користувачів (використовуй Handlers)
- Короткочасних задач (використовуй MediatR Commands)
- Реакції на події в реальному часі (використовуй Telegram Update Handlers)

---

## Базовий шаблон Background Service

```csharp
public class {TaskName}BackgroundService : BackgroundService
{
    private readonly ILogger<{TaskName}BackgroundService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly TimeSpan _interval;

    public {TaskName}BackgroundService(
        ILogger<{TaskName}BackgroundService> logger,
        IServiceProvider serviceProvider,
        IConfiguration configuration)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        
        // Read interval from appsettings.json
        _interval = TimeSpan.FromMinutes(
            configuration.GetValue<int>("BackgroundServices:{TaskName}:IntervalMinutes", 60)
        );
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("{ServiceName} started", nameof({TaskName}BackgroundService));

        // Wait for application to start completely
        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await DoWorkAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in {ServiceName}", nameof({TaskName}BackgroundService));
            }

            // Wait before next iteration
            await Task.Delay(_interval, stoppingToken);
        }

        _logger.LogInformation("{ServiceName} stopped", nameof({TaskName}BackgroundService));
    }

    private async Task DoWorkAsync(CancellationToken cancellationToken)
    {
        _logger.LogDebug("{ServiceName} executing at {Time}", 
            nameof({TaskName}BackgroundService), DateTime.UtcNow);

        // Create scope for scoped services (DbContext, Repositories)
        using var scope = _serviceProvider.CreateScope();
        
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        // or get specific services:
        // var repository = scope.ServiceProvider.GetRequiredService<IAppealRepository>();

        // Execute your logic here
        var command = new {YourCommand}();
        var result = await mediator.Send(command, cancellationToken);

        if (result.IsSuccess)
        {
            _logger.LogInformation("{ServiceName} completed successfully", 
                nameof({TaskName}BackgroundService));
        }
        else
        {
            _logger.LogWarning("{ServiceName} failed: {Error}", 
                nameof({TaskName}BackgroundService), result.ErrorMessage);
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("{ServiceName} is stopping", nameof({TaskName}BackgroundService));
        await base.StopAsync(cancellationToken);
    }
}
```

---

## Реєстрація у Program.cs

```csharp
// У Program.cs
builder.Services.AddHostedService<{TaskName}BackgroundService>();
```

---

## Конфігурація у appsettings.json

```json
{
  "BackgroundServices": {
    "{TaskName}": {
      "Enabled": true,
      "IntervalMinutes": 60,
      "StartDelay": 5
    },
    "DatabaseCleanup": {
      "Enabled": true,
      "IntervalMinutes": 1440,
      "RetentionDays": 90
    }
  }
}
```

---

## Приклад: Database Cleanup Service

```csharp
public class DatabaseCleanupBackgroundService : BackgroundService
{
    private readonly ILogger<DatabaseCleanupBackgroundService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly TimeSpan _interval;
    private readonly int _retentionDays;

    public DatabaseCleanupBackgroundService(
        ILogger<DatabaseCleanupBackgroundService> logger,
        IServiceProvider serviceProvider,
        IConfiguration configuration)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
        
        var config = configuration.GetSection("BackgroundServices:DatabaseCleanup");
        _interval = TimeSpan.FromMinutes(config.GetValue<int>("IntervalMinutes", 1440)); // 24h
        _retentionDays = config.GetValue<int>("RetentionDays", 90);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Database Cleanup Service started. Running every {Interval}", _interval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CleanupOldDataAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during database cleanup");
            }

            await Task.Delay(_interval, stoppingToken);
        }
    }

    private async Task CleanupOldDataAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<BotDbContext>();

        var cutoffDate = DateTime.UtcNow.AddDays(-_retentionDays);

        // Delete old notifications
        var oldNotifications = await context.Notifications
            .Where(n => n.CreatedAt < cutoffDate && n.IsSent)
            .ToListAsync(cancellationToken);

        context.Notifications.RemoveRange(oldNotifications);

        // Delete old file attachments
        var oldFiles = await context.FileAttachments
            .Where(f => f.CreatedAt < cutoffDate)
            .ToListAsync(cancellationToken);

        context.FileAttachments.RemoveRange(oldFiles);

        var deletedCount = await context.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Deleted {Count} old records (older than {Days} days)", 
            deletedCount, _retentionDays);
    }
}
```

---

## Приклад: Appeal Reminder Service

```csharp
public class AppealReminderBackgroundService : BackgroundService
{
    private readonly ILogger<AppealReminderBackgroundService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly TimeSpan _checkInterval = TimeSpan.FromHours(6);

    public AppealReminderBackgroundService(
        ILogger<AppealReminderBackgroundService> logger,
        IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Appeal Reminder Service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckOverdueAppealsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking overdue appeals");
            }

            await Task.Delay(_checkInterval, stoppingToken);
        }
    }

    private async Task CheckOverdueAppealsAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        // Use MediatR command to send reminders
        var command = new SendOverdueAppealRemindersCommand();
        var result = await mediator.Send(command, cancellationToken);

        if (result.IsSuccess)
        {
            _logger.LogInformation("Sent {Count} appeal reminders", result.Value);
        }
    }
}
```

---

## Важливі правила

### 1. Scoped Services
```csharp
// ❌ НЕ роби так - DbContext має Scoped lifetime
public class MyService : BackgroundService
{
    private readonly BotDbContext _context; // WRONG!
}

// ✅ Правильно - створюй scope
using var scope = _serviceProvider.CreateScope();
var context = scope.ServiceProvider.GetRequiredService<BotDbContext>();
```

### 2. Exception Handling
```csharp
// ✅ Обгортай try-catch, щоб сервіс не падав
while (!stoppingToken.IsCancellationRequested)
{
    try
    {
        await DoWorkAsync(stoppingToken);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error in background service");
        // Service continues running
    }
}
```

### 3. Graceful Shutdown
```csharp
public override async Task StopAsync(CancellationToken cancellationToken)
{
    _logger.LogInformation("Service stopping - finishing current work");
    
    // Finish current iteration gracefully
    await base.StopAsync(cancellationToken);
}
```

### 4. Використовуй CancellationToken
```csharp
// ✅ Передавай stoppingToken у всі async операції
await Task.Delay(_interval, stoppingToken);
await context.SaveChangesAsync(stoppingToken);
```

---

## Існуючі сервіси у проєкті

Подивись на приклади:
- `Infrastructure/BackgroundServices/BackupService.cs`
- Інші сервіси у `Infrastructure/BackgroundServices/`
