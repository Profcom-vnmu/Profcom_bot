using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace StudentUnionBot.Data;

public class BotDbContextFactory : IDesignTimeDbContextFactory<BotDbContext>
{
    public BotDbContext CreateDbContext(string[] args)
    {
        var dbPath = Path.Combine(AppContext.BaseDirectory, "bot.db");
        return new BotDbContext(dbPath);
    }
}