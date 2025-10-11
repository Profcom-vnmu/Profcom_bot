using FluentAssertions;
using StudentUnionBot.Application.Common.Models;
using StudentUnionBot.Application.News.Commands.CreateNews;
using StudentUnionBot.Domain.Enums;
using Xunit;

namespace StudentUnionBot.Tests.Application.News.Commands;

public class CreateNewsCommandValidatorTests
{
    private readonly CreateNewsCommandValidator _validator;

    public CreateNewsCommandValidatorTests()
    {
        _validator = new CreateNewsCommandValidator();
    }

    [Fact]
    public void Validate_WithValidCommand_ShouldBeValid()
    {
        // Arrange
        var command = new CreateNewsCommand
        {
            Title = "Тестова новина",
            Content = "Це контент тестової новини з достатньою кількістю символів",
            Category = NewsCategory.Important,
            AuthorId = 12345,
            Language = Language.Ukrainian,
            PublishImmediately = true
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_WithEmptyTitle_ShouldBeInvalid()
    {
        // Arrange
        var command = new CreateNewsCommand
        {
            Title = "",
            Content = "Контент",
            Category = NewsCategory.Important,
            AuthorId = 12345,
            PublishImmediately = true
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Title");
        result.Errors.Should().Contain(e => e.ErrorMessage == "Заголовок новини обов'язковий");
    }

    [Fact]
    public void Validate_WithWhitespaceOnlyTitle_ShouldBeInvalid()
    {
        // Arrange
        var command = new CreateNewsCommand
        {
            Title = "   ",
            Content = "Контент",
            Category = NewsCategory.Important,
            AuthorId = 12345,
            PublishImmediately = true
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Заголовок не може містити тільки пробіли");
    }

    [Fact]
    public void Validate_WithTooLongTitle_ShouldBeInvalid()
    {
        // Arrange
        var command = new CreateNewsCommand
        {
            Title = new string('A', 201), // 201 символ
            Content = "Контент",
            Category = NewsCategory.Important,
            AuthorId = 12345,
            PublishImmediately = true
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Заголовок не може перевищувати 200 символів");
    }

    [Fact]
    public void Validate_WithEmptyContent_ShouldBeInvalid()
    {
        // Arrange
        var command = new CreateNewsCommand
        {
            Title = "Заголовок",
            Content = "",
            Category = NewsCategory.Important,
            AuthorId = 12345,
            PublishImmediately = true
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Content");
        result.Errors.Should().Contain(e => e.ErrorMessage == "Контент новини обов'язковий");
    }

    [Fact]
    public void Validate_WithTooLongContent_ShouldBeInvalid()
    {
        // Arrange
        var command = new CreateNewsCommand
        {
            Title = "Заголовок",
            Content = new string('A', 10001), // 10001 символ
            Category = NewsCategory.Important,
            AuthorId = 12345,
            PublishImmediately = true
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Контент не може перевищувати 10000 символів");
    }

    [Fact]
    public void Validate_WithTooLongSummary_ShouldBeInvalid()
    {
        // Arrange
        var command = new CreateNewsCommand
        {
            Title = "Заголовок",
            Content = "Контент",
            Summary = new string('A', 501), // 501 символ
            Category = NewsCategory.Important,
            AuthorId = 12345,
            PublishImmediately = true
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Короткий опис не може перевищувати 500 символів");
    }

    [Fact]
    public void Validate_WithInvalidCategory_ShouldBeInvalid()
    {
        // Arrange
        var command = new CreateNewsCommand
        {
            Title = "Заголовок",
            Content = "Контент",
            Category = (NewsCategory)999, // Invalid enum value
            AuthorId = 12345,
            PublishImmediately = true
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Невалідна категорія новини");
    }

    [Fact]
    public void Validate_WithZeroAuthorId_ShouldBeInvalid()
    {
        // Arrange
        var command = new CreateNewsCommand
        {
            Title = "Заголовок",
            Content = "Контент",
            Category = NewsCategory.Important,
            AuthorId = 0,
            PublishImmediately = true
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "ID автора повинен бути позитивним числом");
    }

    [Fact]
    public void Validate_WithNegativeAuthorId_ShouldBeInvalid()
    {
        // Arrange
        var command = new CreateNewsCommand
        {
            Title = "Заголовок",
            Content = "Контент",
            Category = NewsCategory.Important,
            AuthorId = -1,
            PublishImmediately = true
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "ID автора повинен бути позитивним числом");
    }

    [Fact]
    public void Validate_WithScheduledDateInPast_ShouldBeInvalid()
    {
        // Arrange
        var command = new CreateNewsCommand
        {
            Title = "Заголовок",
            Content = "Контент",
            Category = NewsCategory.Important,
            AuthorId = 12345,
            PublishImmediately = false,
            ScheduledPublishDate = DateTime.UtcNow.AddMinutes(-10) // В минулому
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Дата запланованої публікації має бути принаймні через 5 хвилин");
    }

    [Fact]
    public void Validate_WithScheduledDateTooSoon_ShouldBeInvalid()
    {
        // Arrange
        var command = new CreateNewsCommand
        {
            Title = "Заголовок",
            Content = "Контент",
            Category = NewsCategory.Important,
            AuthorId = 12345,
            PublishImmediately = false,
            ScheduledPublishDate = DateTime.UtcNow.AddMinutes(2) // Менше 5 хвилин
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Дата запланованої публікації має бути принаймні через 5 хвилин");
    }

    [Fact]
    public void Validate_WithoutScheduledDateWhenNotPublishingImmediately_ShouldBeInvalid()
    {
        // Arrange
        var command = new CreateNewsCommand
        {
            Title = "Заголовок",
            Content = "Контент",
            Category = NewsCategory.Important,
            AuthorId = 12345,
            PublishImmediately = false,
            ScheduledPublishDate = null
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Якщо новина не публікується одразу, треба вказати дату публікації");
    }

    [Fact]
    public void Validate_WithValidScheduledDate_ShouldBeValid()
    {
        // Arrange
        var command = new CreateNewsCommand
        {
            Title = "Заголовок",
            Content = "Контент",
            Category = NewsCategory.Important,
            AuthorId = 12345,
            PublishImmediately = false,
            ScheduledPublishDate = DateTime.UtcNow.AddHours(2) // Більше 5 хвилин
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithTooManyAttachments_ShouldBeInvalid()
    {
        // Arrange
        var attachments = Enumerable.Range(1, 11).Select(i => new FileAttachmentDto
        {
            FileId = $"file_{i}",
            FileType = FileType.Image
        }).ToList();

        var command = new CreateNewsCommand
        {
            Title = "Заголовок",
            Content = "Контент",
            Category = NewsCategory.Important,
            AuthorId = 12345,
            PublishImmediately = true,
            Attachments = attachments
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Не можна додавати більше 10 файлів до однієї новини");
    }

    [Fact]
    public void Validate_WithEmptyFileId_ShouldBeInvalid()
    {
        // Arrange
        var command = new CreateNewsCommand
        {
            Title = "Заголовок",
            Content = "Контент",
            Category = NewsCategory.Important,
            AuthorId = 12345,
            PublishImmediately = true,
            Attachments = new List<FileAttachmentDto>
            {
                new() { FileId = "", FileType = FileType.Image }
            }
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "ID файлу не може бути порожнім");
    }

    [Fact]
    public void Validate_UrgentNewsShouldPublishImmediately_WhenNotImmediate_ShouldBeInvalid()
    {
        // Arrange
        var command = new CreateNewsCommand
        {
            Title = "Термінова новина",
            Content = "Важливий контент",
            Category = NewsCategory.Urgent,
            AuthorId = 12345,
            PublishImmediately = false,
            ScheduledPublishDate = DateTime.UtcNow.AddHours(1)
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Термінові новини мають публікуватися одразу");
    }

    [Fact]
    public void Validate_UrgentNewsShouldHavePushNotification_WhenDisabled_ShouldBeInvalid()
    {
        // Arrange
        var command = new CreateNewsCommand
        {
            Title = "Термінова новина",
            Content = "Важливий контент",
            Category = NewsCategory.Urgent,
            AuthorId = 12345,
            PublishImmediately = true,
            SendPushNotification = false
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Для термінових новин рекомендується увімкнути push-повідомлення");
    }

    [Fact]
    public void Validate_WithTooLongTags_ShouldBeInvalid()
    {
        // Arrange
        var command = new CreateNewsCommand
        {
            Title = "Заголовок",
            Content = "Контент",
            Category = NewsCategory.Important,
            AuthorId = 12345,
            Tags = new string('A', 201),
            PublishImmediately = true
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Теги не можуть перевищувати 200 символів");
    }
}
