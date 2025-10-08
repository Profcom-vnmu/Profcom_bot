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
using StudentUnionBot.Presentation.Bot.Handlers;
using StudentUnionBot.Presentation.Bot.Services;
using Telegram.Bot;

// Налаштування Serilog
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
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

    // Реєстрація MediatR
    builder.Services.AddMediatR(cfg =>
    {
        cfg.RegisterServicesFromAssembly(typeof(CreateAppealCommand).Assembly);
        cfg.Lifetime = ServiceLifetime.Scoped; // Важливо для роботи зі Scoped сервісами
    });

    // Реєстрація FluentValidation
    builder.Services.AddValidatorsFromAssembly(typeof(CreateAppealCommand).Assembly);

    // Реєстрація Repository Pattern
    builder.Services.AddScoped<IUserRepository, UserRepository>();
    builder.Services.AddScoped<IAppealRepository, AppealRepository>();
    builder.Services.AddScoped<INewsRepository, NewsRepository>();
    builder.Services.AddScoped<IEventRepository, EventRepository>();
    builder.Services.AddScoped<IPartnerRepository, PartnerRepository>();
    builder.Services.AddScoped<IContactInfoRepository, ContactInfoRepository>();
    builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

    // Реєстрація State Management
    builder.Services.AddSingleton<StudentUnionBot.Application.Common.Interfaces.IUserStateManager, Infrastructure.Services.UserStateManager>();

    // Реєстрація Rate Limiter
    builder.Services.AddSingleton<IRateLimiter, Infrastructure.Services.RateLimiter>();

    // Реєстрація Email Service
    builder.Services.AddScoped<IEmailService, EmailService>();

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

    // Health checks
    builder.Services.AddHealthChecks();

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
    app.MapHealthChecks("/health");

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
