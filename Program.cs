using Microsoft.Extensions.Configuration;using Microsoft.Extensions.Configuration;

using StudentUnionBot.Services;using StudentUnionBot.Services;

using StudentUnionBot.Data;using StudentUnionBot.Data;

using Microsoft.EntityFrameworkCore;using Microsoft.EntityFrameworkCore;

using Telegram.Bot;using Telegram.Bot;

using Telegram.Bot.Polling;using Telegram.Bot.Polling;

using Telegram.Bot.Types.Enums;using Telegram.Bot.Types.Enums;



var configuration = new ConfigurationBuilder()var configuration = new ConfigurationBuilder()

    .SetBasePath(Directory.GetCurrentDirectory())    .SetBasePath(Directory.GetCurrentDirectory())

    .AddJsonFile("appsettings.json", optional: true)    .AddJsonFile("appsettings.json", optional: true)

    .AddJsonFile("appsettings.local.json", optional: true) // Локальний файл з токеном (не йде в Git)    .AddJsonFile("appsettings.local.json", optional: true) // Локальний файл з токеном (не йде в Git)

    .Build();    .Build();



// Читаємо токен: спочатку з змінних середовища, потім з appsettings.local.json, потім з appsettings.json// Читаємо токен: спочатку з змінних середовища, потім з appsettings.local.json, потім з appsettings.json

var botToken = Environment.GetEnvironmentVariable("BotToken") var botToken = Environment.GetEnvironmentVariable("BotToken") 

    ?? configuration["BotConfiguration:BotToken"]     ?? configuration["BotConfiguration:BotToken"] 

    ?? configuration["BotToken"]    ?? configuration["BotToken"]

    ?? throw new ArgumentNullException("BotToken", "Bot token is missing. Set BotToken environment variable or add to appsettings.local.json");    ?? throw new ArgumentNullException("BotToken", "Bot token is missing. Set BotToken environment variable or add to appsettings.local.json");



var dbPath = Environment.GetEnvironmentVariable("DatabasePath")var dbPath = Environment.GetEnvironmentVariable("DatabasePath")

    ?? configuration["BotConfiguration:DatabasePath"]     ?? configuration["BotConfiguration:DatabasePath"] 

    ?? "Data/studentunion.db";    ?? "Data/studentunion.db";



// Initialize database// Initialize database

var dbContext = new BotDbContext(dbPath);var dbContext = new BotDbContext(dbPath);

dbContext.Database.Migrate();dbContext.Database.Migrate();



var botService = new BotService(botToken, dbPath);var botService = new BotService(botToken, dbPath);

var botClient = new TelegramBotClient(botToken);var botClient = new TelegramBotClient(botToken);



using var cts = new CancellationTokenSource();using var cts = new CancellationTokenSource();



var receiverOptions = new ReceiverOptionsvar receiverOptions = new ReceiverOptions

{{

    AllowedUpdates = Array.Empty<UpdateType>()    AllowedUpdates = Array.Empty<UpdateType>()

};};



botClient.StartReceiving(botClient.StartReceiving(

    updateHandler: async (client, update, token) =>    updateHandler: async (client, update, token) =>

    {    {

        try        try

        {        {

            await botService.HandleUpdateAsync(update);            await botService.HandleUpdateAsync(update);

        }        }

        catch (Exception ex)        catch (Exception ex)

        {        {

            Console.WriteLine($"Error handling update: {ex}");            Console.WriteLine($"Error handling update: {ex}");

        }        }

    },    },

    pollingErrorHandler: (client, exception, token) =>    pollingErrorHandler: (client, exception, token) =>

    {    {

        Console.WriteLine($"Error polling: {exception}");        Console.WriteLine($"Error polling: {exception}");

        return Task.CompletedTask;        return Task.CompletedTask;

    },    },

    receiverOptions: receiverOptions,    receiverOptions: receiverOptions,

    cancellationToken: cts.Token    cancellationToken: cts.Token

););



Console.WriteLine("Bot started successfully. Press Ctrl+C to exit.");Console.WriteLine("Bot started successfully. Press Ctrl+C to exit.");`n`n// ??????? ?? ?????? ??????????`nvar exitEvent = new ManualResetEventSlim(false);`nConsole.CancelKeyPress += (sender, e) => { e.Cancel = true; exitEvent.Set(); };`nexitEvent.Wait();



// Чекаємо на сигнал завершення (Ctrl+C) - працює як локально, так і на хостингуcts.Cancel();

var exitEvent = new ManualResetEventSlim(false);
Console.CancelKeyPress += (sender, e) =>
{
    e.Cancel = true;
    exitEvent.Set();
};

// Тримаємо додаток запущеним
exitEvent.Wait();

cts.Cancel();
Console.WriteLine("Bot stopped.");
