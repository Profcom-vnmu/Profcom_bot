using Microsoft.EntityFrameworkCore;
using StudentUnionBot.Domain.Entities;
using StudentUnionBot.Domain.Enums;

namespace StudentUnionBot.Infrastructure.Data;

public class BotDbContext : DbContext
{
    public BotDbContext(DbContextOptions<BotDbContext> options) : base(options)
    {
    }

    public DbSet<BotUser> Users => Set<BotUser>();
    public DbSet<Appeal> Appeals => Set<Appeal>();
    public DbSet<AppealMessage> AppealMessages => Set<AppealMessage>();
    public DbSet<News> News => Set<News>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<ContactInfo> Contacts => Set<ContactInfo>();
    public DbSet<Partner> Partners => Set<Partner>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // BotUser configuration
        modelBuilder.Entity<BotUser>(entity =>
        {
            entity.HasKey(e => e.TelegramId);
            
            entity.Property(e => e.Username).HasMaxLength(100);
            entity.Property(e => e.FirstName).HasMaxLength(100);
            entity.Property(e => e.LastName).HasMaxLength(100);
            entity.Property(e => e.FullName).HasMaxLength(200);
            entity.Property(e => e.Faculty).HasMaxLength(200);
            entity.Property(e => e.Group).HasMaxLength(50);
            entity.Property(e => e.Email).HasMaxLength(200);
            entity.Property(e => e.VerificationCode).HasMaxLength(10);
            entity.Property(e => e.Language).HasMaxLength(5).IsRequired();
            entity.Property(e => e.TimeZone).HasMaxLength(50);
            entity.Property(e => e.BanReason).HasMaxLength(500);
            
            // Role mapping - simple approach without backing field
            entity.Property(e => e.Role)
                .HasConversion<int>()
                .IsRequired();

            entity.HasIndex(e => e.Email);
            entity.HasIndex(e => e.Username);

            // Relationships
            entity.HasMany(e => e.Appeals)
                .WithOne(e => e.Student)
                .HasForeignKey(e => e.StudentId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Appeal configuration
        modelBuilder.Entity<Appeal>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.StudentName).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Subject).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Message).HasMaxLength(4000).IsRequired();
            entity.Property(e => e.ClosedReason).HasMaxLength(500);
            entity.Property(e => e.RatingComment).HasMaxLength(1000);

            entity.HasIndex(e => e.StudentId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.Category);
            entity.HasIndex(e => e.CreatedAt);

            // Relationships
            entity.HasMany(e => e.Messages)
                .WithOne(e => e.Appeal)
                .HasForeignKey(e => e.AppealId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // AppealMessage configuration
        modelBuilder.Entity<AppealMessage>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.SenderName).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Text).HasMaxLength(4000).IsRequired();
            entity.Property(e => e.PhotoFileId).HasMaxLength(200);
            entity.Property(e => e.DocumentFileId).HasMaxLength(200);
            entity.Property(e => e.DocumentFileName).HasMaxLength(300);

            entity.HasIndex(e => e.AppealId);
            entity.HasIndex(e => e.SenderId);
            entity.HasIndex(e => e.SentAt);
        });

        // News configuration
        modelBuilder.Entity<News>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.Title).HasMaxLength(300).IsRequired();
            entity.Property(e => e.Content).HasMaxLength(10000).IsRequired();
            entity.Property(e => e.Summary).HasMaxLength(500);
            entity.Property(e => e.AuthorName).HasMaxLength(200).IsRequired();
            entity.Property(e => e.PhotoFileId).HasMaxLength(200);
            entity.Property(e => e.DocumentFileId).HasMaxLength(200);

            entity.HasIndex(e => e.Category);
            entity.HasIndex(e => e.IsPublished);
            entity.HasIndex(e => e.IsPinned);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => e.PublishAt);
        });

        // Event configuration
        modelBuilder.Entity<Event>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.Title).HasMaxLength(300).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(10000).IsRequired();
            entity.Property(e => e.Summary).HasMaxLength(500);
            entity.Property(e => e.Location).HasMaxLength(300);
            entity.Property(e => e.Address).HasMaxLength(500);
            entity.Property(e => e.ContactPerson).HasMaxLength(200);
            entity.Property(e => e.ContactInfo).HasMaxLength(200);
            entity.Property(e => e.OrganizerName).HasMaxLength(200).IsRequired();
            entity.Property(e => e.PhotoFileId).HasMaxLength(200);

            entity.HasIndex(e => e.Type);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.IsPublished);
            entity.HasIndex(e => e.IsFeatured);
            entity.HasIndex(e => e.StartDate);
            entity.HasIndex(e => e.CreatedAt);
        });

        // ContactInfo configuration
        modelBuilder.Entity<ContactInfo>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.Title).HasMaxLength(200).IsRequired();
            entity.Property(e => e.PersonName).HasMaxLength(200);
            entity.Property(e => e.Position).HasMaxLength(200);
            entity.Property(e => e.PhoneNumber).HasMaxLength(50);
            entity.Property(e => e.Email).HasMaxLength(200);
            entity.Property(e => e.TelegramUsername).HasMaxLength(100);
            entity.Property(e => e.Address).HasMaxLength(500);
            entity.Property(e => e.OfficeNumber).HasMaxLength(50);
            entity.Property(e => e.WorkingHours).HasMaxLength(300);
            entity.Property(e => e.Description).HasMaxLength(1000);

            entity.HasIndex(e => e.Type);
            entity.HasIndex(e => e.IsActive);
            entity.HasIndex(e => e.DisplayOrder);
        });

        // Partner configuration
        modelBuilder.Entity<Partner>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.Name).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Description).HasMaxLength(1000);
            entity.Property(e => e.DiscountInfo).HasMaxLength(500);
            entity.Property(e => e.PromoCode).HasMaxLength(100);
            entity.Property(e => e.Website).HasMaxLength(500);
            entity.Property(e => e.Address).HasMaxLength(500);
            entity.Property(e => e.PhoneNumber).HasMaxLength(50);
            entity.Property(e => e.Instagram).HasMaxLength(100);
            entity.Property(e => e.Facebook).HasMaxLength(200);
            entity.Property(e => e.Telegram).HasMaxLength(100);
            entity.Property(e => e.LogoFileId).HasMaxLength(200);
            entity.Property(e => e.TermsAndConditions).HasMaxLength(2000);

            entity.HasIndex(e => e.Type);
            entity.HasIndex(e => e.IsActive);
            entity.HasIndex(e => e.IsFeatured);
            entity.HasIndex(e => e.DisplayOrder);
        });
    }
}
