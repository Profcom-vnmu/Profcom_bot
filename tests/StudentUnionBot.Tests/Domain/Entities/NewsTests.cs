using FluentAssertions;
using StudentUnionBot.Domain.Entities;
using StudentUnionBot.Domain.Enums;
using StudentUnionBot.Tests.Helpers;
using Xunit;

namespace StudentUnionBot.Tests.Domain.Entities;

/// <summary>
/// Тести для News entity
/// </summary>
public class NewsTests : TestBase
{
    [Fact]
    public void Create_WithValidData_CreatesNews()
    {
        // Arrange & Act
        var news = News.Create(
            title: "Важлива новина",
            content: "Зміст новини про студентське життя",
            category: NewsCategory.Important,
            authorId: 12345,
            authorName: "Адмін Петренко",
            summary: "Короткий опис",
            photoFileId: "photo_123",
            documentFileId: null,
            publishImmediately: true,
            publishAt: null);

        // Assert
        news.Should().NotBeNull();
        news.Title.Should().Be("Важлива новина");
        news.Content.Should().Be("Зміст новини про студентське життя");
        news.Category.Should().Be(NewsCategory.Important);
        news.AuthorId.Should().Be(12345);
        news.AuthorName.Should().Be("Адмін Петренко");
        news.Summary.Should().Be("Короткий опис");
        news.PhotoFileId.Should().Be("photo_123");
        news.DocumentFileId.Should().BeNull();
        news.IsPublished.Should().BeTrue();
        news.IsPinned.Should().BeFalse();
        news.IsArchived.Should().BeFalse();
        news.ViewCount.Should().Be(0);
        news.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Create_AsDraft_CreatesUnpublishedNews()
    {
        // Arrange & Act
        var news = News.Create(
            title: "Чернетка новини",
            content: "Зміст чернетки",
            category: NewsCategory.Event,
            authorId: 12345,
            authorName: "Адмін",
            publishImmediately: false);

        // Assert
        news.IsPublished.Should().BeFalse();
        news.PublishAt.Should().BeNull();
    }

    [Fact]
    public void Create_WithScheduledPublish_SetsPublishDate()
    {
        // Arrange
        var scheduledDate = DateTime.UtcNow.AddDays(1);

        // Act
        var news = News.Create(
            title: "Заплановане оголошення",
            content: "Буде опубліковано завтра",
            category: NewsCategory.Important,
            authorId: 12345,
            authorName: "Адмін",
            publishImmediately: false,
            publishAt: scheduledDate);

        // Assert
        news.IsPublished.Should().BeFalse();
        news.PublishAt.Should().Be(scheduledDate);
    }

    [Fact]
    public void Update_ChangesNewsProperties()
    {
        // Arrange
        var news = CreateTestNews();
        var originalUpdatedAt = news.UpdatedAt;
        Thread.Sleep(10); // Ensure time difference

        // Act
        news.Update(
            title: "Оновлена назва",
            content: "Оновлений зміст",
            category: NewsCategory.Education,
            summary: "Новий опис",
            photoFileId: "new_photo_456",
            documentFileId: "doc_789");

        // Assert
        news.Title.Should().Be("Оновлена назва");
        news.Content.Should().Be("Оновлений зміст");
        news.Category.Should().Be(NewsCategory.Education);
        news.Summary.Should().Be("Новий опис");
        news.PhotoFileId.Should().Be("new_photo_456");
        news.DocumentFileId.Should().Be("doc_789");
        news.UpdatedAt.Should().BeAfter(originalUpdatedAt);
    }

    [Fact]
    public void Publish_SetsPublishedToTrue()
    {
        // Arrange
        var news = News.Create(
            title: "Чернетка",
            content: "Зміст",
            category: NewsCategory.Important,
            authorId: 12345,
            authorName: "Адмін",
            publishImmediately: false);

        // Act
        news.Publish();

        // Assert
        news.IsPublished.Should().BeTrue();
        news.PublishAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Unpublish_SetsPublishedToFalse()
    {
        // Arrange
        var news = CreateTestNews();
        news.Publish();

        // Act
        news.Unpublish();

        // Assert
        news.IsPublished.Should().BeFalse();
    }

    [Fact]
    public void Pin_SetsPinnedToTrue()
    {
        // Arrange
        var news = CreateTestNews();

        // Act
        news.Pin();

        // Assert
        news.IsPinned.Should().BeTrue();
    }

    [Fact]
    public void Unpin_SetsPinnedToFalse()
    {
        // Arrange
        var news = CreateTestNews();
        news.Pin();

        // Act
        news.Unpin();

        // Assert
        news.IsPinned.Should().BeFalse();
    }

    [Fact]
    public void Archive_SetsArchivedToTrueAndUnpublishes()
    {
        // Arrange
        var news = CreateTestNews();
        news.Publish();

        // Act
        news.Archive();

        // Assert
        news.IsArchived.Should().BeTrue();
        news.IsPublished.Should().BeFalse();
    }

    [Fact]
    public void Restore_SetsArchivedToFalse()
    {
        // Arrange
        var news = CreateTestNews();
        news.Archive();

        // Act
        news.Restore();

        // Assert
        news.IsArchived.Should().BeFalse();
    }

    [Fact]
    public void IncrementViewCount_IncreasesViewCount()
    {
        // Arrange
        var news = CreateTestNews();
        var initialCount = news.ViewCount;

        // Act
        news.IncrementViewCount();
        news.IncrementViewCount();
        news.IncrementViewCount();

        // Assert
        news.ViewCount.Should().Be(initialCount + 3);
    }

    [Fact]
    public void AddNewsAttachment_AddsAttachmentWithCorrectOrder()
    {
        // Arrange
        var news = CreateTestNews();

        // Act
        news.AddNewsAttachment("photo_1", FileType.Image, "photo1.jpg");
        news.AddNewsAttachment("photo_2", FileType.Image, "photo2.jpg");
        news.AddNewsAttachment("doc_1", FileType.Document, "document.pdf");

        // Assert
        news.NewsAttachments.Should().HaveCount(3);
        news.NewsAttachments.First().FileId.Should().Be("photo_1");
        news.NewsAttachments.Last().FileId.Should().Be("doc_1");
    }

    [Fact]
    public void RemoveNewsAttachment_RemovesAttachmentAndReordersRemaining()
    {
        // Arrange
        var news = CreateTestNews();
        news.AddNewsAttachment("photo_1", FileType.Image);
        news.AddNewsAttachment("photo_2", FileType.Image);
        news.AddNewsAttachment("photo_3", FileType.Image);
        
        var attachmentToRemove = news.NewsAttachments.ElementAt(1); // photo_2

        // Act
        news.RemoveNewsAttachment(attachmentToRemove);

        // Assert
        news.NewsAttachments.Should().HaveCount(2);
        news.NewsAttachments.Should().NotContain(a => a.FileId == "photo_2");
    }

    [Fact]
    public void ClearNewsAttachments_RemovesAllAttachments()
    {
        // Arrange
        var news = CreateTestNews();
        news.AddNewsAttachment("photo_1", FileType.Image);
        news.AddNewsAttachment("photo_2", FileType.Image);
        news.AddNewsAttachment("doc_1", FileType.Document);

        // Act
        news.ClearNewsAttachments();

        // Assert
        news.NewsAttachments.Should().BeEmpty();
    }

    [Fact]
    public void GetFirstPhotoFileId_ReturnsFirstPhoto()
    {
        // Arrange
        var news = CreateTestNews();
        news.AddNewsAttachment("photo_1", FileType.Image);
        news.AddNewsAttachment("photo_2", FileType.Image);
        news.AddNewsAttachment("doc_1", FileType.Document);

        // Act
        var firstPhoto = news.GetFirstPhotoFileId();

        // Assert
        firstPhoto.Should().Be("photo_1");
    }

    [Fact]
    public void GetFirstPhotoFileId_WithNoPhotos_ReturnsLegacyPhotoFileId()
    {
        // Arrange
        var news = News.Create(
            title: "News",
            content: "Content",
            category: NewsCategory.Important,
            authorId: 12345,
            authorName: "Admin",
            photoFileId: "legacy_photo");

        // Act
        var firstPhoto = news.GetFirstPhotoFileId();

        // Assert
        firstPhoto.Should().Be("legacy_photo");
    }

    [Fact]
    public void GetAllPhotoFileIds_ReturnsOnlyPhotos()
    {
        // Arrange
        var news = CreateTestNews();
        news.AddNewsAttachment("photo_1", FileType.Image);
        news.AddNewsAttachment("doc_1", FileType.Document);
        news.AddNewsAttachment("photo_2", FileType.Image);
        news.AddNewsAttachment("video_1", FileType.Video);

        // Act
        var photos = news.GetAllPhotoFileIds();

        // Assert
        photos.Should().HaveCount(2);
        photos.Should().Contain("photo_1");
        photos.Should().Contain("photo_2");
        photos.Should().NotContain("doc_1");
        photos.Should().NotContain("video_1");
    }

    [Fact]
    public void GetAllDocumentFileIds_ReturnsOnlyDocuments()
    {
        // Arrange
        var news = CreateTestNews();
        news.AddNewsAttachment("photo_1", FileType.Image);
        news.AddNewsAttachment("doc_1", FileType.Document);
        news.AddNewsAttachment("doc_2", FileType.Document);

        // Act
        var documents = news.GetAllDocumentFileIds();

        // Assert
        documents.Should().HaveCount(2);
        documents.Should().Contain("doc_1");
        documents.Should().Contain("doc_2");
        documents.Should().NotContain("photo_1");
    }
}
