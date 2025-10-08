using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace StudentUnionBot.Data;

public class BotDbContextFactory : IDesignTimeDbContextFactory<BotDbContext>
{
    public BotDbContext CreateDbContext(string[] args)
    {
        // Для міграцій використовуємо SQLite (локально)
        var dbPath = Path.Combine(AppContext.BaseDirectory, "Data", "studentunion.db");
        return new BotDbContext(dbPath, isPostgreSQL: false);
    }
}