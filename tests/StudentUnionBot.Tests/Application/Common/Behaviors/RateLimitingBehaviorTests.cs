using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using StudentUnionBot.Application.Common.Attributes;
using StudentUnionBot.Application.Common.Behaviors;
using StudentUnionBot.Application.Common.Interfaces;
using StudentUnionBot.Core.Results;
using Xunit;

namespace StudentUnionBot.Tests.Application.Common.Behaviors;

public class RateLimitingBehaviorTests
{
    private readonly Mock<IRateLimiter> _mockRateLimiter;
    private readonly Mock<ICurrentUserService> _mockCurrentUserService;
    private readonly Mock<ILogger<RateLimitingBehavior<TestCommand, Result<string>>>> _mockLogger;
    private readonly RateLimitingBehavior<TestCommand, Result<string>> _behavior;

    public RateLimitingBehaviorTests()
    {
        _mockRateLimiter = new Mock<IRateLimiter>();
        _mockCurrentUserService = new Mock<ICurrentUserService>();
        _mockLogger = new Mock<ILogger<RateLimitingBehavior<TestCommand, Result<string>>>>();

        _behavior = new RateLimitingBehavior<TestCommand, Result<string>>(
            _mockRateLimiter.Object,
            _mockCurrentUserService.Object,
            _mockLogger.Object
        );
    }

    [Fact]
    public async Task Handle_CommandWithoutRateLimitAttribute_ShouldNotCheckRateLimit()
    {
        // Arrange
        var behaviorWithoutRateLimit = new RateLimitingBehavior<TestCommandWithoutRateLimit, Result<string>>(
            _mockRateLimiter.Object,
            _mockCurrentUserService.Object,
            new Mock<ILogger<RateLimitingBehavior<TestCommandWithoutRateLimit, Result<string>>>>().Object
        );
        
        var command = new TestCommandWithoutRateLimit();
        var expectedResult = Result<string>.Ok("Success");
        
        RequestHandlerDelegate<Result<string>> next = () => Task.FromResult(expectedResult);

        // Act
        var result = await behaviorWithoutRateLimit.Handle(command, next, CancellationToken.None);

        // Assert
        result.Should().Be(expectedResult);
        _mockRateLimiter.Verify(x => x.AllowAsync(It.IsAny<long>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_CommandWithRateLimitButNoCurrentUser_ShouldSkipCheck()
    {
        // Arrange
        var command = new TestCommand();
        var expectedResult = Result<string>.Ok("Success");
        _mockCurrentUserService.Setup(x => x.UserId).Returns((long?)null);
        
        RequestHandlerDelegate<Result<string>> next = () => Task.FromResult(expectedResult);

        // Act
        var result = await _behavior.Handle(command, next, CancellationToken.None);

        // Assert
        result.Should().Be(expectedResult);
        _mockRateLimiter.Verify(x => x.AllowAsync(It.IsAny<long>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_RateLimitAllowed_ShouldProceedToNextHandler()
    {
        // Arrange
        var command = new TestCommand();
        var expectedResult = Result<string>.Ok("Success");
        const long userId = 12345;

        _mockCurrentUserService.Setup(x => x.UserId).Returns(userId);
        _mockRateLimiter.Setup(x => x.AllowAsync(userId, "TestAction", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        
        RequestHandlerDelegate<Result<string>> next = () => Task.FromResult(expectedResult);

        // Act
        var result = await _behavior.Handle(command, next, CancellationToken.None);

        // Assert
        result.Should().Be(expectedResult);
        result.IsSuccess.Should().BeTrue();
        _mockRateLimiter.Verify(x => x.AllowAsync(userId, "TestAction", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_RateLimitExceeded_ShouldReturnFailResult()
    {
        // Arrange
        var command = new TestCommand();
        const long userId = 12345;

        _mockCurrentUserService.Setup(x => x.UserId).Returns(userId);
        _mockRateLimiter.Setup(x => x.AllowAsync(userId, "TestAction", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _mockRateLimiter.Setup(x => x.GetTimeUntilResetAsync(userId, "TestAction", It.IsAny<CancellationToken>()))
            .ReturnsAsync(TimeSpan.FromMinutes(5));
        
        RequestHandlerDelegate<Result<string>> next = () => Task.FromResult(Result<string>.Ok("Should not reach here"));

        // Act
        var result = await _behavior.Handle(command, next, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Перевищено ліміт запитів");
        result.Error.Should().Contain("5");
        _mockRateLimiter.Verify(x => x.AllowAsync(userId, "TestAction", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_RateLimitExceededWithoutTimeUntilReset_ShouldReturnGenericFailMessage()
    {
        // Arrange
        var command = new TestCommand();
        const long userId = 12345;

        _mockCurrentUserService.Setup(x => x.UserId).Returns(userId);
        _mockRateLimiter.Setup(x => x.AllowAsync(userId, "TestAction", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _mockRateLimiter.Setup(x => x.GetTimeUntilResetAsync(userId, "TestAction", It.IsAny<CancellationToken>()))
            .ReturnsAsync((TimeSpan?)null);
        
        RequestHandlerDelegate<Result<string>> next = () => Task.FromResult(Result<string>.Ok("Should not reach here"));

        // Act
        var result = await _behavior.Handle(command, next, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("Перевищено ліміт запитів. Спробуйте пізніше.");
    }

    [Fact]
    public async Task Handle_RateLimiterThrowsException_ShouldProceedToNextHandler()
    {
        // Arrange
        var command = new TestCommand();
        var expectedResult = Result<string>.Ok("Success");
        const long userId = 12345;

        _mockCurrentUserService.Setup(x => x.UserId).Returns(userId);
        _mockRateLimiter.Setup(x => x.AllowAsync(userId, "TestAction", It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Rate limiter error"));
        
        RequestHandlerDelegate<Result<string>> next = () => Task.FromResult(expectedResult);

        // Act
        var result = await _behavior.Handle(command, next, CancellationToken.None);

        // Assert
        result.Should().Be(expectedResult);
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ResultTypeWithoutGenericParameter_ShouldHandleCorrectly()
    {
        // Arrange
        var behavior = new RateLimitingBehavior<TestCommandReturningResult, Result>(
            _mockRateLimiter.Object,
            _mockCurrentUserService.Object,
            new Mock<ILogger<RateLimitingBehavior<TestCommandReturningResult, Result>>>().Object
        );

        var command = new TestCommandReturningResult();
        const long userId = 12345;

        _mockCurrentUserService.Setup(x => x.UserId).Returns(userId);
        _mockRateLimiter.Setup(x => x.AllowAsync(userId, "TestAction2", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _mockRateLimiter.Setup(x => x.GetTimeUntilResetAsync(userId, "TestAction2", It.IsAny<CancellationToken>()))
            .ReturnsAsync(TimeSpan.FromMinutes(10));
        
        RequestHandlerDelegate<Result> next = () => Task.FromResult(Result.Ok());

        // Act
        var result = await behavior.Handle(command, next, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Перевищено ліміт запитів");
    }

    // Test commands
    [RateLimit("TestAction")]
    public class TestCommand : IRequest<Result<string>>
    {
    }

    public class TestCommandWithoutRateLimit : IRequest<Result<string>>
    {
    }

    [RateLimit("TestAction2")]
    public class TestCommandReturningResult : IRequest<Result>
    {
    }
}
