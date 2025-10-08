using Microsoft.Extensions.Configuration;
using StudentUnionBot.Services;
using StudentUnionBot.Data;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types.Enums;

var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: true)
    .AddJsonFile("appsettings.local.json", optional: true)
    .Build();

var botToken = Environment.GetEnvironmentVariable("BotToken") 
    ?? configuration["BotConfiguration:BotToken"] 
    ?? configuration["BotToken"]
    ?? throw new ArgumentNullException("BotToken", "Bot token is missing.");

var dbPath = Environment.GetEnvironmentVariable("DatabasePath")
    ?? configuration["BotConfiguration:DatabasePath"] 
    ?? "Data/studentunion.db";

Console.WriteLine($"📁 Database path: {dbPath}");

var dbContext = new BotDbContext(dbPath);

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

var botService = new BotService(botToken, dbPath);
var botClient = new TelegramBotClient(botToken);

using var cts = new CancellationTokenSource();

var receiverOptions = new ReceiverOptions
{
    AllowedUpdates = Array.Empty<UpdateType>()
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

Console.WriteLine("Bot started successfully. Press Ctrl+C to exit.");

var exitEvent = new ManualResetEventSlim(false);
Console.CancelKeyPress += (sender, e) =>
{
    e.Cancel = true;
    exitEvent.Set();
};

exitEvent.Wait();

cts.Cancel();
Console.WriteLine("Bot stopped.");
