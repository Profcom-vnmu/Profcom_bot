using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using StudentUnionBot.Domain.Entities;
using StudentUnionBot.Domain.Enums;
using StudentUnionBot.Infrastructure.Data;

namespace StudentUnionBot.Tests.Helpers;

/// <summary>
/// Базовий клас для всіх тестів
/// Надає загальні методи для створення моків та InMemory БД
/// </summary>
public abstract class TestBase : IDisposable
{
    /// <summary>
    /// Створює InMemory DbContext для тестів
    /// Кожен тест отримує свою окрему БД
    /// </summary>
    protected BotDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<BotDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .EnableSensitiveDataLogging()
            .Options;

        return new BotDbContext(options);
    }

    /// <summary>
    /// Створює мок для ILogger<T>
    /// </summary>
    protected Mock<ILogger<T>> CreateLoggerMock<T>()
    {
        return new Mock<ILogger<T>>();
    }

    /// <summary>
    /// Створює тестового користувача через factory method
    /// </summary>
    protected BotUser CreateTestUser(
        long telegramId = 123456789,
        string? username = "testuser",
        string? firstName = "Test",
        string? lastName = "User",
        Language language = Language.Ukrainian)
    {
        return BotUser.Create(
            telegramId: telegramId,
            username: username,
            firstName: firstName,
            lastName: lastName,
            language: language);
    }

    /// <summary>
    /// Створює тестове звернення через factory method
    /// </summary>
    protected Appeal CreateTestAppeal(
        long studentId = 123456789,
        string studentName = "Test Student",
        AppealCategory category = AppealCategory.Scholarship,
        string subject = "Test Subject",
        string message = "Test message with more than 10 characters for validation")
    {
        return Appeal.Create(
            studentId: studentId,
            studentName: studentName,
            category: category,
            subject: subject,
            message: message);
    }

    /// <summary>
    /// Створює тестову новину через factory method
    /// </summary>
    protected News CreateTestNews(
        long authorId = 123456789,
        string authorName = "Test Admin",
        NewsCategory category = NewsCategory.Education,
        string title = "Test News Title",
        string content = "Test news content with enough text for validation purposes",
        string? summary = null,
        bool publishImmediately = true)
    {
        return News.Create(
            title: title,
            content: content,
            category: category,
            authorId: authorId,
            authorName: authorName,
            summary: summary,
            publishImmediately: publishImmediately);
    }

    /// <summary>
    /// Створює тестову подію через factory method
    /// </summary>
    protected Event CreateTestEvent(
        long organizerId = 123456789,
        string organizerName = "Test Organizer",
        string title = "Test Event",
        string description = "Test event description with enough content",
        EventCategory category = EventCategory.Academic,
        EventType type = EventType.Educational,
        DateTime? startDate = null,
        string? location = "Test Location")
    {
        return Event.Create(
            title: title,
            description: description,
            category: category,
            type: type,
            startDate: startDate ?? DateTime.UtcNow.AddDays(7),
            organizerId: organizerId,
            organizerName: organizerName,
            location: location);
    }

    /// <summary>
    /// Cleanup після кожного тесту
    /// </summary>
    public virtual void Dispose()
    {
        GC.SuppressFinalize(this);
    }
}
