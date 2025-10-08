using Microsoft.EntityFrameworkCore;
using StudentUnionBot.Data;
using StudentUnionBot.Models;
using Telegram.Bot.Types;

namespace StudentUnionBot.Services;

public class UserService
{
    private readonly BotDbContext _context;
    private readonly HashSet<long> _adminIds;
    private readonly HashSet<long> _bannedUserIds;
    private readonly string _adminsFilePath;
    private readonly string _bannedFilePath;

    public UserService(BotDbContext context)
    {
        _context = context;
        _adminsFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "admins.txt");
        _bannedFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ban.txt");
        _adminIds = LoadAdminIds();
        _bannedUserIds = LoadBannedUserIds();
    }

    private HashSet<long> LoadAdminIds()
    {
        var adminIds = new HashSet<long>();
        
        if (!System.IO.File.Exists(_adminsFilePath))
        {
            Console.WriteLine($"⚠️ Файл admins.txt не знайдено за шляхом: {_adminsFilePath}");
            Console.WriteLine("Створіть файл admins.txt і додайте в нього Telegram ID адміністраторів (один на рядок)");
            return adminIds;
        }

        try
        {
            var lines = System.IO.File.ReadAllLines(_adminsFilePath);
            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();
                // Ігноруємо порожні рядки та коментарі
                if (string.IsNullOrWhiteSpace(trimmedLine) || trimmedLine.StartsWith("#"))
                    continue;

                if (long.TryParse(trimmedLine, out long adminId))
                {
                    adminIds.Add(adminId);
                }
                else
                {
                    Console.WriteLine($"⚠️ Некоректний ID в admins.txt: {trimmedLine}");
                }
            }

            Console.WriteLine($"✅ Завантажено {adminIds.Count} адміністраторів з admins.txt");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Помилка при читанні admins.txt: {ex.Message}");
        }

        return adminIds;
    }

    private HashSet<long> LoadBannedUserIds()
    {
        var bannedIds = new HashSet<long>();
        
        if (!System.IO.File.Exists(_bannedFilePath))
        {
            Console.WriteLine($"ℹ️ Файл ban.txt не знайдено за шляхом: {_bannedFilePath}");
            Console.WriteLine("Список заблокованих користувачів порожній");
            return bannedIds;
        }

        try
        {
            var lines = System.IO.File.ReadAllLines(_bannedFilePath);
            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();
                // Ігноруємо порожні рядки та коментарі
                if (string.IsNullOrWhiteSpace(trimmedLine) || trimmedLine.StartsWith("#"))
                    continue;

                if (long.TryParse(trimmedLine, out long bannedId))
                {
                    bannedIds.Add(bannedId);
                }
                else
                {
                    Console.WriteLine($"⚠️ Некоректний ID в ban.txt: {trimmedLine}");
                }
            }

            if (bannedIds.Count > 0)
            {
                Console.WriteLine($"🚫 Завантажено {bannedIds.Count} заблокованих користувачів з ban.txt");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Помилка при читанні ban.txt: {ex.Message}");
        }

        return bannedIds;
    }

    public async Task<BotUser> EnsureUserCreatedAsync(Telegram.Bot.Types.User telegramUser)
    {
        var user = await _context.Users.FindAsync(telegramUser.Id);
        
        if (user == null)
        {
            user = new BotUser
            {
                TelegramId = telegramUser.Id,
                Username = telegramUser.Username,
                FirstName = telegramUser.FirstName,
                LastName = telegramUser.LastName,
                IsActive = true,
                JoinedAt = DateTime.UtcNow
            };
            
            _context.Users.Add(user);
        }
        else
        {
            // Update user info in case it changed
            user.Username = telegramUser.Username;
            user.FirstName = telegramUser.FirstName;
            user.LastName = telegramUser.LastName;
            user.IsActive = true;
            
            _context.Users.Update(user);
        }

        await _context.SaveChangesAsync();
        return user;
    }

    public bool IsAdmin(long telegramId)
    {
        return _adminIds.Contains(telegramId);
    }

    public Task<bool> IsAdminAsync(long telegramId)
    {
        return Task.FromResult(IsAdmin(telegramId));
    }

    public List<long> GetAllAdminIds()
    {
        return _adminIds.ToList();
    }

    public Task<List<long>> GetAdminIdsAsync()
    {
        return Task.FromResult(_adminIds.ToList());
    }

    public async Task<BotUser?> GetUserByIdAsync(long telegramId)
    {
        return await _context.Users.FindAsync(telegramId);
    }

    public void ReloadAdmins()
    {
        _adminIds.Clear();
        var newAdmins = LoadAdminIds();
        foreach (var adminId in newAdmins)
        {
            _adminIds.Add(adminId);
        }
    }

    public bool IsBanned(long telegramId)
    {
        return _bannedUserIds.Contains(telegramId);
    }

    public Task<bool> IsBannedAsync(long telegramId)
    {
        return Task.FromResult(IsBanned(telegramId));
    }

    public void ReloadBannedUsers()
    {
        _bannedUserIds.Clear();
        var newBanned = LoadBannedUserIds();
        foreach (var bannedId in newBanned)
        {
            _bannedUserIds.Add(bannedId);
        }
    }

    /// <summary>
    /// Отримати статистику користувачів
    /// </summary>
    public async Task<Dictionary<string, int>> GetUserStatisticsAsync()
    {
        var totalUsers = await _context.Users.CountAsync();
        var activeUsers = await _context.Users.CountAsync(u => u.IsActive);
        var usersWithProfile = await _context.Users.CountAsync(u => u.ProfileUpdatedAt.HasValue);
        
        // Статистика по курсам
        var usersByCourse = new Dictionary<int, int>();
        for (int i = 1; i <= 6; i++)
        {
            usersByCourse[i] = await _context.Users.CountAsync(u => u.Course == i);
        }
        
        // Статистика по факультетах
        var usersByFaculty = await _context.Users
            .Where(u => !string.IsNullOrEmpty(u.Faculty))
            .GroupBy(u => u.Faculty)
            .Select(g => new { Faculty = g.Key, Count = g.Count() })
            .ToListAsync();

        var stats = new Dictionary<string, int>
        {
            ["total"] = totalUsers,
            ["active"] = activeUsers,
            ["withProfile"] = usersWithProfile,
            ["course1"] = usersByCourse.GetValueOrDefault(1, 0),
            ["course2"] = usersByCourse.GetValueOrDefault(2, 0),
            ["course3"] = usersByCourse.GetValueOrDefault(3, 0),
            ["course4"] = usersByCourse.GetValueOrDefault(4, 0),
            ["course5"] = usersByCourse.GetValueOrDefault(5, 0),
            ["course6"] = usersByCourse.GetValueOrDefault(6, 0)
        };

        // Додаємо факультети
        foreach (var faculty in usersByFaculty)
        {
            stats[$"faculty_{faculty.Faculty}"] = faculty.Count;
        }

        return stats;
    }

    /// <summary>
    /// Експортувати користувачів у CSV формат
    /// </summary>
    public async Task<string> ExportUsersToCsvAsync()
    {
        var users = await _context.Users
            .OrderBy(u => u.JoinedAt)
            .ToListAsync();

        var csv = new System.Text.StringBuilder();
        
        // Заголовки
        csv.AppendLine("ID,Username,FirstName,LastName,FullName,Faculty,Course,Group,Email,JoinedAt,IsActive,ProfileUpdated");

        // Дані
        foreach (var user in users)
        {
            csv.AppendLine($"{user.TelegramId}," +
                          $"\"{user.Username ?? ""}\"," +
                          $"\"{user.FirstName ?? ""}\"," +
                          $"\"{user.LastName ?? ""}\"," +
                          $"\"{user.FullName ?? ""}\"," +
                          $"\"{user.Faculty ?? ""}\"," +
                          $"{user.Course?.ToString() ?? ""}," +
                          $"\"{user.Group ?? ""}\"," +
                          $"\"{user.Email ?? ""}\"," +
                          $"{user.JoinedAt:yyyy-MM-dd HH:mm:ss}," +
                          $"{(user.IsActive ? "Yes" : "No")}," +
                          $"{(user.ProfileUpdatedAt.HasValue ? user.ProfileUpdatedAt.Value.ToString("yyyy-MM-dd HH:mm:ss") : "")}");
        }

        return csv.ToString();
    }
}