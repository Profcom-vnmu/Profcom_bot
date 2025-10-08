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
    .AddJsonFile("appsettings.local.json", optional: true) // Локальний файл з токеном (не йде в Git)
    .Build();

// Читаємо токен: спочатку з змінних середовища, потім з appsettings.local.json, потім з appsettings.json
var botToken = Environment.GetEnvironmentVariable("BotToken") 
    ?? configuration["BotConfiguration:BotToken"] 
    ?? configuration["BotToken"]
    ?? throw new ArgumentNullException("BotToken", "Bot token is missing. Set BotToken environment variable or add to appsettings.local.json");

var dbPath = Environment.GetEnvironmentVariable("DatabasePath")
    ?? configuration["BotConfiguration:DatabasePath"] 
    ?? "Data/studentunion.db";

// Initialize database
var dbContext = new BotDbContext(dbPath);
dbContext.Database.Migrate();

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

Console.WriteLine("Bot started successfully. Press Enter to exit.");
Console.ReadLine();

cts.Cancel();