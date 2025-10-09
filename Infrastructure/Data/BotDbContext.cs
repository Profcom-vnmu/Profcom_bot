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
    public DbSet<AdminWorkload> AdminWorkloads => Set<AdminWorkload>();
    public DbSet<AdminCategoryExpertise> AdminCategoryExpertises => Set<AdminCategoryExpertise>();
    public DbSet<FileAttachment> FileAttachments => Set<FileAttachment>();
    public DbSet<AppealFileAttachment> AppealFileAttachments => Set<AppealFileAttachment>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<NotificationPreference> NotificationPreferences => Set<NotificationPreference>();
    public DbSet<NotificationTemplate> NotificationTemplates => Set<NotificationTemplate>();

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
            
            // Many-to-many relationship with BotUser (event participants)
            entity.HasMany(e => e.RegisteredParticipants)
                .WithMany()
                .UsingEntity(j => j.ToTable("EventParticipants"));
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

        // AdminWorkload configuration
        modelBuilder.Entity<AdminWorkload>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.AdminId).IsRequired();
            entity.Property(e => e.ActiveAppealsCount).HasDefaultValue(0);
            entity.Property(e => e.TotalAppealsCount).HasDefaultValue(0);
            entity.Property(e => e.IsAvailable).HasDefaultValue(true);

            entity.HasIndex(e => e.AdminId).IsUnique();
            entity.HasIndex(e => e.IsAvailable);
            entity.HasIndex(e => e.LastActivityAt);

            // Relationships
            entity.HasOne(w => w.Admin)
                .WithOne()
                .HasForeignKey<AdminWorkload>(w => w.AdminId)
                .HasPrincipalKey<BotUser>(u => u.TelegramId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(w => w.CategoryExpertises)
                .WithOne(e => e.AdminWorkload)
                .HasForeignKey(e => e.AdminId)
                .HasPrincipalKey(w => w.AdminId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // AdminCategoryExpertise configuration
        modelBuilder.Entity<AdminCategoryExpertise>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.AdminId).IsRequired();
            entity.Property(e => e.Category).HasConversion<int>().IsRequired();
            entity.Property(e => e.ExperienceLevel).HasDefaultValue(1);
            entity.Property(e => e.SuccessfulResolutions).HasDefaultValue(0);
            entity.Property(e => e.TotalResolutions).HasDefaultValue(0);

            entity.HasIndex(e => new { e.AdminId, e.Category }).IsUnique();
            entity.HasIndex(e => e.Category);
            entity.HasIndex(e => e.ExperienceLevel);

            // Relationships
            entity.HasOne(e => e.Admin)
                .WithMany()
                .HasForeignKey(e => e.AdminId)
                .HasPrincipalKey(u => u.TelegramId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // FileAttachment configuration
        modelBuilder.Entity<FileAttachment>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.FileName).HasMaxLength(300).IsRequired();
            entity.Property(e => e.OriginalFileName).HasMaxLength(300).IsRequired();
            entity.Property(e => e.FilePath).HasMaxLength(500).IsRequired();
            entity.Property(e => e.ContentType).HasMaxLength(100).IsRequired();
            entity.Property(e => e.FileHash).HasMaxLength(64).IsRequired(); // SHA256 hex string
            entity.Property(e => e.ThumbnailPath).HasMaxLength(500);
            entity.Property(e => e.ScanResult).HasMaxLength(1000);

            entity.HasIndex(e => e.FileHash);
            entity.HasIndex(e => e.UploadedByUserId);
            entity.HasIndex(e => e.FileType);
            entity.HasIndex(e => e.ScanStatus);
            entity.HasIndex(e => e.UploadedAt);
            entity.HasIndex(e => e.IsDeleted);
            entity.HasIndex(e => new { e.IsDeleted, e.FileType });
            entity.HasIndex(e => new { e.IsDeleted, e.ScanStatus });

            // Relationships
            entity.HasOne(f => f.UploadedBy)
                .WithMany()
                .HasForeignKey(f => f.UploadedByUserId)
                .HasPrincipalKey(u => u.TelegramId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasMany(f => f.AppealAttachments)
                .WithOne(a => a.FileAttachment)
                .HasForeignKey(a => a.FileAttachmentId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // AppealFileAttachment configuration
        modelBuilder.Entity<AppealFileAttachment>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.Description).HasMaxLength(500);

            entity.HasIndex(e => e.AppealId);
            entity.HasIndex(e => e.FileAttachmentId);
            entity.HasIndex(e => e.AttachedByUserId);
            entity.HasIndex(e => e.IsEvidence);
            entity.HasIndex(e => e.AttachedAt);
            entity.HasIndex(e => new { e.AppealId, e.FileAttachmentId }).IsUnique();

            // Relationships
            entity.HasOne(a => a.Appeal)
                .WithMany(appeal => appeal.FileAttachments)
                .HasForeignKey(a => a.AppealId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(a => a.AttachedBy)
                .WithMany()
                .HasForeignKey(a => a.AttachedByUserId)
                .HasPrincipalKey(u => u.TelegramId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Notification configuration
        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.Title).HasMaxLength(200).IsRequired();
            entity.Property(e => e.Message).HasMaxLength(4000).IsRequired();
            entity.Property(e => e.Data).HasMaxLength(2000);
            entity.Property(e => e.ErrorMessage).HasMaxLength(500);

            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.Event);
            entity.HasIndex(e => e.Type);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => e.CreatedAt);
            entity.HasIndex(e => e.ScheduledFor);
            entity.HasIndex(e => new { e.UserId, e.Status });
            entity.HasIndex(e => new { e.Status, e.ScheduledFor });

            // Relationships
            entity.HasOne(n => n.User)
                .WithMany()
                .HasForeignKey(n => n.UserId)
                .HasPrincipalKey(u => u.TelegramId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(n => n.RelatedAppeal)
                .WithMany()
                .HasForeignKey(n => n.RelatedAppealId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(n => n.RelatedNews)
                .WithMany()
                .HasForeignKey(n => n.RelatedNewsId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(n => n.RelatedEvent)
                .WithMany()
                .HasForeignKey(n => n.RelatedEventId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // NotificationPreference configuration
        modelBuilder.Entity<NotificationPreference>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.Event);
            entity.HasIndex(e => new { e.UserId, e.Event }).IsUnique();

            // Relationships
            entity.HasOne(np => np.User)
                .WithMany()
                .HasForeignKey(np => np.UserId)
                .HasPrincipalKey(u => u.TelegramId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // NotificationTemplate configuration
        modelBuilder.Entity<NotificationTemplate>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.Language).HasMaxLength(5).IsRequired();
            entity.Property(e => e.TitleTemplate).HasMaxLength(500).IsRequired();
            entity.Property(e => e.MessageTemplate).HasMaxLength(4000).IsRequired();

            entity.HasIndex(e => e.Event);
            entity.HasIndex(e => e.Type);
            entity.HasIndex(e => e.Language);
            entity.HasIndex(e => e.IsActive);
            entity.HasIndex(e => new { e.Event, e.Type, e.Language });
        });
    }
}
