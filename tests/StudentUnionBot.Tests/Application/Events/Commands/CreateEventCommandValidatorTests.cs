using FluentAssertions;
using StudentUnionBot.Application.Common.Models;
using StudentUnionBot.Application.Events.Commands.CreateEvent;
using StudentUnionBot.Domain.Enums;
using Xunit;

namespace StudentUnionBot.Tests.Application.Events.Commands;

public class CreateEventCommandValidatorTests
{
    private readonly CreateEventCommandValidator _validator;

    public CreateEventCommandValidatorTests()
    {
        _validator = new CreateEventCommandValidator();
    }

    [Fact]
    public void Validate_WithValidCommand_ShouldBeValid()
    {
        // Arrange
        var command = new CreateEventCommand
        {
            Title = "Тестова подія",
            Description = "Це опис тестової події з достатньою кількістю символів",
            EventDate = DateTime.UtcNow.AddDays(7),
            Location = "Актова зала",
            Category = EventCategory.Academic,
            Type = EventType.Educational,
            Requirements = "Базові знання", // Додано для Educational типу
            OrganizerId = 12345,
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
        var command = new CreateEventCommand
        {
            Title = "",
            Description = "Опис події",
            EventDate = DateTime.UtcNow.AddDays(7),
            Location = "Локація",
            Category = EventCategory.Academic,
            Type = EventType.Educational,
            OrganizerId = 12345
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Title" && e.ErrorMessage == "Назва події обов'язкова");
    }

    [Fact]
    public void Validate_WithWhitespaceOnlyTitle_ShouldBeInvalid()
    {
        // Arrange
        var command = new CreateEventCommand
        {
            Title = "   ",
            Description = "Опис події",
            EventDate = DateTime.UtcNow.AddDays(7),
            Location = "Локація",
            Category = EventCategory.Academic,
            Type = EventType.Educational,
            OrganizerId = 12345
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Назва не може містити тільки пробіли");
    }

    [Fact]
    public void Validate_WithTooLongTitle_ShouldBeInvalid()
    {
        // Arrange
        var command = new CreateEventCommand
        {
            Title = new string('A', 201),
            Description = "Опис події",
            EventDate = DateTime.UtcNow.AddDays(7),
            Location = "Локація",
            Category = EventCategory.Academic,
            Type = EventType.Educational,
            OrganizerId = 12345
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Назва не може перевищувати 200 символів");
    }

    [Fact]
    public void Validate_WithEmptyDescription_ShouldBeInvalid()
    {
        // Arrange
        var command = new CreateEventCommand
        {
            Title = "Назва події",
            Description = "",
            EventDate = DateTime.UtcNow.AddDays(7),
            Location = "Локація",
            Category = EventCategory.Academic,
            Type = EventType.Educational,
            OrganizerId = 12345
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Опис події обов'язковий");
    }

    [Fact]
    public void Validate_WithTooLongDescription_ShouldBeInvalid()
    {
        // Arrange
        var command = new CreateEventCommand
        {
            Title = "Назва події",
            Description = new string('A', 5001),
            EventDate = DateTime.UtcNow.AddDays(7),
            Location = "Локація",
            Category = EventCategory.Academic,
            Type = EventType.Educational,
            OrganizerId = 12345
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Опис не може перевищувати 5000 символів");
    }

    [Fact]
    public void Validate_WithEventDateInPast_ShouldBeInvalid()
    {
        // Arrange
        var command = new CreateEventCommand
        {
            Title = "Назва події",
            Description = "Опис події",
            EventDate = DateTime.UtcNow.AddMinutes(-10),
            Location = "Локація",
            Category = EventCategory.Academic,
            Type = EventType.Educational,
            OrganizerId = 12345
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Дата події має бути принаймні через 30 хвилин");
    }

    [Fact]
    public void Validate_WithEventDateTooSoon_ShouldBeInvalid()
    {
        // Arrange
        var command = new CreateEventCommand
        {
            Title = "Назва події",
            Description = "Опис події",
            EventDate = DateTime.UtcNow.AddMinutes(15), // Менше 30 хвилин
            Location = "Локація",
            Category = EventCategory.Academic,
            Type = EventType.Educational,
            OrganizerId = 12345
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Дата події має бути принаймні через 30 хвилин");
    }

    [Fact]
    public void Validate_WithEndDateBeforeStartDate_ShouldBeInvalid()
    {
        // Arrange
        var command = new CreateEventCommand
        {
            Title = "Назва події",
            Description = "Опис події",
            EventDate = DateTime.UtcNow.AddDays(7),
            EndDate = DateTime.UtcNow.AddDays(6), // Раніше ніж початок
            Location = "Локація",
            Category = EventCategory.Academic,
            Type = EventType.Educational,
            OrganizerId = 12345
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Дата закінчення має бути після дати початку");
    }

    [Fact]
    public void Validate_WithEmptyLocation_ShouldBeInvalid()
    {
        // Arrange
        var command = new CreateEventCommand
        {
            Title = "Назва події",
            Description = "Опис події",
            EventDate = DateTime.UtcNow.AddDays(7),
            Location = "",
            Category = EventCategory.Academic,
            Type = EventType.Educational,
            OrganizerId = 12345
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Місце проведення обов'язкове");
    }

    [Fact]
    public void Validate_WithTooLongLocation_ShouldBeInvalid()
    {
        // Arrange
        var command = new CreateEventCommand
        {
            Title = "Назва події",
            Description = "Опис події",
            EventDate = DateTime.UtcNow.AddDays(7),
            Location = new string('A', 301),
            Category = EventCategory.Academic,
            Type = EventType.Educational,
            OrganizerId = 12345
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Місце проведення не може перевищувати 300 символів");
    }

    [Fact]
    public void Validate_WithNegativeMaxParticipants_ShouldBeInvalid()
    {
        // Arrange
        var command = new CreateEventCommand
        {
            Title = "Назва події",
            Description = "Опис події",
            EventDate = DateTime.UtcNow.AddDays(7),
            Location = "Локація",
            Category = EventCategory.Academic,
            Type = EventType.Educational,
            MaxParticipants = -5,
            OrganizerId = 12345
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Максимальна кількість учасників має бути позитивним числом");
    }

    [Fact]
    public void Validate_WithRegistrationDeadlineAfterEventDate_ShouldBeInvalid()
    {
        // Arrange
        var command = new CreateEventCommand
        {
            Title = "Назва події",
            Description = "Опис події",
            EventDate = DateTime.UtcNow.AddDays(7),
            RegistrationDeadline = DateTime.UtcNow.AddDays(8), // Після події
            Location = "Локація",
            Category = EventCategory.Academic,
            Type = EventType.Educational,
            OrganizerId = 12345
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Дедлайн реєстрації має бути до початку події");
    }

    [Fact]
    public void Validate_WithRegistrationDeadlineInPast_ShouldBeInvalid()
    {
        // Arrange
        var command = new CreateEventCommand
        {
            Title = "Назва події",
            Description = "Опис події",
            EventDate = DateTime.UtcNow.AddDays(7),
            RegistrationDeadline = DateTime.UtcNow.AddDays(-1), // В минулому
            Location = "Локація",
            Category = EventCategory.Academic,
            Type = EventType.Educational,
            OrganizerId = 12345
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Дедлайн реєстрації має бути в майбутньому");
    }

    [Fact]
    public void Validate_RequiresRegistrationWithoutDeadline_ShouldBeInvalid()
    {
        // Arrange
        var command = new CreateEventCommand
        {
            Title = "Назва події",
            Description = "Опис події",
            EventDate = DateTime.UtcNow.AddDays(7),
            Location = "Локація",
            Category = EventCategory.Academic,
            Type = EventType.Educational,
            RequiresRegistration = true,
            RegistrationDeadline = null,
            OrganizerId = 12345
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Для події з реєстрацією треба вказати дедлайн реєстрації");
    }

    [Fact]
    public void Validate_WithNegativePrice_ShouldBeInvalid()
    {
        // Arrange
        var command = new CreateEventCommand
        {
            Title = "Назва події",
            Description = "Опис події",
            EventDate = DateTime.UtcNow.AddDays(7),
            Location = "Локація",
            Category = EventCategory.Academic,
            Type = EventType.Educational,
            Price = -100,
            OrganizerId = 12345
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Вартість не може бути від'ємною");
    }

    [Fact]
    public void Validate_PaidEventWithoutCurrency_ShouldBeInvalid()
    {
        // Arrange
        var command = new CreateEventCommand
        {
            Title = "Назва події",
            Description = "Опис події",
            EventDate = DateTime.UtcNow.AddDays(7),
            Location = "Локація",
            Category = EventCategory.Academic,
            Type = EventType.Educational,
            Price = 100,
            Currency = "",
            OrganizerId = 12345
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Валюта обов'язкова");
    }

    [Fact]
    public void Validate_PaidEventWithInvalidCurrencyCode_ShouldBeInvalid()
    {
        // Arrange
        var command = new CreateEventCommand
        {
            Title = "Назва події",
            Description = "Опис події",
            EventDate = DateTime.UtcNow.AddDays(7),
            Location = "Локація",
            Category = EventCategory.Academic,
            Type = EventType.Educational,
            Price = 100,
            Currency = "UAHH", // 4 символи замість 3
            OrganizerId = 12345
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Валюта має бути 3-символьним кодом");
    }

    [Fact]
    public void Validate_WithZeroOrganizerId_ShouldBeInvalid()
    {
        // Arrange
        var command = new CreateEventCommand
        {
            Title = "Назва події",
            Description = "Опис події",
            EventDate = DateTime.UtcNow.AddDays(7),
            Location = "Локація",
            Category = EventCategory.Academic,
            Type = EventType.Educational,
            OrganizerId = 0
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "ID організатора повинен бути позитивним числом");
    }

    [Fact]
    public void Validate_WithTooManyAttachments_ShouldBeInvalid()
    {
        // Arrange
        var attachments = Enumerable.Range(1, 6).Select(i => new FileAttachmentDto
        {
            FileId = $"file_{i}",
            FileType = FileType.Image
        }).ToList();

        var command = new CreateEventCommand
        {
            Title = "Назва події",
            Description = "Опис події",
            EventDate = DateTime.UtcNow.AddDays(7),
            Location = "Локація",
            Category = EventCategory.Academic,
            Type = EventType.Educational,
            OrganizerId = 12345,
            Attachments = attachments
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Не можна додавати більше 5 файлів до однієї події");
    }

    [Fact]
    public void Validate_EducationalEventWithoutRequirements_ShouldBeInvalid()
    {
        // Arrange
        var command = new CreateEventCommand
        {
            Title = "Навчальний семінар",
            Description = "Опис семінару",
            EventDate = DateTime.UtcNow.AddDays(7),
            Location = "Локація",
            Category = EventCategory.Academic,
            Type = EventType.Educational,
            Requirements = null,
            OrganizerId = 12345
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Для освітніх заходів рекомендується вказати вимоги до учасників");
    }

    [Fact]
    public void Validate_SportsEventWithoutMaxParticipants_ShouldBeInvalid()
    {
        // Arrange
        var command = new CreateEventCommand
        {
            Title = "Футбольний матч",
            Description = "Опис матчу",
            EventDate = DateTime.UtcNow.AddDays(7),
            Location = "Стадіон",
            Category = EventCategory.Sports,
            Type = EventType.Sports,
            MaxParticipants = null,
            OrganizerId = 12345
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Для спортивних заходів рекомендується обмежити кількість учасників");
    }

    [Fact]
    public void Validate_PaidEventWithoutContactInfo_ShouldBeInvalid()
    {
        // Arrange
        var command = new CreateEventCommand
        {
            Title = "Платний майстер-клас",
            Description = "Опис майстер-класу",
            EventDate = DateTime.UtcNow.AddDays(7),
            Location = "Локація",
            Category = EventCategory.Professional,
            Type = EventType.Educational,
            Price = 200,
            Currency = "UAH",
            ContactInfo = null,
            OrganizerId = 12345
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage == "Для платних заходів треба вказати контактну інформацію для оплати");
    }

    [Fact]
    public void Validate_WithValidPaidEvent_ShouldBeValid()
    {
        // Arrange
        var command = new CreateEventCommand
        {
            Title = "Платний майстер-клас",
            Description = "Опис майстер-класу",
            EventDate = DateTime.UtcNow.AddDays(7),
            Location = "Локація",
            Category = EventCategory.Professional,
            Type = EventType.Educational,
            Price = 200,
            Currency = "UAH",
            ContactInfo = "Telegram: @organizer",
            Requirements = "Базові знання",
            OrganizerId = 12345,
            PublishImmediately = true
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }
}
