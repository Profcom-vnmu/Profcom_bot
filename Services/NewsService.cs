using Microsoft.EntityFrameworkCore;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using StudentUnionBot.Data;
using StudentUnionBot.Models;

namespace StudentUnionBot.Services;

public class NewsService
{
    private readonly BotDbContext _context;
    private readonly ITelegramBotClient _botClient;

    public NewsService(BotDbContext context, ITelegramBotClient botClient)
    {
        _context = context;
        _botClient = botClient;
    }

    public async Task<News> CreateAndBroadcastNewsAsync(
        string title, 
        string content, 
        bool sendImmediately = true, 
        string? photoFileId = null,
        List<int>? courses = null,
        List<string>? faculties = null)
    {
        var news = new News
        {
            Title = title,
            Content = content,
            PhotoFileId = photoFileId,
            IsPublished = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.News.Add(news);
        await _context.SaveChangesAsync();

        if (sendImmediately)
        {
            await BroadcastNewsAsync(news, courses, faculties);
        }

        return news;
    }

    public async Task BroadcastNewsAsync(News news, List<int>? courses = null, List<string>? faculties = null)
    {
        var query = _context.Users.Where(u => u.IsActive);

        // Фільтрація за курсами
        if (courses != null && courses.Any())
        {
            query = query.Where(u => u.Course.HasValue && courses.Contains(u.Course.Value));
        }

        // Фільтрація за факультетами
        if (faculties != null && faculties.Any())
        {
            query = query.Where(u => !string.IsNullOrEmpty(u.Faculty) && faculties.Contains(u.Faculty));
        }

        var activeUsers = await query.ToListAsync();

        var messageText = $"📢 <b>{news.Title}</b>\n\n" +
                         $"{news.Content}\n\n" +
                         $"<i>Опубліковано: {news.CreatedAt:dd.MM.yyyy HH:mm}</i>";

        foreach (var user in activeUsers)
        {
            try
            {
                // Якщо є фото - відправляємо з фото
                if (!string.IsNullOrEmpty(news.PhotoFileId))
                {
                    await _botClient.SendPhotoAsync(
                        chatId: user.TelegramId,
                        photo: InputFile.FromFileId(news.PhotoFileId),
                        caption: messageText,
                        parseMode: ParseMode.Html
                    );
                }
                else
                {
                    // Інакше - текстове повідомлення
                    await _botClient.SendTextMessageAsync(
                        chatId: user.TelegramId,
                        text: messageText,
                        parseMode: ParseMode.Html
                    );
                }
                await Task.Delay(35); // Avoid hitting rate limits
            }
            catch (Exception)
            {
                // If we can't send message to user, mark them as inactive
                user.IsActive = false;
                _context.Users.Update(user);
            }
        }

        await _context.SaveChangesAsync();
    }

    public async Task<List<News>> GetLatestNewsAsync(int count = 5)
    {
        return await _context.News
            .Where(n => n.IsPublished)
            .OrderByDescending(n => n.CreatedAt)
            .Take(count)
            .ToListAsync();
    }

    public async Task<News?> GetNewsByIdAsync(int id)
    {
        return await _context.News.FindAsync(id);
    }

    public async Task UpdateNewsAsync(News news)
    {
        _context.News.Update(news);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteNewsAsync(int id)
    {
        var news = await _context.News.FindAsync(id);
        if (news != null)
        {
            _context.News.Remove(news);
            await _context.SaveChangesAsync();
        }
    }
}