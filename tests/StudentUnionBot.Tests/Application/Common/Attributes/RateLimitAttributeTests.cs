using FluentAssertions;
using StudentUnionBot.Application.Common.Attributes;
using Xunit;

namespace StudentUnionBot.Tests.Application.Common.Attributes;

public class RateLimitAttributeTests
{
    [Fact]
    public void Constructor_WithValidAction_ShouldSetAction()
    {
        // Arrange
        const string action = "CreateAppeal";

        // Act
        var attribute = new RateLimitAttribute(action);

        // Assert
        attribute.Action.Should().Be(action);
    }

    [Fact]
    public void Constructor_WithNullAction_ShouldThrowArgumentException()
    {
        // Arrange & Act
        Action act = () => new RateLimitAttribute(null!);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Action cannot be null or whitespace*");
    }

    [Fact]
    public void Constructor_WithEmptyAction_ShouldThrowArgumentException()
    {
        // Arrange & Act
        Action act = () => new RateLimitAttribute(string.Empty);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Action cannot be null or whitespace*");
    }

    [Fact]
    public void Constructor_WithWhitespaceAction_ShouldThrowArgumentException()
    {
        // Arrange & Act
        Action act = () => new RateLimitAttribute("   ");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Action cannot be null or whitespace*");
    }

    [Theory]
    [InlineData("CreateAppeal")]
    [InlineData("SendMessage")]
    [InlineData("CreateNews")]
    [InlineData("RegisterEvent")]
    public void Constructor_WithDifferentActions_ShouldSetCorrectAction(string action)
    {
        // Act
        var attribute = new RateLimitAttribute(action);

        // Assert
        attribute.Action.Should().Be(action);
    }
}
