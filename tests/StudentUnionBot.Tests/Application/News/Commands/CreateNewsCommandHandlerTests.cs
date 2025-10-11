using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using StudentUnionBot.Application.Common.Interfaces;
using StudentUnionBot.Application.Common.Models;
using StudentUnionBot.Application.News.Commands.CreateNews;
using StudentUnionBot.Domain.Entities;
using StudentUnionBot.Domain.Enums;
using StudentUnionBot.Domain.Interfaces;
using StudentUnionBot.Tests.Helpers;
using Xunit;

namespace StudentUnionBot.Tests.Application.News.Commands;

/// <summary>
/// Тести для CreateNewsCommandHandler
/// </summary>
public class CreateNewsCommandHandlerTests : TestBase
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<INewsRepository> _newsRepositoryMock;
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IStudentUnionCacheService> _cacheServiceMock;
    private readonly Mock<INotificationService> _notificationServiceMock;
    private readonly Mock<ILogger<CreateNewsCommandHandler>> _loggerMock;
    private readonly CreateNewsCommandHandler _handler;

    public CreateNewsCommandHandlerTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _newsRepositoryMock = new Mock<INewsRepository>();
        _userRepositoryMock = new Mock<IUserRepository>();
        _cacheServiceMock = new Mock<IStudentUnionCacheService>();
        _notificationServiceMock = new Mock<INotificationService>();
        _loggerMock = CreateLoggerMock<CreateNewsCommandHandler>();

        // Setup UnitOfWork to return mocked repositories
        _unitOfWorkMock.Setup(x => x.News).Returns(_newsRepositoryMock.Object);
        _unitOfWorkMock.Setup(x => x.Users).Returns(_userRepositoryMock.Object);

        _handler = new CreateNewsCommandHandler(
            _unitOfWorkMock.Object,
            _cacheServiceMock.Object,
            _notificationServiceMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_WithValidCommand_CreatesNewsSuccessfully()
    {
        // Arrange
        var author = CreateTestUser(12345, "admin", "Адмін", "Петренко");
        
        _userRepositoryMock
            .Setup(x => x.GetByTelegramIdAsync(12345, It.IsAny<CancellationToken>()))
            .ReturnsAsync(author);

        var command = new CreateNewsCommand
        {
            Title = "Важлива новина",
            Content = "Детальний опис новини про студентське життя",
            Summary = "Короткий опис",
            Category = NewsCategory.Important,
            AuthorId = 12345,
            PublishImmediately = true,
            SendPushNotification = false
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Title.Should().Be("Важлива новина");
        result.Value.Content.Should().Be("Детальний опис новини про студентське життя");
        result.Value.Category.Should().Be(NewsCategory.Important);
        result.Value.AuthorId.Should().Be(12345);
        result.Value.IsPublished.Should().BeTrue();

        // Verify repository calls
        _newsRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<StudentUnionBot.Domain.Entities.News>(), It.IsAny<CancellationToken>()),
            Times.Once);
        
        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);

        // Verify cache invalidation
        _cacheServiceMock.Verify(
            x => x.InvalidateNewsAsync(null, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenAuthorNotFound_ReturnsFailure()
    {
        // Arrange
        _userRepositoryMock
            .Setup(x => x.GetByTelegramIdAsync(99999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((BotUser?)null);

        var command = new CreateNewsCommand
        {
            Title = "Новина",
            Content = "Контент",
            Category = NewsCategory.Important,
            AuthorId = 99999
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Автор не знайдений");

        // Verify news was NOT saved
        _newsRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<StudentUnionBot.Domain.Entities.News>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WithAttachments_AddsAttachmentsToNews()
    {
        // Arrange
        var author = CreateTestUser(12345, "admin", "Адмін", "Петренко");
        
        _userRepositoryMock
            .Setup(x => x.GetByTelegramIdAsync(12345, It.IsAny<CancellationToken>()))
            .ReturnsAsync(author);

        var command = new CreateNewsCommand
        {
            Title = "Новина з медіа",
            Content = "Контент з фото та документами",
            Category = NewsCategory.Important,
            AuthorId = 12345,
            Attachments = new List<FileAttachmentDto>
            {
                new() { FileId = "photo_123", FileType = FileType.Image, FileName = "photo.jpg" },
                new() { FileId = "doc_456", FileType = FileType.Document, FileName = "document.pdf" },
                new() { FileId = "photo_789", FileType = FileType.Image, FileName = "photo2.jpg" }
            }
        };

        StudentUnionBot.Domain.Entities.News? capturedNews = null;
        _newsRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<StudentUnionBot.Domain.Entities.News>(), It.IsAny<CancellationToken>()))
            .Callback<StudentUnionBot.Domain.Entities.News, CancellationToken>((news, _) => capturedNews = news);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        capturedNews.Should().NotBeNull();
        capturedNews!.NewsAttachments.Should().HaveCount(3);
        capturedNews.PhotoFileId.Should().Be("photo_123"); // Legacy field from first image
        capturedNews.DocumentFileId.Should().Be("doc_456"); // Legacy field from first document
    }

    [Fact]
    public async Task Handle_WithPublishImmediatelyAndSendPush_SendsNotification()
    {
        // Arrange
        var author = CreateTestUser(12345, "admin", "Адмін", "Петренко");
        
        _userRepositoryMock
            .Setup(x => x.GetByTelegramIdAsync(12345, It.IsAny<CancellationToken>()))
            .ReturnsAsync(author);

        var command = new CreateNewsCommand
        {
            Title = "Термінова новина",
            Content = "Важливе повідомлення для всіх студентів",
            Summary = "Термінове повідомлення",
            Category = NewsCategory.Urgent,
            AuthorId = 12345,
            PublishImmediately = true,
            SendPushNotification = true
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();

        // Verify notification was sent
        _notificationServiceMock.Verify(
            x => x.SendNewsPublishedNotificationAsync(
                It.IsAny<int>(),
                "Термінова новина",
                It.Is<string>(s => s.Contains("Термінове повідомлення")),
                NewsCategory.Urgent,
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithPublishImmediatelyFalse_DoesNotSendNotification()
    {
        // Arrange
        var author = CreateTestUser(12345, "admin", "Адмін", "Петренко");
        
        _userRepositoryMock
            .Setup(x => x.GetByTelegramIdAsync(12345, It.IsAny<CancellationToken>()))
            .ReturnsAsync(author);

        var command = new CreateNewsCommand
        {
            Title = "Чернетка новини",
            Content = "Контент чернетки",
            Category = NewsCategory.Important,
            AuthorId = 12345,
            PublishImmediately = false, // Draft mode
            SendPushNotification = true // Should not send because not published
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.IsPublished.Should().BeFalse();

        // Verify notification was NOT sent (because news is not published)
        _notificationServiceMock.Verify(
            x => x.SendNewsPublishedNotificationAsync(
                It.IsAny<int>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<NewsCategory>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenNotificationFails_StillReturnsSuccess()
    {
        // Arrange
        var author = CreateTestUser(12345, "admin", "Адмін", "Петренко");
        
        _userRepositoryMock
            .Setup(x => x.GetByTelegramIdAsync(12345, It.IsAny<CancellationToken>()))
            .ReturnsAsync(author);

        _notificationServiceMock
            .Setup(x => x.SendNewsPublishedNotificationAsync(
                It.IsAny<int>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<NewsCategory>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Notification service unavailable"));

        var command = new CreateNewsCommand
        {
            Title = "Новина",
            Content = "Контент",
            Category = NewsCategory.Important,
            AuthorId = 12345,
            PublishImmediately = true,
            SendPushNotification = true
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue("News should be created even if notification fails");
        result.Value.Should().NotBeNull();

        // Verify news was still saved
        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithScheduledPublishDate_SetsPublishAtCorrectly()
    {
        // Arrange
        var author = CreateTestUser(12345, "admin", "Адмін", "Петренко");
        var scheduledDate = DateTime.UtcNow.AddDays(3);
        
        _userRepositoryMock
            .Setup(x => x.GetByTelegramIdAsync(12345, It.IsAny<CancellationToken>()))
            .ReturnsAsync(author);

        var command = new CreateNewsCommand
        {
            Title = "Заплановане оголошення",
            Content = "Буде опубліковано пізніше",
            Category = NewsCategory.Important,
            AuthorId = 12345,
            PublishImmediately = false,
            ScheduledPublishDate = scheduledDate
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.PublishAt.Should().Be(scheduledDate);
        result.Value.IsPublished.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_WhenRepositoryThrowsException_ReturnsFailure()
    {
        // Arrange
        var author = CreateTestUser(12345, "admin", "Адмін", "Петренко");
        
        _userRepositoryMock
            .Setup(x => x.GetByTelegramIdAsync(12345, It.IsAny<CancellationToken>()))
            .ReturnsAsync(author);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database connection error"));

        var command = new CreateNewsCommand
        {
            Title = "Новина",
            Content = "Контент",
            Category = NewsCategory.Important,
            AuthorId = 12345
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Помилка при створенні новини");
    }

    [Fact]
    public async Task Handle_SetsAuthorNameFromUser()
    {
        // Arrange
        var author = CreateTestUser(12345, "adminuser", "Іван", "Петренко");
        
        _userRepositoryMock
            .Setup(x => x.GetByTelegramIdAsync(12345, It.IsAny<CancellationToken>()))
            .ReturnsAsync(author);

        var command = new CreateNewsCommand
        {
            Title = "Новина",
            Content = "Контент",
            Category = NewsCategory.Important,
            AuthorId = 12345
        };

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.AuthorName.Should().Be("Іван");
    }
}
