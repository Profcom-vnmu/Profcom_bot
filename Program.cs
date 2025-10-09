using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Events;
using StudentUnionBot.Application.Appeals.Commands.CreateAppeal;
using StudentUnionBot.Application.Common.Interfaces;
using StudentUnionBot.Domain.Interfaces;
using StudentUnionBot.Infrastructure.Data;
using StudentUnionBot.Infrastructure.Repositories;
using StudentUnionBot.Infrastructure.Services;
using StudentUnionBot.Infrastructure.Services.Notifications;
using StudentUnionBot.Presentation.Bot.Handlers;
using StudentUnionBot.Presentation.Bot.Services;
using Telegram.Bot;

// Налаштування Serilog
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console(
        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.File(
        path: "logs/bot-.txt",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

try
{
    Log.Information("Запуск StudentUnionBot");

    var builder = WebApplication.CreateBuilder(args);

    // Використовуємо Serilog
    builder.Host.UseSerilog();

    // Налаштування DbContext
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
                          ?? builder.Configuration["BotConfiguration:DatabasePath"];
    
    builder.Services.AddDbContext<BotDbContext>(options =>
    {
        if (builder.Environment.IsDevelopment())
        {
            var dbPath = connectionString ?? "Data/studentunion_dev.db";
            options.UseSqlite($"Data Source={dbPath}");
            options.EnableSensitiveDataLogging();
            options.EnableDetailedErrors();
        }
        else
        {
            options.UseNpgsql(connectionString);
        }
    });

    // Реєстрація MediatR з Pipeline Behaviors
    builder.Services.AddMediatR(cfg =>
    {
        cfg.RegisterServicesFromAssembly(typeof(CreateAppealCommand).Assembly);
        cfg.Lifetime = ServiceLifetime.Scoped; // Важливо для роботи зі Scoped сервісами
        
        // Додаємо Pipeline Behaviors в правильному порядку
        cfg.AddOpenBehavior(typeof(StudentUnionBot.Application.Common.Behaviors.ValidationBehavior<,>));
        cfg.AddOpenBehavior(typeof(StudentUnionBot.Application.Common.Behaviors.LoggingBehavior<,>));
        cfg.AddOpenBehavior(typeof(StudentUnionBot.Application.Common.Behaviors.PerformanceBehavior<,>));
    });

    // Реєстрація FluentValidation
    builder.Services.AddValidatorsFromAssembly(typeof(CreateAppealCommand).Assembly);

    // Реєстрація Repository Pattern
    builder.Services.AddScoped<IUserRepository, UserRepository>();
    builder.Services.AddScoped<IAppealRepository, AppealRepository>();
    builder.Services.AddScoped<IAdminWorkloadRepository, AdminWorkloadRepository>();
    builder.Services.AddScoped<INewsRepository, NewsRepository>();
    builder.Services.AddScoped<IEventRepository, EventRepository>();
    builder.Services.AddScoped<IPartnerRepository, PartnerRepository>();
    builder.Services.AddScoped<IContactInfoRepository, ContactInfoRepository>();
    builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

    // Реєстрація State Management
    builder.Services.AddSingleton<StudentUnionBot.Application.Common.Interfaces.IUserStateManager, Infrastructure.Services.UserStateManager>();

    // Реєстрація Rate Limiter
    builder.Services.AddSingleton<IRateLimiter, Infrastructure.Services.RateLimiter>();

    // Реєстрація Redis Cache Services
    var redisConnectionString = builder.Configuration.GetConnectionString("Redis");
    if (!string.IsNullOrEmpty(redisConnectionString))
    {
        builder.Services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = redisConnectionString;
            options.InstanceName = "StudentUnionBot";
        });
        
        Log.Information("Redis кеш налаштовано з рядком підключення: {RedisConnection}", 
            redisConnectionString.Substring(0, Math.Min(20, redisConnectionString.Length)) + "...");
    }
    else
    {
        Log.Warning("Redis connection string не знайдено, кеш буде відключено");
    }
    
    builder.Services.AddScoped<IRedisCacheService, RedisCacheService>();
    builder.Services.AddScoped<IStudentUnionCacheService, StudentUnionCacheService>();

    // Реєстрація Email Service
    builder.Services.AddScoped<EmailTemplateService>();
    builder.Services.AddScoped<IEmailService, EmailService>();

    // Реєстрація Backup Service
    builder.Services.AddScoped<IBackupService, BackupService>();

    // Реєстрація Localization Service
    builder.Services.AddSingleton<ILocalizationService, LocalizationService>();

    // Реєстрація Appeal Assignment Service
    builder.Services.AddScoped<IAppealAssignmentService, AppealAssignmentService>();

    // Реєстрація File Management Services
    builder.Services.AddScoped<IFileStorageService, LocalFileStorageService>();
    builder.Services.AddScoped<IFileValidationService, FileValidationService>();
    builder.Services.AddScoped<IFileAttachmentRepository, FileAttachmentRepository>();
    builder.Services.AddScoped<IAppealFileAttachmentRepository, AppealFileAttachmentRepository>();

    // Реєстрація Notification Services
    builder.Services.AddScoped<INotificationService, NotificationService>();
    builder.Services.AddScoped<IReminderService, ReminderService>();
    builder.Services.AddScoped<IEmailNotificationProvider, SmtpEmailNotificationProvider>();
    builder.Services.AddScoped<IPushNotificationProvider, TelegramPushNotificationProvider>();

    // Telegram Bot Client
    var botToken = builder.Configuration["BotConfiguration:BotToken"] 
                   ?? throw new InvalidOperationException("Bot token не знайдено в конфігурації");
    
    builder.Services.AddSingleton<ITelegramBotClient>(new TelegramBotClient(botToken));

    // HTTP Client для Telegram
    builder.Services.AddHttpClient("telegram_bot_client")
        .AddTypedClient<ITelegramBotClient>((httpClient, sp) =>
        {
            var token = sp.GetRequiredService<IConfiguration>()["BotConfiguration:BotToken"]!;
            return new TelegramBotClient(token, httpClient);
        });

    // Bot Handlers
    builder.Services.AddSingleton<IBotUpdateHandler, UpdateHandler>();
    builder.Services.AddHostedService<BotBackgroundService>();

    // Background Services
    builder.Services.AddHostedService<StudentUnionBot.Infrastructure.BackgroundServices.NotificationReminderService>();
    builder.Services.AddHostedService<StudentUnionBot.Infrastructure.BackgroundServices.DataCleanupService>();

    // Health checks
    builder.Services.AddHealthChecks()
        .AddCheck<StudentUnionBot.Infrastructure.HealthChecks.DatabaseHealthCheck>("database", tags: new[] { "db", "critical" })
        .AddCheck<StudentUnionBot.Infrastructure.HealthChecks.FileStorageHealthCheck>("file_storage", tags: new[] { "storage" })
        .AddCheck<StudentUnionBot.Infrastructure.HealthChecks.MemoryHealthCheck>("memory", tags: new[] { "memory" })
        .AddCheck<StudentUnionBot.Infrastructure.HealthChecks.TelegramBotHealthCheck>("telegram_bot", tags: new[] { "telegram", "critical" })
        .AddCheck<StudentUnionBot.Infrastructure.HealthChecks.RedisHealthCheck>("redis_cache", tags: new[] { "cache", "redis" });

    var app = builder.Build();

    // Застосування міграцій при запуску (тільки в Development)
    if (app.Environment.IsDevelopment())
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<BotDbContext>();
        db.Database.Migrate();
        Log.Information("Міграції БД застосовано");
    }

    app.MapGet("/", () => "StudentUnionBot працює ✅");
    
    // Health check endpoints
    app.MapHealthChecks("/health");
    app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
    {
        Predicate = check => check.Tags.Contains("critical")
    });
    app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
    {
        Predicate = _ => false // Завжди повертає Healthy (для kubernetes liveness probe)
    });

    Log.Information("StudentUnionBot готовий до роботи");
    
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Критична помилка при запуску StudentUnionBot");
    throw;
}
finally
{
    Log.CloseAndFlush();
}
