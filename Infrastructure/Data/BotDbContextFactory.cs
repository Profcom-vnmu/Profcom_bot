using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace StudentUnionBot.Infrastructure.Data;

/// <summary>
/// Factory для створення DbContext під час design-time операцій (міграції)
/// </summary>
public class BotDbContextFactory : IDesignTimeDbContextFactory<BotDbContext>
{
    public BotDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<BotDbContext>();
        
        // Використовуємо SQLite для локальної розробки
        optionsBuilder.UseSqlite("Data Source=Data/studentunion_dev.db");
        
        return new BotDbContext(optionsBuilder.Options);
    }
}
