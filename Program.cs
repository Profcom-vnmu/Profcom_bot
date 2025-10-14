using Microsoft.Extensions.Configuration;
using StudentUnionBot.Services;
using StudentUnionBot.Data;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types.Enums;
using System.Net;

// Функція для конвертації PostgreSQL URL в connection string
static string ConvertPostgresUrlToConnectionString(string databaseUrl)
{
    var uri = new Uri(databaseUrl);
    var userInfo = uri.UserInfo.Split(':');
    
    return $"Host={uri.Host};Port={uri.Port};Database={uri.AbsolutePath.Substring(1)};Username={userInfo[0]};Password={userInfo[1]};SSL Mode=Require;Trust Server Certificate=true";
}

var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: true)
    .AddJsonFile("appsettings.local.json", optional: true)
    .Build();

var botToken = Environment.GetEnvironmentVariable("BotToken") 
    ?? configuration["BotConfiguration:BotToken"] 
    ?? configuration["BotToken"]
    ?? throw new ArgumentNullException("BotToken", "Bot token is missing.");

// Налаштування PostgreSQL - автоматичне визначення середовища
var renderDatabaseUrl = Environment.GetEnvironmentVariable("DATABASE_URL");
var isRenderEnvironment = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("RENDER")) 
                         || !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("PORT"));

BotDbContext dbContext;

if (isRenderEnvironment)
{
    // Render.com PostgreSQL - конвертуємо URL в connection string
    string connectionString;
    
    if (!string.IsNullOrEmpty(renderDatabaseUrl))
    {
        // Використовуємо DATABASE_URL якщо є
        connectionString = ConvertPostgresUrlToConnectionString(renderDatabaseUrl);
    }
    else
    {
        // Fallback - вбудований connection string
        connectionString = "Host=dpg-d3n9jjb3fgac73af7550-a;Port=5432;Database=render_postgresql_5nyk;Username=render_postgresql_5nyk_user;Password=JYvtkcQIhpAtroaF8LOoT5W1qEdgptnI;SSL Mode=Require;Trust Server Certificate=true";
    }
    
    Console.WriteLine("🐘 Using Render PostgreSQL database");
    dbContext = new BotDbContext(connectionString, isPostgreSQL: true);
    Console.WriteLine($"📊 Database: Render PostgreSQL");
}
else
{
    // Локальна розробка - PostgreSQL
    var localConnectionString = "Host=localhost;Database=studentunion;Username=postgres;Password=password";
    Console.WriteLine("🐘 Using local PostgreSQL database");
    dbContext = new BotDbContext(localConnectionString, isPostgreSQL: true);
    Console.WriteLine($"📊 Database: PostgreSQL (localhost)");
}

Console.WriteLine("🔄 Running database migrations...");
try
{
    dbContext.Database.Migrate();
    Console.WriteLine("✅ Database migrations completed successfully");
    
    // Перевіряємо чи таблиці створені
    var canConnect = dbContext.Database.CanConnect();
    Console.WriteLine($"📊 Database connection: {(canConnect ? "OK" : "FAILED")}");
}
catch (Exception ex)
{
    Console.WriteLine($"❌ Migration failed: {ex.Message}");
    throw;
}

var botService = new BotService(botToken, dbContext);
var botClient = new TelegramBotClient(botToken);

using var cts = new CancellationTokenSource();

// Скидаємо всі pending updates щоб уникнути конфліктів
Console.WriteLine("🔄 Clearing pending updates...");
await botClient.DeleteWebhookAsync(dropPendingUpdates: true, cancellationToken: cts.Token);
Console.WriteLine("✅ Pending updates cleared");

var receiverOptions = new ReceiverOptions
{
    AllowedUpdates = Array.Empty<UpdateType>(),
    ThrowPendingUpdates = true
};

botClient.StartReceiving(
    updateHandler: async (client, update, token) =>
    {
        try
        {
            await botService.HandleUpdateAsync(update);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error handling update: {ex}");
        }
    },
    pollingErrorHandler: (client, exception, token) =>
    {
        Console.WriteLine($"Error polling: {exception}");
        return Task.CompletedTask;
    },
    receiverOptions: receiverOptions,
    cancellationToken: cts.Token
);

// Запускаємо HTTP сервер для Render health checks
var port = Environment.GetEnvironmentVariable("PORT") ?? "10000";
var httpListener = new HttpListener();
httpListener.Prefixes.Add($"http://*:{port}/");

try
{
    httpListener.Start();
    Console.WriteLine($"🌐 HTTP server started on port {port}");
    
    // Обробка HTTP запитів в окремому потоці
    _ = Task.Run(async () =>
    {
        while (httpListener.IsListening)
        {
            try
            {
                var context = await httpListener.GetContextAsync();
                var response = context.Response;
                
                string responseString = $"{{\"status\":\"ok\",\"bot\":\"running\",\"time\":\"{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}\"}}";
                byte[] buffer = System.Text.Encoding.UTF8.GetBytes(responseString);
                
                response.ContentType = "application/json";
                response.ContentLength64 = buffer.Length;
                response.StatusCode = 200;
                
                await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
                response.OutputStream.Close();
            }
            catch (Exception ex)
            {
                if (httpListener.IsListening)
                {
                    Console.WriteLine($"HTTP error: {ex.Message}");
                }
            }
        }
    }, cts.Token);
}
catch (Exception ex)
{
    Console.WriteLine($"⚠️ Failed to start HTTP server: {ex.Message}");
}

Console.WriteLine("Bot started successfully. Press Ctrl+C to exit.");

var exitEvent = new ManualResetEventSlim(false);
Console.CancelKeyPress += (sender, e) =>
{
    e.Cancel = true;
    exitEvent.Set();
};

exitEvent.Wait();

httpListener.Stop();
cts.Cancel();
Console.WriteLine("Bot stopped.");
