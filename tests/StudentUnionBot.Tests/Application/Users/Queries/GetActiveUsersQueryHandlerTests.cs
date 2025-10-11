using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using StudentUnionBot.Application.Users.DTOs;
using StudentUnionBot.Application.Users.Queries.GetActiveUsers;
using StudentUnionBot.Domain.Entities;
using StudentUnionBot.Domain.Enums;
using StudentUnionBot.Domain.Interfaces;
using StudentUnionBot.Tests.Helpers;
using Xunit;

namespace StudentUnionBot.Tests.Application.Users.Queries;

/// <summary>
/// Тести для GetActiveUsersQueryHandler
/// </summary>
public class GetActiveUsersQueryHandlerTests : TestBase
{
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<ILogger<GetActiveUsersQueryHandler>> _loggerMock;
    private readonly GetActiveUsersQueryHandler _handler;

    public GetActiveUsersQueryHandlerTests()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _loggerMock = CreateLoggerMock<GetActiveUsersQueryHandler>();
        
        _handler = new GetActiveUsersQueryHandler(
            _userRepositoryMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_WithActiveUsers_ReturnsSuccessWithUsers()
    {
        // Arrange
        var user1 = CreateTestUser(12345, "user1", "Іван", "Петренко");
        var user2 = CreateTestUser(67890, "user2", "Марія", "Коваленко");
        var activeUsers = new List<BotUser> { user1, user2 };

        _userRepositoryMock
            .Setup(x => x.GetActiveUsersAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(activeUsers);

        var query = new GetActiveUsersQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value.Should().HaveCount(2);
        result.Value![0].TelegramId.Should().Be(12345);
        result.Value![0].FirstName.Should().Be("Іван");
        result.Value![1].TelegramId.Should().Be(67890);
        result.Value![1].FirstName.Should().Be("Марія");
        
        _userRepositoryMock.Verify(
            x => x.GetActiveUsersAsync(It.IsAny<CancellationToken>()), 
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithNoActiveUsers_ReturnsSuccessWithEmptyList()
    {
        // Arrange
        var emptyList = new List<BotUser>();

        _userRepositoryMock
            .Setup(x => x.GetActiveUsersAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(emptyList);

        var query = new GetActiveUsersQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WhenRepositoryThrowsException_ReturnsFailure()
    {
        // Arrange
        _userRepositoryMock
            .Setup(x => x.GetActiveUsersAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Database connection error"));

        var query = new GetActiveUsersQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Database connection error");
    }

    [Fact]
    public async Task Handle_MapsUserPropertiesCorrectly()
    {
        // Arrange
        var user = CreateTestUser(12345, "testuser", "Тест", "Користувач");
        user.UpdateProfile("КН", 3, "КН-301", "test@vnmu.edu.ua");
        
        _userRepositoryMock
            .Setup(x => x.GetActiveUsersAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<BotUser> { user });

        var query = new GetActiveUsersQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        var userDto = result.Value![0];
        userDto.TelegramId.Should().Be(12345);
        userDto.Username.Should().Be("testuser");
        userDto.FirstName.Should().Be("Тест");
        userDto.LastName.Should().Be("Користувач");
        userDto.Faculty.Should().Be("КН");
        userDto.Course.Should().Be(3);
        userDto.Group.Should().Be("КН-301");
        userDto.Email.Should().Be("test@vnmu.edu.ua");
        userDto.IsActive.Should().BeTrue();
        userDto.Role.Should().Be(UserRole.Student);
        userDto.Language.Should().Be("uk");
    }

    [Fact]
    public async Task Handle_WithEnglishLanguage_MapsLanguageCorrectly()
    {
        // Arrange
        var user = CreateTestUser(12345, "user", "Test", "User");
        user.SetLanguage(Language.English);
        
        _userRepositoryMock
            .Setup(x => x.GetActiveUsersAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<BotUser> { user });

        var query = new GetActiveUsersQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Value![0].Language.Should().Be("en");
    }
}
