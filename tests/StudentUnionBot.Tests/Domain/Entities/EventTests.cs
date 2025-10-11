using FluentAssertions;
using StudentUnionBot.Domain.Entities;
using StudentUnionBot.Domain.Enums;
using StudentUnionBot.Tests.Helpers;
using Xunit;

namespace StudentUnionBot.Tests.Domain.Entities;

/// <summary>
/// Тести для Event entity
/// </summary>
public class EventTests : TestBase
{
    [Fact]
    public void Create_WithValidData_CreatesEvent()
    {
        // Arrange & Act
        var startDate = DateTime.UtcNow.AddDays(7);
        var ev = Event.Create(
            title: "Студентська вечірка",
            description: "Велика вечірка для всіх студентів",
            category: EventCategory.Social,
            type: EventType.Social,
            startDate: startDate,
            organizerId: 12345,
            organizerName: "Профком ВНМУ",
            summary: "Весела вечірка",
            location: "Актова зала",
            maxParticipants: 100,
            requiresRegistration: true);

        // Assert
        ev.Should().NotBeNull();
        ev.Title.Should().Be("Студентська вечірка");
        ev.Description.Should().Be("Велика вечірка для всіх студентів");
        ev.Category.Should().Be(EventCategory.Social);
        ev.Type.Should().Be(EventType.Social);
        ev.StartDate.Should().Be(startDate);
        ev.Location.Should().Be("Актова зала");
        ev.MaxParticipants.Should().Be(100);
        ev.CurrentParticipants.Should().Be(0);
        ev.RequiresRegistration.Should().BeTrue();
        ev.Status.Should().Be(EventStatus.Planned);
        ev.IsPublished.Should().BeTrue();
        ev.IsFeatured.Should().BeFalse();
    }

    [Fact]
    public void Create_AsDraft_CreatesUnpublishedEvent()
    {
        // Arrange & Act
        var ev = Event.Create(
            title: "Чернетка події",
            description: "Опис",
            category: EventCategory.Academic,
            type: EventType.Educational,
            startDate: DateTime.UtcNow.AddDays(1),
            organizerId: 12345,
            organizerName: "Організатор",
            publishImmediately: false);

        // Assert
        ev.IsPublished.Should().BeFalse();
    }

    [Fact]
    public void Update_ChangesEventProperties()
    {
        // Arrange
        var ev = CreateTestEvent();
        var originalUpdatedAt = ev.UpdatedAt;
        Thread.Sleep(10);

        // Act
        ev.Update(
            title: "Оновлена назва",
            description: "Оновлений опис",
            category: EventCategory.Entertainment,
            type: EventType.Cultural,
            startDate: DateTime.UtcNow.AddDays(14),
            location: "Нове місце");

        // Assert
        ev.Title.Should().Be("Оновлена назва");
        ev.Description.Should().Be("Оновлений опис");
        ev.Category.Should().Be(EventCategory.Entertainment);
        ev.Type.Should().Be(EventType.Cultural);
        ev.Location.Should().Be("Нове місце");
        ev.UpdatedAt.Should().BeAfter(originalUpdatedAt);
    }

    [Fact]
    public void Publish_SetsPublishedToTrue()
    {
        // Arrange
        var ev = Event.Create(
            title: "Подія",
            description: "Опис",
            category: EventCategory.Social,
            type: EventType.Social,
            startDate: DateTime.UtcNow.AddDays(1),
            organizerId: 12345,
            organizerName: "Організатор",
            publishImmediately: false);

        // Act
        ev.Publish();

        // Assert
        ev.IsPublished.Should().BeTrue();
    }

    [Fact]
    public void Unpublish_SetsPublishedToFalse()
    {
        // Arrange
        var ev = CreateTestEvent();

        // Act
        ev.Unpublish();

        // Assert
        ev.IsPublished.Should().BeFalse();
    }

    [Fact]
    public void Cancel_ChangesStatusToCancelled()
    {
        // Arrange
        var ev = CreateTestEvent();

        // Act
        ev.Cancel("Поганая погода");

        // Assert
        ev.Status.Should().Be(EventStatus.Cancelled);
    }

    [Fact]
    public void Complete_ChangesStatusToCompleted()
    {
        // Arrange
        var ev = CreateTestEvent();

        // Act
        ev.Complete();

        // Assert
        ev.Status.Should().Be(EventStatus.Completed);
    }

    [Fact]
    public void Start_ChangesStatusToInProgress()
    {
        // Arrange
        var ev = CreateTestEvent();

        // Act
        ev.Start();

        // Assert
        ev.Status.Should().Be(EventStatus.InProgress);
    }

    [Fact]
    public void Feature_SetsFeaturedToTrue()
    {
        // Arrange
        var ev = CreateTestEvent();

        // Act
        ev.Feature();

        // Assert
        ev.IsFeatured.Should().BeTrue();
    }

    [Fact]
    public void Unfeature_SetsFeaturedToFalse()
    {
        // Arrange
        var ev = CreateTestEvent();
        ev.Feature();

        // Act
        ev.Unfeature();

        // Assert
        ev.IsFeatured.Should().BeFalse();
    }

    [Fact]
    public void CanRegisterParticipant_WhenAllConditionsMet_ReturnsTrue()
    {
        // Arrange
        var ev = Event.Create(
            title: "Подія",
            description: "Опис",
            category: EventCategory.Social,
            type: EventType.Social,
            startDate: DateTime.UtcNow.AddDays(7),
            organizerId: 12345,
            organizerName: "Організатор",
            maxParticipants: 50,
            requiresRegistration: true,
            registrationDeadline: DateTime.UtcNow.AddDays(5));

        // Act
        var canRegister = ev.CanRegisterParticipant();

        // Assert
        canRegister.Should().BeTrue();
    }

    [Fact]
    public void CanRegisterParticipant_WhenNotPublished_ReturnsFalse()
    {
        // Arrange
        var ev = Event.Create(
            title: "Подія",
            description: "Опис",
            category: EventCategory.Social,
            type: EventType.Social,
            startDate: DateTime.UtcNow.AddDays(7),
            organizerId: 12345,
            organizerName: "Організатор",
            requiresRegistration: true,
            publishImmediately: false);

        // Act
        var canRegister = ev.CanRegisterParticipant();

        // Assert
        canRegister.Should().BeFalse();
    }

    [Fact]
    public void CanRegisterParticipant_WhenMaxParticipantsReached_ReturnsFalse()
    {
        // Arrange
        var ev = Event.Create(
            title: "Подія",
            description: "Опис",
            category: EventCategory.Social,
            type: EventType.Social,
            startDate: DateTime.UtcNow.AddDays(7),
            organizerId: 12345,
            organizerName: "Організатор",
            maxParticipants: 2,
            requiresRegistration: true);

        var user1 = CreateTestUser(111, "user1", "User", "One");
        var user2 = CreateTestUser(222, "user2", "User", "Two");
        
        ev.RegisterParticipant(user1);
        ev.RegisterParticipant(user2);

        // Act
        var canRegister = ev.CanRegisterParticipant();

        // Assert
        canRegister.Should().BeFalse();
        ev.CurrentParticipants.Should().Be(2);
    }

    [Fact]
    public void RegisterParticipant_WhenAllowed_AddsParticipantAndIncrementsCount()
    {
        // Arrange
        var ev = Event.Create(
            title: "Подія",
            description: "Опис",
            category: EventCategory.Social,
            type: EventType.Social,
            startDate: DateTime.UtcNow.AddDays(7),
            organizerId: 12345,
            organizerName: "Організатор",
            maxParticipants: 50,
            requiresRegistration: true);

        var user = CreateTestUser(12345, "testuser", "Test", "User");

        // Act
        var result = ev.RegisterParticipant(user);

        // Assert
        result.IsSuccess.Should().BeTrue();
        ev.CurrentParticipants.Should().Be(1);
        ev.IsUserRegistered(user.TelegramId).Should().BeTrue();
    }

    [Fact]
    public void RegisterParticipant_WhenAlreadyRegistered_ReturnsFailure()
    {
        // Arrange
        var ev = Event.Create(
            title: "Подія",
            description: "Опис",
            category: EventCategory.Social,
            type: EventType.Social,
            startDate: DateTime.UtcNow.AddDays(7),
            organizerId: 12345,
            organizerName: "Організатор",
            requiresRegistration: true);

        var user = CreateTestUser(12345, "testuser", "Test", "User");
        ev.RegisterParticipant(user);

        // Act
        var result = ev.RegisterParticipant(user);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeEmpty();
        ev.CurrentParticipants.Should().Be(1);
    }

    [Fact]
    public void UnregisterParticipant_WhenRegistered_RemovesParticipantAndDecrementsCount()
    {
        // Arrange
        var ev = Event.Create(
            title: "Подія",
            description: "Опис",
            category: EventCategory.Social,
            type: EventType.Social,
            startDate: DateTime.UtcNow.AddDays(7),
            organizerId: 12345,
            organizerName: "Організатор",
            requiresRegistration: true);

        var user = CreateTestUser(12345, "testuser", "Test", "User");
        ev.RegisterParticipant(user);

        // Act
        var result = ev.UnregisterParticipant(user);

        // Assert
        result.IsSuccess.Should().BeTrue();
        ev.CurrentParticipants.Should().Be(0);
        ev.IsUserRegistered(user.TelegramId).Should().BeFalse();
    }

    [Fact]
    public void UnregisterParticipant_WhenNotRegistered_ReturnsFailure()
    {
        // Arrange
        var ev = CreateTestEvent();
        var user = CreateTestUser(12345, "testuser", "Test", "User");

        // Act
        var result = ev.UnregisterParticipant(user);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().NotBeEmpty();
    }

    [Fact]
    public void AddEventAttachment_AddsAttachmentWithCorrectOrder()
    {
        // Arrange
        var ev = CreateTestEvent();

        // Act
        ev.AddEventAttachment("photo_1", FileType.Image, "poster.jpg");
        ev.AddEventAttachment("photo_2", FileType.Image, "photo2.jpg");
        ev.AddEventAttachment("doc_1", FileType.Document, "program.pdf");

        // Assert
        ev.EventAttachments.Should().HaveCount(3);
        ev.EventAttachments.First().FileId.Should().Be("photo_1");
        ev.EventAttachments.Last().FileId.Should().Be("doc_1");
    }

    [Fact]
    public void RemoveEventAttachment_RemovesAttachmentAndReorders()
    {
        // Arrange
        var ev = CreateTestEvent();
        ev.AddEventAttachment("photo_1", FileType.Image);
        ev.AddEventAttachment("photo_2", FileType.Image);
        ev.AddEventAttachment("photo_3", FileType.Image);
        
        var attachmentToRemove = ev.EventAttachments.ElementAt(1);

        // Act
        ev.RemoveEventAttachment(attachmentToRemove);

        // Assert
        ev.EventAttachments.Should().HaveCount(2);
        ev.EventAttachments.Should().NotContain(a => a.FileId == "photo_2");
    }

    [Fact]
    public void GetFirstPhotoFileId_ReturnsFirstPhoto()
    {
        // Arrange
        var ev = CreateTestEvent();
        ev.AddEventAttachment("photo_1", FileType.Image);
        ev.AddEventAttachment("photo_2", FileType.Image);
        ev.AddEventAttachment("doc_1", FileType.Document);

        // Act
        var firstPhoto = ev.GetFirstPhotoFileId();

        // Assert
        firstPhoto.Should().Be("photo_1");
    }

    [Fact]
    public void GetAllPhotoFileIds_ReturnsOnlyPhotos()
    {
        // Arrange
        var ev = CreateTestEvent();
        ev.AddEventAttachment("photo_1", FileType.Image);
        ev.AddEventAttachment("doc_1", FileType.Document);
        ev.AddEventAttachment("photo_2", FileType.Image);

        // Act
        var photos = ev.GetAllPhotoFileIds();

        // Assert
        photos.Should().HaveCount(2);
        photos.Should().Contain("photo_1");
        photos.Should().Contain("photo_2");
        photos.Should().NotContain("doc_1");
    }
}
