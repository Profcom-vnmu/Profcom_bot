using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using StudentUnionBot.Application.Events.Commands.CreateEvent;
using StudentUnionBot.Application.Events.DTOs;
using StudentUnionBot.Core.Results;
using StudentUnionBot.Domain.Entities;
using StudentUnionBot.Domain.Enums;
using StudentUnionBot.Domain.Interfaces;
using Xunit;

namespace StudentUnionBot.Tests.Application.Events.Commands;

public class CreateEventCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IEventRepository> _eventRepositoryMock;
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<INotificationService> _notificationServiceMock;
    private readonly Mock<ILogger<CreateEventCommandHandler>> _loggerMock;
    private readonly CreateEventCommandHandler _handler;

    public CreateEventCommandHandlerTests()
    {
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _eventRepositoryMock = new Mock<IEventRepository>();
        _userRepositoryMock = new Mock<IUserRepository>();
        _notificationServiceMock = new Mock<INotificationService>();
        _loggerMock = new Mock<ILogger<CreateEventCommandHandler>>();

        // Setup UnitOfWork to return mocked repositories
        _unitOfWorkMock.Setup(x => x.Events).Returns(_eventRepositoryMock.Object);
        _unitOfWorkMock.Setup(x => x.Users).Returns(_userRepositoryMock.Object);

        _handler = new CreateEventCommandHandler(
            _unitOfWorkMock.Object,
            _notificationServiceMock.Object,
            _loggerMock.Object
        );
    }

    [Fact]
    public async Task Handle_WithValidCommand_CreatesEventSuccessfully()
    {
        // Arrange
        var organizer = BotUser.Create(12345, "TestOrganizer", "Test", "Organizer", Language.Ukrainian);
        
        var command = new CreateEventCommand
        {
            Title = "Студентська конференція",
            Description = "Науково-практична конференція для студентів медичного факультету",
            Summary = "Конференція з медицини",
            Category = EventCategory.Academic,
            Type = EventType.Educational,
            EventDate = DateTime.UtcNow.AddDays(30),
            EndDate = DateTime.UtcNow.AddDays(31),
            Location = "Актова зала",
            MaxParticipants = 100,
            RequiresRegistration = true,
            RegistrationDeadline = DateTime.UtcNow.AddDays(25),
            OrganizerId = 12345,
            PublishImmediately = true,
            SendNotification = false,
            Price = 0,
            Currency = "UAH",
            Language = Language.Ukrainian
        };

        _userRepositoryMock
            .Setup(x => x.GetByTelegramIdAsync(12345, It.IsAny<CancellationToken>()))
            .ReturnsAsync(organizer);

        StudentUnionBot.Domain.Entities.Event? capturedEvent = null;
        _eventRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<StudentUnionBot.Domain.Entities.Event>(), It.IsAny<CancellationToken>()))
            .Callback<StudentUnionBot.Domain.Entities.Event, CancellationToken>((evt, _) => capturedEvent = evt)
            .Returns(Task.CompletedTask);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Title.Should().Be("Студентська конференція");
        result.Value.Description.Should().Be("Науково-практична конференція для студентів медичного факультету");
        result.Value.Category.Should().Be(EventCategory.Academic);
        result.Value.Type.Should().Be(EventType.Educational);
        result.Value.MaxParticipants.Should().Be(100);
        result.Value.RequiresRegistration.Should().BeTrue();
        result.Value.OrganizerId.Should().Be(12345);

        capturedEvent.Should().NotBeNull();
        capturedEvent!.Title.Should().Be("Студентська конференція");
        capturedEvent.IsPublished.Should().BeTrue();
        
        _eventRepositoryMock.Verify(x => x.AddAsync(It.IsAny<StudentUnionBot.Domain.Entities.Event>(), It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenOrganizerNotFound_ReturnsFailure()
    {
        // Arrange
        var command = new CreateEventCommand
        {
            Title = "Тестова подія",
            Description = "Опис події",
            Category = EventCategory.Social,
            Type = EventType.Social,
            EventDate = DateTime.UtcNow.AddDays(10),
            Location = "Тестова локація",
            OrganizerId = 99999,
            PublishImmediately = true
        };

        _userRepositoryMock
            .Setup(x => x.GetByTelegramIdAsync(99999, It.IsAny<CancellationToken>()))
            .ReturnsAsync((BotUser?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Організатор не знайдений");
        
        _eventRepositoryMock.Verify(x => x.AddAsync(It.IsAny<StudentUnionBot.Domain.Entities.Event>(), It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithAttachments_AddsAttachmentsToEvent()
    {
        // Arrange
        var organizer = BotUser.Create(12345, "TestOrganizer", "Test", "Organizer", Language.Ukrainian);
        
        var command = new CreateEventCommand
        {
            Title = "Подія з медіа",
            Description = "Подія з фото та документами",
            Category = EventCategory.Entertainment,
            Type = EventType.Cultural,
            EventDate = DateTime.UtcNow.AddDays(15),
            Location = "Тестова локація",
            OrganizerId = 12345,
            PublishImmediately = true,
            Attachments = new List<StudentUnionBot.Application.Common.Models.FileAttachmentDto>
            {
                new() { FileId = "photo_123", FileType = FileType.Image, FileName = "poster.jpg" },
                new() { FileId = "doc_456", FileType = FileType.Document, FileName = "program.pdf" }
            }
        };

        _userRepositoryMock
            .Setup(x => x.GetByTelegramIdAsync(12345, It.IsAny<CancellationToken>()))
            .ReturnsAsync(organizer);

        StudentUnionBot.Domain.Entities.Event? capturedEvent = null;
        _eventRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<StudentUnionBot.Domain.Entities.Event>(), It.IsAny<CancellationToken>()))
            .Callback<StudentUnionBot.Domain.Entities.Event, CancellationToken>((evt, _) => capturedEvent = evt)
            .Returns(Task.CompletedTask);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        capturedEvent.Should().NotBeNull();
        capturedEvent!.EventAttachments.Should().HaveCount(2);
        capturedEvent.EventAttachments.First().FileId.Should().Be("photo_123");
        capturedEvent.EventAttachments.Last().FileId.Should().Be("doc_456");
        capturedEvent.PhotoFileId.Should().Be("photo_123"); // Перше фото стає основним
    }

    [Fact]
    public async Task Handle_WithPublishImmediatelyAndSendNotification_SendsNotification()
    {
        // Arrange
        var organizer = BotUser.Create(12345, "TestOrganizer", "Test", "Organizer", Language.Ukrainian);
        
        var command = new CreateEventCommand
        {
            Title = "Важлива подія",
            Description = "Дуже важлива подія для всіх студентів",
            Summary = "Важлива подія",
            Category = EventCategory.Academic,
            Type = EventType.Educational,
            EventDate = DateTime.UtcNow.AddDays(7),
            Location = "Головний корпус",
            OrganizerId = 12345,
            PublishImmediately = true,
            SendNotification = true
        };

        _userRepositoryMock
            .Setup(x => x.GetByTelegramIdAsync(12345, It.IsAny<CancellationToken>()))
            .ReturnsAsync(organizer);

        _eventRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<StudentUnionBot.Domain.Entities.Event>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _notificationServiceMock
            .Setup(x => x.SendEventCreatedNotificationAsync(
                It.IsAny<int>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<DateTime>(),
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<int>.Ok(50));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        
        _notificationServiceMock.Verify(x => x.SendEventCreatedNotificationAsync(
            It.IsAny<int>(),
            "Важлива подія",
            "Важлива подія",
            It.IsAny<DateTime>(),
            "Головний корпус",
            It.IsAny<string?>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithPublishImmediatelyFalse_DoesNotSendNotification()
    {
        // Arrange
        var organizer = BotUser.Create(12345, "TestOrganizer", "Test", "Organizer", Language.Ukrainian);
        
        var command = new CreateEventCommand
        {
            Title = "Чернетка події",
            Description = "Подія ще не готова до публікації",
            Category = EventCategory.Social,
            Type = EventType.Social,
            EventDate = DateTime.UtcNow.AddDays(20),
            Location = "TBD",
            OrganizerId = 12345,
            PublishImmediately = false,
            SendNotification = true // Навіть якщо вказано true
        };

        _userRepositoryMock
            .Setup(x => x.GetByTelegramIdAsync(12345, It.IsAny<CancellationToken>()))
            .ReturnsAsync(organizer);

        _eventRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<StudentUnionBot.Domain.Entities.Event>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        
        // Сповіщення не відправляється для unpublished події
        _notificationServiceMock.Verify(x => x.SendEventCreatedNotificationAsync(
            It.IsAny<int>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<DateTime>(),
            It.IsAny<string>(),
            It.IsAny<string?>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenNotificationFails_StillReturnsSuccess()
    {
        // Arrange
        var organizer = BotUser.Create(12345, "TestOrganizer", "Test", "Organizer", Language.Ukrainian);
        
        var command = new CreateEventCommand
        {
            Title = "Подія з проблемою нотифікації",
            Description = "Опис події",
            Category = EventCategory.Community,
            Type = EventType.Social,
            EventDate = DateTime.UtcNow.AddDays(5),
            Location = "Локація",
            OrganizerId = 12345,
            PublishImmediately = true,
            SendNotification = true
        };

        _userRepositoryMock
            .Setup(x => x.GetByTelegramIdAsync(12345, It.IsAny<CancellationToken>()))
            .ReturnsAsync(organizer);

        _eventRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<StudentUnionBot.Domain.Entities.Event>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        _notificationServiceMock
            .Setup(x => x.SendEventCreatedNotificationAsync(
                It.IsAny<int>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<DateTime>(),
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<int>.Fail("Notification service error"));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue(); // Подія створена успішно
        result.Value.Should().NotBeNull();
        
        _eventRepositoryMock.Verify(x => x.AddAsync(It.IsAny<StudentUnionBot.Domain.Entities.Event>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithMaxParticipantsAndRegistrationDeadline_SetsPropertiesCorrectly()
    {
        // Arrange
        var organizer = BotUser.Create(12345, "TestOrganizer", "Test", "Organizer", Language.Ukrainian);
        var deadline = DateTime.UtcNow.AddDays(20);
        
        var command = new CreateEventCommand
        {
            Title = "Подія з реєстрацією",
            Description = "Подія з обмеженою кількістю місць",
            Category = EventCategory.Professional,
            Type = EventType.Career,
            EventDate = DateTime.UtcNow.AddDays(25),
            Location = "Конференц-зал",
            OrganizerId = 12345,
            MaxParticipants = 50,
            RequiresRegistration = true,
            RegistrationDeadline = deadline,
            PublishImmediately = true
        };

        _userRepositoryMock
            .Setup(x => x.GetByTelegramIdAsync(12345, It.IsAny<CancellationToken>()))
            .ReturnsAsync(organizer);

        StudentUnionBot.Domain.Entities.Event? capturedEvent = null;
        _eventRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<StudentUnionBot.Domain.Entities.Event>(), It.IsAny<CancellationToken>()))
            .Callback<StudentUnionBot.Domain.Entities.Event, CancellationToken>((evt, _) => capturedEvent = evt)
            .Returns(Task.CompletedTask);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        capturedEvent.Should().NotBeNull();
        capturedEvent!.MaxParticipants.Should().Be(50);
        capturedEvent.RequiresRegistration.Should().BeTrue();
        capturedEvent.RegistrationDeadline.Should().Be(deadline);
        capturedEvent.CurrentParticipants.Should().Be(0);
    }

    [Fact]
    public async Task Handle_WhenRepositoryThrowsException_ReturnsFailure()
    {
        // Arrange
        var organizer = BotUser.Create(12345, "TestOrganizer", "Test", "Organizer", Language.Ukrainian);
        
        var command = new CreateEventCommand
        {
            Title = "Проблемна подія",
            Description = "Опис",
            Category = EventCategory.Social,
            Type = EventType.Social,
            EventDate = DateTime.UtcNow.AddDays(10),
            Location = "Локація",
            OrganizerId = 12345,
            PublishImmediately = true
        };

        _userRepositoryMock
            .Setup(x => x.GetByTelegramIdAsync(12345, It.IsAny<CancellationToken>()))
            .ReturnsAsync(organizer);

        _eventRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<StudentUnionBot.Domain.Entities.Event>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database error"));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Помилка при створенні події");
        
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_SetsOrganizerNameFromUser()
    {
        // Arrange
        var organizer = BotUser.Create(12345, "Ivan", "Іван", "Петренко", Language.Ukrainian);
        organizer.UpdateProfile("Медичний факультет", 3, "МД-31", "ivan@test.com");
        
        var command = new CreateEventCommand
        {
            Title = "Тест імені організатора",
            Description = "Опис",
            Category = EventCategory.Social,
            Type = EventType.Social,
            EventDate = DateTime.UtcNow.AddDays(10),
            Location = "Локація",
            OrganizerId = 12345,
            PublishImmediately = true
        };

        _userRepositoryMock
            .Setup(x => x.GetByTelegramIdAsync(12345, It.IsAny<CancellationToken>()))
            .ReturnsAsync(organizer);

        StudentUnionBot.Domain.Entities.Event? capturedEvent = null;
        _eventRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<StudentUnionBot.Domain.Entities.Event>(), It.IsAny<CancellationToken>()))
            .Callback<StudentUnionBot.Domain.Entities.Event, CancellationToken>((evt, _) => capturedEvent = evt)
            .Returns(Task.CompletedTask);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        capturedEvent.Should().NotBeNull();
        capturedEvent!.OrganizerName.Should().Be("Іван");
        result.Value.OrganizerName.Should().Be("Іван");
    }
}
