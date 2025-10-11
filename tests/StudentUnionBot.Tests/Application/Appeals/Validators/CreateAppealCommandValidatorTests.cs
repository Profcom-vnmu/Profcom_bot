using FluentAssertions;
using StudentUnionBot.Application.Appeals.Commands.CreateAppeal;
using StudentUnionBot.Domain.Enums;
using Xunit;

namespace StudentUnionBot.Tests.Application.Appeals.Validators;

/// <summary>
/// Тести для CreateAppealCommandValidator
/// </summary>
public class CreateAppealCommandValidatorTests
{
    private readonly CreateAppealCommandValidator _validator;

    public CreateAppealCommandValidatorTests()
    {
        _validator = new CreateAppealCommandValidator();
    }

    [Fact]
    public void Validate_WithValidCommand_ShouldPass()
    {
        // Arrange
        var command = new CreateAppealCommand
        {
            StudentId = 123456789,
            StudentName = "Test Student",
            Category = AppealCategory.Scholarship,
            Subject = "Need scholarship information",
            Message = "I need detailed information about scholarship application process."
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-999)]
    public void Validate_WithInvalidStudentId_ShouldFail(long invalidId)
    {
        // Arrange
        var command = new CreateAppealCommand
        {
            StudentId = invalidId,
            StudentName = "Test Student",
            Category = AppealCategory.Scholarship,
            Subject = "Need help",
            Message = "I need information about scholarship."
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateAppealCommand.StudentId));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Validate_WithEmptyStudentName_ShouldFail(string? invalidName)
    {
        // Arrange
        var command = new CreateAppealCommand
        {
            StudentId = 123456789,
            StudentName = invalidName!,
            Category = AppealCategory.Scholarship,
            Subject = "Need help",
            Message = "I need information about scholarship."
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateAppealCommand.StudentName));
    }

    [Fact]
    public void Validate_WithTooLongStudentName_ShouldFail()
    {
        // Arrange
        var command = new CreateAppealCommand
        {
            StudentId = 123456789,
            StudentName = new string('A', 201), // 201 symbols (max is 200)
            Category = AppealCategory.Scholarship,
            Subject = "Need help",
            Message = "I need information about scholarship."
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => 
            e.PropertyName == nameof(CreateAppealCommand.StudentName) &&
            e.ErrorMessage.Contains("200"));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    [InlineData("Help")] // Too short (less than 5 chars)
    public void Validate_WithInvalidSubject_ShouldFail(string? invalidSubject)
    {
        // Arrange
        var command = new CreateAppealCommand
        {
            StudentId = 123456789,
            StudentName = "Test Student",
            Category = AppealCategory.Scholarship,
            Subject = invalidSubject!,
            Message = "I need information about scholarship application process."
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateAppealCommand.Subject));
    }

    [Fact]
    public void Validate_WithTooLongSubject_ShouldFail()
    {
        // Arrange
        var command = new CreateAppealCommand
        {
            StudentId = 123456789,
            StudentName = "Test Student",
            Category = AppealCategory.Scholarship,
            Subject = new string('A', 201), // 201 symbols (max is 200)
            Message = "I need information about scholarship."
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => 
            e.PropertyName == nameof(CreateAppealCommand.Subject) &&
            e.ErrorMessage.Contains("200"));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    [InlineData("Too short")] // Less than 10 chars
    public void Validate_WithInvalidMessage_ShouldFail(string? invalidMessage)
    {
        // Arrange
        var command = new CreateAppealCommand
        {
            StudentId = 123456789,
            StudentName = "Test Student",
            Category = AppealCategory.Scholarship,
            Subject = "Need help with scholarship",
            Message = invalidMessage!
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateAppealCommand.Message));
    }

    [Fact]
    public void Validate_WithTooLongMessage_ShouldFail()
    {
        // Arrange
        var command = new CreateAppealCommand
        {
            StudentId = 123456789,
            StudentName = "Test Student",
            Category = AppealCategory.Scholarship,
            Subject = "Need help",
            Message = new string('A', 4001) // 4001 symbols (max is 4000)
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => 
            e.PropertyName == nameof(CreateAppealCommand.Message) &&
            e.ErrorMessage.Contains("4000"));
    }

    [Fact]
    public void Validate_WithMultipleErrors_ShouldReturnAllErrors()
    {
        // Arrange
        var command = new CreateAppealCommand
        {
            StudentId = -1, // Invalid
            StudentName = "", // Invalid
            Category = AppealCategory.Scholarship,
            Subject = "ABC", // Too short
            Message = "Short" // Too short
        };

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().HaveCountGreaterThanOrEqualTo(4);
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateAppealCommand.StudentId));
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateAppealCommand.StudentName));
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateAppealCommand.Subject));
        result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateAppealCommand.Message));
    }
}
