using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using StudentUnionBot.Application.Appeals.Commands.CreateAppeal;
using StudentUnionBot.Application.Common.Interfaces;
using StudentUnionBot.Domain.Entities;
using StudentUnionBot.Domain.Enums;
using StudentUnionBot.Domain.Interfaces;
using StudentUnionBot.Tests.Helpers;
using Xunit;

#pragma warning disable CS8620 // Argument of type nullable reference

namespace StudentUnionBot.Tests.Application.Appeals.Commands;

/// <summary>
/// Тести для CreateAppealCommandHandler
/// </summary>
public class CreateAppealCommandHandlerTests : TestBase
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IRateLimiter> _rateLimiterMock;
    private readonly Mock<INotificationService> _notificationServiceMock;
    private readonly Mock<IAppealAssignmentService> _assignmentServiceMock;
    private readonly Mock<ILogger<CreateAppealCommandHandler>> _loggerMock;
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IAppealRepository> _appealRepositoryMock;
    private readonly CreateAppealCommandHandler _handler;

    public CreateAppealCommandHandlerTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _rateLimiterMock = new Mock<IRateLimiter>();
        _notificationServiceMock = new Mock<INotificationService>();
        _assignmentServiceMock = new Mock<IAppealAssignmentService>();
        _loggerMock = CreateLoggerMock<CreateAppealCommandHandler>();
        _userRepositoryMock = new Mock<IUserRepository>();
        _appealRepositoryMock = new Mock<IAppealRepository>();

        // Setup default UnitOfWork behavior
        _unitOfWorkMock.Setup(x => x.Users).Returns(_userRepositoryMock.Object);
        _unitOfWorkMock.Setup(x => x.Appeals).Returns(_appealRepositoryMock.Object);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _handler = new CreateAppealCommandHandler(
            _unitOfWorkMock.Object,
            _rateLimiterMock.Object,
            _notificationServiceMock.Object,
            _assignmentServiceMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_WithValidCommand_ShouldCreateAppeal()
    {
        // Arrange
        var user = CreateTestUser(telegramId: 123456789);
        var command = new CreateAppealCommand
        {
            StudentId = 123456789,
            StudentName = "Test Student",
            Category = AppealCategory.Scholarship,
            Subject = "Need scholarship info",
            Message = "I need information about scholarship application process."
        };

        _rateLimiterMock
            .Setup(x => x.AllowAsync(It.IsAny<long>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _userRepositoryMock
            .Setup(x => x.GetByTelegramIdAsync(command.StudentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _notificationServiceMock
            .Setup(x => x.NotifyAllAdminsAsync(
                It.IsAny<NotificationEvent>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<NotificationPriority>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Core.Results.Result<int>.Ok(5));

        _assignmentServiceMock
            .Setup(x => x.AssignAppealAsync(It.IsAny<Appeal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Core.Results.Result<BotUser>.Fail("No admins available"));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.StudentId.Should().Be(command.StudentId);
        result.Value.Category.Should().Be(command.Category);
        result.Value.Subject.Should().Be(command.Subject);
        result.Value.Status.Should().Be(AppealStatus.New);

        // Verify interactions
        _appealRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<Appeal>(), It.IsAny<CancellationToken>()),
            Times.Once);

        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.AtLeastOnce);

        _notificationServiceMock.Verify(
            x => x.NotifyAllAdminsAsync(
                NotificationEvent.AppealCreated,
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<NotificationPriority>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenRateLimitExceeded_ShouldReturnError()
    {
        // Arrange
        var command = new CreateAppealCommand
        {
            StudentId = 123456789,
            StudentName = "Test Student",
            Category = AppealCategory.Scholarship,
            Subject = "Need help",
            Message = "I need information about scholarship."
        };

        _rateLimiterMock
            .Setup(x => x.AllowAsync(It.IsAny<long>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _rateLimiterMock
            .Setup(x => x.GetTimeUntilResetAsync(It.IsAny<long>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(TimeSpan.FromMinutes(10));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("перевищили ліміт");

        // Verify appeal was NOT created
        _appealRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<Appeal>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenUserNotFound_ShouldReturnError()
    {
        // Arrange
        var command = new CreateAppealCommand
        {
            StudentId = 123456789,
            StudentName = "Test Student",
            Category = AppealCategory.Scholarship,
            Subject = "Need help",
            Message = "I need information about scholarship."
        };

        _rateLimiterMock
            .Setup(x => x.AllowAsync(It.IsAny<long>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _userRepositoryMock
            .Setup(x => x.GetByTelegramIdAsync(command.StudentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((BotUser?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("не знайдений");

        // Verify appeal was NOT created
        _appealRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<Appeal>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WhenUserIsBanned_ShouldReturnError()
    {
        // Arrange
        var user = CreateTestUser(telegramId: 123456789);
        user.Ban("Spam detected");

        var command = new CreateAppealCommand
        {
            StudentId = 123456789,
            StudentName = "Test Student",
            Category = AppealCategory.Scholarship,
            Subject = "Need help",
            Message = "I need information about scholarship."
        };

        _rateLimiterMock
            .Setup(x => x.AllowAsync(It.IsAny<long>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _userRepositoryMock
            .Setup(x => x.GetByTelegramIdAsync(command.StudentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("заблоковано");

        // Verify appeal was NOT created
        _appealRepositoryMock.Verify(
            x => x.AddAsync(It.IsAny<Appeal>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WithPhotoAndDocument_ShouldCreateAppealWithFiles()
    {
        // Arrange
        var user = CreateTestUser(telegramId: 123456789);
        var command = new CreateAppealCommand
        {
            StudentId = 123456789,
            StudentName = "Test Student",
            Category = AppealCategory.Scholarship,
            Subject = "Need scholarship info",
            Message = "I need information about scholarship application process.",
            PhotoFileId = "photo_123",
            DocumentFileId = "doc_456",
            DocumentFileName = "document.pdf"
        };

        _rateLimiterMock
            .Setup(x => x.AllowAsync(It.IsAny<long>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _userRepositoryMock
            .Setup(x => x.GetByTelegramIdAsync(command.StudentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        _notificationServiceMock
            .Setup(x => x.NotifyAllAdminsAsync(
                It.IsAny<NotificationEvent>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<NotificationPriority>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Core.Results.Result<int>.Ok(5));

        _assignmentServiceMock
            .Setup(x => x.AssignAppealAsync(It.IsAny<Appeal>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Core.Results.Result<BotUser>.Fail("No admins available"));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.IsSuccess.Should().BeTrue();

        // Verify appeal was updated with message (files)
        _unitOfWorkMock.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.AtLeast(2)); // Once for appeal creation, once for adding message

        _appealRepositoryMock.Verify(
            x => x.Update(It.IsAny<Appeal>()),
            Times.Once);
    }
}
