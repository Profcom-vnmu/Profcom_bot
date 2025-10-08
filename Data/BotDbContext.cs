using Microsoft.EntityFrameworkCore;
using StudentUnionBot.Models;

namespace StudentUnionBot.Data;

public class BotDbContext : DbContext
{
    public DbSet<Appeal> Appeals { get; set; } = null!;
    public DbSet<AppealMessage> AppealMessages { get; set; } = null!;
    public DbSet<News> News { get; set; } = null!;
    public DbSet<BotUser> Users { get; set; } = null!;
    public DbSet<ContactInfo> ContactInfo { get; set; } = null!;
    public DbSet<PartnersInfo> PartnersInfo { get; set; } = null!;
    public DbSet<EventsInfo> EventsInfo { get; set; } = null!;

    private readonly string _connectionString;
    private readonly bool _isPostgreSQL;

    public BotDbContext(string connectionString, bool isPostgreSQL = false)
    {
        _connectionString = connectionString;
        _isPostgreSQL = isPostgreSQL;
    }

    protected override void OnConfiguring(DbContextOptionsBuilder options)
    {
        if (_isPostgreSQL)
        {
            options.UseNpgsql(_connectionString);
        }
        else
        {
            // Для локальної розробки - SQLite
            options.UseSqlite($"Data Source={_connectionString}");
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Appeal>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.StudentName).IsRequired();
            entity.Property(e => e.Message).IsRequired();
        });

        modelBuilder.Entity<News>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired();
            entity.Property(e => e.Content).IsRequired();
        });

        modelBuilder.Entity<BotUser>(entity =>
        {
            entity.HasKey(e => e.TelegramId);
            entity.Property(e => e.JoinedAt).IsRequired();
        });

        modelBuilder.Entity<AppealMessage>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Text).IsRequired();
            entity.HasOne(e => e.Appeal)
                .WithMany(a => a.Messages)
                .HasForeignKey(e => e.AppealId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ContactInfo>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired();
            entity.Property(e => e.Content).IsRequired();
        });

        modelBuilder.Entity<PartnersInfo>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired();
            entity.Property(e => e.Content).IsRequired();
        });

        modelBuilder.Entity<EventsInfo>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired();
            entity.Property(e => e.Content).IsRequired();
        });
    }
}